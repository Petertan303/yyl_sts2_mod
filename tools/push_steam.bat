@echo off
setlocal
rem ============================================================
rem  push_steam.bat - Push compiled mod to Steam Workshop
rem
rem  Steps:
rem    1) copy build outputs (json/dll/pck/pdb) from the game mod
rem       dir to ModUploader's content dir (overwrite)
rem    2) run: ModUploader.exe upload -w yyl_sts2_mod
rem
rem  Usage: double-click, or run from any terminal.
rem  NOTE: all comments/output kept in English on purpose - cmd
rem        batch files are parsed with the OEM codepage and UTF-8
rem        Chinese text would garble.
rem ============================================================

set "SRC=G:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\yyl_sts2_mod"
set "WORK=C:\Download\ModUploader-win-x64"
set "DST=%WORK%\yyl_sts2_mod\content"

echo [1/3] Checking inputs...
if not exist "%SRC%\yyl_sts2_mod.pck" (
    echo [ERROR] yyl_sts2_mod.pck not found in "%SRC%"
    echo         Run the publish build first.
    goto :fail
)
if not exist "%WORK%\ModUploader.exe" (
    echo [ERROR] ModUploader.exe not found in "%WORK%"
    goto :fail
)

echo [2/3] Copying build outputs to content dir...
if not exist "%DST%" mkdir "%DST%"

for %%F in (yyl_sts2_mod.json yyl_sts2_mod.dll yyl_sts2_mod.pck yyl_sts2_mod.pdb) do (
    copy /Y "%SRC%\%%F" "%DST%\%%F" >nul
    if errorlevel 1 (
        echo [ERROR] Failed to copy %%F
        goto :fail
    )
    echo   copied %%F
)

echo [3/3] Uploading to Steam Workshop...
cd /d "%WORK%"
ModUploader.exe upload -w yyl_sts2_mod
if errorlevel 1 (
    echo [ERROR] ModUploader returned a non-zero exit code.
    echo         Check "%WORK%\mod-uploader.log" for details.
    goto :fail
)

echo.
echo [DONE] Upload finished.
pause
exit /b 0

:fail
echo.
echo [FAILED] Push aborted - see messages above.
pause
exit /b 1
