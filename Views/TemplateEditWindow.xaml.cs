using System.Linq;
using System.Windows;
using DocumentChecker.Models;
using DocumentChecker.Services;

namespace DocumentChecker.Views
{
    public partial class TemplateEditWindow : Window
    {
        private TemplateService _templateService;
        private Template _template;

        public TemplateEditWindow(TemplateService templateService, Template? template)
        {
            InitializeComponent();
            _templateService = templateService;
            
            if (template == null)
            {
                _template = _templateService.CreateDefaultTemplate("Новый шаблон");
            }
            else
            {
                _template = template;
            }
            
            DataContext = _template;
            FieldsDataGrid.ItemsSource = _template.Fields;
        }

        private void ValidVariants_Click(object sender, RoutedEventArgs e)
        {
            if (FieldsDataGrid.SelectedItem is Field field)
            {
                var dialog = new VariantsWindow(field, true);
                dialog.ShowDialog();
                FieldsDataGrid.Items.Refresh();
            }
            else
            {
                MessageBox.Show("Выберите поле", "Информация", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void InvalidVariants_Click(object sender, RoutedEventArgs e)
        {
            if (FieldsDataGrid.SelectedItem is Field field)
            {
                var dialog = new VariantsWindow(field, false);
                dialog.ShowDialog();
                FieldsDataGrid.Items.Refresh();
            }
            else
            {
                MessageBox.Show("Выберите поле", "Информация", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_template.Name))
            {
                MessageBox.Show("Введите название шаблона", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _templateService.SaveTemplate(_template);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
