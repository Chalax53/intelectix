using System;
using System.Collections.Generic;
using System.Linq;


namespace Services
{
    public static class BleuScoreEvaluator
    {
        public static double ComputeBLEU(string reference, string translation, int maxN = 4)
        {
            var referenceTokens = reference.Split(' ');
            var translationTokens = translation.Split(' ');

            double brevityPenalty = translationTokens.Length < referenceTokens.Length
                ? Math.Exp(1.0 - (double)referenceTokens.Length / translationTokens.Length)
                : 1.0;

            double[] precisions = new double[maxN];

            for (int n = 1; n <= maxN; n++)
            {
                var refNGrams = GetNGrams(referenceTokens, n);
                var candNGrams = GetNGrams(translationTokens, n);

                int matchCount = candNGrams.Count(ng => refNGrams.Contains(ng));
                precisions[n - 1] = candNGrams.Count > 0 ? (double)matchCount / candNGrams.Count : 0.0;
            }

            double geometricMean = precisions.Any(p => p == 0) ? 0 : Math.Exp(precisions.Select(p => Math.Log(p)).Average());

            return brevityPenalty * geometricMean;
        }

        private static List<string> GetNGrams(string[] tokens, int n)
        {
            var ngrams = new List<string>();
            for (int i = 0; i <= tokens.Length - n; i++)
            {
                ngrams.Add(string.Join(" ", tokens.Skip(i).Take(n)));
            }
            return ngrams;
        }
    }
}