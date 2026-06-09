namespace Ama.Enterprise.Samples.HttpDnsFeatureFlags;

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ama.Enterprise.CRDT.Distributed.Extensions;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.FeatureFlags.Extensions;
using Ama.Enterprise.FeatureFlags.Services;
using Ama.Enterprise.Licensing.Extensions;
using Ama.Enterprise.P2p.Extensions;
using Ama.Enterprise.P2p.AspNetCore.Extensions;
using Ama.Enterprise.P2p.AspNetCore.Models;

public sealed class Program
{
    public static void Main(string[] args)
    {
        // Use CreateSlimBuilder for AOT compatibility. It removes reflection-heavy ASP.NET Core features.
        var builder = WebApplication.CreateSlimBuilder(args);

        // Register the AOT JSON Serializer Context to serialize DTOs without reflection.
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        // Read configuration from arguments, environment variables, or appsettings.json.
        var advertisedHost = builder.Configuration.GetValue<string>("AdvertisedHost") ?? "*";
        var advertisedPort = builder.Configuration.GetValue<int>("AdvertisedPort", 5290);
        var discoveryHostname = builder.Configuration.GetValue<string>("DiscoveryHostname") ?? "localhost";
        var discoveryPort = builder.Configuration.GetValue<int>("DiscoveryPort", 5290);
        
        // Retrieve HTTPS preference from configuration, defaulting to true.
        var useHttps = builder.Configuration.GetValue<bool>("UseHttps", true);

        // Configure ASP.NET Core to listen on the advertised port.
        var scheme = useHttps ? "https" : "http";
        builder.WebHost.UseUrls($"{scheme}://*:{advertisedPort}");

        if (useHttps)
        {
            builder.WebHost.UseKestrelHttpsConfiguration();
        }

        // Load Authentication Certificate and Wire Encryption Key from disk
        var certPath = builder.Configuration.GetValue<string>("ClusterCertificatePath") ?? "cluster.cer";
        var encryptionKeyPath = builder.Configuration.GetValue<string>("EncryptionKeyPath") ?? "encryption.key";

        if (!File.Exists(certPath))
        {
            throw new FileNotFoundException($"Cluster certificate not found at '{certPath}'. Please provide a valid certificate for node authentication.");
        }

        if (!File.Exists(encryptionKeyPath))
        {
            throw new FileNotFoundException($"Encryption key not found at '{encryptionKeyPath}'. Please provide a valid AES-GCM base64 key for wire encryption.");
        }

        using var clusterCert = X509CertificateLoader.LoadCertificateFromFile(certPath);
        var certBytes = clusterCert.Export(X509ContentType.Cert);
        var encryptionKey = File.ReadAllText(encryptionKeyPath).Trim();

        // Add services to the container.
        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();

        // 1. Configure Open Source Licensing.
        builder.Services.ConfigureAmaCommunityLicense();

        var replicaId = $"node-{Guid.NewGuid():N}";

        // 2. Register the CRDT replica with a unique ID and configure synchronization settings.
        builder.Services.AddDistributedCrdtReplica(replicaId);
        
        builder.Services.AddDistributedCrdtCore(options =>
        {
            options.ActiveSyncEnabled = true;
            options.CheckpointIntervalSeconds = (int)TimeSpan.FromHours(1).TotalSeconds;
            options.AntiEntropyIntervalSeconds = 5;
            options.AntiEntropyInitialDelaySeconds = 3;
        });

        // 3. Link the P2P mesh network to the CRDT replica.
        builder.Services.AddDistributedCrdtP2p("feature-flags-http-mesh", replicaId);

        // 4. Add the Feature Flags services.
        builder.Services.AddFeatureFlags();

        // 5. Configure the Mesh Network to use HTTP transport and DNS discovery.
        builder.Services.AddP2pMesh("feature-flags-http-mesh")
            .AddGossipNetwork(options =>
            {
                options.GossipInterval = TimeSpan.FromMilliseconds(1500);
                options.Fanout = 3;
                options.DefaultTimeToLive = 3;
            })
            .AddAspNetCoreTransport(options =>
            {
                options.HostingMode = AspNetCoreHostingMode.Integrated;
                options.AdvertisedHost = advertisedHost;
                options.AdvertisedPort = advertisedPort;
                options.UseHttps = useHttps;
            })
            .AddAspNetCorePeerHandshake(options =>
            {
                options.HostingMode = AspNetCoreHostingMode.Integrated;
                options.UseHttps = useHttps;
            })
            .AddDnsPeerDiscovery(options =>
            {
                options.Hostname = discoveryHostname;
                options.TargetPort = discoveryPort;
            })
            .AddCertificateAuthenticator(options =>
            {
                options.LocalCertificateBytes = certBytes;
                options.AllowedThumbprints.Add(clusterCert.Thumbprint);
                options.ValidateCertificateChain = false;
            })
            .AddWireEncoder(options =>
            {
                options.IsEncryptionEnabled = true;
                options.EncryptionKeyBase64 = encryptionKey;
            });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        if (useHttps)
        {
            app.UseHttpsRedirection();
        }
        
        app.UseAuthorization();

        // Map P2P endpoints to ASP.NET Core.
        app.MapP2pMeshEndpoints();
        app.MapP2pMeshHandshakes();

        // --- Default Minimal API Endpoints ---
        
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        app.MapGet("/weatherforecast", () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = summaries[Random.Shared.Next(summaries.Length)]
                })
                .ToArray();
            
            return TypedResults.Ok(forecast);
        })
        .WithName("GetWeatherForecast");

        // --- Feature Flags Endpoints ---

        app.MapGet("/flags", (DistributedCrdtScopeManager scopeManager) =>
        {
            var crdtScope = scopeManager.GetOrCreateScope(replicaId);
            var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagClusterManager>();

            var flags = clusterManager.GetFlags()
                .Select(f => new FlagModel(f.Key, f.Value.IsEnabled))
                .ToArray();

            return TypedResults.Ok(flags);
        })
        .WithName("GetFeatureFlags");

        app.MapPost("/flags/{name}", async (string name, bool isEnabled, DistributedCrdtScopeManager scopeManager) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.BadRequest("Flag name cannot be empty.");
            }

            var crdtScope = scopeManager.GetOrCreateScope(replicaId);
            var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagClusterManager>();

            await clusterManager.SetFlagAsync(name, isEnabled);
            
            return Results.Ok(new FlagSetResponse(name, isEnabled));
        })
        .WithName("SetFeatureFlag");

        app.MapDelete("/flags/{name}", async (string name, DistributedCrdtScopeManager scopeManager) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.BadRequest("Flag name cannot be empty.");
            }

            var crdtScope = scopeManager.GetOrCreateScope(replicaId);
            var clusterManager = crdtScope.ServiceProvider.GetRequiredService<IFeatureFlagClusterManager>();

            await clusterManager.RemoveFlagAsync(name);
            
            return Results.Ok(new FlagDeleteResponse(name, true));
        })
        .WithName("DeleteFeatureFlag");

        app.Run();
    }
}

[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSerializable(typeof(FlagModel[]))]
[JsonSerializable(typeof(FlagSetResponse))]
[JsonSerializable(typeof(FlagDeleteResponse))]
[JsonSerializable(typeof(string))] // Required for the string output in Results.BadRequest
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext
{
}