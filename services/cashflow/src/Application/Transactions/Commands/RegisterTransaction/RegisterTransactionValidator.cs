using ArchChallenge.CashFlow.Infrastructure.CrossCutting.I18n;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;

public class RegisterTransactionValidator : AbstractValidator<RegisterTransactionCommand>
{
    public RegisterTransactionValidator(IStringLocalizer<Messages> localizer)
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
