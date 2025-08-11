
namespace BankingApp.Domain.Utility.Interfaces
{
    public interface ICacheService
    {
        Task CacheAbsoluteObject<T>(string cacheKey, T Item, int timeInMinutes);
        Task<T> RetrieveFromCacheAsync<T>(string cacheKey);
        Task PersistToCacheAsync<T>(T objectToPersist, string cacheKey, double durationInMinutes, bool useSlidingExpiration = false) where T : class;
        Task CacheObject<T>(string cacheKey, T Item, int timeInMinutes);
        Task RemoveAsync(string key, CancellationToken token = default);
    }
}
