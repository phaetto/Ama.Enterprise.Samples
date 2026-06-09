# 08 - Live CRDT Schema Migration (V1 to V2)

When evolving a decentralized system, simple additive changes can be handled in-place using `IExtensibleDistributedPayload`. However, when you need to make breaking structural or semantic changes to a CRDT document without cluster downtime, you can run a Live Migration Mediator between the Legacy V1 cluster and the Modern V2 cluster.

This sample demonstrates how to migrate from the built-in V1 Feature Flags model (`FeatureFlagState`) to a breaking V2 Feature Flags model (`FeatureFlagStateV2`) while both V1 and V2 nodes are running live.

---

## V1 vs. V2 Schema Comparison

| Aspect | Legacy V1 (`FeatureFlagState`) | Modern V2 (`FeatureFlagStateV2`) |
| --- | --- | --- |
| Document Type Alias | `feature-flags` | `feature-flags-v2` |
| Map Property Name | `Flags` (`Dictionary<string, FeatureFlagItem>`) | `Rules` (`Dictionary<string, FeatureFlagRuleV2>`) |
| Activation Model | Boolean `IsEnabled` (`true` / `false`) | Progressive `State` (`"Active"`, `"Gradual"`, `"Inactive"`) + `RolloutPercentage` (`0..100`) |
| Ownership Metadata | Nested `FeatureFlagOwnership` & `FeatureFlagAudit` objects | Flattened `Owner` string + `SchemaVersion = 2` |
| P2P Mesh / Multicast | `feature-flags-v1-mesh` (UDP Port `8037`) | `feature-flags-v2-mesh` (UDP Port `8038`) |

---

## Architecture: How the Live Migration Bridge Works

```
+-----------------------+          +-----------------------------------+          +-----------------------+
|   Legacy V1 Cluster   |          |      Migrator / Mediator Node     |          |   Modern V2 Cluster   |
| (feature-flags-v1)    | <======> |                                   | <======> | (feature-flags-v2)    |
|                       |  Gossip  |  [V1 Replica] ---> [Migrator]     |  Gossip  |                       |
|  Flags["DarkMode"]    |          |                        |          |          |  Rules["DarkMode"]    |
|    IsEnabled = true   |          |                        v          |          |    State = "Active"   |
|                       |          |                    [V2 Replica]   |          |    Rollout = 100%     |
+-----------------------+          +-----------------------------------+          +-----------------------+
```

1. Dual-Replica Hosting & Topology Partitioning: The `migrator` node hosts two isolated CRDT replicas in the same process:
   - `node-v1-<port>` joined to `feature-flags-v1-mesh`
   - `node-v2-<port>` joined to `feature-flags-v2-mesh`
   - To prevent cross-mesh Gossip and Anti-Entropy noise inside the single `IHost` process, the `migrator` node registers `MigrationScopeTopologyProvider` (`IScopeTopologyProvider`), which queries `IPeerRegistry` so the V1 replica scope only exchanges CRDT data with `feature-flags-v1-mesh` peers and the V2 replica scope only exchanges CRDT data with `feature-flags-v2-mesh` peers.
2. Custom `ICrdtDocumentMigrator<TSource, TTarget>`: `FeatureFlagV1ToV2Migrator` inspects the V1 `FeatureFlagState` alongside `previousSourceDocument` (for three-way merging) and emits CRDT intents (`MapSetIntent` and `MapRemoveIntent`) onto the V2 `FeatureFlagStateV2` orchestrator:
   - `IsEnabled == true` maps to `State = "Active", RolloutPercentage = 100`.
   - `IsEnabled == false` maps to `State = "Inactive", RolloutPercentage = 0`.
   - Nested ownership (`Ownership.Owner` or `Audit.LastModifiedBy`) is flattened into `Rule.Owner`.
   - Deleted V1 flags are detected by comparing `previousSourceDocument` against `sourceDocument` and removed from the V2 `Rules` map without wiping out V2-native rules created directly on the V2 cluster.
