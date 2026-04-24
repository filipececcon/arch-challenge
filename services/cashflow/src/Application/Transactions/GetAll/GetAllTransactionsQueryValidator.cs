using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.GetAll;

public sealed class GetAllTransactionsQueryValidator : AbstractValidator<GetAllTransactionsQuery>
{
    public GetAllTransactionsQueryValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(q => q.UserId).NotEmpty();

        When(q => !string.IsNullOrWhiteSpace(q.Type), () =>
        {
            RuleFor(q => q.Type!)
                .Must(t => Enum.TryParse<TransactionType>(t, ignoreCase: true, out _))
                .WithMessage(_ => localizer[MessageKeys.Validation.TransactionTypeInvalid]);
        });

        When(q => q.MinAmount.HasValue && q.MaxAmount.HasValue, () =>
        {
            RuleFor(q => q.MaxAmount!.Value)
                .GreaterThanOrEqualTo(q => q.MinAmount!.Value)
                .WithMessage(_ => localizer[MessageKeys.Validation.GetAllAmountRange]);
        });

        When(q => q.CreatedFrom.HasValue && q.CreatedTo.HasValue, () =>
        {
            RuleFor(q => q.CreatedTo!.Value)
                .GreaterThanOrEqualTo(q => q.CreatedFrom!.Value)
                .WithMessage(_ => localizer[MessageKeys.Validation.GetAllCreatedAtRange]);
        });
    }
}
