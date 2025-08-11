
using BankingApp.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BankingApp.Infrastructure.Persistence.Context
{
    public class BankingDbContext : IdentityDbContext<ApplicationUser>
    {
        public BankingDbContext(DbContextOptions<BankingDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUser { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Example: enforce AccountNumber uniqueness
            modelBuilder.Entity<Account>()
                .HasIndex(a => a.AccountNumber)
                .IsUnique();

            // Relationships
            modelBuilder.Entity<Account>()
                .HasMany(a => a.Transactions)
                .WithOne()
                .HasForeignKey("AccountId");

            modelBuilder.Entity<ApplicationUser>()
                 .HasOne(u => u.CustomerDetails)
                 .WithOne(c => c.ApplicationUser)
                 .HasForeignKey<Customer>(c => c.ApplicationUserId)
                 .OnDelete(DeleteBehavior.Cascade);

            // one-to-one ApplicationUser -> Admin
            //modelBuilder.Entity<ApplicationUser>()
            //    .HasOne(u => u.AdminDetails)
            //    .WithOne(a => a.ApplicationUser)
            //    .HasForeignKey<Admin>(a => a.ApplicationUserId)
            //    .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
