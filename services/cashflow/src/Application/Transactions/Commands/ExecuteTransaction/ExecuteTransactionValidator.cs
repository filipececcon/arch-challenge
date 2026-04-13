using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.I18n;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

public class ExecuteTransactionValidator(IStringLocalizer<Messages> localizer) : EnqueueTransactionValidator(localizer);