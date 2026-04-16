namespace ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;

public class EnqueueTransactionValidator : AbstractValidator<EnqueueTransaction>
{
    public EnqueueTransactionValidator(IStringLocalizer<Messages> localizer)
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage(_ => localizer[MessageKeys.Validation.TransactionTypeInvalid]);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage(_ => localizer[MessageKeys.Validation.AmountGreaterThanZero]);

        RuleFor(x => x.Description)
            .MaximumLength(255)
            .WithMessage(_ => localizer[MessageKeys.Validation.DescriptionMaxLength])
            .When(x => x.Description is not null);
    }
}
