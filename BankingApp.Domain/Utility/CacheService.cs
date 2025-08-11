
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BankingApp.Domain.Utility.Interfaces;

namespace BankingApp.Domain.Utility
{   
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task CacheAbsoluteObject<T>(string cacheKey, T Item, int timeInMinutes)
        {
            try
            {
                var serializeObject = JsonConvert.SerializeObject(Item);

                var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(timeInMinutes));
                await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(serializeObject), options);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error storing to redis: " + ex.Message);

            }
        }

        public async Task<T> RetrieveFromCacheAsync<T>(string cacheKey)
        {
            try
            {
                //return default;
                ///  return null;
                var result = await _cache.GetAsync(cacheKey);
                return result == null ? default : JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(result));
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving from redis: " + ex.Message);
                return default;
            }
        }

        public async Task CacheObject<T>(string cacheKey, T Item, int timeInMinutes)
        {
            try
            {
                var serializeObject = JsonConvert.SerializeObject(Item);

                var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(timeInMinutes));
                await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(serializeObject), options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task PersistToCacheAsync<T>(T objectToPersist, string cacheKey, double durationInMinutes, bool useSlidingExpiration = false) where T : class
        {
            try
            {
                if (objectToPersist != default(T))
                {
                    var objectBytes = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(objectToPersist,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            ReferenceHandler = ReferenceHandler.Preserve,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        }));

                    await _cache.SetAsync(cacheKey, objectBytes,
                        useSlidingExpiration ?
                        new DistributedCacheEntryOptions
                        {
                            SlidingExpiration = TimeSpan.FromMinutes(durationInMinutes)
                        } :
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(durationInMinutes)
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            try
            {
                await _cache.RemoveAsync(key, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
