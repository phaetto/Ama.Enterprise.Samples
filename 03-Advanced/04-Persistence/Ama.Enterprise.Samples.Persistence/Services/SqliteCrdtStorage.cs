namespace Ama.Enterprise.Samples.Persistence.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Ama.CRDT.Models;
using Ama.CRDT.Services;
using Ama.CRDT.Services.Serialization;
using Ama.Enterprise.CRDT.Distributed.Models;
using Ama.Enterprise.CRDT.Distributed.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

/// <summary>
/// Custom SQLite implementation of <see cref="IDistributedCrdtStorage"/> that persists CRDT documents,
/// Dotted Version Vectors (DVV), cluster state snapshots, and the operation journal to a local SQLite database.
/// Utilizes a Single-Writer / Multiple-Reader WAL pattern for Native AOT friendly asynchronous throughput without lock contention.
/// </summary>
public sealed class SqliteCrdtStorage : IDistributedCrdtStorage, IDisposable
{
    private readonly ICrdtSerializer serializer;
    private readonly ILogger<SqliteCrdtStorage> logger;
    private readonly string connectionString;

    // SQLite allows unlimited concurrent readers, but strictly 1 writer.
    // Dedicating a single permanently open connection for writes protected by a SemaphoreSlim prevents "database is locked" errors.
    private readonly SqliteConnection writeConnection;
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private bool disposed;

    public SqliteCrdtStorage(
        ReplicaContext replicaContext,
        ICrdtSerializer serializer,
        ILogger<SqliteCrdtStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(replicaContext);
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var dbFileName = $"crdt_storage_{replicaContext.ReplicaId}.db";
        this.connectionString = $"Data Source={dbFileName};Pooling=True;";

        InitializeDatabase();

        this.writeConnection = new SqliteConnection(this.connectionString);
        this.writeConnection.Open();
        using var pragmaCmd = this.writeConnection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA synchronous = OFF; PRAGMA temp_store = MEMORY; PRAGMA cache_size = -8000; PRAGMA mmap_size = 0; PRAGMA journal_size_limit = 67108864;";
        pragmaCmd.ExecuteNonQuery();

        this.logger.LogInformation("Initialized SQLite CRDT storage at '{DatabaseFile}' for replica '{ReplicaId}'.", dbFileName, replicaContext.ReplicaId);
    }

    public async Task<CrdtDocument<TState>?> LoadDocumentAsync<TState>(string shardId, string documentId, CancellationToken cancellationToken = default) where TState : class, new()
    {
        ArgumentException.ThrowIfNullOrEmpty(shardId);
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        try
        {
            using var connection = new SqliteConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Payload FROM Documents WHERE ShardId = @ShardId AND DocumentId = @DocId";
            AddParameter(command, "@ShardId", shardId);
            AddParameter(command, "@DocId", documentId);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is byte[] bytes)
            {
                return this.serializer.DeserializeFromBytes<CrdtDocument<TState>>(bytes);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to load document '{DocumentId}' from SQLite", documentId);
            throw;
        }

        return null;
    }

    public Task<CrdtDocument<TState>?> LoadOrphanedDocumentAsync<TState>(string shardId, string documentId, CancellationToken cancellationToken = default) where TState : class, new()
    {
        ArgumentException.ThrowIfNullOrEmpty(shardId);
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        return Task.FromResult<CrdtDocument<TState>?>(null);
    }

    public Task SaveDocumentAsync<TState>(string shardId, string documentId, CrdtDocument<TState> document, CancellationToken cancellationToken = default) where TState : class, new()
    {
        ArgumentException.ThrowIfNullOrEmpty(shardId);
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        var bytes = this.serializer.SerializeToBytes(document);
        return SaveSerializedDocumentAsync<TState>(shardId, documentId, bytes, cancellationToken);
    }

