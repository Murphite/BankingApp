

using BankingApp.Domain.BindingModels.Responses;
using BankingApp.Domain.Constants;
using BankingApp.Domain.Utility;
using BankingApp.Domain.Utility.Interfaces;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankingApp.Application.Features.Transaction.Queries.MonthlyStatement
{
    public class GetMonthlyStatementQueryHandler
    : IRequestHandler<GetMonthlyStatementQuery, ServiceResponse<List<BankingApp.Domain.Models.Transaction>>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetMonthlyStatementQueryHandler> _logger;
        private readonly ICacheService _cacheService;

        public GetMonthlyStatementQueryHandler(IUnitOfWork uow, ILogger<GetMonthlyStatementQueryHandler> logger, ICacheService cacheService)
        {
            _uow = uow;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<ServiceResponse<List<BankingApp.Domain.Models.Transaction>>> Handle(GetMonthlyStatementQuery request, CancellationToken cancellationToken)
        {
            var response = new ServiceResponse<List<BankingApp.Domain.Models.Transaction>>();

            try
            {
                var validator = new GetMonthlyStatementQueryValidator();
                var validationResult = validator.Validate(request);
                if (!validationResult.IsValid)
                    throw new ValidationException(validationResult.Errors);

                string cacheKey = $"MonthlyStatement_{request.AccountId}_{request.Year}_{request.Month}";

                // Try get from cache
                var cachedStatement = await _cacheService.RetrieveFromCacheAsync<List<BankingApp.Domain.Models.Transaction>>(cacheKey);
                if (cachedStatement != null)
                {
                    response.Data = cachedStatement;
                    response.StatusCode = ResponseCode.SUCCESSFUL;
                    response.StatusMessage = $"Monthly statement for {request.Month}/{request.Year} retrieved from cache.";
                    return response;
                }

                // Fetch from DB
                var transactions = _uow.GetRepository<BankingApp.Domain.Models.Transaction>().Get()
                    .Where(t => t.AccountId == request.AccountId
                             && t.TransactionDate.Year == request.Year
                             && t.TransactionDate.Month == request.Month
                             && !t.IsDeleted)
                    .OrderBy(t => t.TransactionDate)
                    .ToList();

                // Cache for 10 minutes
                await _cacheService.CacheAbsoluteObject(cacheKey, transactions, 10);

                response.Data = transactions;
                response.StatusCode = ResponseCode.SUCCESSFUL;
                response.StatusMessage = $"Monthly statement for {request.Month}/{request.Year} retrieved successfully.";
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
