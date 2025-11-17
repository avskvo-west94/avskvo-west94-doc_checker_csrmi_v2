#!/usr/bin/env python3
"""
Document Checker CSRM v2
Программа для проверки документов на соответствие стандартам CSRM
"""

import os
import sys
import argparse
from pathlib import Path
from typing import List, Dict, Tuple
import json

from doc_checker import DocumentChecker
from validators import PDFValidator, DOCXValidator, TextValidator


def main():
    """Главная функция программы"""
    parser = argparse.ArgumentParser(
        description='Проверка документов на соответствие стандартам CSRM'
    )
    parser.add_argument(
        'path',
        type=str,
        help='Путь к файлу или директории с документами'
    )
    parser.add_argument(
        '--config',
        type=str,
        default='config.json',
        help='Путь к файлу конфигурации (по умолчанию: config.json)'
    )
    parser.add_argument(
        '--output',
        type=str,
        default='report.json',
        help='Путь к файлу отчета (по умолчанию: report.json)'
    )
    parser.add_argument(
        '--verbose',
        '-v',
        action='store_true',
        help='Подробный вывод'
    )
    
    args = parser.parse_args()
    
    # Загрузка конфигурации
    config_path = Path(args.config)
    if not config_path.exists():
        print(f"Предупреждение: Файл конфигурации {args.config} не найден. Используются настройки по умолчанию.")
        config = {}
    else:
        with open(config_path, 'r', encoding='utf-8') as f:
            config = json.load(f)
    
    # Инициализация проверщика
    checker = DocumentChecker(config)
    
    # Определение пути к документам
    input_path = Path(args.path)
    if not input_path.exists():
        print(f"Ошибка: Путь {args.path} не существует.")
        sys.exit(1)
    
    # Сбор файлов для проверки
    files_to_check = []
    if input_path.is_file():
        files_to_check.append(input_path)
    elif input_path.is_dir():
        # Поддерживаемые расширения
        extensions = {'.pdf', '.docx', '.doc', '.txt', '.md'}
        for ext in extensions:
            files_to_check.extend(input_path.rglob(f'*{ext}'))
    
    if not files_to_check:
        print("Не найдено файлов для проверки.")
        sys.exit(1)
    
    # Проверка документов
    print(f"Найдено файлов для проверки: {len(files_to_check)}")
    if args.verbose:
        print("\nФайлы:")
        for f in files_to_check:
            print(f"  - {f}")
        print()
    
    results = []
    for file_path in files_to_check:
        if args.verbose:
            print(f"Проверка: {file_path}")
        
        result = checker.check_document(file_path)
        results.append(result)
        
        if args.verbose:
            status = "✓" if result['valid'] else "✗"
            print(f"  {status} {result['status']}")
            if result['errors']:
                for error in result['errors']:
                    print(f"    - {error}")
    
    # Сохранение отчета
    report = {
        'total_files': len(files_to_check),
        'valid_files': sum(1 for r in results if r['valid']),
        'invalid_files': sum(1 for r in results if not r['valid']),
        'results': results
    }
    
    output_path = Path(args.output)
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(report, f, ensure_ascii=False, indent=2)
    
    print(f"\nОтчет сохранен в: {output_path}")
    print(f"Всего файлов: {report['total_files']}")
    print(f"Валидных: {report['valid_files']}")
    print(f"Невалидных: {report['invalid_files']}")
    
    # Возврат кода выхода
    sys.exit(0 if report['invalid_files'] == 0 else 1)


if __name__ == '__main__':
    main()
