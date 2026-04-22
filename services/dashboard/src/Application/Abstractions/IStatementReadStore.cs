using ArchChallenge.Dashboard.Application.Statement;

namespace ArchChallenge.Dashboard.Application.Abstractions;

public interface IStatementReadStore
{
    Task<StatementPageDto> ListAsync(
        string userId,
        DateOnly? from,
        DateOnly? to,
        string? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
