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
    /// Сервис для автоматического исправления несоответствий в документе
    /// </summary>
    public class DocumentFixService
    {
        /// <summary>
        /// Исправить выбранные несоответствия в документе
        /// </summary>
        public void FixDiscrepancies(string filePath, List<Discrepancy> discrepancies, Template template)
        {
            var discrepanciesToFix = discrepancies.Where(d => d.ShouldFix).ToList();
            if (!discrepanciesToFix.Any())
                return;

            using (var wordDoc = WordprocessingDocument.Open(filePath, true))
            {
                var mainPart = wordDoc.MainDocumentPart;
                if (mainPart?.Document?.Body == null)
                    return;

                var document = mainPart.Document;
                var body = document.Body;

                // Группируем несоответствия по контейнеру
                var groupedDiscrepancies = discrepanciesToFix
                    .GroupBy(d => new { d.ContainerType, d.TableIndex, d.RowIndex, d.ColumnIndex, d.ParagraphIndex })
                    .ToList();

                foreach (var group in groupedDiscrepancies)
                {
                    var first = group.First();
                    
                    if (first.ContainerType == "TableCell")
                    {
                        FixTableCell(body, first, group.ToList(), template);
                    }
                    else
                    {
                        FixParagraph(body, first, group.ToList(), template);
                    }
                }

                document.Save();
            }
        }

        private void FixParagraph(Body body, Discrepancy discrepancy, List<Discrepancy> discrepancies, Template template)
        {
            var paragraphs = body.Elements<Paragraph>().ToList();
            if (discrepancy.ParagraphIndex >= paragraphs.Count)
                return;

            var paragraph = paragraphs[discrepancy.ParagraphIndex];
            
            foreach (var disc in discrepancies)
            {
                var field = template.Fields.FirstOrDefault(f => f.Id == disc.FieldId);
                if (field == null)
                    continue;

                ReplaceTextInParagraph(paragraph, disc.FoundText, field.ReferenceValue, disc.StartPosition, disc.Length);
            }
        }

        private void FixTableCell(Body body, Discrepancy discrepancy, List<Discrepancy> discrepancies, Template template)
        {
            if (!discrepancy.TableIndex.HasValue || !discrepancy.RowIndex.HasValue || !discrepancy.ColumnIndex.HasValue)
                return;

            var tables = body.Elements<Table>().ToList();
            if (discrepancy.TableIndex.Value >= tables.Count)
                return;

            var table = tables[discrepancy.TableIndex.Value];
            var rows = table.Elements<TableRow>().ToList();
            
            if (discrepancy.RowIndex.Value >= rows.Count)
                return;

            var row = rows[discrepancy.RowIndex.Value];
            var cells = row.Elements<TableCell>().ToList();
            
            if (discrepancy.ColumnIndex.Value >= cells.Count)
                return;

            var cell = cells[discrepancy.ColumnIndex.Value];
            var paragraphs = cell.Elements<Paragraph>().ToList();
            
            if (paragraphs.Any())
            {
                var paragraph = paragraphs[0];
                foreach (var disc in discrepancies)
                {
                    var field = template.Fields.FirstOrDefault(f => f.Id == disc.FieldId);
                    if (field == null)
                        continue;

                    ReplaceTextInParagraph(paragraph, disc.FoundText, field.ReferenceValue, disc.StartPosition, disc.Length);
                }
            }
        }

        private void ReplaceTextInParagraph(Paragraph paragraph, string oldText, string newText)
        {
            ReplaceTextInParagraph(paragraph, oldText, newText, 0, oldText.Length);
        }

        private void ReplaceTextInParagraph(Paragraph paragraph, string oldText, string newText, int startPosition, int length)
        {
            // Собираем весь текст параграфа и информацию о Run элементах
            var fullText = new StringBuilder();
            var runs = paragraph.Elements<Run>().ToList();
            var runInfo = new List<RunInfo>();
            
            int currentPosition = 0;
            foreach (var run in runs)
            {
                var runStart = currentPosition;
                var runText = new StringBuilder();
                
                foreach (var textElement in run.Elements<Text>())
                {
                    runText.Append(textElement.Text);
                    currentPosition += textElement.Text.Length;
                }
                
                if (runText.Length > 0)
                {
                    runInfo.Add(new RunInfo
                    {
                        Run = run,
                        StartPosition = runStart,
                        Length = runText.Length,
                        Text = runText.ToString()
                    });
                    fullText.Append(runText);
                }
            }

            var paragraphText = fullText.ToString();
            
            // Используем точную позицию, если она указана
            int replaceStart = startPosition;
            int replaceLength = length;
            
            // Если позиция не указана или некорректна, ищем текст
            if (replaceStart < 0 || replaceStart >= paragraphText.Length)
            {
                var index = paragraphText.IndexOf(oldText, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    return;
                replaceStart = index;
                replaceLength = oldText.Length;
            }

            // Проверяем границы
            if (replaceStart + replaceLength > paragraphText.Length)
                replaceLength = paragraphText.Length - replaceStart;

            // Находим Run элементы, которые нужно изменить
            var runsToModify = runInfo
                .Where(r => r.StartPosition < replaceStart + replaceLength && 
                           r.StartPosition + r.Length > replaceStart)
                .ToList();

            if (!runsToModify.Any())
                return;

            // Если заменяемый текст находится в одном Run
            if (runsToModify.Count == 1)
            {
                var runInfoItem = runsToModify[0];
                var run = runInfoItem.Run;
                var textElements = run.Elements<Text>().ToList();
                
                // Вычисляем позицию в рамках этого Run
                var localStart = replaceStart - runInfoItem.StartPosition;
                var localEnd = Math.Min(localStart + replaceLength, runInfoItem.Length);
                
                // Собираем текст до замены, новый текст и текст после
                var beforeText = runInfoItem.Text.Substring(0, localStart);
                var afterText = runInfoItem.Text.Substring(localEnd);
                
                // Очищаем Run от текстовых элементов
                run.RemoveAllChildren<Text>();
                
                // Добавляем текст до замены
                if (!string.IsNullOrEmpty(beforeText))
                {
                    run.Append(new Text(beforeText));
                }
                
                // Добавляем новый текст
                run.Append(new Text(newText));
                
                // Добавляем текст после замены
                if (!string.IsNullOrEmpty(afterText))
                {
                    run.Append(new Text(afterText));
                }
            }
            else
            {
                // Текст разбит на несколько Run - более сложная замена
                // Упрощенный подход: заменяем весь текст параграфа
                var beforeText = paragraphText.Substring(0, replaceStart);
                var afterText = paragraphText.Substring(replaceStart + replaceLength);
                var newParagraphText = beforeText + newText + afterText;
                
                // Сохраняем форматирование первого Run
                var firstRun = runsToModify[0].Run;
                var runProperties = firstRun.RunProperties?.CloneNode(true) as RunProperties;
                
                // Очищаем параграф
                paragraph.RemoveAllChildren<Run>();
                
                // Создаем новый Run с сохраненным форматированием
                var newRun = new Run();
                if (runProperties != null)
                {
                    newRun.RunProperties = runProperties;
                }
                newRun.Append(new Text(newParagraphText));
                paragraph.Append(newRun);
            }
        }

        private class RunInfo
        {
            public Run Run { get; set; } = null!;
            public int StartPosition { get; set; }
            public int Length { get; set; }
            public string Text { get; set; } = string.Empty;
        }
    }
}
