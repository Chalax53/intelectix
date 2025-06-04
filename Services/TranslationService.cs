using System.Net.Http.Json;
using System.Text.Json;
using SimpleTranslationService.Models;

namespace SimpleTranslationService.Services
{
  public class TranslationService
  {
    private readonly HttpClient _httpClient;
    private readonly ILogger<TranslationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ICacheService _cacheService;

    public TranslationService(
      IConfiguration configuration,
      ILogger<TranslationService> logger,
      ICacheService cacheService)
    {
      _configuration = configuration;
      _logger = logger;
      _cacheService = cacheService;

      // Create and configure HTTP client
      _httpClient = new HttpClient
      {
        BaseAddress = new Uri(_configuration["Ollama:BaseUrl"] ?? "http://localhost:11434"),
        Timeout = TimeSpan.FromSeconds(
              int.TryParse(_configuration["Ollama:TimeoutSeconds"], out int timeout) ? timeout : 60)
      };
    }

    public async Task<TranslationResponse> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default)
    {
      _logger.LogInformation("Translating text from {SourceLanguage} to {TargetLanguage}",
          request.SourceLanguage, request.TargetLanguage);

      // Generate cache key
      var cacheKey = _cacheService.GenerateTranslationKey(
          request.SourceLanguage,
          request.TargetLanguage,
          request.Text);

      try
      {
        // Try to get from cache first
        var cachedResponse = await _cacheService.GetAsync<TranslationResponse>(cacheKey, cancellationToken);
        if (cachedResponse != null)
        {
          _logger.LogInformation("Translation found in cache for key: {CacheKey}", cacheKey);
          return cachedResponse;
        }

        _logger.LogInformation("Translation not found in cache, calling Ollama API");

        // If not in cache, call Ollama API
        var translationResponse = await CallOllamaForTranslation(request, cancellationToken);

        // Cache the successful response
        if (translationResponse.Success)
        {
          await _cacheService.SetAsync(cacheKey, translationResponse, cancellationToken: cancellationToken);
          _logger.LogInformation("Translation cached with key: {CacheKey}", cacheKey);
        }

        return translationResponse;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error occurred during translation");

        // Return error response
        return new TranslationResponse
        {
          SourceLanguage = request.SourceLanguage,
          TargetLanguage = request.TargetLanguage,
          OriginalText = request.Text,
          Success = false,
          ErrorMessage = $"Translation failed: {ex.Message}"
        };
      }
    }

    private async Task<TranslationResponse> CallOllamaForTranslation(TranslationRequest request, CancellationToken cancellationToken)
    {
      // Create prompt for translation
      string prompt = $"Translate the following text from {request.SourceLanguage} to {request.TargetLanguage}:\n\n{request.Text}";

      // System prompt to guide the LLM
      string systemPrompt = _configuration["Ollama:SystemPrompt"] ??
          $"You are a translation assistant. Your task is to accurately translate text " +
          $"from {request.SourceLanguage} to {request.TargetLanguage}. " +
          $"Only respond with the translated text, without any additional comments or explanations.";

      // Create request for Ollama
      var ollamaRequest = new OllamaRequest
      {
        Model = _configuration["Ollama:ModelName"] ?? "llama3.1",
        Prompt = prompt,
        System = systemPrompt,
        Stream = false
      };

      // Send request to Ollama
      var response = await _httpClient.PostAsJsonAsync("/api/generate", ollamaRequest, cancellationToken);
      response.EnsureSuccessStatusCode();

      // Parse response
      var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken);

      if (ollamaResponse == null)
      {
        throw new InvalidOperationException("Failed to deserialize response from Ollama");
      }

      // Return successful translation
      return new TranslationResponse
      {
        SourceLanguage = request.SourceLanguage,
        TargetLanguage = request.TargetLanguage,
        OriginalText = request.Text,
        TranslatedText = ollamaResponse.Response.Trim(),
        Success = true
      };
    }
  }
}