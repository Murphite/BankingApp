

namespace BankingApp.Domain.Models
{
    public class Customer : BaseModel
    {
        public string Address { get; set; }
        public ICollection<Account> BankAccounts { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }

        // Foreign key to ApplicationUser
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
    }

}
