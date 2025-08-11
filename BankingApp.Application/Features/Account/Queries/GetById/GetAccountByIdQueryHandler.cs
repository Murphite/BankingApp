

using BankingApp.Domain.BindingModels.Responses;
using BankingApp.Domain.Constants;
using BankingApp.Domain.Utility;
using BankingApp.Domain.Utility.Interfaces;
using BankingApp.Infrastructure.Persistence.UnitOfWork;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankingApp.Application.Features.Account.Queries.GetById
{
    public class GetAccountByIdQueryHandler
     : IRequestHandler<GetAccountByIdQuery, ServiceResponse<BankingApp.Domain.Models.Account>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetAccountByIdQueryHandler> _logger;
        private readonly ICacheService _cacheService;

        public GetAccountByIdQueryHandler(
            IUnitOfWork uow,
            ILogger<GetAccountByIdQueryHandler> logger,
            ICacheService cacheService)
        {
            _uow = uow;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<ServiceResponse<BankingApp.Domain.Models.Account>> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
        {
            var response = new ServiceResponse<BankingApp.Domain.Models.Account>();

            try
            {
                var validator = new GetAccountByIdQueryValidator();
                var result = validator.Validate(request);
                if (!result.IsValid)
                    throw new ValidationException(result.Errors);

                string cacheKey = $"Account_{request.AccountId}";

                // Try get from cache
                var cachedAccount = await _cacheService.RetrieveFromCacheAsync<BankingApp.Domain.Models.Account>(cacheKey);
                if (cachedAccount != null)
                {
                    response.Data = cachedAccount;
                    response.StatusCode = ResponseCode.SUCCESSFUL;
                    response.StatusMessage = "Account retrieved from cache.";
                    return response;
                }

                // Otherwise fetch from DB
                var account = _uow.GetRepository<BankingApp.Domain.Models.Account>().Get()
                    .FirstOrDefault(a => a.Id == request.AccountId && !a.IsDeleted);

                if (account == null)
                    throw new Exception("Account not found.");

                // Cache it for 10 minutes
                await _cacheService.CacheAbsoluteObject(cacheKey, account, 10);

                response.Data = account;
                response.StatusCode = ResponseCode.SUCCESSFUL;
                response.StatusMessage = "Account retrieved successfully.";
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
