

using BankingApp.Domain.Utility.Interfaces;
using Microsoft.Extensions.Logging;

namespace BankingApp.Domain.Utility
{
    public class MockPaystackService : IPaymentGateway
    {
        private readonly ILogger<MockPaystackService> _logger;

        public MockPaystackService(ILogger<MockPaystackService> logger)
        {
            _logger = logger;
        }

        public async Task<PaymentResult> InitiateDepositAsync(long accountNumber, decimal amount)
        {
            _logger.LogInformation("Mock deposit initiated: {Amount} to {Account}", amount, accountNumber);
            await Task.Delay(500); // Simulate network latency

            return new PaymentResult
            {
                Success = true,
                Reference = Guid.NewGuid().ToString(),
                Message = "Deposit simulated successfully"
            };
        }

        public async Task<PaymentResult> InitiateWithdrawalAsync(long accountNumber, decimal amount)
        {
            _logger.LogInformation("Mock withdrawal initiated: {Amount} from {Account}", amount, accountNumber);
            await Task.Delay(500);

            return new PaymentResult
            {
                Success = true,
                Reference = Guid.NewGuid().ToString(),
                Message = "Withdrawal simulated successfully"
            };
        }

        public async Task<PaymentResult> InitiateTransferAsync(long fromAccountNumber, long toAccountNumber, decimal amount)
        {
            _logger.LogInformation("Mock transfer initiated: {Amount} from {From} to {To}", amount, fromAccountNumber, toAccountNumber);
            await Task.Delay(500);

            return new PaymentResult
            {
                Success = true,
                Reference = Guid.NewGuid().ToString(),
                Message = "Transfer simulated successfully"
            };
        }
    }

}
