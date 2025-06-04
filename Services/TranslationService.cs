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
    
    // Filantro Context
    public async Task<TranslationResponse> TranslateFilantroAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Translating (Filantro context) from {SourceLanguage} to {TargetLanguage}",
            request.SourceLanguage, request.TargetLanguage);

        // Build a new cache key that includes Filantro suffix
        var cacheKey = _cacheService.GenerateTranslationKey(
            request.SourceLanguage,
            request.TargetLanguage,
            request.Text + ":Filantro");

        try
        {
            var cached = await _cacheService.GetAsync<TranslationResponse>(
                cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogInformation("Cache hit (Filantro) for key: {CacheKey}", cacheKey);
                return cached;
            }

            _logger.LogInformation("Cache miss (Filantro), calling Ollama API");

            // Filantro prompt
            string systemPrompt = _configuration["Ollama:SystemPromptFactories"] ??
                "You are a translation assistant specializing in factory or maquila related documents. " +
                "Whenever translating, interpret 'planta' as 'factory', not a living organic plant. " +
                "Make sure you keep the context relevant to ensure proper translations. " +
                "Only output the translated text.";

            // The user‐prompt remains the same “Translate the following text …”
            string userPrompt = $"Translate the following text from {request.SourceLanguage} to {request.TargetLanguage}:\n\n{request.Text}";

            var ollamaRequest = new OllamaRequest
            {
                Model = _configuration["Ollama:ModelName"] ?? "llama3.1",
                Prompt = userPrompt,
                System = systemPrompt,
                Stream = false
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/generate", ollamaRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var ollamaResponse = await response.Content
                .ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Failed to deserialize Ollama response");

            var result = new TranslationResponse
            {
                SourceLanguage = request.SourceLanguage,
                TargetLanguage = request.TargetLanguage,
                OriginalText = request.Text,
                TranslatedText = ollamaResponse.Response.Trim(),
                Success = true
            };

            
            await _cacheService.SetAsync(cacheKey, result, cancellationToken: cancellationToken);
            _logger.LogInformation("Cached (Filantro) with key: {CacheKey}", cacheKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 'Filantro' translation");
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

    // Maquila Context
    public async Task<TranslationResponse> TranslateMaquilaAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Translating (Filantro context) from {SourceLanguage} to {TargetLanguage}",
            request.SourceLanguage, request.TargetLanguage);

        var cacheKey = _cacheService.GenerateTranslationKey(
            request.SourceLanguage,
            request.TargetLanguage,
            request.Text + ":Filantro");

        try
        {
            var cached = await _cacheService.GetAsync<TranslationResponse>(
                cacheKey, cancellationToken);
            if (cached != null)
            {
                _logger.LogInformation("Cache hit (Filantro) for key: {CacheKey}", cacheKey);
                return cached;
            }

            _logger.LogInformation("Cache miss (Filantro), calling Ollama API");

            // Prompt
            string systemPrompt = _configuration["Ollama:SystemPromptFilantro"] ??
                "You are a translation assistant specializing in Non-Profit company documents. " +
                "Whenever translating, interpret 'AC' as asociacion civil, in spanish. " +
                "Only output the translated text.";

            string userPrompt = $"Translate the following text from {request.SourceLanguage} to {request.TargetLanguage}:\n\n{request.Text}";

            var ollamaRequest = new OllamaRequest
            {
                Model = _configuration["Ollama:ModelName"] ?? "llama3.1",
                Prompt = userPrompt,
                System = systemPrompt,
                Stream = false
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/generate", ollamaRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var ollamaResponse = await response.Content
                .ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Failed to deserialize Ollama response");

            var result = new TranslationResponse
            {
                SourceLanguage = request.SourceLanguage,
                TargetLanguage = request.TargetLanguage,
                OriginalText = request.Text,
                TranslatedText = ollamaResponse.Response.Trim(),
                Success = true
            };

            
            await _cacheService.SetAsync(cacheKey, result, cancellationToken: cancellationToken);
            _logger.LogInformation("Cached (Filantro) with key: {CacheKey}", cacheKey);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 'Filantro' translation");
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