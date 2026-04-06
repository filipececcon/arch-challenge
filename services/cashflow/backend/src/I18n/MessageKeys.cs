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
        public const string TransactionTypeInvalid              = "Validation_TransactionType_Invalid";
        public const string AmountGreaterThanZero               = "Validation_Amount_GreaterThanZero";
        public const string DescriptionMaxLength                = "Validation_Description_MaxLength";
        public const string TransactionNotFound = "Validation_Transaction_NotFound";
    }
}

