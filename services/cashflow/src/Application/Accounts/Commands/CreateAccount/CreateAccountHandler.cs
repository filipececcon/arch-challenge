using ArchChallenge.CashFlow.Application.Common.Handlers;
using ArchChallenge.CashFlow.Application.Common.Outbox;
using ArchChallenge.CashFlow.Application.Common.Responses;

namespace ArchChallenge.CashFlow.Application.Accounts.Commands.CreateAccount;

public sealed class CreateAccountHandler(
    IUnitOfWork unitOfWork,
    IOutboxRepository outboxRepository,
    IOutboxMapper<CreateAccountCommand, Account, Account> outboxMapper,
    IStringLocalizer<Messages> localizer,
    IReadRepository<Account> readRepository,
    IWriteRepository<Account> writeRepository)
    : SyncCommandHandler<Account, Account, CreateAccountCommand, Result<CreateAccountResult>>(
        unitOfWork, outboxRepository, outboxMapper, localizer)
{
    
    protected override async Task<Account?> ExecuteAsync(
        CreateAccountCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new AccountByUserIdSpec(command.UserId);
        
        var existing = await readRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (existing is not null)
        {
            AddError(localizer[MessageKeys.Validation.AccountAlreadyExists].Value);
            
            return null;
        }

        var account = new Account(command.UserId);
        
        await writeRepository.AddAsync(account, cancellationToken);
        
        return account;
    }

    protected override Result<CreateAccountResult> BuildSuccessResult
        (CreateAccountCommand command, Account entity, Account projection) =>
        Result<CreateAccountResult>.Ok(
            new CreateAccountResult(
                entity.Id,
                entity.UserId,
                entity.Balance,
                entity.CreatedAt),
            201,
            "account_created");

    protected override Result<CreateAccountResult> BuildFailureResult(CreateAccountCommand command, ResultBuilder errors)
    {
        var notFound = localizer[MessageKeys.Validation.EntityNotFound].Value;
        
        var status = IsSingleNotFoundMessage(errors, notFound) ? 404 : 409;
        
        return errors.BuildFailure<CreateAccountResult>(status, outboxMapper.EventName);
    }

    private static bool IsSingleNotFoundMessage(ResultBuilder errors, string notFoundText) =>
        errors.Messages.Count is 1 && string.Equals(errors.Messages[0], notFoundText, StringComparison.Ordinal);
}
