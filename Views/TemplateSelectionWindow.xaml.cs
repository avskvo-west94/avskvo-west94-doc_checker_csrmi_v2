using System.Windows;
using DocumentChecker.Models;
using DocumentChecker.Services;

namespace DocumentChecker.Views
{
    public partial class TemplateSelectionWindow : Window
    {
        private TemplateService _templateService;
        public Template? SelectedTemplate { get; private set; }

        public TemplateSelectionWindow(TemplateService templateService)
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

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (TemplatesDataGrid.SelectedItem is Template template)
            {
                SelectedTemplate = template;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Выберите шаблон", "Информация", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
