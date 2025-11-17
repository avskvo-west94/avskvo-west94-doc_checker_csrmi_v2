using System;

namespace DocumentChecker.Models
{
    /// <summary>
    /// Несоответствие, найденное в документе
    /// </summary>
    public class Discrepancy
    {
        public int FieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public string FoundText { get; set; } = string.Empty;
        public DiscrepancyType Type { get; set; }
        public string Context { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty; // "Параграф 5" или "Таблица 1, строка 3, столбец 2"
        public bool ShouldFix { get; set; } = true;
        
        // Для замены текста в документе
        public string ContainerType { get; set; } = string.Empty; // "Paragraph" или "TableCell"
        public int? TableIndex { get; set; }
        public int? RowIndex { get; set; }
        public int? ColumnIndex { get; set; }
        public int ParagraphIndex { get; set; }
        public int StartPosition { get; set; }
        public int Length { get; set; }
    }
    
    public enum DiscrepancyType
    {
        ExactMismatch,           // Точное несовпадение
        PartialMatch,            // Частичное совпадение / опечатка
        KnownError               // Известная ошибка
    }
}
