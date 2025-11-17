using System.Collections.Generic;

namespace DocumentChecker.Models
{
    /// <summary>
    /// Поле шаблона с эталонным значением и вариантами
    /// </summary>
    public class Field
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ReferenceValue { get; set; } = string.Empty; // Основное эталонное значение
        
        /// <summary>
        /// Допустимые варианты (синонимы, альтернативные формулировки)
        /// </summary>
        public List<string> ValidVariants { get; set; } = new List<string>();
        
        /// <summary>
        /// Известные некорректные варианты (для механизма обучения)
        /// </summary>
        public List<string> InvalidVariants { get; set; } = new List<string>();
    }
}
