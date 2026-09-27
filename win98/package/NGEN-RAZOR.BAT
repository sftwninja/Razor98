@echo off
REM Precompile Razor with ngen so it doesn't stall while you play.
REM Run once after installing and after every update.
echo Running ngen on Razor.exe, this takes a few minutes...
%windir%\Microsoft.NET\Framework\v2.0.50727\ngen.exe install Razor.exe /nologo
if errorlevel 1 goto failed
echo.
echo Done. Run this again after updating Razor.
pause
goto end
:failed
echo.
echo ngen failed. Razor still works, it just starts slower.
pause
:end
