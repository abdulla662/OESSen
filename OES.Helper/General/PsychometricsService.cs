namespace OES.Helper.General
{
    public static class PsychometricsService
    {
        public static double Percentile(IReadOnlyList<double> values, double percentile)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            var sorted = values.Order().ToList();
            double index = percentile / 100.0 * (sorted.Count - 1);
            int lower = (int)Math.Floor(index);
            int upper = (int)Math.Ceiling(index);

            if (lower == upper)
            {
                return sorted[lower];
            }

            return sorted[lower] + (index - lower) * (sorted[upper] - sorted[lower]);
        }

        public static TimeSpan PercentileTimeSpan(IReadOnlyList<TimeSpan> values, double percentile)
        {
            if (values == null || values.Count == 0)
            {
                return TimeSpan.Zero;
            }

            var seconds = values.Select(t => t.TotalSeconds).ToList();

            return TimeSpan.FromSeconds(Percentile(seconds, percentile));
        }

        public static double StdDev(IReadOnlyList<double> values)
        {
            if (values == null || values.Count < 2)
            {
                return 0;
            }

            double mean = values.Average();
            double variance = values.Sum(v => Math.Pow(v - mean, 2)) / (values.Count - 1);

            return Math.Sqrt(variance);
        }

        public static TimeSpan StdDevTimeSpan(IReadOnlyList<TimeSpan> values)
        {
            if (values == null || values.Count < 2)
            {
                return TimeSpan.Zero;
            }

            var seconds = values.Select(t => t.TotalSeconds).ToList();
            return TimeSpan.FromSeconds(StdDev(seconds));
        }

        public static double CronbachAlpha(IReadOnlyList<double> opItemPValues, IReadOnlyList<double> totalScores)
        {
            int k = opItemPValues.Count;

            if (k < 2)
            {
                return 0;
            }

            double itemVarianceSum = opItemPValues.Sum(p => p * (1 - p));
            double totalVariance = Math.Pow(StdDev(totalScores), 2);

            if (totalVariance == 0)
            {
                return 0;
            }

            return k / (double)(k - 1) * (1.0 - itemVarianceSum / totalVariance);
        }

        public static double? PointBiserial(IReadOnlyList<double> itemBinary, IReadOnlyList<double> restScores)
        {
            if (itemBinary == null || itemBinary.Count < 2 || itemBinary.Count != restScores.Count)
            {
                return null;
            }

            double p = itemBinary.Average();

            if (p == 0 || p == 1)
            {
                return null;
            }

            // Direct Pearson correlation — matches Excel CORREL exactly.
            // Classic point-biserial via (M1-M0)/SD uses population SD (÷N),
            // not sample SD (÷N-1); Pearson avoids that ambiguity.
            double meanY = restScores.Average();
            double num = 0, denX = 0, denY = 0;
            for (int i = 0; i < itemBinary.Count; i++)
            {
                double dx = itemBinary[i] - p;
                double dy = restScores[i] - meanY;
                num += dx * dy;
                denX += dx * dx;
                denY += dy * dy;
            }
            double den = Math.Sqrt(denX * denY);

            if (den == 0)
            {
                return null;
            }

            return num / den;
        }

        public static double? OptionTotalCorrelation(string option, IReadOnlyList<string> chosenOptions, IReadOnlyList<double> totalScores)
        {
            if (!chosenOptions.Any(c => c == option))
            {
                return null;
            }

            var binary = chosenOptions.Select(c => string.Equals(c, option, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0).ToList();

            return PointBiserial(binary, totalScores);
        }

        public static double SEM(double scaledScoreStdev, double cronbachAlpha) => scaledScoreStdev * Math.Sqrt(1 - cronbachAlpha);

        public static double Mode(IReadOnlyList<double> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            return values.GroupBy(v => v).OrderByDescending(g => g.Count()).First().Key;
        }

        public static double Skew(IReadOnlyList<double> values)
        {
            if (values == null || values.Count < 3)
            {
                return 0;
            }

            double mean = values.Average();
            double sd = StdDev(values);

            if (sd == 0)
            {
                return 0;
            }

            return values.Sum(v => Math.Pow((v - mean) / sd, 3)) / values.Count;
        }

        public static double Kurtosis(IReadOnlyList<double> values)
        {
            if (values == null || values.Count < 4)
            {
                return 0;
            }

            double mean = values.Average();
            double sd = StdDev(values);

            if (sd == 0)
            {
                return 0;
            }

            return values.Sum(v => Math.Pow((v - mean) / sd, 4)) / values.Count - 3;
        }

        public static double ParseFinalScore(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return 0;
            }

            if (double.TryParse(json, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double plain))
            {
                return plain;
            }

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("FinalScore", out var val))
                    {
                        return val.GetDouble();
                    }

                }
            }
            catch { }
            return 0;
        }

        public static (double upper, double lower) Upper27Lower27CorrectPct(
            IReadOnlyList<(long CandidateId, double RawScore)> candidateScores,
            string questionCode,
            ILookup<string, (long CandidateId, bool IsCorrect)> responsesByQuestion)
        {
            if (candidateScores.Count == 0)
            {
                return (0, 0);
            }

            int n27 = Math.Max(1, (int)Math.Ceiling(candidateScores.Count * 0.27));
            var sorted = candidateScores.OrderByDescending(x => x.RawScore).ToList();
            var upperIds = sorted.Take(n27).Select(x => x.CandidateId).ToHashSet();
            var lowerIds = sorted.TakeLast(n27).Select(x => x.CandidateId).ToHashSet();

            var responses = responsesByQuestion[questionCode].ToList();
            double upper = upperIds.Count > 0
                ? responses.Where(r => upperIds.Contains(r.CandidateId) && r.IsCorrect).Count() / (double)upperIds.Count * 100
                : 0;
            double lower = lowerIds.Count > 0
                ? responses.Where(r => lowerIds.Contains(r.CandidateId) && r.IsCorrect).Count() / (double)lowerIds.Count * 100
                : 0;
            return (upper, lower);
        }
    }
}
