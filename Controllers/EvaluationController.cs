using Microsoft.AspNetCore.Mvc;
using SimpleTranslationService.Models;
using SimpleTranslationService.Services;
using Services;

namespace SimpleTranslationService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EvaluationController : ControllerBase
    {
        private readonly TranslationService _translationService;

        public EvaluationController(TranslationService translationService)
        {
            _translationService = translationService;
        }

        [HttpPost]
        public async Task<IActionResult> EvaluateTranslation([FromBody] TranslationEvaluationRequest request)
        {
            if (request == null || request.TranslationRequest == null || string.IsNullOrWhiteSpace(request.ReferenceTranslation))
            {
                return BadRequest("Invalid request.");
            }

            // Llama al servicio de traducción
            TranslationResponse response = await _translationService.TranslateAsync(request.TranslationRequest);

            if (!response.Success)
            {
                return StatusCode(500, "Error in translation service: " + response.ErrorMessage);
            }

            // Calcula la métrica BLEU
            string generatedTranslation = response.TranslatedText;
            double bleu = BleuScoreEvaluator.ComputeBLEU(request.ReferenceTranslation, generatedTranslation);

            return Ok(new
            {
                Reference = request.ReferenceTranslation,
                Translation = generatedTranslation,
                BLEUScore = bleu
            });
        }
    }
}