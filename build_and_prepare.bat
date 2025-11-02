@echo off
echo ========================================
echo Building and Preparing BetterThrowingSystem for Upload
echo ========================================
echo.

REM Set your game path here
set DUCKOV_PATH=D:\SteamLibrary\steamapps\common\Escape from Duckov

REM Step 1: Build the project
echo [1/3] Building project...
dotnet build BetterThrowingSystem.csproj -c Release /p:DuckovPath="%DUCKOV_PATH%"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed! Please check the error messages above.
    pause
    exit /b 1
)

echo [OK] Build successful!
echo.

REM Step 2: Copy DLL to mod folder
echo [2/3] Copying DLL to mod folder...
copy /Y bin\Release\netstandard2.1\BetterThrowingSystem.dll . >nul
if %ERRORLEVEL% EQU 0 (
    echo [OK] DLL copied to mod folder.
) else (
    echo [WARNING] Failed to copy DLL to mod folder.
)

echo.

REM Step 3: Prepare upload folder (go up one directory to Mods folder)
echo [3/3] Preparing upload folder...
cd ..

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
copy /Y "BetterThrowingSystem\BetterThrowingSystem.dll" "%UPLOAD_FOLDER%\" >nul
if %ERRORLEVEL% EQU 0 (
    echo [OK] BetterThrowingSystem.dll
) else (
    echo [ERROR] Failed to copy DLL!
    pause
    exit /b 1
)

copy /Y "BetterThrowingSystem\info.ini" "%UPLOAD_FOLDER%\" >nul
if %ERRORLEVEL% EQU 0 (
    echo [OK] info.ini
) else (
    echo [WARNING] info.ini not found
)

REM Copy preview.png (not preview.jpg)
if exist "BetterThrowingSystem\preview.png" (
    copy /Y "BetterThrowingSystem\preview.png" "%UPLOAD_FOLDER%\" >nul
    if %ERRORLEVEL% EQU 0 (
        echo [OK] preview.png
    )
) else (
    echo [INFO] preview.png not found (optional)
)

echo.
echo ========================================
echo All done! Ready for upload
echo ========================================
echo.
echo Files in upload folder:
dir /b "%UPLOAD_FOLDER%"
echo.
echo Upload folder location: %CD%\%UPLOAD_FOLDER%
echo.
echo IMPORTANT: Use this folder for Steam Workshop upload!
echo.
pause

