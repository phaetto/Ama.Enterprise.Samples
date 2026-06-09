| File Path | Description |
| --- | --- |
| `$/.github/workflows/ci.yml` | GitHub Actions workflow for continuous integration that restores, builds, and tests the Ama.Enterprise.Samples solution on pull requests and branch pushes. |
| `$/.github/workflows/deploy-http-polling-feature-flags.yml` | GitHub Actions workflow for deploying the HTTP Load Balancer Polling Feature Flags application to Azure via Bicep. It triggers manually via `workflow_dispatch` and exposes options as inputs, now handling the creation of the underlying App Service Plan. |
| `$/.gitignore` | No description provided. |
| `$/01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags/Ama.Enterprise.Samples.TcpUdpFeatureFlags.csproj` | No description provided. |
| `$/01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags/Program.cs` | The entry point for the Beginner TCP/UDP Feature Flags showcase applying DI to structure the cluster mesh natively and registering `ConsoleFeatureFlagObserver` as an `IFeatureFlagStateObserver`. |
| `$/01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags/Services/ConsoleFeatureFlagObserver.cs` | Implementation of `IFeatureFlagStateObserver` that requests a console UI redraw when feature flags are updated across the cluster. |
| `$/01-Beginner/README.md` | Beginner-friendly step-by-step tutorial on bootstrapping a distributed CRDT application using the Feature Flags abstraction, cleaned up and aligned with the current DI and observer APIs. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/Ama.Enterprise.Samples.QuicUdpFeatureFlags.csproj` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/Ama.Enterprise.Samples.QuicUdpFeatureFlags.http` | HTTP file containing test requests for the QUIC UDP Feature Flags API endpoints. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/FlagDeleteResponse.cs` | DTO representing the response returned after successfully removing a feature flag for the QUIC/UDP Feature Flags sample. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/FlagModel.cs` | DTO representing a feature flag's current state for the QUIC/UDP Feature Flags sample. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/FlagSetResponse.cs` | DTO representing the response returned after successfully setting a feature flag for the QUIC/UDP Feature Flags sample. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/Program.cs` | Interactive Console application for the QUIC UDP Feature Flags showcase using DI, certificate/key files, and `ConsoleFeatureFlagObserver` as an `IFeatureFlagStateObserver` to react to distributed state changes. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/README.md` | Clear documentation explaining the QUIC/UDP Feature Flags sample, its 3-layer security model (mTLS, certificate auth, wire encryption), and state observation via `IFeatureFlagStateObserver`. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/WeatherForecast.cs` | DTO representing a weather forecast for the QUIC/UDP Feature Flags API endpoints. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/auth.pfx` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/cluster.pfx` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/encryption.key` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/README.md` | Overview of the Local Network P2P scenarios with links to both the Beginner TCP/UDP and QUIC/UDP Feature Flags samples. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/Ama.Enterprise.Samples.HttpDnsFeatureFlags.csproj` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/Ama.Enterprise.Samples.HttpDnsFeatureFlags.http` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/FlagDeleteResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/FlagModel.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/FlagSetResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/Program.cs` | The main entry point for the HTTP/DNS Feature Flags sample, updated to be Native AOT compliant by switching to `CreateSlimBuilder` and utilizing `System.Text.Json` source generation for Minimal APIs. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/README.md` | Documentation for the HTTP/DNS Feature Flags sample covering ASP.NET Core integration, DNS peer discovery, Zero-Trust security, and Native AOT support. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/WeatherForecast.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/appsettings.Development.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/appsettings.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/cluster.cer` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/dotnet-tools.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/encryption.key` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags.csproj` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags.http` | HTTP file containing test requests for the HTTP Load Balancer Polling Feature Flags API endpoints. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/HttpLoadBalancerPollingFeatureFlags.LoadTesting.http` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Infrastructure/main.bicep` | Bicep template for deploying an App Service Plan, VNet integration, associated Web App, and automatically provisions the `FeatureFlagsDistributedCrdtStorage` Azure Table for the HTTP Load Balancer sample. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Models/FlagDeleteResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Models/FlagModel.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Models/FlagSetResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Program.cs` | The main entry point for the HTTP Load Balancer Polling Feature Flags sample, using Native AOT compilation, Minimal APIs, and HTTP polling for peer discovery natively integrated with ASP.NET Core endpoints. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Properties/serviceDependencies.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Properties/serviceDependencies.local.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/README.md` | Documentation for the HTTP Load Balancer Polling Feature Flags sample covering load balancer peer discovery, VNet sync, Azure Table Storage persistence, MessagePack serialization, and Azure Bicep deployment. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/appsettings.Development.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/appsettings.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/cluster.cer` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/encryption.key` | No description provided. |
| `$/02-Scenarios/02-SassBackend/README.md` | Overview of the SaaS Backend Integration scenarios linking to the HTTP/DNS and HTTP Load Balancer Polling samples. |
| `$/02-Scenarios/03-ConnectionOverInternet/README.md` | Overview of connecting decentralized P2P nodes across the public internet using WebRTC and MQTT. |
| `$/02-Scenarios/README.md` | High-level overview of the three networking scenarios (Local Network, SaaS Backend, and Public Internet). |
| `$/Ama.Enterprise.Samples.Common/Ama.Enterprise.Samples.Common.csproj` | Added reference to `Ama.Enterprise.FeatureFlags` to support shared feature flag UI observers. |
| `$/Ama.Enterprise.Samples.Common/Logging/LockedConsoleLogger.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/Logging/LockedConsoleLoggerProvider.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/Network/NetworkUtilities.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/UI/ConsoleFeatureFlagObserver.cs` | Shared implementation of `IFeatureFlagStateObserver` that requests a console UI redraw on `InteractiveConsoleApp` when feature flags are updated across the cluster. |
| `$/Ama.Enterprise.Samples.Common/UI/ConsoleHelper.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/UI/InteractiveConsoleApp.cs` | Added DI-friendly constructor accepting `ILoggerFactory` and `SetTitle` method to support full dependency injection registration. |
| `$/Ama.Enterprise.Samples.slnx` | No description provided. |
| `$/CodingStandards.md` | No description provided. |
| `$/FilesDescription.md` | No description provided. |
| `$/LICENSE` | No description provided. |
| `$/README.md` | Main repository README providing a clear overview of Ama.Enterprise, scenario links, quick-start instructions, and licensing details. |
| `$/solution.settings.json` | No description provided. |
