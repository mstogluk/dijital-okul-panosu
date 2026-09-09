@echo off
setlocal enabledelayedexpansion

rem ============================================================
rem   GENEL AMACLI Publish Bat Dosyasi
rem   Herhangi bir .NET projesinin KOK klasorune koyup cift tikla.
rem   Icindeki .csproj dosyasini KENDISI bulur (WinForms/WPF/Konsol).
rem ============================================================

set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"
set "OUT=%ROOT%\publish"

echo ============================================
echo   .NET Proje Publish
echo   Klasor: %ROOT%
echo ============================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo HATA: 'dotnet' komutu bulunamadi. .NET SDK kurulu degil ya da PATH'e ekli degil.
    echo https://dotnet.microsoft.com/download adresinden .NET SDK kurabilirsin.
    pause
    exit /b 1
)

rem --- Calistirilabilir (exe uretecek) csproj dosyasini bul ---
rem Once "App", "Console", "Wpf", "WinForms" gibi tipik isimlere sahip olani tercih et,
rem bulamazsa bulunan ilk .csproj'u kullan. bin/obj klasorleri haric tutulur.
set "CSPROJ="
for /f "delims=" %%F in ('dir /s /b "%ROOT%\*.csproj" 2^>nul ^| findstr /v /i "\\bin\\ \\obj\\"') do (
    if not defined CSPROJ set "CSPROJ=%%F"
    echo %%F | findstr /i "App\.csproj Console\.csproj$" >nul && set "CSPROJ=%%F"
)

if not defined CSPROJ (
    echo HATA: Bu klasorde ^(alt klasorler dahil^) hic .csproj dosyasi bulunamadi.
    echo Bu bat dosyasini proje kok klasorune koydugundan emin ol.
    pause
    exit /b 1
)

echo Bulunan proje dosyasi:
echo   %CSPROJ%
echo.
echo Baska bir csproj kullanmak istersen, bu satiri elle degistirip tekrar calistirabilirsin.
echo.

echo Onceki publish klasoru temizleniyor ^(varsa^)...
if exist "%OUT%" rmdir /s /q "%OUT%"

echo.
echo Publish baslatiliyor, bu birkac dakika surebilir ^(self-contained oldugu icin
echo .NET calisma zamani da pakete dahil ediliyor^)...
echo.

dotnet publish "%CSPROJ%" -c Release -r win-x64 --self-contained true -o "%OUT%"

if errorlevel 1 (
    echo.
    echo ============================================
    echo   PUBLISH BASARISIZ - yukaridaki hatayi kontrol et
    echo ============================================
    pause
    exit /b 1
)

echo.
echo ============================================
echo   PUBLISH TAMAMLANDI
echo   Cikti: %OUT%
echo ============================================
echo.

explorer "%OUT%"
pause