

using Microsoft.EntityFrameworkCore;

namespace BankingApp.Domain.Models
{
    public class Transaction : BaseModel
    {
        public long AccountId { get; set; }
        [Precision(18, 2)]
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } // Deposit, Withdrawal, Transfer
        public string Description { get; set; }
        [Precision(18, 2)]
        public decimal BalanceAfterTransaction { get; set; }
    }

}
