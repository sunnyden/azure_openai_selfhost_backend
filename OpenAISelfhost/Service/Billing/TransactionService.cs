using OpenAISelfhost.DatabaseContext;
using OpenAISelfhost.DataContracts.DataTables;

namespace OpenAISelfhost.Service.Billing
{
    public class TransactionService : ITransactionService
    {
        private readonly ServiceDatabaseContext databaseContext;
        public TransactionService(ServiceDatabaseContext databaseContext)
        {
            this.databaseContext = databaseContext;
        }

        public IEnumerable<Transaction> GetTransactions()
        {
            return databaseContext.Transactions;
        }

        public IEnumerable<Transaction> GetTransactionsForUser(int userId)
        {
            return databaseContext.Transactions.Where(t => t.UserId == userId);
        }

        public void RecordTransaction(int userId, string transactionId, int promptToken, int responseToken, int totalToken, string model, double cost)
        {
            var transaction = new Transaction()
            {
                UserId = userId,
                TransactionId = transactionId,
                RequestedService = model,
                PromptTokens = promptToken,
                ResponseTokens = responseToken,
                TotalTokens = totalToken,
                Time = DateTime.Now,
                Cost = cost
            };
            databaseContext.Add(transaction);
            databaseContext.SaveChanges();
        }
    }
}
