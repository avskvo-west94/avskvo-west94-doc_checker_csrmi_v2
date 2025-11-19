# Инструкции по сборке исполняемого файла

## Автоматическая сборка через GitHub Actions

При каждом push в ветку `main` автоматически запускается сборка, и готовый `.exe` файл будет доступен в разделе **Actions** → **Artifacts**.

1. Перейдите в репозиторий: https://github.com/avskvo-west94/avskvo-west94-doc_checker_csrmi_v2
2. Откройте вкладку **Actions**
3. Выберите последний успешный workflow
4. В разделе **Artifacts** скачайте `DocumentChecker-Release.zip`

## Ручная сборка на Windows

### Требования:
- Windows 10/11 (x64)
- .NET 8.0 SDK (скачать: https://dotnet.microsoft.com/download/dotnet/8.0)

### Шаги:

1. **Клонируйте репозиторий:**
   ```bash
   git clone https://github.com/avskvo-west94/avskvo-west94-doc_checker_csrmi_v2.git
   cd avskvo-west94-doc_checker_csrmi_v2
   ```

2. **Запустите скрипт сборки:**
   ```cmd
   build-release.bat
   ```

   Или выполните команды вручную:
   ```cmd
   dotnet restore
   dotnet build --configuration Release
   dotnet publish --configuration Release ^
       -p:PublishSingleFile=true ^
       -p:SelfContained=true ^
       -p:RuntimeIdentifier=win-x64 ^
       -o ./publish
   ```

3. **Готовый файл:**
   - Исполняемый файл: `./publish/DocumentChecker.exe`
   - Это самодостаточный файл, не требует установки .NET на целевом компьютере

## Использование

После сборки файл `DocumentChecker.exe` можно:
- Скопировать на любой компьютер с Windows 10/11
- Запустить без установки дополнительных компонентов
- Распространять как отдельный файл

## Примечания

- Размер файла будет около 50-100 МБ (включает .NET runtime)
- Для работы требуется Windows 10/11 (x64)
- Microsoft Word не требуется - используется Open XML SDK
