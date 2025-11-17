using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DocumentChecker.Models;

namespace DocumentChecker.Services
{
    /// <summary>
    /// Сервис для работы с шаблонами (сохранение/загрузка из JSON)
    /// </summary>
    public class TemplateService
    {
        private readonly string _templatesDirectory;
        private const string TemplatesFileName = "templates.json";

        public TemplateService()
        {
            _templatesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DocumentChecker"
            );
            
            if (!Directory.Exists(_templatesDirectory))
            {
                Directory.CreateDirectory(_templatesDirectory);
            }
        }

        /// <summary>
        /// Сохранить шаблон
        /// </summary>
        public void SaveTemplate(Template template)
        {
            template.ModifiedDate = DateTime.Now;
            
            var templates = LoadAllTemplates();
            var existingIndex = templates.FindIndex(t => t.Id == template.Id);
            
            if (existingIndex >= 0)
            {
                templates[existingIndex] = template;
            }
            else
            {
                templates.Add(template);
            }
            
            SaveAllTemplates(templates);
        }

        /// <summary>
        /// Загрузить все шаблоны
        /// </summary>
        public List<Template> LoadAllTemplates()
        {
            var filePath = Path.Combine(_templatesDirectory, TemplatesFileName);
            
            if (!File.Exists(filePath))
            {
                return new List<Template>();
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var templates = JsonSerializer.Deserialize<List<Template>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                });
                
                return templates ?? new List<Template>();
            }
            catch
            {
                return new List<Template>();
            }
        }

        /// <summary>
        /// Загрузить шаблон по ID
        /// </summary>
        public Template? LoadTemplate(string id)
        {
            var templates = LoadAllTemplates();
            return templates.FirstOrDefault(t => t.Id == id);
        }

        /// <summary>
        /// Удалить шаблон
        /// </summary>
        public void DeleteTemplate(string id)
        {
            var templates = LoadAllTemplates();
            templates.RemoveAll(t => t.Id == id);
            SaveAllTemplates(templates);
        }

        /// <summary>
        /// Создать новый шаблон с полями по умолчанию
        /// </summary>
        public Template CreateDefaultTemplate(string name)
        {
            var template = new Template
            {
                Name = name,
                Fields = FieldDefinition.DefaultFields.Select(f => new Field
                {
                    Id = f.Id,
                    Name = f.Name,
                    ReferenceValue = string.Empty,
                    ValidVariants = new List<string>(),
                    InvalidVariants = new List<string>()
                }).ToList()
            };
            
            return template;
        }

        private void SaveAllTemplates(List<Template> templates)
        {
            var filePath = Path.Combine(_templatesDirectory, TemplatesFileName);
            var json = JsonSerializer.Serialize(templates, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(filePath, json);
        }
    }
}
