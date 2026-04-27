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
        public static class Transaction
        {
            public const string NotFound                = "Validation_Transaction_NotFound";
            public const string TypeInvalid             = "Validation_Transaction_Type_Invalid";
            public const string AmountGreaterThanZero   = "Validation_Transaction_Amount_GreaterThanZero";
            public const string DescriptionMaxLength    = "Validation_Transaction_Description_MaxLength";
            public const string AccountDeactivated      = "Validation_Transaction_Account_Deactivated";
        
            /// <summary>Filtro de lista: <c>minAmount</c> não pode ser maior que <c>maxAmount</c>.</summary>
            public const string GetAllAmountRange = "Validation_Transaction_GetAll_AmountRange";

            /// <summary>Filtro de lista: <c>createdFrom</c> não pode ser posterior a <c>createdTo</c>.</summary>
            public const string GetAllCreatedAtRange = "Validation_Transaction_GetAll_CreatedAtRange";
        }

        public static class Account
        {
            public const string NotFound         = "Validation_Account_NotFound";
            public const string AlreadyExists    = "Validation_Account_AlreadyExists";
        }
    }
}

