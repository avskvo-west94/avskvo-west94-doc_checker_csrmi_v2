using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DocumentChecker.Models;
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
                foreach (var paragraph in document.Body.Elements<Paragraph>())
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
                            template
                        );
                        discrepancies.AddRange(foundDiscrepancies);
                    }
                    paragraphIndex++;
                }

                // Извлечение текста из таблиц
                int tableIndex = 0;
                foreach (var table in document.Body.Elements<Table>())
                {
                    int rowIndex = 0;
                    foreach (var row in table.Elements<TableRow>())
                    {
                        int columnIndex = 0;
                        foreach (var cell in row.Elements<TableCell>())
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
                                    template
                                );
                                discrepancies.AddRange(foundDiscrepancies);
                            }
                            columnIndex++;
                        }
                        rowIndex++;
                    }
                    tableIndex++;
                }
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

        private List<Discrepancy> CheckTextFragment(
            string text,
            int paragraphIndex,
            string containerType,
            int? tableIndex,
            int? rowIndex,
            int? columnIndex,
            Template template)
        {
            var discrepancies = new List<Discrepancy>();
            var normalizedText = TextNormalizer.Normalize(text);

            foreach (var field in template.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.ReferenceValue))
                    continue;

                var normalizedReference = TextNormalizer.Normalize(field.ReferenceValue);
                var normalizedVariants = field.ValidVariants
                    .Select(v => TextNormalizer.Normalize(v))
                    .ToList();
                var normalizedInvalidVariants = field.InvalidVariants
                    .Select(v => TextNormalizer.Normalize(v))
                    .ToList();

                // Проверка на точное совпадение с эталоном или допустимыми вариантами
                bool isExactMatch = normalizedText.Contains(normalizedReference) ||
                                   normalizedVariants.Any(v => normalizedText.Contains(v));

                // Проверка на известные ошибочные варианты
                bool isKnownError = normalizedInvalidVariants.Any(v => normalizedText.Contains(v));

                // Проверка на fuzzy-совпадение
                bool isFuzzyMatch = false;
                if (!isExactMatch && !isKnownError)
                {
                    // Разбиваем текст на слова и фразы для проверки
                    var words = normalizedText.Split(new[] { ' ', ',', '.', ';', ':', '!', '?' }, 
                        StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var word in words)
                    {
                        if (FuzzyMatcher.IsSimilar(word, normalizedReference) ||
                            normalizedVariants.Any(v => FuzzyMatcher.IsSimilar(word, v)))
                        {
                            isFuzzyMatch = true;
                            break;
                        }
                    }
                }

                // Если найдено несоответствие
                if (!isExactMatch)
                {
                    // Проверяем на известные ошибки
                    if (isKnownError)
                    {
                        var discrepancy = new Discrepancy
                        {
                            FieldId = field.Id,
                            FieldName = field.Name,
                            ExpectedValue = field.ReferenceValue,
                            FoundText = text,
                            Type = DiscrepancyType.KnownError,
                            Context = GetContext(text, 30),
                            Location = GetLocation(containerType, tableIndex, rowIndex, columnIndex, paragraphIndex),
                            ContainerType = containerType,
                            TableIndex = tableIndex,
                            RowIndex = rowIndex,
                            ColumnIndex = columnIndex,
                            ParagraphIndex = paragraphIndex,
                            ShouldFix = true
                        };
                        discrepancies.Add(discrepancy);
                    }
                    // Проверяем на fuzzy-совпадение (опечатки)
                    else if (isFuzzyMatch)
                    {
                        var discrepancy = new Discrepancy
                        {
                            FieldId = field.Id,
                            FieldName = field.Name,
                            ExpectedValue = field.ReferenceValue,
                            FoundText = text,
                            Type = DiscrepancyType.PartialMatch,
                            Context = GetContext(text, 30),
                            Location = GetLocation(containerType, tableIndex, rowIndex, columnIndex, paragraphIndex),
                            ContainerType = containerType,
                            TableIndex = tableIndex,
                            RowIndex = rowIndex,
                            ColumnIndex = columnIndex,
                            ParagraphIndex = paragraphIndex,
                            ShouldFix = true
                        };
                        discrepancies.Add(discrepancy);
                    }
                    // Проверяем на частичное вхождение (если эталон не пустой и достаточно длинный)
                    else if (!string.IsNullOrWhiteSpace(normalizedReference) && normalizedReference.Length >= 5)
                    {
                        // Ищем частичное вхождение эталона в тексте
                        var minLength = Math.Min(5, normalizedReference.Length);
                        var searchSubstring = normalizedReference.Substring(0, minLength);
                        
                        if (normalizedText.Contains(searchSubstring))
                        {
                            // Дополнительная проверка: текст должен быть похож на эталон
                            var similarity = FuzzyMatcher.Similarity(normalizedText, normalizedReference);
                            if (similarity < 0.7) // Порог ниже, чем для fuzzy match
                            {
                                var discrepancy = new Discrepancy
                                {
                                    FieldId = field.Id,
                                    FieldName = field.Name,
                                    ExpectedValue = field.ReferenceValue,
                                    FoundText = text,
                                    Type = DiscrepancyType.ExactMismatch,
                                    Context = GetContext(text, 30),
                                    Location = GetLocation(containerType, tableIndex, rowIndex, columnIndex, paragraphIndex),
                                    ContainerType = containerType,
                                    TableIndex = tableIndex,
                                    RowIndex = rowIndex,
                                    ColumnIndex = columnIndex,
                                    ParagraphIndex = paragraphIndex,
                                    ShouldFix = true
                                };
                                discrepancies.Add(discrepancy);
                            }
                        }
                    }
                }
            }

            return discrepancies;
        }

        private string GetContext(string text, int contextLength)
        {
            if (text.Length <= contextLength)
                return text;
            
            return text.Substring(0, Math.Min(contextLength, text.Length)) + "...";
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
