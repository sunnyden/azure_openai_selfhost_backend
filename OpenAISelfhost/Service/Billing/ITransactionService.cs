using OpenAISelfhost.DataContracts.DataTables;

namespace OpenAISelfhost.Service.Billing
{
    public interface ITransactionService
    {
        public void RecordTransaction(int userId, string transactionId, int promptToken, int responseToken, int totalToken, string model, double cost);
        public IEnumerable<Transaction> GetTransactions();
        public IEnumerable<Transaction> GetTransactionsForUser(int userId);
    }
}
