namespace SimpleTranslationService.Models
{
    // Request model for translation
    public class TranslationRequest
    {
        public string SourceLanguage { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

        public string Context { get; set; } = string.Empty;
    }

    // Response model with translation result
    public class TranslationResponse
    {
        public string SourceLanguage { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string OriginalText { get; set; } = string.Empty;
        public string TranslatedText { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // Internal model for Ollama API requests
    internal class OllamaRequest
    {
        public string Model { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string? System { get; set; }
        public bool Stream { get; set; } = false;
    }

    // Internal model for Ollama API responses
    internal class OllamaResponse
    {
        public string Model { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public bool Done { get; set; }
    }

    // Request model for BLEU evaluation
    public class TranslationEvaluationRequest
    {
        public TranslationRequest TranslationRequest { get; set; } = new();
        public string ReferenceTranslation { get; set; } = string.Empty;
    }
}