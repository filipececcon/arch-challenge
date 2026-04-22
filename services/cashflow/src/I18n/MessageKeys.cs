namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.I18n;

/// <summary>Strongly-typed keys for all localized messages.</summary>
public static class MessageKeys
{
    public static class Exception
    {
        public const string InternalError   = "Exception_InternalError";
        public const string ValidationError = "Exception_ValidationError";
        public const string DomainError     = "Exception_DomainError";
    }

    public static class Validation
    {
        public const string EntityNotFound  = "Validation_Entity_NotFound";
        
        public const string TransactionTypeInvalid  = "Validation_TransactionType_Invalid";
        public const string AmountGreaterThanZero   = "Validation_Amount_GreaterThanZero";
        public const string DescriptionMaxLength    = "Validation_Description_MaxLength";
        public const string TransactionNotFound     = "Validation_Transaction_NotFound";
        public const string AccountAlreadyExists    = "Validation_Account_AlreadyExists";
        public const string AccountNotFound         = "Validation_Account_NotFound";

        /// <summary>Filtro de lista: <c>minAmount</c> não pode ser maior que <c>maxAmount</c>.</summary>
        public const string GetAllAmountRange = "Validation_GetAll_AmountRange";

        /// <summary>Filtro de lista: <c>createdFrom</c> não pode ser posterior a <c>createdTo</c>.</summary>
        public const string GetAllCreatedAtRange = "Validation_GetAll_CreatedAtRange";
    }
}

