using BankingApp.Domain.Models;
using BankingApp.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BankingApp.API.Extensions
{
    public static class DbRegistration
    {
        public static void AddDbServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<BankingDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                 sqlOptions => sqlOptions.MigrationsAssembly(typeof(BankingDbContext).Assembly.GetName().Name)));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<BankingDbContext>().AddDefaultTokenProviders();
        }
    }

    public class BankingDbContextFactory : IDesignTimeDbContextFactory<BankingDbContext>
    {
        public BankingDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BankingDbContext>();

            optionsBuilder.UseSqlServer("Server=DESKTOP-F2KEAIR\\MSSQLSERVER02;Initial Catalog=bankingappdb;Persist Security Info=False;User ID=sa;Password=devuser;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=False;Connection Timeout=60;");

            return new BankingDbContext(optionsBuilder.Options);
        }
    }
}
