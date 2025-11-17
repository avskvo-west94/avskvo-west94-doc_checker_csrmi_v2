using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using DocumentChecker.Models;
using DocumentChecker.Services;
using DocumentChecker.Views;

namespace DocumentChecker
{
    public partial class MainWindow : Window
    {
        private TemplateService _templateService;
        private DocumentCheckerService _checkerService;
        private DocumentFixService _fixService;
        private Template? _currentTemplate;
        private string? _currentDocumentPath;
        private List<Discrepancy> _allDiscrepancies = new List<Discrepancy>();

        public MainWindow()
        {
            InitializeComponent();
            
            _templateService = new TemplateService();
            _checkerService = new DocumentCheckerService();
            _fixService = new DocumentFixService();
            
            UpdateFieldsDisplay();
        }

        private void OpenDocument_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Word документы (*.docx)|*.docx|Все файлы (*.*)|*.*",
                Title = "Выберите документ для проверки"
            };

            if (dialog.ShowDialog() == true)
            {
                _currentDocumentPath = dialog.FileName;
                MessageBox.Show($"Документ загружен: {System.IO.Path.GetFileName(_currentDocumentPath)}", 
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SaveDocumentAs_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentDocumentPath))
            {
                MessageBox.Show("Сначала откройте документ", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Word документы (*.docx)|*.docx|Все файлы (*.*)|*.*",
                Title = "Сохранить исправленный документ",
                FileName = System.IO.Path.GetFileNameWithoutExtension(_currentDocumentPath) + "_исправлено.docx"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    System.IO.File.Copy(_currentDocumentPath, dialog.FileName, true);
                    MessageBox.Show("Документ сохранен", "Успех", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SelectTemplate_Click(object sender, RoutedEventArgs e)
        {
            var window = new TemplateSelectionWindow(_templateService);
            if (window.ShowDialog() == true && window.SelectedTemplate != null)
            {
                _currentTemplate = window.SelectedTemplate;
                SelectedTemplateText.Text = _currentTemplate.Name;
                UpdateFieldsDisplay();
            }
        }

        private void ManageTemplates_Click(object sender, RoutedEventArgs e)
        {
            var window = new TemplateManagementWindow(_templateService);
            window.ShowDialog();
            
            // Обновляем текущий шаблон, если он был изменен
            if (_currentTemplate != null)
            {
                var updated = _templateService.LoadTemplate(_currentTemplate.Id);
                if (updated != null)
                {
                    _currentTemplate = updated;
                    UpdateFieldsDisplay();
                }
            }
        }

        private void UpdateFieldsDisplay()
        {
            if (_currentTemplate == null)
            {
                FieldsList.ItemsSource = null;
                return;
            }

            FieldsList.ItemsSource = _currentTemplate.Fields;
        }

        private void CheckDocument_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTemplate == null)
            {
                MessageBox.Show("Сначала выберите шаблон", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_currentDocumentPath))
            {
                MessageBox.Show("Сначала откройте документ", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Cursor = System.Windows.Input.Cursors.Wait;
                _allDiscrepancies = _checkerService.CheckDocument(_currentDocumentPath, _currentTemplate);
                
                UpdateResultsDisplay();
                
                MessageBox.Show($"Проверка завершена. Найдено несоответствий: {_allDiscrepancies.Count}", 
                    "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке документа: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private void UpdateResultsDisplay()
        {
            var filtered = _allDiscrepancies.AsEnumerable();

            // Фильтр по полю
            if (FieldFilterComboBox.SelectedItem != null && 
                FieldFilterComboBox.SelectedItem is ComboBoxItem item &&
                item.Content.ToString() != "Все")
            {
                var fieldName = item.Content.ToString();
                filtered = filtered.Where(d => d.FieldName == fieldName);
            }

            // Фильтр по типу
            if (TypeFilterComboBox.SelectedIndex > 0)
            {
                var typeIndex = TypeFilterComboBox.SelectedIndex;
                var type = typeIndex == 1 ? DiscrepancyType.ExactMismatch :
                          typeIndex == 2 ? DiscrepancyType.PartialMatch :
                          DiscrepancyType.KnownError;
                filtered = filtered.Where(d => d.Type == type);
            }

            ResultsDataGrid.ItemsSource = filtered.ToList();
            
            // Обновляем список полей для фильтра
            UpdateFieldFilter();
        }

        private void UpdateFieldFilter()
        {
            var currentSelection = FieldFilterComboBox.SelectedItem;
            FieldFilterComboBox.Items.Clear();
            FieldFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все" });
            
            foreach (var fieldName in _allDiscrepancies.Select(d => d.FieldName).Distinct())
            {
                FieldFilterComboBox.Items.Add(new ComboBoxItem { Content = fieldName });
            }
            
            if (currentSelection != null)
            {
                FieldFilterComboBox.SelectedItem = currentSelection;
            }
            else
            {
                FieldFilterComboBox.SelectedIndex = 0;
            }
        }

        private void FieldFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            UpdateResultsDisplay();
        }

        private void TypeFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            UpdateResultsDisplay();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ResultsDataGrid.Items)
            {
                if (item is Discrepancy disc)
                {
                    disc.ShouldFix = true;
                }
            }
            ResultsDataGrid.Items.Refresh();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ResultsDataGrid.Items)
            {
                if (item is Discrepancy disc)
                {
                    disc.ShouldFix = false;
                }
            }
            ResultsDataGrid.Items.Refresh();
        }

        private void FixSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTemplate == null || string.IsNullOrEmpty(_currentDocumentPath))
            {
                MessageBox.Show("Сначала выберите шаблон и откройте документ", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedDiscrepancies = _allDiscrepancies.Where(d => d.ShouldFix).ToList();
            if (!selectedDiscrepancies.Any())
            {
                MessageBox.Show("Выберите несоответствия для исправления", "Информация", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Исправить {selectedDiscrepancies.Count} несоответствий в документе?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Cursor = System.Windows.Input.Cursors.Wait;
                    _fixService.FixDiscrepancies(_currentDocumentPath, selectedDiscrepancies, _currentTemplate);
                    
                    MessageBox.Show("Исправления применены. Не забудьте сохранить документ!", 
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при исправлении: {ex.Message}", "Ошибка", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Cursor = System.Windows.Input.Cursors.Arrow;
                }
            }
        }

        private void AddErrorVariant_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTemplate == null)
            {
                MessageBox.Show("Сначала выберите шаблон", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new Views.LearningWindow(_templateService, _currentTemplate);
            if (window.ShowDialog() == true)
            {
                // Обновляем текущий шаблон
                var updated = _templateService.LoadTemplate(_currentTemplate.Id);
                if (updated != null)
                {
                    _currentTemplate = updated;
                    UpdateFieldsDisplay();
                }
            }
        }
    }
}
