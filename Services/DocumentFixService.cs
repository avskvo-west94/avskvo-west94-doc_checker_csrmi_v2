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

                ReplaceTextInParagraph(paragraph, disc.FoundText, field.ReferenceValue);
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

                    ReplaceTextInParagraph(paragraph, disc.FoundText, field.ReferenceValue);
                }
            }
        }

        private void ReplaceTextInParagraph(Paragraph paragraph, string oldText, string newText)
        {
            // Собираем весь текст параграфа
            var fullText = new StringBuilder();
            var runs = paragraph.Elements<Run>().ToList();
            
            foreach (var run in runs)
            {
                foreach (var text in run.Elements<Text>())
                {
                    fullText.Append(text.Text);
                }
            }

            var paragraphText = fullText.ToString();
            if (!paragraphText.Contains(oldText))
                return;

            // Заменяем текст
            var newParagraphText = paragraphText.Replace(oldText, newText);

            // Очищаем параграф
            paragraph.RemoveAllChildren<Run>();

            // Создаем новый Run с замененным текстом
            var newRun = new Run();
            var newTextElement = new Text(newParagraphText);
            newRun.Append(newTextElement);
            paragraph.Append(newRun);
        }
    }
}
