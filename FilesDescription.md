| File Path | Description |
| --- | --- |
| `$/.github/workflows/ci.yml` | GitHub Actions workflow for continuous integration that restores, builds, and tests the Ama.Enterprise.Samples solution across all branches on push, pull requests, and manual triggers using .NET 10. |
| `$/.github/workflows/deploy-http-polling-feature-flags.yml` | GitHub Actions workflow for deploying the HTTP Load Balancer Polling Feature Flags application to Azure via Bicep. It triggers manually via `workflow_dispatch` and exposes options as inputs, now handling the creation of the underlying App Service Plan. |
| `$/.gitignore` | No description provided. |
| `$/01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags/Ama.Enterprise.Samples.TcpUdpFeatureFlags.csproj` | No description provided. |
| `$/01-Beginner/Ama.Enterprise.Samples.TcpUdpFeatureFlags/Program.cs` | The entry point for the Beginner TCP/UDP Feature Flags showcase using `Host.CreateEmptyApplicationBuilder` for AOT-friendly DI and hosting lifecycle management. |
| `$/01-Beginner/README.md` | Beginner-friendly step-by-step tutorial on bootstrapping a distributed CRDT application using the Feature Flags abstraction, cleaned up and aligned with the current DI and observer APIs. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/Ama.Enterprise.Samples.QuicUdpFeatureFlags.csproj` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/Ama.Enterprise.Samples.QuicUdpFeatureFlags.http` | HTTP file containing test requests for the QUIC UDP Feature Flags API endpoints. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/Program.cs` | Interactive Console application for the QUIC UDP Feature Flags showcase using `Host.CreateEmptyApplicationBuilder` for AOT-friendly DI, certificate/key files, and `ConsoleFeatureFlagObserver`. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/README.md` | Clear documentation explaining the QUIC/UDP Feature Flags sample, its 3-layer security model (mTLS, certificate auth, wire encryption), and state observation via `IFeatureFlagStateObserver`. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/auth.pfx` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/cluster.pfx` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/Ama.Enterprise.Samples.QuicUdpFeatureFlags/encryption.key` | No description provided. |
| `$/02-Scenarios/01-LocalNetwork/README.md` | Overview of the Local Network P2P scenarios with links to both the Beginner TCP/UDP and QUIC/UDP Feature Flags samples. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/Ama.Enterprise.Samples.HttpDnsFeatureFlags.csproj` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/Ama.Enterprise.Samples.HttpDnsFeatureFlags.http` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/FlagDeleteResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/FlagModel.cs` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/FlagSetResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/Program.cs` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/README.md` | Documentation for the HTTP/DNS Feature Flags sample, showing ASP.NET Core Kestrel integration and DNS peer discovery. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/appsettings.Development.json` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/appsettings.json` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/cluster.cer` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/dotnet-tools.json` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpDnsFeatureFlags/encryption.key` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags.csproj` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags.http` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/HttpLoadBalancerPollingFeatureFlags.LoadTesting.http` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Infrastructure/main.bicep` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Models/FlagDeleteResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Models/FlagModel.cs` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Models/FlagSetResponse.cs` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Program.cs` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Properties/serviceDependencies.json` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/Properties/serviceDependencies.local.json` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/README.md` | Documentation for the HTTP Load Balancer Polling Feature Flags sample with Azure Table Storage and Bicep deployment. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/appsettings.Development.json` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/appsettings.json` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/cluster.cer` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/Ama.Enterprise.Samples.HttpLoadBalancerPollingFeatureFlags/encryption.key` | No description provided. |
| `$/02-Scenarios/02-SaasBackend/README.md` | Overview of SaaS and Cloud Backend P2P scenarios using HTTP transport, DNS discovery, and load balancer polling. |
| `$/02-Scenarios/03-ConnectionOverInternet/README.md` | Overview of connecting decentralized P2P nodes across the public internet using WebRTC and MQTT. |
| `$/02-Scenarios/README.md` | High-level overview of the three networking scenarios (Local Network, SaaS Backend, and Public Internet). |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Ama.Enterprise.Samples.CustomCrdt.csproj` | No description provided. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Constants.cs` | Defines channel constants, AOT discriminators, spam quorum count, max history size, and initial seed chat items. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Extensions/ChatServiceCollectionExtensions.cs` | DI extension registering the single `ChatChannelState` document type, AOT/JSON contexts, custom document ID provider, and composable chat manager. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Models/ChatChannelState.cs` | Root chat channel CRDT model containing both the `[CrdtPriorityQueueStrategy]` message list and nested `[CrdtLwwMapStrategy]` per-message states (`MessageStates`). |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Models/ChatCrdtAotContext.cs` | Native AOT CRDT reflection context for `ChatChannelState` and its nested `ChatMessageState` dictionary. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Models/ChatItemDefinition.cs` | No description provided. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Models/ChatItemView.cs` | No description provided. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Models/ChatJsonContext.cs` | Native AOT `System.Text.Json` source-generated context for `ChatChannelState` and its nested collections. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Models/ChatMessageState.cs` | No description provided. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Program.cs` | No description provided. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Services/ChatCrdtManager.cs` | Implementation of `IChatCrdtManager` demonstrating deep composable CRDT operations via `ICrdtPatcher.GenerateOperation` on nested dictionary values within a single channel document. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Services/ChatDocumentIdProvider.cs` | Custom `IDocumentIdProvider` for `ChatChannelState.ChannelId`. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Services/ChatItemElementComparer.cs` | No description provided. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Services/ConsoleChatChannelStateObserver.cs` | No description provided. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Services/ConsoleChatStateObserver.cs` | No description provided. |
| `$/03-Advanced/01-CustomCrdt/Ama.Enterprise.Samples.CustomCrdt/Services/IChatCrdtManager.cs` | No description provided. |
| `$/03-Advanced/01-CustomCrdt/README.md` | Documentation for the Custom CRDT chat messenger sample demonstrating Priority Queues and deep composable operations (`ICrdtPatcher.GenerateOperation`) for nested OR-Sets and Approval Quorums. |
| `$/03-Advanced/02-MultipleMeshes/Ama.Enterprise.Samples.MultipleMeshes/Ama.Enterprise.Samples.MultipleMeshes.csproj` | No description provided. |
| `$/03-Advanced/02-MultipleMeshes/Ama.Enterprise.Samples.MultipleMeshes/Constants.cs` | Defines mesh names (`mesh-alpha`, `mesh-beta`) and UDP multicast ports (`8035`, `8036`) for the Multiple Meshes bridge showcase. |
| `$/03-Advanced/02-MultipleMeshes/Ama.Enterprise.Samples.MultipleMeshes/Program.cs` | Interactive console showcase demonstrating a CRDT replica acting as a Bridge / Gateway node between two isolated P2P meshes (`alpha`, `beta`, and `bridge` modes). |
| `$/03-Advanced/02-MultipleMeshes/README.md` | Documentation and step-by-step walkthrough for the Multiple Meshes Bridge / Gateway topology sample. |
| `$/03-Advanced/03-MultipleReplicas/Ama.Enterprise.Samples.MultipleReplicas/Ama.Enterprise.Samples.MultipleReplicas.csproj` | No description provided. |
| `$/03-Advanced/03-MultipleReplicas/Ama.Enterprise.Samples.MultipleReplicas/Constants.cs` | Defines the dedicated `staging` and `prod` mesh names, scope keys, and TCP/UDP multicast ports for the Multiple Replicas showcase. |
| `$/03-Advanced/03-MultipleReplicas/Ama.Enterprise.Samples.MultipleReplicas/Program.cs` | Interactive console showcase demonstrating multi-tenant/multi-environment CRDT replica isolation (`staging` and `prod` scopes) hosted in a single process using separate `IServiceCollection` / `IHost` containers so the global Gossip algorithm does not bridge them. |
| `$/03-Advanced/03-MultipleReplicas/README.md` | Documentation and walkthrough explaining why separate `IServiceCollection` / `IHost` containers are required to host multiple isolated CRDT replicas in a single process without bridging their global Gossip networks. |
| `$/03-Advanced/04-Persistence/Ama.Enterprise.Samples.Persistence/Ama.Enterprise.Samples.Persistence.csproj` | Project file for the SQLite Persistence sample referencing `Microsoft.Data.Sqlite` and `Ama.Enterprise.FeatureFlags`. |
| `$/03-Advanced/04-Persistence/Ama.Enterprise.Samples.Persistence/Constants.cs` | Defines default P2P mesh name and port constants for the SQLite Persistence showcase. |
| `$/03-Advanced/04-Persistence/Ama.Enterprise.Samples.Persistence/Program.cs` | Interactive console showcase demonstrating durable CRDT state, DVV, and journal persistence across node restarts using `SqliteCrdtStorage`. |
| `$/03-Advanced/04-Persistence/Ama.Enterprise.Samples.Persistence/Services/SqliteCrdtStorage.cs` | Custom Native AOT-friendly SQLite implementation of `IDistributedCrdtStorage` using the Single-Writer / Multiple-Reader WAL pattern. |
| `$/03-Advanced/04-Persistence/README.md` | Documentation and step-by-step walkthrough explaining how to implement and register a custom `IDistributedCrdtStorage` backed by SQLite, as well as how to use the built-in `Ama.Enterprise.CRDT.Distributed.TableStorage` provider. |
| `$/03-Advanced/05-ZeroTrustRouting/Ama.Enterprise.Samples.ZeroTrustRouting/Ama.Enterprise.Samples.ZeroTrustRouting.csproj` | No description provided. |
| `$/03-Advanced/05-ZeroTrustRouting/Ama.Enterprise.Samples.ZeroTrustRouting/Constants.cs` | Defines default P2P mesh name, multicast settings, ports, and session claim keys (`role`, `region`) for the Zero-Trust Routing showcase. |
| `$/03-Advanced/05-ZeroTrustRouting/Ama.Enterprise.Samples.ZeroTrustRouting/Models/NodeSessionContext.cs` | No description provided. |
| `$/03-Advanced/05-ZeroTrustRouting/Ama.Enterprise.Samples.ZeroTrustRouting/Program.cs` | Interactive console showcase demonstrating P2P session authentication, zero-trust gossip routing policies, and session-aware CRDT scope topology partitioning. |
| `$/03-Advanced/05-ZeroTrustRouting/Ama.Enterprise.Samples.ZeroTrustRouting/Services/RbacMeshRoutingPolicy.cs` | Implementation of `IMeshRoutingPolicy` enforcing RBAC role isolation and directional geo-fencing (`US -> EU` allowed, `EU -> US` blocked) on live P2P gossip messages. |
| `$/03-Advanced/05-ZeroTrustRouting/Ama.Enterprise.Samples.ZeroTrustRouting/Services/RbacScopeTopologyProvider.cs` | Implementation of `IScopeTopologyProvider` that queries `IPeerSessionRegistry` so background CRDT Anti-Entropy and DVV tracking respect session RBAC and geo-fencing boundaries. |
| `$/03-Advanced/05-ZeroTrustRouting/Ama.Enterprise.Samples.ZeroTrustRouting/Services/RbacSessionTokenValidator.cs` | Implementation of `ISessionTokenValidator` that generates and validates handshake session tokens to populate peer `SessionContext` role and region claims. |
| `$/03-Advanced/05-ZeroTrustRouting/README.md` | Documentation and step-by-step walkthrough for Zero-Trust Routing, Session Authentication, and CRDT topology partitioning. |
| `$/03-Advanced/06-Monitoring/Ama.Enterprise.Samples.Monitoring/Ama.Enterprise.Samples.Monitoring.csproj` | No description provided. |
| `$/03-Advanced/06-Monitoring/Ama.Enterprise.Samples.Monitoring/Program.cs` | No description provided. |
| `$/03-Advanced/06-Monitoring/README.md` | Overview of cluster monitoring, diagnostics, and observability for distributed CRDT meshes. |
| `$/03-Advanced/07-Serialization/Ama.Enterprise.Samples.Serialization/Ama.Enterprise.Samples.Serialization.csproj` | Project file for the Serialization sample referencing `Ama.Enterprise.FeatureFlags` and `Ama.Enterprise.CRDT.MessagePack`. |
| `$/03-Advanced/07-Serialization/Ama.Enterprise.Samples.Serialization/Constants.cs` | Defines mesh networking constants and serializer CLI mode names (`json`, `brotli`, `msgpack`) for the Serialization showcase. |
| `$/03-Advanced/07-Serialization/Ama.Enterprise.Samples.Serialization/Program.cs` | Interactive console showcase demonstrating how to configure `AddCrdtSystemTextJson` (plain and Brotli-compressed) and `AddCrdtMessagePack`, displaying live serialized document byte footprints. |
| `$/03-Advanced/07-Serialization/README.md` | Documentation and walkthrough explaining the three CRDT serialization options (`System.Text.Json`, Brotli-compressed JSON, and `Ama.Enterprise.CRDT.MessagePack`). |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Ama.Enterprise.Samples.Migration.csproj` | No description provided. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Constants.cs` | Defines mesh names, multicast ports, document/payload type aliases, and migration IDs for the V1-to-V2 migration showcase. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Extensions/FeatureFlagV2ServiceCollectionExtensions.cs` | DI extension methods registering the breaking V2 feature flag CRDT models, AOT contexts, `IDocumentStateObserver<FeatureFlagStateV2>`, and `IFeatureFlagV2CrdtManager`. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Models/FeatureFlagRuleV2.cs` | Breaking V2 feature flag rule model replacing boolean `IsEnabled` with progressive `State`, `RolloutPercentage`, and flattened ownership. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Models/FeatureFlagStateV2.cs` | Root V2 CRDT document state replacing the V1 `Flags` map with a `Rules` map of `FeatureFlagRuleV2` items. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Models/FeatureFlagsV2CrdtAotContext.cs` | Native AOT CRDT reflection context for the V2 feature flag models. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Models/FeatureFlagsV2JsonContext.cs` | Native AOT `System.Text.Json` source generation context for the V2 feature flag models. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Models/ReplicaMeshMapping.cs` | Sealed reference model implementing `IEquatable<ReplicaMeshMapping>` that maps a CRDT replica scope identifier to its dedicated P2P mesh network name for DI singleton registration. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Program.cs` | Interactive console showcase supporting `v1`, `v2`, and `migrator` node modes, applying `MigrationScopeTopologyProvider` on the `migrator` node to partition V1 and V2 CRDT gossip/sync flows. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Services/ConsoleFeatureFlagV2Observer.cs` | Implementation of `IDocumentStateObserver<FeatureFlagStateV2>` that triggers a console UI redraw when V2 rules change. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Services/FeatureFlagV1ToV2Migrator.cs` | Implementation of `ICrdtDocumentMigrator<FeatureFlagState, FeatureFlagStateV2>` that translates V1 flags and deletions into V2 rule intents. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Services/FeatureFlagV2CrdtManager.cs` | Scoped manager service for reading and mutating V2 feature flag rules via `ICrdtDocumentOrchestrator`. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Services/IFeatureFlagV2CrdtManager.cs` | Interface for managing V2 feature flag rules in the cluster. |
| `$/03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration/Services/MigrationScopeTopologyProvider.cs` | Scoped `IScopeTopologyProvider` for the migration mediator node that checks `IPeerRegistry` so V1 and V2 replicas only exchange CRDT state with peers belonging to their own mesh. |
| `$/03-Advanced/08-Migration/README.md` | Documentation and step-by-step walkthrough for running Legacy V1 nodes, Modern V2 nodes, and a partitioned Live Migration Mediator node. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Ama.Enterprise.Samples.Sharding.csproj` | No description provided. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Constants.cs` | Defines mesh networking constants, default replication factor, document type alias, and shard prefix for the Horizontal CRDT Sharding sample. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Extensions/ShardingServiceCollectionExtensions.cs` | DI extension methods registering `TenantFeatureFlagState`, AOT contexts, `TenantShardIdProvider`, `ConsoleShardingObserver`, and `ITenantFlagManager`. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Models/ShardingCrdtAotContext.cs` | Native AOT CRDT reflection context for `TenantFeatureFlagState`. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Models/ShardingJsonContext.cs` | Native AOT `System.Text.Json` source generation context for `TenantFeatureFlagState`. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Models/TenantFeatureFlagState.cs` | Sharded tenant feature flag CRDT document state implementing `IEquatable<TenantFeatureFlagState>`. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Models/TenantShardOverviewDto.cs` | Readonly record struct DTO representing a tenant document's shard placement, owner replicas, local hosting status, and memory footprint. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Program.cs` | Interactive console showcase demonstrating consistent hash ring CRDT sharding, live shard rebalancing, local vs. remote memory hosting, and transparent P2P RPC read/write proxying. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Services/ConsoleShardingObserver.cs` | Implementation of `IDocumentStateObserver<TenantFeatureFlagState>` and `ICrdtOrchestratorObserver` that triggers a UI redraw on document or shard topology changes. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Services/ITenantFlagManager.cs` | Interface for creating, mutating, reading, and inspecting sharded tenant feature flag documents across the cluster. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Services/TenantFlagManager.cs` | Implementation of `ITenantFlagManager` using `ICrdtDocumentOrchestrator` and `IShardDistributionManager`. |
| `$/03-Advanced/09-Sharding/Ama.Enterprise.Samples.Sharding/Services/TenantShardIdProvider.cs` | Custom `IShardIdProvider` mapping each `TenantFeatureFlagState` document to `tenant-shard-{documentId}` for consistent hash ring partitioning. |
| `$/03-Advanced/09-Sharding/README.md` | Comprehensive documentation and step-by-step walkthrough for the Horizontal CRDT Sharding sample. |
| `$/03-Advanced/README.md` | Overview of all 9 advanced architecture and CRDT capability samples. |
| `$/Ama.Enterprise.Samples.Common/Ama.Enterprise.Samples.Common.csproj` | Added reference to `Ama.Enterprise.FeatureFlags` to support shared feature flag UI observers. |
| `$/Ama.Enterprise.Samples.Common/Logging/LockedConsoleLogger.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/Logging/LockedConsoleLoggerProvider.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/Network/NetworkUtilities.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/UI/ConsoleFeatureFlagObserver.cs` | Shared implementation of `IFeatureFlagStateObserver` that requests a console UI redraw on `InteractiveConsoleApp` when feature flags are updated across the cluster. |
| `$/Ama.Enterprise.Samples.Common/UI/ConsoleHelper.cs` | No description provided. |
| `$/Ama.Enterprise.Samples.Common/UI/InteractiveConsoleApp.cs` | Added DI-friendly constructor accepting `ILoggerFactory` and `SetTitle` method to support full dependency injection registration. |
| `$/Ama.Enterprise.Samples.slnx` | Solution file organizing all projects, folders, documentation READMEs, and repository solution items to mirror the physical directory structure. |
| `$/CodingStandards.md` | No description provided. |
| `$/FilesDescription.md` | No description provided. |
| `$/LICENSE` | No description provided. |
| `$/README.md` | Main repository README providing a clear overview of Ama.Enterprise, beginner/scenario/advanced links, quick-start instructions, and licensing details. |
| `$/solution.settings.json` | No description provided. |
