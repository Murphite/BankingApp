

using Microsoft.AspNetCore.Http;

namespace BankingApp.Infrastructure.Services
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        string FullName { get; }
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid UserId => Guid.Parse(
            _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value ?? Guid.Empty.ToString()
        );

        public string FullName =>
            _httpContextAccessor.HttpContext?.User?.FindFirst("FullName")?.Value ?? "System";
    }

}
