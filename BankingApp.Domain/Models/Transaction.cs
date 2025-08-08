

namespace BankingApp.Domain.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; } // Deposit, Withdraw, Transfer
        public string Description { get; set; }
    }

}
