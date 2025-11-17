"""
Модуль для проверки документов
"""

from pathlib import Path
from typing import Dict, List, Optional
import mimetypes

from validators import PDFValidator, DOCXValidator, TextValidator


class DocumentChecker:
    """Класс для проверки документов на соответствие стандартам"""
    
    def __init__(self, config: Optional[Dict] = None):
        """
        Инициализация проверщика документов
        
        Args:
            config: Словарь с настройками проверки
        """
        self.config = config or {}
        
        # Инициализация валидаторов
        self.validators = {
            'application/pdf': PDFValidator(self.config),
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document': DOCXValidator(self.config),
            'application/msword': DOCXValidator(self.config),  # Старый формат .doc
            'text/plain': TextValidator(self.config),
            'text/markdown': TextValidator(self.config),
        }
    
    def check_document(self, file_path: Path) -> Dict:
        """
        Проверка документа
        
        Args:
            file_path: Путь к файлу для проверки
            
        Returns:
            Словарь с результатами проверки:
            {
                'file': str,
                'valid': bool,
                'status': str,
                'errors': List[str],
                'warnings': List[str]
            }
        """
        if not file_path.exists():
            return {
                'file': str(file_path),
                'valid': False,
                'status': 'Файл не найден',
                'errors': [f'Файл {file_path} не существует'],
                'warnings': []
            }
        
        # Определение типа файла
        mime_type, _ = mimetypes.guess_type(str(file_path))
        
        if not mime_type:
            # Попытка определить по расширению
            ext = file_path.suffix.lower()
            mime_map = {
                '.pdf': 'application/pdf',
                '.docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
                '.doc': 'application/msword',
                '.txt': 'text/plain',
                '.md': 'text/markdown',
            }
            mime_type = mime_map.get(ext)
        
        if not mime_type or mime_type not in self.validators:
            return {
                'file': str(file_path),
                'valid': False,
                'status': 'Неподдерживаемый формат файла',
                'errors': [f'Формат файла {mime_type or "неизвестен"} не поддерживается'],
                'warnings': []
            }
        
        # Выполнение проверки
        validator = self.validators[mime_type]
        try:
            result = validator.validate(file_path)
            result['file'] = str(file_path)
            return result
        except Exception as e:
            return {
                'file': str(file_path),
                'valid': False,
                'status': 'Ошибка при проверке',
                'errors': [f'Ошибка: {str(e)}'],
                'warnings': []
            }
