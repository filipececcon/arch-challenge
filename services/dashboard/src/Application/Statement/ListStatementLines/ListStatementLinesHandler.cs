using ArchChallenge.Dashboard.Application.Abstractions;

namespace ArchChallenge.Dashboard.Application.Statement.ListStatementLines;

public class ListStatementLinesHandler(IStatementReadStore readStore)
    : IRequestHandler<ListStatementLinesQuery, StatementPageDto>
{
    public Task<StatementPageDto> Handle(ListStatementLinesQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var page     = Math.Max(1, request.Page);

        return readStore.ListAsync(request.UserId, request.From, request.To, request.Type, page, pageSize, cancellationToken);
    }
}
