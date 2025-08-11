

using BankingApp.Application.Exceptions;
using BankingApp.Domain.BindingModels.Responses;
using BankingApp.Domain.Constants;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankingApp.Application.Features.Account.Command.Create
{
    public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, ServiceResponse>
    {
        private readonly ILogger<CreateAccountHandler> _logger;
        private readonly IUnitOfWork _uow;

        public CreateAccountHandler(ILogger<CreateAccountHandler> logger, IUnitOfWork uow)
        {
            _logger = logger;
            _uow = uow;
        }

        public async Task<ServiceResponse> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            using var transaction = _uow.BeginTransaction();
            var response = new ServiceResponse();

            _logger.LogInformation("Starting account creation for {AccountHolderName} with initial deposit {InitialDeposit}",
                request.AccountHolderName, request.InitialDeposit);

            try
            {
                var validator = new CreateAccountCommandValidator(_uow);
                var result = validator.Validate(request);
                if (!result.IsValid)
                {
                    _logger.LogWarning("Validation failed for account creation: {Errors}", result.Errors);
                    throw new FluentValidation.ValidationException(result.Errors);
                }

                var newAccount = new BankingApp.Domain.Models.Account
                {
                    AccountNumber = GenerateAccountNumber(),
                    AccountHolderName = request.AccountHolderName,
                    Balance = request.InitialDeposit,
                    CreatedDate = DateTime.UtcNow,
                    IsDeleted = false
                };

                _uow.GetRepository<BankingApp.Domain.Models.Account>().Insert(newAccount);
                _uow.Save();

                if (request.InitialDeposit > 0)
                {
                    _logger.LogInformation("Recording initial deposit transaction for account {AccountNumber}", newAccount.AccountNumber);
                    var depositTransaction = new BankingApp.Domain.Models.Transaction
                    {
                        AccountId = newAccount.Id,
                        Amount = request.InitialDeposit,
                        TransactionDate = DateTime.UtcNow,
                        TransactionType = "Deposit",
                        Description = "Initial deposit on account creation",
                        BalanceAfterTransaction = newAccount.Balance,
                        IsDeleted = false
                    };
                    _uow.GetRepository<BankingApp.Domain.Models.Transaction>().Insert(depositTransaction);
                    _uow.Save();
                }

                transaction.Commit();

                response.StatusCode = ResponseCode.SUCCESSFUL;
                response.StatusMessage = $"Account created successfully. Account Number: {newAccount.AccountNumber}";

                _logger.LogInformation("Account {AccountNumber} created successfully for {AccountHolderName}",
                    newAccount.AccountNumber, newAccount.AccountHolderName);
            }
            catch (ValidationException e)
            {
                _logger.LogWarning(e, "Account creation validation error");
                response.StatusMessage = string.Join(Environment.NewLine, e.ValidationErrors);
                response.StatusCode = ResponseCode.BadRequest;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error during account creation");
                response.StatusMessage = e.Message;
                response.StatusCode = ResponseCode.BadRequest;
                transaction.Rollback();
            }

            return response;
        }

        private long GenerateAccountNumber()
        {
            var random = new Random();
            return random.Next(1000000000, int.MaxValue);
        }
    }

}
