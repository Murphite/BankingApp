

using Microsoft.AspNetCore.Identity;

namespace BankingApp.Domain.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public new string PhoneNumber { get; set; }
        public string UserType { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties 
        public Admin? AdminDetails { get; set; }
        public Customer? CustomerDetails { get; set; }
    }
}
