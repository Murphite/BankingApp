
using BankingApp.Domain.BindingModels.Responses;
using BankingApp.Domain.Constants;
using BankingApp.Domain.Utility.Interfaces;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankingApp.Application.Features.Account.Command.Transfer
{
    public class TransferCommandHandler : IRequestHandler<TransferCommand, ServiceResponse>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<TransferCommandHandler> _logger;
        private readonly IPaymentGateway _paymentGateway;

        public TransferCommandHandler(IUnitOfWork uow, ILogger<TransferCommandHandler> logger, IPaymentGateway paymentGateway)
        {
            _uow = uow;
            _logger = logger;
            _paymentGateway = paymentGateway;
        }

        public async Task<ServiceResponse> Handle(TransferCommand request, CancellationToken cancellationToken)
        {
            var response = new ServiceResponse();

            // Payment gateway mock call
            var paymentResult = await _paymentGateway.InitiateTransferAsync(request.FromAccountId, request.ToAccountId, request.Amount);
            if (!paymentResult.Success)
            {
                _logger.LogWarning("Transfer failed via payment gateway: {Message}", paymentResult.Message);
                response.StatusCode = ResponseCode.BadRequest;
                response.StatusMessage = paymentResult.Message;
                return response;
            }

            _logger.LogInformation("Payment gateway transfer reference: {Ref}", paymentResult.Reference);

            using var transaction = _uow.BeginTransaction();

            try
            {
                var validator = new TransferCommandValidator(_uow);
                var result = validator.Validate(request);
                if (!result.IsValid)
                {
                    _logger.LogWarning("Validation failed for transfer: {Errors}", result.Errors);
                    throw new ValidationException(result.Errors);
                }

                var fromAccount = _uow.GetRepository<BankingApp.Domain.Models.Account>().Get()
                    .First(a => a.Id == request.FromAccountId && !a.IsDeleted);
                var toAccount = _uow.GetRepository<BankingApp.Domain.Models.Account>().Get()
                    .First(a => a.Id == request.ToAccountId && !a.IsDeleted);

                fromAccount.Balance -= request.Amount;
                toAccount.Balance += request.Amount;

                _uow.GetRepository<BankingApp.Domain.Models.Transaction>().Insert(new BankingApp.Domain.Models.Transaction
                {
                    AccountId = fromAccount.Id,
                    Amount = request.Amount,
                    TransactionDate = DateTime.UtcNow,
                    TransactionType = "Transfer Out",
                    Description = request.Description,
                    BalanceAfterTransaction = fromAccount.Balance,
                    IsDeleted = false
                });

                _uow.GetRepository<BankingApp.Domain.Models.Transaction>().Insert(new BankingApp.Domain.Models.Transaction
                {
                    AccountId = toAccount.Id,
                    Amount = request.Amount,
                    TransactionDate = DateTime.UtcNow,
                    TransactionType = "Transfer In",
                    Description = request.Description,
                    BalanceAfterTransaction = toAccount.Balance,
                    IsDeleted = false
                });

                await _uow.SaveAsync(cancellationToken);
                transaction.Commit();

                response.StatusCode = ResponseCode.SUCCESSFUL;
                response.StatusMessage = "Transfer completed successfully.";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error occurred during transfer from {FromAccountId} to {ToAccountId}",
                    request.FromAccountId, request.ToAccountId);
                response.StatusMessage = ex.Message;
                response.StatusCode = ResponseCode.BadRequest;
            }

            return response;
        }
    }

}
