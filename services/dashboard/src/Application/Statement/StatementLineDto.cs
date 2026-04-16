namespace ArchChallenge.Dashboard.Application.Statement;

public record StatementLineDto(
    Guid Id,
    DateOnly Date,
    DateTime OccurredAt,
    string Type,
    decimal Amount);
