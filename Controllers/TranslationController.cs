using Microsoft.AspNetCore.Mvc;
using SimpleTranslationService.Models;
using SimpleTranslationService.Services;

namespace SimpleTranslationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranslationController : ControllerBase
    {
        private readonly TranslationService _translationService;
        private readonly ILogger<TranslationController> _logger;

        public TranslationController(
            TranslationService translationService,
            ILogger<TranslationController> logger)
        {
            _translationService = translationService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(TranslationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Translate([FromBody] TranslationRequest request, CancellationToken cancellationToken)
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.SourceLanguage))
                return BadRequest("Source language is required");

            if (string.IsNullOrWhiteSpace(request.TargetLanguage))
                return BadRequest("Target language is required");

            if (string.IsNullOrWhiteSpace(request.Text))
                return BadRequest("Text to translate is required");

            _logger.LogInformation("Received translation request from {SourceLanguage} to {TargetLanguage}", 
                request.SourceLanguage, request.TargetLanguage);

            // Process translation according to given context
            TranslationResponse response;
            if (request.Context?.Equals("Filantro", StringComparison.OrdinalIgnoreCase) == true)
            {
                response = await _translationService
                    .TranslateFilantroAsync(request, cancellationToken);
            }
            else if (request.Context?.Equals("Maquila", StringComparison.OrdinalIgnoreCase) == true)
            {
                response = await _translationService
                    .TranslateMaquilaAsync(request, cancellationToken);
            }
            else
            {
                // default (no special context)
                response = await _translationService
                    .TranslateAsync(request, cancellationToken);
            }

            if (!response.Success)
                return StatusCode(StatusCodes.Status500InternalServerError, response);

            return Ok(response);
        }
    }
}