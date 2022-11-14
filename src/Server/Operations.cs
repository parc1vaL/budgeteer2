namespace Budgeteer.Server;

public static class Operations
{
    public static class Accounts
    {
        public const string GetList = "GetAccounts";
        public const string GetDetails = "GetAccount";
        public const string Create = "CreateAccount";
        public const string Update = "UpdateAccount";
        public const string Delete = "DeleteAccount";
    }

    public static class Transactions
    {
        public const string GetList = "GetTransactions";
        public const string GetDetails = "GetTransaction";
        public const string Create = "CreateTransaction";
        public const string Update = "UpdateTransaction";
        public const string Delete = "DeleteTransaction";
    }
}