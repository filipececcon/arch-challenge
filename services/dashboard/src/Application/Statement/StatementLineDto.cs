namespace ArchChallenge.Dashboard.Application.Statement;

public record StatementLineDto(
    Guid Id,
    Guid AccountId,
    DateOnly Date,
    DateTime OccurredAt,
    string Type,
    decimal Amount);
