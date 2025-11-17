using System;
using System.Collections.Generic;

namespace DocumentChecker.Models
{
    /// <summary>
    /// Шаблон с эталонными значениями для проверки документов
    /// </summary>
    public class Template
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 11 обязательных полей
        /// </summary>
        public List<Field> Fields { get; set; } = new List<Field>();
    }
}
