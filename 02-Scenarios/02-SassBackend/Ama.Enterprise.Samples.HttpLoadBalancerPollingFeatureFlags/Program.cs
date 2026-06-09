namespace Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags.FeatureFlagsApi;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ama.Enterprise.CRDT.Distributed.Extensions;
using Ama.Enterprise.CRDT.Distributed.Services;
using Ama.Enterprise.FeatureFlags.Extensions;
using Ama.Enterprise.FeatureFlags.Services;
using Ama.Enterprise.Licensing.Extensions;
using Ama.Enterprise.P2p.Extensions;
using Ama.Enterprise.P2p.AspNetCore.Extensions;
using Ama.Enterprise.P2p.AspNetCore.Models;
using Ama.Enterprise.CRDT.Distributed.TableStorage.Extensions;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Ama.Enterprise.CRDT.MessagePack.Extensions;
using Ama.Enterprise.CRDT.MessagePack.Resolvers;
using Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags.Models;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        var advertisedHost = GetVNetIpAddress();

        if (!int.TryParse(builder.Configuration.GetValue<string>("AdvertisedPort"), out var advertisedPort))
        {
            var envPortStr = Environment.GetEnvironmentVariable("PORT");
            advertisedPort = int.TryParse(envPortStr, out var envPort) ? envPort : 5031;
        }

        var privatePort = 5000;
        var privatePortsEnv = builder.Configuration.GetValue<string>("WEBSITE_PRIVATE_PORTS");
        if (!string.IsNullOrWhiteSpace(privatePortsEnv))
        {
            var firstPort = privatePortsEnv.Split(',').FirstOrDefault();
            if (int.TryParse(firstPort, out var parsedPrivate))
            {
                privatePort = parsedPrivate;
            }
        }

        if (!bool.TryParse(builder.Configuration.GetValue<string>("UseHttps"), out var useHttps))
        {
            useHttps = false;
        }

        var targetHost = builder.Configuration.GetValue<string>("TargetHost");
        if (string.IsNullOrWhiteSpace(targetHost))
        {
            targetHost = "localhost";
        }

        if (!int.TryParse(builder.Configuration.GetValue<string>("TargetPort"), out var targetPort))
        {
            targetPort = privatePort;
        }

        X509Certificate2 clusterCert;
        var certBase64 = builder.Configuration.GetValue<string>("ClusterCertificateBase64");
        
        if (!string.IsNullOrWhiteSpace(certBase64))
        {
            clusterCert = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certBase64));
        }
        else
        {
            var certPath = builder.Configuration.GetValue<string>("ClusterCertificatePath") ?? "cluster.cer";
            if (!File.Exists(certPath))
            {
                throw new FileNotFoundException($"Cluster certificate not found at '{certPath}' and 'ClusterCertificateBase64' configuration is missing. Please provide a valid certificate for node authentication.");
            }
            
            clusterCert = X509CertificateLoader.LoadCertificateFromFile(certPath);
        }

        using var certToDispose = clusterCert;
        var certBytes = clusterCert.Export(X509ContentType.Cert);

        var encryptionKey = builder.Configuration.GetValue<string>("EncryptionKeyBase64");
        
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            var encryptionKeyPath = builder.Configuration.GetValue<string>("EncryptionKeyPath") ?? "encryption.key";
            if (!File.Exists(encryptionKeyPath))
            {
                throw new FileNotFoundException($"Encryption key not found at '{encryptionKeyPath}' and 'EncryptionKeyBase64' configuration is missing. Please provide a valid AES-GCM base64 key for wire encryption.");
            }
            
            encryptionKey = File.ReadAllText(encryptionKeyPath).Trim();
        }

        var replicaId = builder.Configuration.GetValue<string>("ReplicaId");
        if (string.IsNullOrWhiteSpace(replicaId))
        {
            var azureInstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
            if (string.IsNullOrWhiteSpace(azureInstanceId))
            {
                azureInstanceId = Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME");
            }

            var stableIdentifier = !string.IsNullOrWhiteSpace(azureInstanceId) 
                ? azureInstanceId 
                : Environment.MachineName.ToLowerInvariant();

            replicaId = $"node-{stableIdentifier}";
        }

        var dataStorageConnectionString = builder.Configuration.GetValue<string>("DataStorageConnectionString");
        if (string.IsNullOrWhiteSpace(dataStorageConnectionString) && (builder.Environment.IsDevelopment() || Debugger.IsAttached))
        {
            dataStorageConnectionString = "UseDevelopmentStorage=true";
        }

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddDebug();
        }
        
        builder.Logging.AddAzureWebAppDiagnostics();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
        });

        var scheme = useHttps ? "https" : "http";
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"{scheme}://*:{advertisedPort}",
            $"{scheme}://*:{privatePort}"
        };
        
        builder.WebHost.UseUrls(urls.ToArray());

        if (useHttps)
        {
            builder.WebHost.UseKestrelHttpsConfiguration();
        }

        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();

        builder.Services.ConfigureAmaCommunityLicense();

        builder.Services
            .AddDistributedCrdtReplica(replicaId)
            .AddDistributedCrdtCore(options =>
            {
                options.ActiveSyncEnabled = true;
                options.CheckpointIntervalSeconds = (int)TimeSpan.FromMinutes(5).TotalSeconds;
                options.MaintenanceIntervalSeconds = (int)TimeSpan.FromHours(1).TotalSeconds;
                options.CompactionTtlSeconds = (int)TimeSpan.FromHours(24).TotalSeconds;
                options.AntiEntropyIntervalSeconds = (int)TimeSpan.FromMinutes(2).TotalSeconds;
                options.AntiEntropyInitialDelaySeconds = 5;
                options.AvoidBlindCheckpointWrites = true; // Optimization for services with rare calls in big intervals (affects CheckpointIntervalSeconds)

                // First 1 hour, replicas can come and go, but journals are not trimmed
                // Then for 1 minute the same replica names cannot be used
                // After that the replica name can be used again
                options.PeerEvictionTtlSeconds = (int)TimeSpan.FromHours(1).TotalSeconds;
                options.PeerTombstoneCooldownSeconds = 1; // Cool down pretty fast, we don't care about old state
            });

        builder.Services.AddDistributedCrdtP2p("feature-flags-http-mesh", replicaId);

        builder.Services.AddFeatureFlags();

        builder.Services.AddDistributedCrdtTableStorage(options =>
        {
            options.TableName = "FeatureFlagsDistributedCrdtStorage";
            options.ConnectionString = dataStorageConnectionString ?? throw new InvalidOperationException("Table storage connection string is mandatory");
            options.UseBinarySerialization = true;
        });

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
                options.AdvertisedPort = privatePort;
                options.UseHttps = false;
                options.StandaloneListenPort = privatePort;
            })
            .AddAspNetCorePeerDiscovery(options =>
            {
                options.HostingMode = AspNetCoreHostingMode.Integrated;
                options.TargetHost = targetHost;
                options.TargetPort = targetPort;
                options.TargetUseHttps = useHttps;
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

        // Needs to go last to override the serializer
        builder.Services.AddCrdtMessagePack(
            Ama_Enterprise_CRDT_MessagePack_MessagePackResolver.Instance,
            Ama_Enterprise_Samples_HttpLoadBalancerPollingFeatureFlags_MessagePackResolver.Instance
        );

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        if (useHttps)
        {
            app.UseHttpsRedirection();
        }
        
        app.UseAuthorization();

        app.MapP2pMeshEndpoints();
        app.MapP2pMeshHandshakes();
        app.MapP2pMeshDiscovery();

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

    private static string GetVNetIpAddress()
    {
        if (Debugger.IsAttached)
        {
            return "*";
        }

        var privateIp = Environment.GetEnvironmentVariable("WEBSITE_PRIVATE_IP");
        if (!string.IsNullOrWhiteSpace(privateIp))
        {
            return privateIp;
        }

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus == OperationalStatus.Up)
            {
                var properties = networkInterface.GetIPProperties();
                foreach (var address in properties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var bytes = address.Address.GetAddressBytes();
                        if (bytes[0] == 10 && bytes[1] == 0)
                        {
                            return address.Address.ToString();
                        }
                    }
                }
            }
        }

        throw new InvalidOperationException("No local IP could be found.");
    }
}

[JsonSerializable(typeof(FlagModel[]))]
[JsonSerializable(typeof(FlagSetResponse))]
[JsonSerializable(typeof(FlagDeleteResponse))]
[JsonSerializable(typeof(string))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext
{
}