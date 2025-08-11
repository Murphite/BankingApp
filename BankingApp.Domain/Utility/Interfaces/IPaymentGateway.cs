
namespace BankingApp.Domain.Utility.Interfaces
{
    public interface IPaymentGateway
    {
        Task<PaymentResult> InitiateDepositAsync(long accountNumber, decimal amount);
        Task<PaymentResult> InitiateWithdrawalAsync(long accountNumber, decimal amount);
        Task<PaymentResult> InitiateTransferAsync(long fromAccountNumber, long toAccountNumber, decimal amount);
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Reference { get; set; }
        public string Message { get; set; }
    }

}
