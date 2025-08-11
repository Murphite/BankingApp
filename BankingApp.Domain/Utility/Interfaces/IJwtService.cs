
using BankingApp.Domain.Models;

namespace BankingApp.Domain.Utility.Interfaces
{
    public interface IJwtService
    {
        public string GenerateToken(ApplicationUser user, IList<string> roles);
    }

}
