using System.Collections.Concurrent;
using ImmuDB;
using ImmuDB.SQL;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Immutable.Writers;

public sealed class AuditWriter(IOptions<ImmuDbOptions> options, ILogger<AuditWriter> logger)
    : IAuditWriter, IAsyncDisposable
{
    private readonly ImmuDbOptions _opt   = options.Value;
    private readonly SemaphoreSlim _init  = new(1, 1);
    private readonly SemaphoreSlim _write = new(1, 1);

    /// <summary>Cache de tabelas já criadas nesta instância (singleton) para evitar DDL repetido.</summary>
    private readonly ConcurrentDictionary<string, bool> _tableEnsured = new(StringComparer.OrdinalIgnoreCase);

    private ImmuClient? _client;
    private bool        _opened;

    public async Task WriteAuditEntryAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        await _write.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await EnsureSessionAsync(cancellationToken).ConfigureAwait(false);

            var tableName = AuditTableConventions.TableName(entry.AggregateType);

            await EnsureTableExistsAsync(tableName).ConfigureAwait(false);

            await InsertAuditRowAsync(tableName, entry).ConfigureAwait(false);
        }
        finally
        {
            _write.Release();
        }
    }

    private async Task EnsureTableExistsAsync(string tableName)
    {
        if (_tableEnsured.ContainsKey(tableName)) return;

        var ddl = $"""
            CREATE TABLE IF NOT EXISTS {tableName} (
                ID              VARCHAR[36]    NOT NULL,
                ID_USER         VARCHAR[36]    NOT NULL,
                ID_AGGREGATE    VARCHAR[36]    NOT NULL,
                NM_EVENT        VARCHAR[256]   NOT NULL,
                DT_OCCURRED_AT  TIMESTAMP      NOT NULL,
                DS_PAYLOAD      VARCHAR[65535] NOT NULL,
                PRIMARY KEY (ID)
            )
            """;

        await _client!.SQLExec(ddl).ConfigureAwait(false);

        _tableEnsured[tableName] = true;

        logger.LogInformation("Table {Table} ensured in immudb.", tableName);
    }

    private Task InsertAuditRowAsync(string tableName, AuditEntry entry)
    {
        var sql = $"""
            INSERT INTO {tableName} (ID, ID_USER, ID_AGGREGATE, NM_EVENT, DT_OCCURRED_AT, DS_PAYLOAD)
            VALUES (@id, @id_user, @id_aggregate, @nm_event, @dt_occurred_at, @ds_payload)
            """;

        return _client!.SQLExec(sql,
            new SQLParameter(entry.AuditId,     "id"),
            new SQLParameter(entry.UserId,      "id_user"),
            new SQLParameter(entry.AggregateId, "id_aggregate"),
            new SQLParameter(entry.EventName,   "nm_event"),
            new SQLParameter(entry.OccurredAt,  "dt_occurred_at"),
            new SQLParameter(entry.Payload,     "ds_payload"));
    }

    private async Task EnsureSessionAsync(CancellationToken cancellationToken)
    {
        if (_opened && _client is not null) return;

        await _init.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_opened && _client is not null) return;

            _client = ImmuClient.NewBuilder()
                .WithServerUrl(_opt.Host)
                .WithServerPort(_opt.Port)
                .CheckDeploymentInfo(_opt.CheckDeploymentInfo)
                .Build();

            await _client.Open(_opt.Username, _opt.Password, _opt.Database).ConfigureAwait(false);

            _opened = true;

            logger.LogInformation("ImmuDB session opened ({Host}:{Port}, db={Database}).",
                _opt.Host, _opt.Port, _opt.Database);
        }
        finally
        {
            _init.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is null) return;

        try
        {
            await _client.Close().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "ImmuDB Close ignored.");
        }

        try
        {
            await ImmuClient.ReleaseSdkResources().ConfigureAwait(false);
        }
        catch
        {
             /* ignore */
        }
    }
}
