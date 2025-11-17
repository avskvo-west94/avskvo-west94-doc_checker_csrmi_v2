"""
Модуль с валидаторами для различных типов документов
"""

from pathlib import Path
from typing import Dict, List, Optional
import re


class BaseValidator:
    """Базовый класс для валидаторов"""
    
    def __init__(self, config: Optional[Dict] = None):
        self.config = config or {}
        self.min_size = self.config.get('min_file_size', 0)  # в байтах
        self.max_size = self.config.get('max_file_size', 100 * 1024 * 1024)  # 100 МБ по умолчанию
        self.required_keywords = self.config.get('required_keywords', [])
        self.forbidden_keywords = self.config.get('forbidden_keywords', [])
    
    def validate(self, file_path: Path) -> Dict:
        """
        Валидация файла
        
        Args:
            file_path: Путь к файлу
            
        Returns:
            Словарь с результатами проверки
        """
        errors = []
        warnings = []
        
        # Проверка размера файла
        file_size = file_path.stat().st_size
        if file_size < self.min_size:
            errors.append(f'Файл слишком мал: {file_size} байт (минимум: {self.min_size})')
        if file_size > self.max_size:
            errors.append(f'Файл слишком велик: {file_size} байт (максимум: {self.max_size})')
        
        # Проверка имени файла
        filename_errors = self._validate_filename(file_path.name)
        errors.extend(filename_errors)
        
        # Дополнительные проверки (переопределяются в подклассах)
        content_errors, content_warnings = self._validate_content(file_path)
        errors.extend(content_errors)
        warnings.extend(content_warnings)
        
        valid = len(errors) == 0
        
        return {
            'valid': valid,
            'status': 'Валиден' if valid else 'Невалиден',
            'errors': errors,
            'warnings': warnings
        }
    
    def _validate_filename(self, filename: str) -> List[str]:
        """Проверка имени файла"""
        errors = []
        
        # Проверка на запрещенные символы
        forbidden_chars = self.config.get('forbidden_filename_chars', ['<', '>', ':', '"', '|', '?', '*'])
        for char in forbidden_chars:
            if char in filename:
                errors.append(f'Имя файла содержит запрещенный символ: {char}')
        
        # Проверка длины имени
        max_filename_length = self.config.get('max_filename_length', 255)
        if len(filename) > max_filename_length:
            errors.append(f'Имя файла слишком длинное: {len(filename)} символов (максимум: {max_filename_length})')
        
        return errors
    
    def _validate_content(self, file_path: Path) -> tuple[List[str], List[str]]:
        """
        Проверка содержимого файла
        
        Returns:
            Кортеж (ошибки, предупреждения)
        """
        return [], []


class PDFValidator(BaseValidator):
    """Валидатор для PDF файлов"""
    
    def _validate_content(self, file_path: Path) -> tuple[List[str], List[str]]:
        """Проверка PDF файла"""
        errors = []
        warnings = []
        
        # Базовая проверка: файл должен начинаться с PDF заголовка
        try:
            with open(file_path, 'rb') as f:
                header = f.read(4)
                if header != b'%PDF':
                    errors.append('Файл не является валидным PDF (отсутствует PDF заголовок)')
        except Exception as e:
            errors.append(f'Ошибка при чтении PDF файла: {str(e)}')
        
        # Дополнительные проверки можно добавить с использованием библиотеки PyPDF2 или pdfplumber
        if self.config.get('check_pdf_structure', False):
            # Здесь можно добавить более глубокую проверку структуры PDF
            pass
        
        return errors, warnings


class DOCXValidator(BaseValidator):
    """Валидатор для DOCX файлов"""
    
    def _validate_content(self, file_path: Path) -> tuple[List[str], List[str]]:
        """Проверка DOCX файла"""
        errors = []
        warnings = []
        
        # Базовая проверка: DOCX файл - это ZIP архив
        try:
            import zipfile
            with zipfile.ZipFile(file_path, 'r') as zip_file:
                # Проверка наличия основных файлов структуры DOCX
                required_files = ['word/document.xml']
                for req_file in required_files:
                    if req_file not in zip_file.namelist():
                        errors.append(f'DOCX файл поврежден: отсутствует {req_file}')
        except zipfile.BadZipFile:
            errors.append('Файл не является валидным DOCX (не является ZIP архивом)')
        except Exception as e:
            errors.append(f'Ошибка при чтении DOCX файла: {str(e)}')
        
        # Проверка ключевых слов в содержимом (если требуется)
        if self.required_keywords or self.forbidden_keywords:
            try:
                import zipfile
                from xml.etree import ElementTree as ET
                
                with zipfile.ZipFile(file_path, 'r') as zip_file:
                    if 'word/document.xml' in zip_file.namelist():
                        xml_content = zip_file.read('word/document.xml')
                        root = ET.fromstring(xml_content)
                        text_content = ''.join(root.itertext()).lower()
                        
                        # Проверка обязательных ключевых слов
                        for keyword in self.required_keywords:
                            if keyword.lower() not in text_content:
                                errors.append(f'Документ не содержит обязательное ключевое слово: {keyword}')
                        
                        # Проверка запрещенных ключевых слов
                        for keyword in self.forbidden_keywords:
                            if keyword.lower() in text_content:
                                errors.append(f'Документ содержит запрещенное ключевое слово: {keyword}')
            except Exception as e:
                warnings.append(f'Не удалось проверить содержимое документа: {str(e)}')
        
        return errors, warnings


class TextValidator(BaseValidator):
    """Валидатор для текстовых файлов"""
    
    def _validate_content(self, file_path: Path) -> tuple[List[str], List[str]]:
        """Проверка текстового файла"""
        errors = []
        warnings = []
        
        try:
            # Определение кодировки
            encodings = ['utf-8', 'cp1251', 'latin-1']
            content = None
            used_encoding = None
            
            for encoding in encodings:
                try:
                    with open(file_path, 'r', encoding=encoding) as f:
                        content = f.read()
                        used_encoding = encoding
                        break
                except UnicodeDecodeError:
                    continue
            
            if content is None:
                errors.append('Не удалось прочитать файл (неизвестная кодировка)')
                return errors, warnings
            
            # Проверка на пустой файл
            if not content.strip():
                warnings.append('Файл пуст или содержит только пробелы')
            
            # Проверка обязательных ключевых слов
            content_lower = content.lower()
            for keyword in self.required_keywords:
                if keyword.lower() not in content_lower:
                    errors.append(f'Документ не содержит обязательное ключевое слово: {keyword}')
            
            # Проверка запрещенных ключевых слов
            for keyword in self.forbidden_keywords:
                if keyword.lower() in content_lower:
                    errors.append(f'Документ содержит запрещенное ключевое слово: {keyword}')
            
            # Проверка минимальной длины содержимого
            min_content_length = self.config.get('min_content_length', 0)
            if len(content.strip()) < min_content_length:
                warnings.append(f'Содержимое файла слишком короткое: {len(content.strip())} символов')
            
        except Exception as e:
            errors.append(f'Ошибка при чтении текстового файла: {str(e)}')
        
        return errors, warnings
