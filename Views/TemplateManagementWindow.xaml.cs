using System.Linq;
using System.Windows;
using DocumentChecker.Models;
using DocumentChecker.Services;

namespace DocumentChecker.Views
{
    public partial class TemplateManagementWindow : Window
    {
        private TemplateService _templateService;

        public TemplateManagementWindow(TemplateService templateService)
        {
            InitializeComponent();
            _templateService = templateService;
            LoadTemplates();
        }

        private void LoadTemplates()
        {
            var templates = _templateService.LoadAllTemplates();
            TemplatesDataGrid.ItemsSource = templates;
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TemplateEditWindow(_templateService, null);
            if (dialog.ShowDialog() == true)
            {
                LoadTemplates();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesDataGrid.SelectedItem is Template template)
            {
                var dialog = new TemplateEditWindow(_templateService, template);
                if (dialog.ShowDialog() == true)
                {
                    LoadTemplates();
                }
            }
            else
            {
                MessageBox.Show("Выберите шаблон для редактирования", "Информация", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Clone_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesDataGrid.SelectedItem is Template template)
            {
                var cloned = CloneTemplate(template);
                var dialog = new TemplateEditWindow(_templateService, cloned);
                if (dialog.ShowDialog() == true)
                {
                    LoadTemplates();
                }
            }
            else
            {
                MessageBox.Show("Выберите шаблон для клонирования", "Информация", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private Template CloneTemplate(Template source)
        {
            var cloned = new Template
            {
                Name = source.Name + " (копия)",
                Description = source.Description,
                Fields = source.Fields.Select(f => new Field
                {
                    Id = f.Id,
                    Name = f.Name,
                    ReferenceValue = f.ReferenceValue,
                    ValidVariants = new System.Collections.Generic.List<string>(f.ValidVariants),
                    InvalidVariants = new System.Collections.Generic.List<string>(f.InvalidVariants)
                }).ToList()
            };
            return cloned;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesDataGrid.SelectedItem is Template template)
            {
                var result = MessageBox.Show(
                    $"Удалить шаблон '{template.Name}'?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _templateService.DeleteTemplate(template.Id);
                    LoadTemplates();
                }
            }
            else
            {
                MessageBox.Show("Выберите шаблон для удаления", "Информация", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
