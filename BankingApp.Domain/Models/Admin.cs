

namespace BankingApp.Domain.Models
{
    public class Admin : BaseModel
    {
        public string Department { get; set; }
        public string ApplicationUser { get; set; }
        public string UserId { get; set; } = default!;
        public ApplicationUser? User { get; set; }
    }
}
