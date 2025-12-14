
#Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser

Stop-Process -Name acad -Force
dotnet build 
Start-Process "C:\Program Files\Autodesk\AutoCAD 2024\acad.exe" -ArgumentList "C:\Users\alicj\OneDrive\Pulpit\PROJEKT.dwg"