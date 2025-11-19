@echo off
echo Building DocumentChecker Release...
echo.

REM Restore dependencies
dotnet restore
if %errorlevel% neq 0 (
    echo Restore failed!
    exit /b 1
)

REM Build in Release mode
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo Build failed!
    exit /b 1
)

REM Publish as single file
dotnet publish --configuration Release --no-build ^
    -p:PublishSingleFile=true ^
    -p:SelfContained=true ^
    -p:RuntimeIdentifier=win-x64 ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:PublishReadyToRun=true ^
    -o ./publish

if %errorlevel% neq 0 (
    echo Publish failed!
    exit /b 1
)

echo.
echo Build completed successfully!
echo Executable file: ./publish/DocumentChecker.exe
echo.
