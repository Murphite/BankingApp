

using BankingApp.Application.Exceptions;
using BankingApp.Domain.BindingModels.Responses;
using BankingApp.Domain.Constants;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using BankingApp.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankingApp.Application.Features.Account.Command.Create
{
    public class CreateAccountHandler : IRequestHandler<CreateAccountCommand, ServiceResponse>
    {
        private readonly ILogger<CreateAccountHandler> _logger;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public CreateAccountHandler(
            ILogger<CreateAccountHandler> logger,
            IUnitOfWork uow,
            ICurrentUserService currentUser)
        {
            _logger = logger;
            _uow = uow;
            _currentUser = currentUser;
        }

        public async Task<ServiceResponse> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            using var transaction = _uow.BeginTransaction();
            var response = new ServiceResponse();

            try
            {
                var validator = new CreateAccountCommandValidator(_uow);
                var result = validator.Validate(request);
                if (!result.IsValid)
                    throw new FluentValidation.ValidationException(result.Errors);

                var newAccount = new BankingApp.Domain.Models.Account
                {
                    AccountNumber = GenerateAccountNumber(),
                    AccountHolderName = request.AccountHolderName,
                    Balance = request.InitialDeposit,
                    CreatedDate = DateTime.UtcNow,
                    OwnerId = _currentUser.UserId.ToString(),      
                    CreatedBy = _currentUser.FullName,  
                    IsDeleted = false
                };

                _uow.GetRepository<BankingApp.Domain.Models.Account>().Insert(newAccount);
                _uow.Save();

                if (request.InitialDeposit > 0)
                {
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
                transaction.Rollback();
                response.StatusMessage = e.Message;
                response.StatusCode = ResponseCode.BadRequest;
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