    public async Task SaveSerializedDocumentAsync<TState>(string shardId, string documentId, byte[] serializedDocument, CancellationToken cancellationToken = default) where TState : class, new()
    {
        ArgumentException.ThrowIfNullOrEmpty(shardId);
        ArgumentException.ThrowIfNullOrEmpty(documentId);
        ArgumentNullException.ThrowIfNull(serializedDocument);

        await this.writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var command = this.writeConnection.CreateCommand();
            command.CommandText = "INSERT OR REPLACE INTO Documents (DocumentId, ShardId, Payload) VALUES (@DocId, @ShardId, @Payload)";
            AddParameter(command, "@DocId", documentId);
            AddParameter(command, "@ShardId", shardId);
            AddParameter(command, "@Payload", serializedDocument);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to save document '{DocumentId}' to SQLite", documentId);
            throw;
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    public async Task DeleteDocumentAsync(string shardId, string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(shardId);
        if (string.IsNullOrEmpty(documentId))
        {
            return;
        }

        var rawDocId = ExtractRawDocumentId(documentId);

        await this.writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var transaction = this.writeConnection.BeginTransaction();

            using var cmd1 = this.writeConnection.CreateCommand();
            cmd1.Transaction = transaction;
            cmd1.CommandText = "DELETE FROM Documents WHERE ShardId = @ShardId AND DocumentId = @DocId";
            AddParameter(cmd1, "@ShardId", shardId);
            AddParameter(cmd1, "@DocId", documentId);
            await cmd1.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            using var cmd2 = this.writeConnection.CreateCommand();
            cmd2.Transaction = transaction;
            cmd2.CommandText = "DELETE FROM Journal WHERE ShardId = @ShardId AND (DocumentId = @DocId OR DocumentId = @RawDocId)";
            AddParameter(cmd2, "@ShardId", shardId);
            AddParameter(cmd2, "@DocId", documentId);
            AddParameter(cmd2, "@RawDocId", rawDocId);
            await cmd2.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to delete CRDT document '{DocumentId}' from SQLite", documentId);
            throw;
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    public async Task<ShardVersionVectorMap?> LoadShardVersionVectorMapAsync(string replicaId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(replicaId);

        try
        {
            using var connection = new SqliteConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Payload FROM GlobalDvv WHERE ReplicaId = @RepId";
            AddParameter(command, "@RepId", replicaId);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is byte[] bytes)
            {
                return this.serializer.DeserializeFromBytes<ShardVersionVectorMap>(bytes);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to load global DVV for '{ReplicaId}' from SQLite", replicaId);
            throw;
        }

        return null;
    }

    public async Task SaveShardVersionVectorMapAsync(string replicaId, ShardVersionVectorMap shardVersionVectorMap, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(replicaId);
        ArgumentNullException.ThrowIfNull(shardVersionVectorMap);

        var bytes = this.serializer.SerializeToBytes(shardVersionVectorMap);

        await this.writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var command = this.writeConnection.CreateCommand();
            command.CommandText = "INSERT OR REPLACE INTO GlobalDvv (ReplicaId, Payload) VALUES (@RepId, @Payload)";
            AddParameter(command, "@RepId", replicaId);
            AddParameter(command, "@Payload", bytes);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to save global DVV to SQLite");
            throw;
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    public async Task<ClusterStateSnapshotDto?> LoadClusterStateAsync(string replicaId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(replicaId);

        try
        {
            using var connection = new SqliteConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Payload FROM ClusterState WHERE ReplicaId = @RepId";
            AddParameter(command, "@RepId", replicaId);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is byte[] bytes)
            {
                return this.serializer.DeserializeFromBytes<ClusterStateSnapshotDto>(bytes);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to load cluster state snapshot for '{ReplicaId}' from SQLite", replicaId);
            throw;
        }

        return null;
    }

    public async Task SaveClusterStateAsync(string replicaId, ClusterStateSnapshotDto state, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(replicaId);
        ArgumentNullException.ThrowIfNull(state);

        var bytes = this.serializer.SerializeToBytes(state);

        await this.writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var command = this.writeConnection.CreateCommand();
            command.CommandText = "INSERT OR REPLACE INTO ClusterState (ReplicaId, Payload) VALUES (@RepId, @Payload)";
            AddParameter(command, "@RepId", replicaId);
            AddParameter(command, "@Payload", bytes);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to save cluster state snapshot for '{ReplicaId}' to SQLite", replicaId);
            throw;
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    public void Append(IComparable documentId, IReadOnlyList<CrdtOperation> operationsList)
    {
        var docIdStr = documentId?.ToString();
        ArgumentException.ThrowIfNullOrEmpty(docIdStr, nameof(documentId));
        ArgumentNullException.ThrowIfNull(operationsList);
        if (operationsList.Count == 0)
        {
            return;
        }

        this.writeLock.Wait();
        try
        {
            using var transaction = this.writeConnection.BeginTransaction();

            using var command = this.writeConnection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT OR IGNORE INTO Journal (Id, DocumentId, ShardId, OriginReplicaId, ShardClock, InsertedAt, Payload) VALUES (@Id, @DocId, @ShardId, @OriginRepId, @Clock, @InsertedAt, @Payload)";

            var pId = command.CreateParameter(); pId.ParameterName = "@Id"; command.Parameters.Add(pId);
            var pDoc = command.CreateParameter(); pDoc.ParameterName = "@DocId"; command.Parameters.Add(pDoc);
            var pShard = command.CreateParameter(); pShard.ParameterName = "@ShardId"; command.Parameters.Add(pShard);
            var pRep = command.CreateParameter(); pRep.ParameterName = "@OriginRepId"; command.Parameters.Add(pRep);
            var pClock = command.CreateParameter(); pClock.ParameterName = "@Clock"; command.Parameters.Add(pClock);
            var pInsertedAt = command.CreateParameter(); pInsertedAt.ParameterName = "@InsertedAt"; command.Parameters.Add(pInsertedAt);
            var pPayload = command.CreateParameter(); pPayload.ParameterName = "@Payload"; command.Parameters.Add(pPayload);

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var op in operationsList)
            {
                var journaled = new JournaledOperation(documentId!, op);
                pId.Value = op.Id;
                pDoc.Value = docIdStr;
                pShard.Value = op.ShardId;
                pRep.Value = op.ReplicaId;
                pClock.Value = op.ShardClock;
                pInsertedAt.Value = now;
                pPayload.Value = this.serializer.SerializeToBytes(journaled);

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to append synchronously to SQLite journal");
            throw;
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    public async Task AppendAsync(IComparable documentId, IReadOnlyList<CrdtOperation> operationsList, CancellationToken cancellationToken = default)
    {
        var docIdStr = documentId?.ToString();
        ArgumentException.ThrowIfNullOrEmpty(docIdStr, nameof(documentId));
        ArgumentNullException.ThrowIfNull(operationsList);
        if (operationsList.Count == 0)
        {
            return;
        }

        await this.writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var transaction = this.writeConnection.BeginTransaction();

            using var command = this.writeConnection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT OR IGNORE INTO Journal (Id, DocumentId, ShardId, OriginReplicaId, ShardClock, InsertedAt, Payload) VALUES (@Id, @DocId, @ShardId, @OriginRepId, @Clock, @InsertedAt, @Payload)";

            var pId = command.CreateParameter(); pId.ParameterName = "@Id"; command.Parameters.Add(pId);
            var pDoc = command.CreateParameter(); pDoc.ParameterName = "@DocId"; command.Parameters.Add(pDoc);
            var pShard = command.CreateParameter(); pShard.ParameterName = "@ShardId"; command.Parameters.Add(pShard);
            var pRep = command.CreateParameter(); pRep.ParameterName = "@OriginRepId"; command.Parameters.Add(pRep);
            var pClock = command.CreateParameter(); pClock.ParameterName = "@Clock"; command.Parameters.Add(pClock);
            var pInsertedAt = command.CreateParameter(); pInsertedAt.ParameterName = "@InsertedAt"; command.Parameters.Add(pInsertedAt);
            var pPayload = command.CreateParameter(); pPayload.ParameterName = "@Payload"; command.Parameters.Add(pPayload);

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var op in operationsList)
            {
                var journaled = new JournaledOperation(documentId!, op);
                pId.Value = op.Id;
                pDoc.Value = docIdStr;
                pShard.Value = op.ShardId;
                pRep.Value = op.ReplicaId;
                pClock.Value = op.ShardClock;
                pInsertedAt.Value = now;
                pPayload.Value = this.serializer.SerializeToBytes(journaled);

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to asynchronously append operations to SQLite journal");
            throw;
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    public async Task AppendBatchAsync(IReadOnlyList<JournaledOperation> operations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operations);
        if (operations.Count == 0)
        {
            return;
        }

        await this.writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var transaction = this.writeConnection.BeginTransaction();

            using var command = this.writeConnection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT OR IGNORE INTO Journal (Id, DocumentId, ShardId, OriginReplicaId, ShardClock, InsertedAt, Payload) VALUES (@Id, @DocId, @ShardId, @OriginRepId, @Clock, @InsertedAt, @Payload)";

            var pId = command.CreateParameter(); pId.ParameterName = "@Id"; command.Parameters.Add(pId);
            var pDoc = command.CreateParameter(); pDoc.ParameterName = "@DocId"; command.Parameters.Add(pDoc);
            var pShard = command.CreateParameter(); pShard.ParameterName = "@ShardId"; command.Parameters.Add(pShard);
            var pRep = command.CreateParameter(); pRep.ParameterName = "@OriginRepId"; command.Parameters.Add(pRep);
            var pClock = command.CreateParameter(); pClock.ParameterName = "@Clock"; command.Parameters.Add(pClock);
            var pInsertedAt = command.CreateParameter(); pInsertedAt.ParameterName = "@InsertedAt"; command.Parameters.Add(pInsertedAt);
            var pPayload = command.CreateParameter(); pPayload.ParameterName = "@Payload"; command.Parameters.Add(pPayload);

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            foreach (var journaled in operations)
            {
                var op = journaled.Operation;
                pId.Value = op.Id;
                pDoc.Value = journaled.DocumentId?.ToString() ?? string.Empty;
                pShard.Value = op.ShardId;
                pRep.Value = op.ReplicaId;
                pClock.Value = op.ShardClock;
                pInsertedAt.Value = now;
                pPayload.Value = this.serializer.SerializeToBytes(journaled);

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to asynchronously append operation batch to SQLite journal");
            throw;
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    public async IAsyncEnumerable<JournaledOperation> GetOperationsByRangeAsync(string shardId, string originReplicaId, long minShardClock, long maxShardClock, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(shardId);
        ArgumentException.ThrowIfNullOrEmpty(originReplicaId);

        using var connection = new SqliteConnection(this.connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Payload FROM Journal WHERE ShardId = @ShardId AND OriginReplicaId = @RepId AND ShardClock > @Min AND ShardClock <= @Max ORDER BY ShardClock ASC";
        AddParameter(command, "@ShardId", shardId);
        AddParameter(command, "@RepId", originReplicaId);
        AddParameter(command, "@Min", minShardClock);
        AddParameter(command, "@Max", maxShardClock);

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (reader["Payload"] is byte[] bytes)
            {
                var op = this.serializer.DeserializeFromBytes<JournaledOperation>(bytes);
                yield return op;
            }
        }
    }

    public async IAsyncEnumerable<JournaledOperation> GetOperationsByDotsAsync(string shardId, string originReplicaId, IEnumerable<long> shardClocks, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(shardId);
        ArgumentException.ThrowIfNullOrEmpty(originReplicaId);
        ArgumentNullException.ThrowIfNull(shardClocks);

        var clocksList = shardClocks.ToList();
        if (clocksList.Count == 0)
        {
            yield break;
        }

        using var connection = new SqliteConnection(this.connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        foreach (var chunk in clocksList.Chunk(900))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var inClause = string.Join(",", chunk);

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT Payload FROM Journal WHERE ShardId = @ShardId AND OriginReplicaId = @RepId AND ShardClock IN ({inClause}) ORDER BY ShardClock ASC";
            AddParameter(command, "@ShardId", shardId);
            AddParameter(command, "@RepId", originReplicaId);

            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (reader["Payload"] is byte[] bytes)
                {
                    var op = this.serializer.DeserializeFromBytes<JournaledOperation>(bytes);
                    yield return op;
                }
            }
        }
    }

    public async IAsyncEnumerable<JournaledOperation> GetAllJournaledOperationsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(this.connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Payload FROM Journal ORDER BY ShardId, ShardClock ASC";

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (reader["Payload"] is byte[] bytes)
            {
                var op = this.serializer.DeserializeFromBytes<JournaledOperation>(bytes);
                yield return op;
            }
        }
    }

    public async Task<long> GetJournalCountAsync(string shardId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(shardId);

        try
        {
            using var connection = new SqliteConnection(this.connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Journal WHERE ShardId = @ShardId";
            AddParameter(command, "@ShardId", shardId);

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt64(result);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to get SQLite journal count");
            throw;
        }
    }

    public void Trim(string shardId, IReadOnlyDictionary<string, long> gmvv) => Trim(shardId, gmvv, null);

    public void Trim(string shardId, IReadOnlyDictionary<string, long> gmvv, DateTimeOffset? retainBufferTime)
    {
        ArgumentException.ThrowIfNullOrEmpty(shardId);
        ArgumentNullException.ThrowIfNull(gmvv);
        if (gmvv.Count == 0)
        {
            return;
        }

        this.writeLock.Wait();
        try
        {
            using var transaction = this.writeConnection.BeginTransaction();

            using var command = this.writeConnection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Journal WHERE ShardId = @ShardId AND OriginReplicaId = @OriginRepId AND ShardClock <= @MaxClock";

            if (retainBufferTime.HasValue)
            {
                command.CommandText += " AND InsertedAt < @RetainTime";
                var pTime = command.CreateParameter();
                pTime.ParameterName = "@RetainTime";
                pTime.Value = retainBufferTime.Value.ToUnixTimeMilliseconds();
                command.Parameters.Add(pTime);
            }

            var pShardId = command.CreateParameter(); pShardId.ParameterName = "@ShardId"; command.Parameters.Add(pShardId);
            var pRepId = command.CreateParameter(); pRepId.ParameterName = "@OriginRepId"; command.Parameters.Add(pRepId);
            var pMax = command.CreateParameter(); pMax.ParameterName = "@MaxClock"; command.Parameters.Add(pMax);

            pShardId.Value = shardId;
            foreach (var kvp in gmvv)
            {
                pRepId.Value = kvp.Key;
                pMax.Value = kvp.Value;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to synchronously trim SQLite journal");
            throw;
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    public Task TrimAsync(string shardId, IReadOnlyDictionary<string, long> globalMinimumVersionVector, CancellationToken cancellationToken = default)
        => TrimAsync(shardId, globalMinimumVersionVector, null, cancellationToken);

    public async Task TrimAsync(string shardId, IReadOnlyDictionary<string, long> globalMinimumVersionVector, DateTimeOffset? retainBufferTime, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(shardId);
        ArgumentNullException.ThrowIfNull(globalMinimumVersionVector);
        if (globalMinimumVersionVector.Count == 0)
        {
            return;
        }

        await this.writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var transaction = this.writeConnection.BeginTransaction();

            using var command = this.writeConnection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Journal WHERE ShardId = @ShardId AND OriginReplicaId = @OriginRepId AND ShardClock <= @MaxClock";

            if (retainBufferTime.HasValue)
            {
                command.CommandText += " AND InsertedAt < @RetainTime";
                var pTime = command.CreateParameter();
                pTime.ParameterName = "@RetainTime";
                pTime.Value = retainBufferTime.Value.ToUnixTimeMilliseconds();
                command.Parameters.Add(pTime);
            }

            var pShardId = command.CreateParameter(); pShardId.ParameterName = "@ShardId"; command.Parameters.Add(pShardId);
            var pRepId = command.CreateParameter(); pRepId.ParameterName = "@OriginRepId"; command.Parameters.Add(pRepId);
            var pMax = command.CreateParameter(); pMax.ParameterName = "@MaxClock"; command.Parameters.Add(pMax);

            pShardId.Value = shardId;
            foreach (var kvp in globalMinimumVersionVector)
            {
                pRepId.Value = kvp.Key;
                pMax.Value = kvp.Value;
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to asynchronously trim SQLite journal");
            throw;
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.writeConnection.Dispose();
        this.writeLock.Dispose();
    }

    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SqliteConnection(this.connectionString);
            connection.Open();

            using var initCommand = connection.CreateCommand();
            initCommand.CommandText = "PRAGMA journal_mode = 'wal'; PRAGMA synchronous = OFF; PRAGMA temp_store = MEMORY; PRAGMA cache_size = -8000; PRAGMA mmap_size = 0; PRAGMA journal_size_limit = 67108864;";
            initCommand.ExecuteNonQuery();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Documents (
                    DocumentId TEXT PRIMARY KEY,
                    ShardId TEXT NOT NULL DEFAULT '',
                    Payload BLOB NOT NULL
                );
                CREATE TABLE IF NOT EXISTS GlobalDvv (
                    ReplicaId TEXT PRIMARY KEY,
                    Payload BLOB NOT NULL
                );
                CREATE TABLE IF NOT EXISTS ClusterState (
                    ReplicaId TEXT PRIMARY KEY,
                    Payload BLOB NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Journal (
                    Id TEXT PRIMARY KEY,
                    DocumentId TEXT NOT NULL,
                    ShardId TEXT NOT NULL DEFAULT '',
                    OriginReplicaId TEXT NOT NULL DEFAULT '',
                    ShardClock INTEGER NOT NULL DEFAULT 0,
                    InsertedAt INTEGER NOT NULL DEFAULT 0,
                    Payload BLOB NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IDX_Journal_Shard_Clock ON Journal(ShardId, ShardClock);
            ";
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            this.logger.LogCritical(ex, "Failed to initialize SQLite CRDT storage schema");
            throw;
        }
    }

    private static void AddParameter(SqliteCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string ExtractRawDocumentId(string documentId)
    {
        var colonIndex = documentId.IndexOf(':', StringComparison.Ordinal);
        return colonIndex >= 0 && colonIndex < documentId.Length - 1
            ? documentId[(colonIndex + 1)..]
            : documentId;
    }
}