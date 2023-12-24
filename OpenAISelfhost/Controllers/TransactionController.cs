using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAISelfhost.DataContracts.DataTables;
using OpenAISelfhost.DataContracts.Enums;
using OpenAISelfhost.DataContracts.Response.Common;
using OpenAISelfhost.Service.Billing;

namespace OpenAISelfhost.Controllers
{
    [Route("transaction")]
    [ApiController]
    public class TransactionController : ApiControllerBase
    {
        private readonly ITransactionService transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            this.transactionService = transactionService;
        }

        [HttpGet("list")]
        [Authorize]
        public ApiResponse<IEnumerable<Transaction>> ListTransactions()
        {
            return new()
            {
                Data = transactionService.GetTransactionsForUser(GetUserId())
            };
        }

        [HttpGet("all")]
        [Authorize(Roles = UserType.Admin)]
        public ApiResponse<IEnumerable<Transaction>> ListAllTransactions()
        {
            return new()
            {
                Data = transactionService.GetTransactions()
            };
        }
    }
}
