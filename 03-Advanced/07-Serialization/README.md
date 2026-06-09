# 07 - CRDT Serialization (`System.Text.Json`, Brotli, and MessagePack)

By default, `Ama.CRDT` serializes operations, state snapshots, metadata, and Dotted Version Vectors (DVVs) using Native AOT-friendly `System.Text.Json`. Depending on your network bandwidth, storage footprint, and CPU trade-offs, you can swap the underlying `ICrdtSerializer` implementation via Dependency Injection.

This sample demonstrates the three built-in serialization configurations provided by `Ama.CRDT` and `Ama.Enterprise.CRDT.MessagePack`, and displays live serialized byte footprints for active CRDT documents.

---

## Available Serialization Options

### 1. Uncompressed `System.Text.Json` (Default)

When you call `services.AddCrdt()`, `JsonCrdtSerializer` is registered by default. It produces human-readable UTF-8 JSON and uses source-generated `JsonSerializerContext` resolvers for full Native AOT compatibility.

```csharp
using Ama.CRDT.Extensions;

// Explicitly configure uncompressed UTF-8 JSON:
builder.Services.AddCrdtSystemTextJson(useBrotliCompression: false);
```

When using custom CRDT document models in Native AOT, register your `JsonSerializerContext` and polymorphic payload discriminators:

```csharp
builder.Services.AddCrdt()
    .AddCrdtJsonTypeInfoResolver(MyCustomJsonContext.Default)
    .AddCrdtAotContext<MyCustomCrdtAotContext>()
    .AddCrdtSerializableType<MyItemPayload>("my-item-payload")
    .AddCrdtSystemTextJson(useBrotliCompression: false);
```

---

### 2. Brotli-Compressed `System.Text.Json`

To significantly reduce network packet sizes and disk storage footprints while keeping `System.Text.Json` contracts, enable Brotli compression by passing `useBrotliCompression: true` to `AddCrdtSystemTextJson`. This replaces `JsonCrdtSerializer` with `BrotliJsonCrdtSerializer`.

```csharp
using Ama.CRDT.Extensions;

// Replaces the default JSON serializer with Brotli-compressed JSON:
builder.Services.AddCrdtSystemTextJson(useBrotliCompression: true);
```

---

### 3. Binary `MessagePack` (`Ama.Enterprise.CRDT.MessagePack`)

For high-throughput, low-latency binary serialization, the `Ama.Enterprise.CRDT.MessagePack` package provides `MessagePackCrdtSerializer`. It bridges `Ama.CRDT`'s string-based polymorphic type registry (`AddCrdtSerializableType<T>("discriminator")`) directly into binary MessagePack formatters.

```csharp
using Ama.CRDT.Extensions;
using Ama.Enterprise.CRDT.MessagePack.Extensions;
using MessagePack.Resolvers;

builder.Services.AddCrdt()
    .AddCrdtAotContext<MyCustomCrdtAotContext>()
    .AddCrdtSerializableType<MyItemPayload>("my-item-payload");

// Replace System.Text.Json with MessagePack binary serialization.
// Pass the built-in CRDT resolver along with your project's source-generated MessagePack resolver:
builder.Services.AddCrdtMessagePack(
    Ama_Enterprise_CRDT_MessagePack_MessagePackResolver.Instance,
    MyProject_MessagePackResolver.Instance);
```

> Note: When using `AddCrdtMessagePack` with custom models in Native AOT, annotate your custom CRDT state and payload models with `[MessagePackObject]` and `[Key(n)]` attributes so the MessagePack source generator includes them in your assembly's `IFormatterResolver`.

---

## Running the Sample

You can pass the desired serializer mode (`brotli`, `json`, or `msgpack`) and an optional port number as command-line arguments.

> Important: All nodes participating in the same P2P mesh must use the same serialization mode so they can decode each other's gossip and anti-entropy payloads.

### Compare `brotli` vs `json` Payload Sizes

1. Start a node using Brotli-compressed JSON (default):
   ```bash
   dotnet run --project 03-Advanced/07-Serialization/Ama.Enterprise.Samples.Serialization -- brotli 8100
   ```

2. Add a few feature flags:
   ```text
   > set DarkMode true
   > set BetaCheckout false
   > set NewDashboard true
   ```

3. Observe the `[Serialized Document Footprint]` section printed in the console (`Data`, `Meta`, `Total`, and `DVV` byte counts).

4. Stop the node (`exit`) and start it with Uncompressed JSON:
   ```bash
   dotnet run --project 03-Advanced/07-Serialization/Ama.Enterprise.Samples.Serialization -- json 8100
   ```

5. Set the same flags and compare the serialized byte sizes reported by `EstimateMemoryFootprintAsync`.

---
[⬅ Back to Advanced Overview](../README.md)