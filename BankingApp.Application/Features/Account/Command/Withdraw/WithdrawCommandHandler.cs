
using BankingApp.Domain.Utility.Interfaces;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankingApp.Application.Features.Account.Command.Withdraw
{
    public class WithdrawCommandHandler : IRequestHandler<WithdrawCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<WithdrawCommandHandler> _logger;
        private readonly IPaymentGateway _paymentGateway;

        public WithdrawCommandHandler(IUnitOfWork uow, ILogger<WithdrawCommandHandler> logger, IPaymentGateway paymentGateway)
        {
            _uow = uow;
            _logger = logger;
            _paymentGateway = paymentGateway;
        }

        public async Task<bool> Handle(WithdrawCommand request, CancellationToken cancellationToken)
        {
            // Payment gateway mock call
            var paymentResult = await _paymentGateway.InitiateWithdrawalAsync(request.AccountId, request.Amount);
            if (!paymentResult.Success)
            {
                _logger.LogWarning("Withdrawal failed via payment gateway: {Message}", paymentResult.Message);
                return false;
            }

            _logger.LogInformation("Payment gateway withdrawal reference: {Ref}", paymentResult.Reference);

            var account = _uow.GetRepository<BankingApp.Domain.Models.Account>()
                .Get()
                .FirstOrDefault(a => a.AccountNumber == request.AccountId && !a.IsDeleted);

            if (account == null) return false;
            if (request.Amount <= 0) throw new ArgumentException("Amount must be positive");
            if (account.Balance < request.Amount) throw new InvalidOperationException("Insufficient funds");

            account.Balance -= request.Amount;

            _uow.GetRepository<BankingApp.Domain.Models.Transaction>().Insert(new BankingApp.Domain.Models.Transaction
            {
                AccountId = account.Id,
                Amount = request.Amount,
                TransactionType = "Withdraw",
                TransactionDate = DateTime.UtcNow,
                BalanceAfterTransaction = account.Balance,
                IsDeleted = false
            });

            return await _uow.SaveAsync(cancellationToken) > 0;
        }
    }

}
