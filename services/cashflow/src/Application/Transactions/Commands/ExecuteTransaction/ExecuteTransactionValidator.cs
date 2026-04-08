using ArchChallenge.CashFlow.Application.Transactions.Commands.CreateTransaction;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.I18n;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

public class ExecuteTransactionValidator(IStringLocalizer<Messages> localizer) : CreateTransactionValidator(localizer);