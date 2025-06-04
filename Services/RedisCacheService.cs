using StackExchange.Redis;
using System.Text.Json;

namespace SimpleTranslationService.Services
{
  public class RedisCacheService : ICacheService, IDisposable
  {
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _keyPrefix;
    private readonly TimeSpan _defaultExpiration;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer,
                           ILogger<RedisCacheService> logger,
                           IConfiguration configuration)
    {
      _connectionMultiplexer = connectionMultiplexer;
      _database = connectionMultiplexer.GetDatabase();
      _logger = logger;
      _configuration = configuration;

      _keyPrefix = _configuration["Redis:KeyPrefix"] ?? "translation:";
      var expirationHours = int.TryParse(_configuration["Redis:CacheExpirationHours"], out int hours) ? hours : 24;
      _defaultExpiration = TimeSpan.FromHours(expirationHours);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
      try
      {
        var fullKey = _keyPrefix + key;
        var value = await _database.StringGetAsync(fullKey);

        if (!value.HasValue)
        {
          _logger.LogDebug("Cache miss for key: {Key}", fullKey);
          return default(T);
        }

        _logger.LogDebug("Cache hit for key: {Key}", fullKey);
        return JsonSerializer.Deserialize<T>(value!);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
        return default(T);
      }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
      try
      {
        var fullKey = _keyPrefix + key;
        var serializedValue = JsonSerializer.Serialize(value);
        var exp = expiration ?? _defaultExpiration;

        await _database.StringSetAsync(fullKey, serializedValue, exp);
        _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", fullKey, exp);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error setting cache for key: {Key}", key);
        // Don't throw - caching failures shouldn't break the application
      }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
      try
      {
        var fullKey = _keyPrefix + key;
        await _database.KeyDeleteAsync(fullKey);
        _logger.LogDebug("Removed cache key: {Key}", fullKey);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error removing cache key: {Key}", key);
      }
    }

    public string GenerateTranslationKey(string sourceLanguage, string targetLanguage, string text)
    {
      // Create a consistent cache key by combining source, target, and a hash of the text
      var textHash = text.GetHashCode().ToString();
      return $"{sourceLanguage}:{targetLanguage}:{textHash}";
    }

    public void Dispose()
    {
      _connectionMultiplexer?.Dispose();
    }
  }
}