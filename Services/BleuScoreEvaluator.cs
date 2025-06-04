using System;
using System.Collections.Generic;
using System.Linq;

using System.Text.RegularExpressions;

namespace Services
{
    public static class BleuScoreEvaluator
    {
        public static double ComputeBLEU(string reference, string translation, int maxN = 4)
        {
            var referenceTokens = Preprocess(reference);
            var translationTokens = Preprocess(translation);

            double brevityPenalty = translationTokens.Count < referenceTokens.Count
                ? Math.Exp(1.0 - (double)referenceTokens.Count / translationTokens.Count)
                : 1.0;

            double[] precisions = new double[maxN];

            for (int n = 1; n <= maxN; n++)
            {
                var refNGrams = GetNGrams(referenceTokens, n);
                var candNGrams = GetNGrams(translationTokens, n);

                int matchCount = candNGrams.Count(ng => refNGrams.Contains(ng));
                precisions[n - 1] = candNGrams.Count > 0 ? (double)matchCount / candNGrams.Count : 0.0;
            }

            double geometricMean = precisions.Any(p => p == 0)
                ? 0
                : Math.Exp(precisions.Select(p => Math.Log(p)).Average());

            return brevityPenalty * geometricMean;
        }

        private static List<string> Preprocess(string text)
        {
            // Convierte a minúsculas y separa palabras y puntuación como tokens
            var pattern = @"\p{L}+|\p{P}";
            return Regex.Matches(text.ToLowerInvariant(), pattern)
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToList();
        }

        private static List<string> GetNGrams(List<string> tokens, int n)
        {
            var ngrams = new List<string>();
            for (int i = 0; i <= tokens.Count - n; i++)
            {
                ngrams.Add(string.Join(" ", tokens.Skip(i).Take(n)));
            }
            return ngrams;
        }
    }
}