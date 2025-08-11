
using BankingApp.Domain.Utility.Interfaces;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankingApp.Application.Features.Account.Command.Deposit
{
    public class DepositCommandHandler : IRequestHandler<DepositCommand, bool>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<DepositCommandHandler> _logger;
        private readonly IPaymentGateway _paymentGateway;

        public DepositCommandHandler(IUnitOfWork uow, ILogger<DepositCommandHandler> logger, IPaymentGateway paymentGateway)
        {
            _uow = uow;
            _logger = logger;
            _paymentGateway = paymentGateway;
        }

        public async Task<bool> Handle(DepositCommand request, CancellationToken cancellationToken)
        {
            var paymentResult = await _paymentGateway.InitiateDepositAsync(request.AccountId, request.Amount);

            if (!paymentResult.Success)
            {
                _logger.LogWarning("Deposit failed via payment gateway: {Message}", paymentResult.Message);
                return false;
            }

            _logger.LogInformation("Payment gateway deposit reference: {Ref}", paymentResult.Reference);

            var account = _uow.GetRepository<BankingApp.Domain.Models.Account>()
                .Get()
                .FirstOrDefault(a => a.AccountNumber == request.AccountId && !a.IsDeleted);

            if (account == null) return false;

            account.Balance += request.Amount;

            _uow.GetRepository<BankingApp.Domain.Models.Transaction>().Insert(new BankingApp.Domain.Models.Transaction
            {
                AccountId = account.Id,
                Amount = request.Amount,
                TransactionType = "Deposit",
                TransactionDate = DateTime.UtcNow,
                BalanceAfterTransaction = account.Balance,
                IsDeleted = false
            });

            return await _uow.SaveAsync(cancellationToken) > 0;
        }
    }

}
