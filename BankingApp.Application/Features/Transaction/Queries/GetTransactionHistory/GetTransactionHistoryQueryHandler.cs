using BankingApp.Domain.BindingModels.Responses;
using BankingApp.Domain.Constants;
using BankingApp.Domain.Utility;
using BankingApp.Domain.Utility.Interfaces;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankingApp.Application.Features.Transaction.Queries.GetTransactionHistory
{
    public class GetTransactionHistoryQueryHandler
    : IRequestHandler<GetTransactionHistoryQuery, ServiceResponse<List<BankingApp.Domain.Models.Transaction>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetTransactionHistoryQueryHandler> _logger;
        private readonly ICacheService _cacheService;

        public GetTransactionHistoryQueryHandler(IUnitOfWork uow, ILogger<GetTransactionHistoryQueryHandler> logger, ICacheService cacheService)
        {
            _uow = uow;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<ServiceResponse<List<BankingApp.Domain.Models.Transaction>>> Handle(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
        {
            var response = new ServiceResponse<List<BankingApp.Domain.Models.Transaction>>();

            try
            {
                var validator = new GetTransactionHistoryQueryValidator();
                var validationResult = validator.Validate(request);
                if (!validationResult.IsValid)
                    throw new ValidationException(validationResult.Errors);

                string cacheKey = $"Transactions_{request.AccountId}";

                // Try get from cache
                var cachedTransactions = await _cacheService.RetrieveFromCacheAsync<List<BankingApp.Domain.Models.Transaction>>(cacheKey);
                if (cachedTransactions != null)
                {
                    response.Data = cachedTransactions;
                    response.StatusCode = ResponseCode.SUCCESSFUL;
                    response.StatusMessage = "Transaction history retrieved from cache.";
                    return response;
                }

                // Fetch from DB
                var transactions = _uow.GetRepository<BankingApp.Domain.Models.Transaction>().Get()
                    .Where(t => t.AccountId == request.AccountId && !t.IsDeleted)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();

                // Cache for 10 minutes
                await _cacheService.CacheAbsoluteObject(cacheKey, transactions, 10);

                response.Data = transactions;
                response.StatusCode = ResponseCode.SUCCESSFUL;
                response.StatusMessage = "Transaction history retrieved successfully.";
            }
            catch (Exception ex)
            {
                response.StatusMessage = ex.Message;
                response.StatusCode = ResponseCode.BadRequest;
            }

            return response;
        }
    }

}
