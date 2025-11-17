using System.Text;
using System.Text.RegularExpressions;

namespace DocumentChecker.Services
{
    /// <summary>
    /// Сервис для нормализации текста перед сравнением
    /// </summary>
    public static class TextNormalizer
    {
        /// <summary>
        /// Нормализовать текст для сравнения
        /// </summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text;
            
            // Приведение к верхнему регистру
            normalized = normalized.ToUpperInvariant();
            
            // Замена всех типов пробелов на обычный пробел
            normalized = Regex.Replace(normalized, @"\s+", " ");
            
            // Унификация кавычек
            normalized = normalized.Replace('"', '"').Replace('"', '"');
            normalized = normalized.Replace(''', ''').Replace(''', ''');
            
            // Унификация дефисов и тире
            normalized = normalized.Replace('–', '-').Replace('—', '-');
            
            // Удаление повторяющихся пробелов
            normalized = Regex.Replace(normalized, @"\s+", " ");
            
            // Удаление переносов строк внутри слова (опционально)
            normalized = normalized.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            
            // Удаление пробелов в начале и конце
            normalized = normalized.Trim();
            
            return normalized;
        }
    }
}
