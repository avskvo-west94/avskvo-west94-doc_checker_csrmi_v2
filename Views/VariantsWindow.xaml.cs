using System.Linq;
using System.Windows;
using DocumentChecker.Models;

namespace DocumentChecker.Views
{
    public partial class VariantsWindow : Window
    {
        private Field _field;
        private bool _isValidVariants;

        public VariantsWindow(Field field, bool isValidVariants)
        {
            InitializeComponent();
            _field = field;
            _isValidVariants = isValidVariants;
            
            TitleText.Text = _isValidVariants 
                ? $"Допустимые варианты для поля: {field.Name}"
                : $"Ошибочные варианты для поля: {field.Name}";
            
            LoadVariants();
        }

        private void LoadVariants()
        {
            VariantsListBox.ItemsSource = _isValidVariants 
                ? _field.ValidVariants.ToList()
                : _field.InvalidVariants.ToList();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var variant = NewVariantTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(variant))
            {
                MessageBox.Show("Введите вариант", "Информация", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_isValidVariants)
            {
                if (!_field.ValidVariants.Contains(variant))
                {
                    _field.ValidVariants.Add(variant);
                }
            }
            else
            {
                if (!_field.InvalidVariants.Contains(variant))
                {
                    _field.InvalidVariants.Add(variant);
                }
            }

            NewVariantTextBox.Clear();
            LoadVariants();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (VariantsListBox.SelectedItem is string variant)
            {
                if (_isValidVariants)
                {
                    _field.ValidVariants.Remove(variant);
                }
                else
                {
                    _field.InvalidVariants.Remove(variant);
                }
                
                LoadVariants();
            }
            else
            {
                MessageBox.Show("Выберите вариант для удаления", "Информация", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
