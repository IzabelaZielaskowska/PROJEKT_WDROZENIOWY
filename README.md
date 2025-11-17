# PROJEKT_WDROZENIOWY

This project is created to generate a legend of layers in Autocad 2024 drawings.
The legend includes layer names, colors, line types, and additional metadata provided by the user.

## System Requirements
- .NET Framework 4.8
- Visual Studio 2019 lub nowszy
- C# 7.3
- Autocad 2024 (it has to be explicitly 2024 Autocad)
- User has to copy files from Autocad to the project local repository:
* accoremgd.dll
* acdbmgd.dll
* AcMgd.dll
And place them in the folder: ...\PROJEKT_WDROZENIOWY\LEGENDA

## Instalation

To use the extention user needs to:

- Clone project from repo: https://github.com/IzabelaZielaskowska/PROJEKT_WDROZENIOWY.git
- Open Autocad 2024
- In Autocad command line type: NETLOAD
- Select the DLL file located in the local project folder: ...\PROJEKT_WDROZENIOWY\LEGENDA\LegendaPlugin\bin\Debug\LegendaPlugin.dll

## Usage

To use the plugin, follow these steps:

- Create a project in autocad as usual.
- When finished, in Autocad command line type: LEGENDA
- Choose layers you want to include in the legend by clicking brackets next to the name.
- When done, fill the data in "Dane metryczki" section.
- Click "OK" to generate the legend in the drawing or "Anuluj" to cancel the operation.


