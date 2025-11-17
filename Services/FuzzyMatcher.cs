using System;

namespace DocumentChecker.Services
{
    /// <summary>
    /// Сервис для нечеткого сравнения строк (расстояние Левенштейна)
    /// </summary>
    public static class FuzzyMatcher
    {
        private const double SimilarityThreshold = 0.85;

        /// <summary>
        /// Вычислить расстояние Левенштейна между двумя строками
        /// </summary>
        public static int LevenshteinDistance(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1))
                return string.IsNullOrEmpty(s2) ? 0 : s2.Length;
            
            if (string.IsNullOrEmpty(s2))
                return s1.Length;

            int n = s1.Length;
            int m = s2.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;
            
            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (s2[j - 1] == s1[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[n, m];
        }

        /// <summary>
        /// Вычислить коэффициент схожести (0.0 - 1.0)
        /// </summary>
        public static double Similarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
                return 1.0;
            
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0.0;

            int maxLength = Math.Max(s1.Length, s2.Length);
            if (maxLength == 0)
                return 1.0;

            int distance = LevenshteinDistance(s1, s2);
            return 1.0 - (double)distance / maxLength;
        }

        /// <summary>
        /// Проверить, похожи ли строки (с учетом порога)
        /// </summary>
        public static bool IsSimilar(string s1, string s2)
        {
            return Similarity(s1, s2) >= SimilarityThreshold;
        }
    }
}
