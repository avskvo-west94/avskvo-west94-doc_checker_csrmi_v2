#!/usr/bin/env python3
"""
Пример использования Document Checker CSRM v2
"""

from pathlib import Path
from doc_checker import DocumentChecker
import json

# Пример 1: Проверка одного файла
def example_single_file():
    """Пример проверки одного файла"""
    print("Пример 1: Проверка одного файла")
    print("-" * 50)
    
    checker = DocumentChecker()
    
    # Создаем тестовый файл
    test_file = Path("test_document.txt")
    test_file.write_text("Это тестовый документ для проверки CSRM.", encoding='utf-8')
    
    result = checker.check_document(test_file)
    print(f"Файл: {result['file']}")
    print(f"Статус: {result['status']}")
    print(f"Валиден: {result['valid']}")
    if result['errors']:
        print("Ошибки:")
        for error in result['errors']:
            print(f"  - {error}")
    if result['warnings']:
        print("Предупреждения:")
        for warning in result['warnings']:
            print(f"  - {warning}")
    print()


# Пример 2: Проверка с конфигурацией
def example_with_config():
    """Пример проверки с пользовательской конфигурацией"""
    print("Пример 2: Проверка с конфигурацией")
    print("-" * 50)
    
    config = {
        'required_keywords': ['CSRM', 'документ'],
        'min_content_length': 10
    }
    
    checker = DocumentChecker(config)
    
    test_file = Path("test_document.txt")
    if not test_file.exists():
        test_file.write_text("Это тестовый документ для проверки CSRM.", encoding='utf-8')
    
    result = checker.check_document(test_file)
    print(f"Файл: {result['file']}")
    print(f"Статус: {result['status']}")
    print(f"Валиден: {result['valid']}")
    if result['errors']:
        print("Ошибки:")
        for error in result['errors']:
            print(f"  - {error}")
    print()


# Пример 3: Проверка директории
def example_directory():
    """Пример проверки директории"""
    print("Пример 3: Проверка директории")
    print("-" * 50)
    
    checker = DocumentChecker()
    
    # Создаем тестовую директорию
    test_dir = Path("test_documents")
    test_dir.mkdir(exist_ok=True)
    
    # Создаем несколько тестовых файлов
    (test_dir / "doc1.txt").write_text("Документ 1", encoding='utf-8')
    (test_dir / "doc2.txt").write_text("Документ 2", encoding='utf-8')
    
    results = []
    for file_path in test_dir.glob("*.txt"):
        result = checker.check_document(file_path)
        results.append(result)
        print(f"{file_path.name}: {result['status']}")
    
    print(f"\nВсего проверено: {len(results)}")
    print(f"Валидных: {sum(1 for r in results if r['valid'])}")
    print()


if __name__ == '__main__':
    print("=" * 50)
    print("Примеры использования Document Checker CSRM v2")
    print("=" * 50)
    print()
    
    try:
        example_single_file()
        example_with_config()
        example_directory()
        
        print("Все примеры выполнены успешно!")
    except Exception as e:
        print(f"Ошибка при выполнении примеров: {e}")
        import traceback
        traceback.print_exc()
