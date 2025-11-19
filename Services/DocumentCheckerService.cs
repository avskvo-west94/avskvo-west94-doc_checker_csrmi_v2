using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DocumentChecker.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocumentChecker.Services
{
    /// <summary>
    /// Сервис для проверки Word документов
    /// </summary>
    public class DocumentCheckerService
    {
        /// <summary>
        /// Проверить документ на соответствие шаблону
        /// </summary>
        public List<Discrepancy> CheckDocument(string filePath, Template template)
        {
            var discrepancies = new List<Discrepancy>();

            // Валидация файла
            if (!System.IO.File.Exists(filePath))
                throw new System.IO.FileNotFoundException($"Файл не найден: {filePath}");

            if (!filePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Файл должен быть в формате .docx");

            try
            {
                using (var wordDoc = WordprocessingDocument.Open(filePath, false))
                {
                    var mainPart = wordDoc.MainDocumentPart;
                    if (mainPart == null)
                        return discrepancies;

                    var document = mainPart.Document;
                    if (document?.Body == null)
                        return discrepancies;

                    // Извлечение текста из параграфов
                    int paragraphIndex = 0;
                    var paragraphs = document.Body.Elements<Paragraph>().ToList();
                    foreach (var paragraph in paragraphs)
                    {
                        var text = GetParagraphText(paragraph);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var foundDiscrepancies = CheckTextFragment(
                                text,
                                paragraphIndex,
                                "Paragraph",
                                null,
                                null,
                                null,
                                template,
                                paragraph
                            );
                            discrepancies.AddRange(foundDiscrepancies);
                        }
                        paragraphIndex++;
                    }

                    // Извлечение текста из таблиц
                    int tableIndex = 0;
                    var tables = document.Body.Elements<Table>().ToList();
                    foreach (var table in tables)
                    {
                        int rowIndex = 0;
                        var rows = table.Elements<TableRow>().ToList();
                        foreach (var row in rows)
                        {
                            int columnIndex = 0;
                            var cells = row.Elements<TableCell>().ToList();
                            foreach (var cell in cells)
                            {
                                var cellText = GetCellText(cell);
                                if (!string.IsNullOrWhiteSpace(cellText))
                                {
                                    var foundDiscrepancies = CheckTextFragment(
                                        cellText,
                                        paragraphIndex,
                                        "TableCell",
                                        tableIndex,
                                        rowIndex,
                                        columnIndex,
                                        template,
                                        null,
                                        cell
                                    );
                                    discrepancies.AddRange(foundDiscrepancies);
                                }
                                columnIndex++;
                            }
                            rowIndex++;
                        }
                        tableIndex++;
                    }

                    // Извлечение текста из заголовков (если есть)
                    try
                    {
                        var headerParts = mainPart.HeaderParts;
                        foreach (var headerPart in headerParts)
                        {
                            var headerText = GetHeaderFooterText(headerPart.Header);
                            if (!string.IsNullOrWhiteSpace(headerText))
                            {
                                var foundDiscrepancies = CheckTextFragment(
                                    headerText,
                                    0,
                                    "Header",
                                    null,
                                    null,
                                    null,
                                    template
                                );
                                discrepancies.AddRange(foundDiscrepancies);
                            }
                        }
                    }
                    catch
                    {
                        // Заголовки могут отсутствовать - это нормально
                    }

                    // Извлечение текста из колонтитулов (если есть)
                    try
                    {
                        var footerParts = mainPart.FooterParts;
                        foreach (var footerPart in footerParts)
                        {
                            var footerText = GetHeaderFooterText(footerPart.Footer);
                            if (!string.IsNullOrWhiteSpace(footerText))
                            {
                                var foundDiscrepancies = CheckTextFragment(
                                    footerText,
                                    0,
                                    "Footer",
                                    null,
                                    null,
                                    null,
                                    template
                                );
                                discrepancies.AddRange(foundDiscrepancies);
                            }
                        }
                    }
                    catch
                    {
                        // Колонтитулы могут отсутствовать - это нормально
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка при чтении документа: {ex.Message}", ex);
            }

            return discrepancies;
        }

        private string GetParagraphText(Paragraph paragraph)
        {
            var text = new StringBuilder();
            foreach (var run in paragraph.Elements<Run>())
            {
                foreach (var textElement in run.Elements<Text>())
                {
                    text.Append(textElement.Text);
                }
            }
            return text.ToString();
        }

        private string GetCellText(TableCell cell)
        {
            var text = new StringBuilder();
            foreach (var paragraph in cell.Elements<Paragraph>())
            {
                text.Append(GetParagraphText(paragraph));
                text.Append(" ");
            }
            return text.ToString().Trim();
        }

        private string GetHeaderFooterText(OpenXmlElement headerFooter)
        {
            var text = new StringBuilder();
            foreach (var paragraph in headerFooter.Elements<Paragraph>())
            {
                text.Append(GetParagraphText(paragraph));
                text.Append(" ");
            }
            return text.ToString().Trim();
        }

        private List<Discrepancy> CheckTextFragment(
            string text,
            int paragraphIndex,
            string containerType,
            int? tableIndex,
            int? rowIndex,
            int? columnIndex,
            Template template,
            Paragraph? paragraph = null,
            TableCell? cell = null)
        {
            var discrepancies = new List<Discrepancy>();
            if (string.IsNullOrWhiteSpace(text))
                return discrepancies;

            var normalizedText = TextNormalizer.Normalize(text);

            foreach (var field in template.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.ReferenceValue))
                    continue;

                var normalizedReference = TextNormalizer.Normalize(field.ReferenceValue);
                var normalizedVariants = field.ValidVariants
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => TextNormalizer.Normalize(v))
                    .ToList();
                var normalizedInvalidVariants = field.InvalidVariants
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => TextNormalizer.Normalize(v))
                    .ToList();

                // Проверка на точное совпадение с эталоном или допустимыми вариантами
                bool hasExactMatch = normalizedText.Contains(normalizedReference) ||
                                    normalizedVariants.Any(v => normalizedText.Contains(v));

                // Если есть точное совпадение, пропускаем это поле
                if (hasExactMatch)
                    continue;

                // Поиск известных ошибочных вариантов
                foreach (var invalidVariant in normalizedInvalidVariants)
                {
                    var matches = FindAllMatches(normalizedText, invalidVariant, text);
                    foreach (var match in matches)
                    {
                        var discrepancy = new Discrepancy
                        {
                            FieldId = field.Id,
                            FieldName = field.Name,
                            ExpectedValue = field.ReferenceValue,
                            FoundText = match.FoundText,
                            Type = DiscrepancyType.KnownError,
                            Context = GetContext(text, match.StartPosition, 30),
                            Location = GetLocation(containerType, tableIndex, rowIndex, columnIndex, paragraphIndex),
                            ContainerType = containerType,
                            TableIndex = tableIndex,
                            RowIndex = rowIndex,
                            ColumnIndex = columnIndex,
                            ParagraphIndex = paragraphIndex,
                            StartPosition = match.StartPosition,
                            Length = match.Length,
                            ShouldFix = true
                        };
                        discrepancies.Add(discrepancy);
                    }
                }

                // Поиск fuzzy-совпадений (опечатки) - проверяем фразы, а не только слова
                var fuzzyMatches = FindFuzzyMatches(normalizedText, normalizedReference, normalizedVariants, text);
                foreach (var match in fuzzyMatches)
                {
                    var discrepancy = new Discrepancy
                    {
                        FieldId = field.Id,
                        FieldName = field.Name,
                        ExpectedValue = field.ReferenceValue,
                        FoundText = match.FoundText,
                        Type = DiscrepancyType.PartialMatch,
                        Context = GetContext(text, match.StartPosition, 30),
                        Location = GetLocation(containerType, tableIndex, rowIndex, columnIndex, paragraphIndex),
                        ContainerType = containerType,
                        TableIndex = tableIndex,
                        RowIndex = rowIndex,
                        ColumnIndex = columnIndex,
                        ParagraphIndex = paragraphIndex,
                        StartPosition = match.StartPosition,
                        Length = match.Length,
                        ShouldFix = true
                    };
                    discrepancies.Add(discrepancy);
                }

                // Поиск частичных совпадений (если эталон достаточно длинный)
                if (normalizedReference.Length >= 5)
                {
                    var partialMatches = FindPartialMatches(normalizedText, normalizedReference, text);
                    foreach (var match in partialMatches)
                    {
                        // Проверяем, что это не дубликат fuzzy match
                        if (!fuzzyMatches.Any(fm => Math.Abs(fm.StartPosition - match.StartPosition) < 5))
                        {
                            var discrepancy = new Discrepancy
                            {
                                FieldId = field.Id,
                                FieldName = field.Name,
                                ExpectedValue = field.ReferenceValue,
                                FoundText = match.FoundText,
                                Type = DiscrepancyType.ExactMismatch,
                                Context = GetContext(text, match.StartPosition, 30),
                                Location = GetLocation(containerType, tableIndex, rowIndex, columnIndex, paragraphIndex),
                                ContainerType = containerType,
                                TableIndex = tableIndex,
                                RowIndex = rowIndex,
                                ColumnIndex = columnIndex,
                                ParagraphIndex = paragraphIndex,
                                StartPosition = match.StartPosition,
                                Length = match.Length,
                                ShouldFix = true
                            };
                            discrepancies.Add(discrepancy);
                        }
                    }
                }
            }

            return discrepancies;
        }

        private List<TextMatch> FindAllMatches(string normalizedText, string searchText, string originalText)
        {
            var matches = new List<TextMatch>();
            if (string.IsNullOrWhiteSpace(searchText))
                return matches;

            int startIndex = 0;
            while (true)
            {
                int index = normalizedText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    break;

                // Находим соответствующий текст в оригинале
                var originalMatch = FindOriginalTextAtPosition(originalText, normalizedText, index, searchText.Length);
                matches.Add(new TextMatch
                {
                    StartPosition = originalMatch.StartPosition,
                    Length = originalMatch.Length,
                    FoundText = originalMatch.FoundText
                });

                startIndex = index + 1;
            }

            return matches;
        }

        private List<TextMatch> FindFuzzyMatches(string normalizedText, string reference, List<string> variants, string originalText)
        {
            var matches = new List<TextMatch>();
            var searchTerms = new List<string> { reference };
            searchTerms.AddRange(variants);

            // Разбиваем текст на фразы (слова и их комбинации)
            var phrases = ExtractPhrases(normalizedText);
            
            foreach (var phrase in phrases)
            {
                foreach (var searchTerm in searchTerms)
                {
                    if (string.IsNullOrWhiteSpace(searchTerm) || phrase.Length < 3)
                        continue;

                    var similarity = FuzzyMatcher.Similarity(phrase, searchTerm);
                    if (similarity >= 0.85) // Порог для fuzzy match
                    {
                        // Находим позицию фразы в тексте
                        var position = normalizedText.IndexOf(phrase, StringComparison.OrdinalIgnoreCase);
                        if (position >= 0)
                        {
                            var originalMatch = FindOriginalTextAtPosition(originalText, normalizedText, position, phrase.Length);
                            matches.Add(new TextMatch
                            {
                                StartPosition = originalMatch.StartPosition,
                                Length = originalMatch.Length,
                                FoundText = originalMatch.FoundText
                            });
                        }
                    }
                }
            }

            return matches;
        }

        private List<TextMatch> FindPartialMatches(string normalizedText, string reference, string originalText)
        {
            var matches = new List<TextMatch>();
            
            // Ищем подстроки длиной от 5 символов
            for (int len = Math.Min(5, reference.Length); len <= reference.Length; len++)
            {
                var substring = reference.Substring(0, len);
                var foundMatches = FindAllMatches(normalizedText, substring, originalText);
                
                foreach (var match in foundMatches)
                {
                    // Проверяем, что это действительно похоже на эталон, но не точно
                    var foundNormalized = TextNormalizer.Normalize(match.FoundText);
                    var similarity = FuzzyMatcher.Similarity(foundNormalized, reference);
                    
                    if (similarity < 0.7 && similarity > 0.3) // Частичное совпадение
                    {
                        matches.Add(match);
                    }
                }
            }

            return matches;
        }

        private List<string> ExtractPhrases(string text)
        {
            var phrases = new List<string>();
            
            // Разбиваем на слова
            var words = Regex.Split(text, @"\s+")
                .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length >= 3)
                .ToList();

            // Добавляем отдельные слова
            phrases.AddRange(words);

            // Добавляем биграммы (пары слов)
            for (int i = 0; i < words.Count - 1; i++)
            {
                phrases.Add($"{words[i]} {words[i + 1]}");
            }

            // Добавляем триграммы (тройки слов)
            for (int i = 0; i < words.Count - 2; i++)
            {
                phrases.Add($"{words[i]} {words[i + 1]} {words[i + 2]}");
            }

            return phrases.Distinct().ToList();
        }

        private TextMatch FindOriginalTextAtPosition(string originalText, string normalizedText, int normalizedPosition, int normalizedLength)
        {
            // Простое приближение: используем позицию в нормализованном тексте
            // В реальности это может быть сложнее из-за нормализации
            if (normalizedPosition >= originalText.Length)
                normalizedPosition = originalText.Length - 1;
            
            var endPosition = Math.Min(normalizedPosition + normalizedLength, originalText.Length);
            var foundText = originalText.Substring(normalizedPosition, endPosition - normalizedPosition);

            return new TextMatch
            {
                StartPosition = normalizedPosition,
                Length = foundText.Length,
                FoundText = foundText
            };
        }

        private class TextMatch
        {
            public int StartPosition { get; set; }
            public int Length { get; set; }
            public string FoundText { get; set; } = string.Empty;
        }

        private string GetContext(string text, int position, int contextLength)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var startPos = Math.Max(0, position - contextLength / 2);
            var endPos = Math.Min(text.Length, position + contextLength / 2);
            var context = text.Substring(startPos, endPos - startPos);
            
            if (startPos > 0)
                context = "..." + context;
            if (endPos < text.Length)
                context = context + "...";
            
            return context;
        }

        private string GetLocation(string containerType, int? tableIndex, int? rowIndex, int? columnIndex, int paragraphIndex)
        {
            if (containerType == "TableCell" && tableIndex.HasValue && rowIndex.HasValue && columnIndex.HasValue)
            {
                return $"Таблица {tableIndex.Value + 1}, строка {rowIndex.Value + 1}, столбец {columnIndex.Value + 1}";
            }
            
            return $"Параграф {paragraphIndex + 1}";
        }
    }
}
