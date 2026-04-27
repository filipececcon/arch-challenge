namespace ArchChallenge.CashFlow.Application.Transactions.Enqueue;

public class EnqueueTransactionValidator : AbstractValidator<EnqueueTransactionCommand>
{
    public EnqueueTransactionValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage(_ => localizer[MessageKeys.Validation.Transaction.TypeInvalid]);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage(_ => localizer[MessageKeys.Validation.Transaction.AmountGreaterThanZero]);

        RuleFor(x => x.Description)
            .MaximumLength(255)
            .WithMessage(_ => localizer[MessageKeys.Validation.Transaction.DescriptionMaxLength])
            .When(x => x.Description is not null);
    }
}
