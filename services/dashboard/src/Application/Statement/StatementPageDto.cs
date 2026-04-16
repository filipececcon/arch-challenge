namespace ArchChallenge.Dashboard.Application.Statement;

public record StatementPageDto(
    IReadOnlyList<StatementLineDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