3. Registration via `AddDistributedCrdtMigration`:
   ```csharp
   builder.Services.AddDistributedCrdtMigration<FeatureFlagState, FeatureFlagStateV2, FeatureFlagV1ToV2Migrator>(options =>
   {
       options.MigrationId = "feature-flags-v1-to-v2";
       options.SourceReplicaId = v1ReplicaId;
       options.SourceTypeAlias = "feature-flags";
       options.TargetReplicaId = v2ReplicaId;
       options.TargetTypeAlias = "feature-flags-v2";
       options.PropagateDocumentTombstones = true;
   });
   ```
4. Background Backfill & Real-Time Streaming: `CrdtMigrationBridgeService` automatically backfills existing V1 documents on startup and hooks into `IDocumentStateObserver<FeatureFlagState>` so every live V1 mutation is immediately translated and broadcast to the V2 cluster.

---

## Step-by-Step Walkthrough

Open three terminals in `03-Advanced/08-Migration/Ama.Enterprise.Samples.Migration`:

### 1. Start a Legacy V1 Node (Terminal 1)
```bash
dotnet run -- v1 8100
```
- The V1 node starts on `feature-flags-v1-mesh` with `(No V1 flags defined)`.
- Seed a few V1 boolean flags using the `set` command:
  ```text
  set DarkMode true alice
  set BetaDashboard false bob
  set LegacyExport true charlie
  ```

### 2. Start a Modern V2 Node (Terminal 2)
```bash
dotnet run -- v2 8200
```
- The V2 node starts on `feature-flags-v2-mesh`.
- Because V1 and V2 use different meshes and different CRDT schemas, the V2 node initially shows `(No V2 rules defined)`.

### 3. Start the Live Migration Mediator Node (Terminal 3)
```bash
dotnet run -- migrator 8300
```
- The `migrator` node joins both `feature-flags-v1-mesh` and `feature-flags-v2-mesh`.
- As soon as it syncs the V1 document from Terminal 1, `FeatureFlagV1ToV2Migrator` translates all V1 boolean flags into V2 progressive rules and gossips them to Terminal 2!
- Watch Terminal 2 (the V2 node) automatically populate with:
  - `=> [V2] BetaDashboard: State=Inactive (0%) | Owner=bob | Schema=v2`
  - `=> [V2] DarkMode: State=Active (100%) | Owner=alice | Schema=v2`
  - `=> [V2] LegacyExport: State=Active (100%) | Owner=charlie | Schema=v2`
- Notice the Migration Pipeline telemetry header on Terminal 3 showing `Converged : True | Backfilled: 1 | Pending Queue: 0 | Failed: 0`.

### 4. Test Live Streaming & Deletions
- In Terminal 1 (V1 Node):
  - Enable `BetaDashboard` on V1:
    ```text
    set BetaDashboard true bob
    ```
  - Immediately watch Terminal 2 (V2 Node) update `BetaDashboard` to `State=Active (100%)`.
  - Delete `LegacyExport` on V1:
    ```text
    del LegacyExport
    ```
  - Watch `LegacyExport` disappear from Terminal 2 (V2 Node) while any V2-native rules created directly on Terminal 2 remain intact.
- In Terminal 2 (V2 Node):
  - Update `DarkMode` using V2's progressive percentage rollout (`0..100`):
    ```text
    set DarkMode 25 alice
    ```
    Notice it transitions to `State=Gradual (25%)`.
  - Create a V2-native rule directly on Terminal 2:
    ```text
    set NewV2Canary 35 v2-team
    ```
    Observe that subsequent V1 updates or deletions on Terminal 1 never overwrite or delete `NewV2Canary`.
- In Terminal 3 (Migrator Node):
  - Run `drain` at any time (or `set-v1` / `del-v1` / `set-v2` / `del-v2`) to force an immediate drain and flush of the migration queue before decommissioning V1 nodes:
    ```text
    drain
    ```

[⬅ Back to Advanced Overview](../README.md)