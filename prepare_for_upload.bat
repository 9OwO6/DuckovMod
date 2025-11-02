@echo off
echo ========================================
echo Preparing BetterThrowingSystem for Steam Upload
echo ========================================
echo.

REM Create a clean upload folder
set UPLOAD_FOLDER=UploadReady
if exist "%UPLOAD_FOLDER%" (
    echo Removing old upload folder...
    rmdir /s /q "%UPLOAD_FOLDER%"
)

echo Creating clean upload folder...
mkdir "%UPLOAD_FOLDER%"

echo.
echo Copying required files only...
copy /Y "BetterThrowingSystem.dll" "%UPLOAD_FOLDER%\" >nul
if %ERRORLEVEL% EQU 0 (
    echo [OK] BetterThrowingSystem.dll
) else (
    echo [ERROR] Failed to copy DLL!
    pause
    exit /b 1
)

copy /Y "info.ini" "%UPLOAD_FOLDER%\" >nul
if %ERRORLEVEL% EQU 0 (
    echo [OK] info.ini
) else (
    echo [WARNING] info.ini not found
)

if exist "preview.png" (
    copy /Y "preview.png" "%UPLOAD_FOLDER%\" >nul
    if %ERRORLEVEL% EQU 0 (
        echo [OK] preview.png
    )
) else (
    echo [INFO] preview.png not found (optional)
)

echo.
echo ========================================
echo Upload folder prepared!
echo ========================================
echo.
echo Folder location: %CD%\%UPLOAD_FOLDER%
echo.
echo Files in upload folder:
dir /b "%UPLOAD_FOLDER%"
echo.
echo IMPORTANT: Use this folder for Steam Workshop upload!
echo You can delete this folder after upload if needed.
echo.
pause

