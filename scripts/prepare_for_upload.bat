@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
echo ========================================
echo Preparing BetterThrowingSystem for Steam Upload
echo ========================================
echo.

REM Use the UploadReady folder in the parent Mods directory (outside BetterThrowingSystem folder)
REM This matches the upload path used by the game's upload interface
set UPLOAD_FOLDER=..\UploadReady
if not exist "%UPLOAD_FOLDER%" (
    echo Creating upload folder in parent directory...
    mkdir "%UPLOAD_FOLDER%"
) else (
    echo Upload folder already exists, will update files...
)

echo.
echo Checking for required files...
echo.

REM Check for DLL - try multiple locations
set DLL_SOURCE=
if exist "bin\Release\netstandard2.1\BetterThrowingSystem.dll" (
    set DLL_SOURCE=bin\Release\netstandard2.1\BetterThrowingSystem.dll
    echo [INFO] Found DLL in: bin\Release\netstandard2.1\
) else if exist "BetterThrowingSystem.dll" (
    set DLL_SOURCE=BetterThrowingSystem.dll
    echo [INFO] Found DLL in current directory
) else (
    echo [ERROR] BetterThrowingSystem.dll not found!
    echo Please build the project first using build.bat
    pause
    exit /b 1
)

REM Copy DLL
echo Copying DLL...
copy /Y "%DLL_SOURCE%" "%UPLOAD_FOLDER%\" >nul
if %ERRORLEVEL% EQU 0 (
    for %%A in ("%UPLOAD_FOLDER%\BetterThrowingSystem.dll") do (
        set DLL_SIZE=%%~zA
    )
    echo [OK] BetterThrowingSystem.dll ^(Size: !DLL_SIZE! bytes^)
) else (
    echo [ERROR] Failed to copy DLL!
    pause
    exit /b 1
)

REM Copy info.ini
echo Copying info.ini...
if exist "info.ini" (
    copy /Y "info.ini" "%UPLOAD_FOLDER%\" >nul
    if %ERRORLEVEL% EQU 0 (
        echo [OK] info.ini
        REM Read version from info.ini (case-insensitive search)
        for /f "tokens=2 delims==" %%A in ('findstr /I /C:"version" info.ini') do (
            set VERSION=%%A
            set VERSION=!VERSION: =!
        )
    ) else (
        echo [WARNING] Failed to copy info.ini
    )
) else (
    echo [ERROR] info.ini not found!
    pause
    exit /b 1
)

REM Copy preview.png
echo Copying preview.png...
if exist "preview.png" (
    copy /Y "preview.png" "%UPLOAD_FOLDER%\" >nul
    if %ERRORLEVEL% EQU 0 (
        echo [OK] preview.png
    ) else (
        echo [WARNING] Failed to copy preview.png
    )
) else if exist "preview.jpg" (
    copy /Y "preview.jpg" "%UPLOAD_FOLDER%\preview.png" >nul
    if %ERRORLEVEL% EQU 0 (
        echo [OK] preview.jpg ^(copied as preview.png^)
    ) else (
        echo [WARNING] Failed to copy preview.jpg
    )
) else (
    echo [WARNING] preview.png not found ^(optional, but recommended^)
)

echo.
echo ========================================
echo Upload folder prepared successfully!
echo ========================================
echo.
if defined VERSION (
    echo Version: !VERSION!
) else (
    echo [WARNING] Version not found in info.ini
)
echo.
echo Upload folder location:
echo   Relative: %CD%\..\%UPLOAD_FOLDER%
echo   Full path: %~dp0..\UploadReady
echo.
echo Files in upload folder:
dir /b "%UPLOAD_FOLDER%"
echo.
echo ========================================
echo IMPORTANT: Use this folder for Steam Workshop upload!
echo ========================================
echo.
echo Next steps:
echo 1. Launch the game
echo 2. Go to Workshop/创意工坊
echo 3. Click "Upload Mod"/"上传模组"
echo 4. Select the UploadReady folder
echo 5. Fill in mod information
echo 6. Upload!
echo.
echo Note: Make sure you have built the latest DLL using build.bat
echo       before running this script!
echo.
pause

