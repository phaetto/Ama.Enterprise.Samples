| File Path | Description |
| --- | --- |
| `$/.gitignore` | No description provided. |
| `$/01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags/Ama.Enterprise.Samples.TcpUdpFeatureFlags.csproj` | No description provided. |
| `$/01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags/Program.cs` | The entry point for the Beginner TCP/UDP Feature Flags showcase applying DI to structure the cluster mesh natively while using the interactive console app boilerplate implicitly. |
| `$/01-Beginner/README.md` | Provides a beginner-friendly, step-by-step tutorial on bootstrapping a distributed CRDT application using the Feature Flags abstraction, detailing P2P network configuration, UDP discovery, TCP transport, and cluster interaction. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/Ama.Enterprise.Samples.QuicUdpFeatureFlags.csproj` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/Ama.Enterprise.Samples.QuicUdpFeatureFlags.http` | HTTP file containing test requests for the QUIC UDP Feature Flags API endpoints. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/FlagDeleteResponse.cs` | DTO representing the response returned after successfully removing a feature flag for the QUIC/UDP Feature Flags sample. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/FlagModel.cs` | DTO representing a feature flag's current state for the QUIC/UDP Feature Flags sample. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/FlagSetResponse.cs` | DTO representing the response returned after successfully setting a feature flag for the QUIC/UDP Feature Flags sample. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/Program.cs` | Replaced the Minimal API implementation with a simpler Interactive Console application for the QUIC UDP Feature Flags showcase, using DI directly and common UI boilerplates. Updated to load certificates and encryption keys from files instead of generating them. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/README.md` | Extensive documentation explaining how the QUIC/UDP Feature Flags sample works, targeting controlled local networks using powerful network primitives like UDP Multicast, multiplexed QUIC transport, and advanced security parameters. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/WeatherForecast.cs` | DTO representing a weather forecast for the QUIC/UDP Feature Flags API endpoints. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/auth.pfx` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/cluster.pfx` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/encryption.key` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/README.md` | Updated to point to the beginner TCP/UDP feature flags sample while retaining the explanation of local network capabilities and discovery mechanisms. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/Ama.Enterprise.Samples.HttpDnsFeatureFlags.csproj` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/Ama.Enterprise.Samples.HttpDnsFeatureFlags.http` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/FlagDeleteResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/FlagModel.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/FlagSetResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/Program.cs` | The main entry point for the HTTP/DNS Feature Flags sample, updated to be Native AOT compliant by switching to `CreateSlimBuilder` and utilizing `System.Text.Json` source generation for Minimal APIs. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/README.md` | Extensive documentation explaining how the HTTP/DNS Feature Flags sample works, targeting cloud environments (SaaS/Kubernetes) using standard ASP.NET Core integrations and DNS peer discovery mechanisms. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/WeatherForecast.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/appsettings.Development.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/appsettings.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/cluster.cer` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/dotnet-tools.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/encryption.key` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags.csproj` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags.http` | HTTP file containing test requests for the HTTP Load Balancer Polling Feature Flags API endpoints. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Infrastructure/main.bicep` | Bicep template for deploying an App Service Plan with 3 Linux VM instances using the B1 tier (cheapest Always On), VNet integration for private P2P networking, and an associated Web App. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Models/FlagDeleteResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Models/FlagModel.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Models/FlagSetResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Models/WeatherForecast.cs` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Program.cs` | The main entry point for the HTTP Load Balancer Polling Feature Flags sample, using Native AOT compilation, Minimal APIs, and HTTP polling for peer discovery natively integrated with ASP.NET Core endpoints. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/README.md` | Extensive documentation explaining how the HTTP Load Balancer Polling Feature Flags sample works, refactored to align with the style and structure of the HTTP/DNS Feature Flags sample, highlighting ASP.NET Core integration, HTTP polling discovery, and Azure App Service deployment specifics. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/appsettings.Development.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/appsettings.json` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/cluster.cer` | No description provided. |
| `$/02-Scenarios/02-SassBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/encryption.key` | No description provided. |
| `$/02-Scenarios/02-SassBackend/README.md` | Main SaaS Backend Scenario readme, updated to link and describe both the HTTP/DNS Feature Flags and the HTTP Load Balancer Polling Feature Flags showcase applications. |
| `$/02-Scenarios/03-ConnectionOverInternet/README.md` | No description provided. |
| `$/02-Scenarios/README.md` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/Ama.Enterprise.Samples.Common.csproj` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/Logging/LockedConsoleLogger.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/Logging/LockedConsoleLoggerProvider.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/Network/NetworkUtilities.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/UI/ConsoleHelper.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/UI/InteractiveConsoleApp.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.slnx` | No description provided. |
| `$/CodingStandards.md` | No description provided. |
| `$/FilesDescription.md` | No description provided. |
| `$/LICENSE` | No description provided. |
| `$/README.md` | The main entry point documentation for the Ama.Enterprise Samples repository. Updated the Getting Started section to point directly to the Beginner tutorials and samples for a better first-time developer experience. |
| `$/solution.settings.json` | No description provided. |
