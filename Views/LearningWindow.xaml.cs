using System.Linq;
using System.Windows;
using DocumentChecker.Models;
using DocumentChecker.Services;

namespace DocumentChecker.Views
{
    public partial class LearningWindow : Window
    {
        private TemplateService _templateService;
        private Template? _currentTemplate;

        public LearningWindow(TemplateService templateService, Template? currentTemplate)
        {
            InitializeComponent();
            _templateService = templateService;
            _currentTemplate = currentTemplate;
            
            if (_currentTemplate != null)
            {
                FieldComboBox.ItemsSource = _currentTemplate.Fields;
                FieldComboBox.SelectedIndex = 0;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTemplate == null)
            {
                MessageBox.Show("Сначала выберите шаблон", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var errorText = ErrorTextTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(errorText))
            {
                MessageBox.Show("Введите текст", "Информация", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (FieldComboBox.SelectedItem is Field field)
            {
                if (!field.InvalidVariants.Contains(errorText))
                {
                    field.InvalidVariants.Add(errorText);
                    _templateService.SaveTemplate(_currentTemplate);
                    
                    MessageBox.Show("Ошибочный вариант добавлен", "Успех", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Такой вариант уже существует", "Информация", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
