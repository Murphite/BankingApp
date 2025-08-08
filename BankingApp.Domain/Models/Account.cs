
namespace BankingApp.Domain.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public string OwnerId { get; set; } // foreign key to User
        public List<Transaction> Transactions { get; set; }
    }
}
