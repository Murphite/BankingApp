
using Microsoft.EntityFrameworkCore;

namespace BankingApp.Domain.Models
{
    public class Account : BaseModel
    {
        public long AccountNumber { get; set; }
        public string AccountHolderName { get; set; }
        [Precision(18, 2)]
        public decimal Balance { get; set; }
        public string OwnerId { get; set; } // foreign key to User
        public List<Transaction> Transactions { get; set; }
    }
}
