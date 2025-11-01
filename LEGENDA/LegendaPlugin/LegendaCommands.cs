// Dołączamy atrybuty i infrastrukturę komend AutoCAD (CommandMethod, itp.)
using Autodesk.AutoCAD.Runtime;
// Dołączamy API dokumentów/aplikacji AutoCAD (DocumentManager, ShowModalDialog, itp.)
using Autodesk.AutoCAD.ApplicationServices;
// Dołączamy API bazy danych rysunku (tabele, rekordy, obiekty jak Table, LayerTable)
using Autodesk.AutoCAD.DatabaseServices;
// Dołączamy typy geometryczne (Point3d) potrzebne do wskazania punktu wstawienia
using Autodesk.AutoCAD.Geometry;
// Dołączamy API edytora (wiersz poleceń, pobieranie punktów)
using Autodesk.AutoCAD.EditorInput;
// Dołączamy kolekcje .NET (List<T>) — będziemy gromadzić listy warstw i par etykieta/wartość
using System.Collections.Generic;
// Dołączamy WinForms (Form, DialogResult) — pokażemy własne okno jako modalne
using System.Windows.Forms;
// Dołączamy System.Drawing, aby nazwać typ koloru RGB (ColorValue → System.Drawing.Color)
using System.Drawing;

// Tworzymy alias "AcadApp" wskazujący wprost na Application z AutoCAD,
// aby nie mylić go z System.Windows.Forms.Application (usuwa konflikt CS0104)
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace LegendaPlugin
{
    // Prosta klasa danych o warstwie — używana w oknie i przy budowie tabeli
    public class LayerInfo
    {
        // Nazwa warstwy (np. "A-Wall")
        public string Name { get; set; }
        // Opis warstwy (LayerTableRecord.Description) — może być pusty
        public string Description { get; set; }
        // Kolor warstwy (AutoCAD.Colors.Color) — z niego odczytamy RGB do tabeli
        public Autodesk.AutoCAD.Colors.Color AcadColor { get; set; }
    }

    // Zbiorczy model danych zwracanych z okna (wybrane warstwy + metryka rysunku)
    public class LegendaData
    {
        // Lista warstw wybranych do legendy
        public List<LayerInfo> SelectedLayers { get; set; } = new List<LayerInfo>();
        // Poniżej pola metryki zgodnie z wymaganiami
        public string JednostkaProjektowa { get; set; }
        public string Inwestor { get; set; }
        public string NazwaIAdresObiektu { get; set; }
        public string TytulRysunku { get; set; }
        public string Projektant { get; set; }
        public string Sprawdzajacy { get; set; }
        public string Opracowujacy { get; set; }
        public string Data { get; set; }
        public string Skala { get; set; }
        public string NumerRysunku { get; set; }
    }

    // Klasa zawierająca komendę LEGENDA oraz logikę wstawiania tabel
    public class LegendaCommands
    {
        // Rejestrujemy metodę jako komendę AutoCAD o nazwie "LEGENDA"
        [CommandMethod("LEGENDA")]
        public void CmdLegenda()
        {
            // Pobieramy aktywny dokument przez alias "AcadApp"
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            // Skrót do edytora (wiersz poleceń) dla komunikatów i promptów
            var ed = doc.Editor;

            // Pobieramy listę wszystkich warstw z rysunku
            var allLayers = GetAllLayers();

            // Tworzymy i pokazujemy okno z wyborem warstw i metryką
            using (var form = new LegendaForm(allLayers))
            {
                // Pokazujemy okno modalnie w kontekście AutoCAD
                var result = AcadApp.ShowModalDialog(form);
                // Gdy użytkownik anulował — kończymy komendę bez zmian w rysunku
                if (result != DialogResult.OK)
                    return;

                // Odbieramy dane z okna (wybór warstw + metryka)
                LegendaData data = form.ResultData;

                // Jeśli nie wybrano warstw — informujemy i przerywamy
                if (data.SelectedLayers.Count == 0)
                {
                    // Komunikat w konsoli
                    ed.WriteMessage("\nNie wybrano żadnych warstw do legendy — przerwano.");
                    // Koniec komendy
                    return;
                }

                // Pytamy o punkt wstawienia (górny-lewy narożnik tabeli metryki)
                var ppr = ed.GetPoint("\nWskaż punkt wstawienia tabel metryki i legendy: ");
                // Reakcja na anulowanie wskazania
                if (ppr.Status != PromptStatus.OK)
                    return;

                // Budujemy i wstawiamy obie tabele w rysunku
                InsertTables(data, ppr.Value);
            }
        }

        // Metoda pomocnicza do pobrania kompletu warstw jako listy LayerInfo
        private List<LayerInfo> GetAllLayers()
        {
            // Inicjujemy listę wynikową
            var result = new List<LayerInfo>();
            // Bieżący dokument
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            // Baza danych rysunku
            var db = doc.Database;

            // Otwieramy transakcję do odczytu tabel rysunku
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Odczyt tabeli warstw
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                // Iteracja po wszystkich ID warstw
                foreach (ObjectId layerId in lt)
                {
                    // Odczyt rekordu warstwy
                    var ltr = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
                    // Składamy rekord DTO
                    var info = new LayerInfo
                    {
                        // Nazwa warstwy
                        Name = ltr.Name,
                        // Opis (może być pusty)
                        Description = ltr.Description,
                        // Kolor AutoCAD (wyciągniemy RGB później)
                        AcadColor = ltr.Color
                    };
                    // Dodajemy do listy wynikowej
                    result.Add(info);
                }
                // Zamykanie (commit przy odczycie – dla porządku)
                tr.Commit();
            }
            // Zwracamy listę
            return result;
        }

        // Metoda, która tworzy i wstawia tabelę metryki oraz legendy w punkcie "basePoint"
        private void InsertTables(LegendaData data, Point3d basePoint)
        {
            // Bieżący dokument
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            // Baza danych rysunku
            var db = doc.Database;
            // Edytor do komunikatów
            var ed = doc.Editor;

            // Blokujemy dokument na czas modyfikacji (bezpieczne w MDI)
            using (var docLock = doc.LockDocument())
            {
                // Transakcja zapisu — tworzymy obiekty
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    // Odczyt tabeli bloków (aby dostać się do ModelSpace)
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    // Otwieramy rekord ModelSpace do zapisu (tam dodamy tabele)
                    var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    // ----------------- TABELA METRYKI -----------------

                    // Tworzymy obiekt tabeli dla metryki
                    var infoTable = new Table();
                    // Styl tabeli bierzemy z bieżącego stylu tabel rysunku
                    infoTable.TableStyle = db.Tablestyle;
                    // Ustawiamy rozmiar tabeli: 1 nagłówek + 10 wierszy pól, 2 kolumny
                    infoTable.SetSize(1 + 10, 2);
                    // Ustawiamy wysokość każdego wiersza
                    infoTable.SetRowHeight(8);
                    // Ustawiamy szerokości kolumn: 0 — etykiety, 1 — wartości
                    infoTable.Columns[0].Width = 50;
                    infoTable.Columns[1].Width = 120;
                    // Ustawiamy pozycję (górny-lewy narożnik) zgodnie z wskazaniem użytkownika
                    infoTable.Position = basePoint;
                    // Ustawiamy jednolitą wysokość tekstu we wszystkich komórkach (własna metoda helper — patrz niżej)
                    SetUniformTextHeight(infoTable, 3.0);

                    // Wpis nagłówka metryki do komórki (0,0)
                    infoTable.Cells[0, 0].TextString = "METRYKA RYSUNKU";
                    // Scalanie komórek nagłówka w poziomie (z 0,0 do 0,1)
                    infoTable.MergeCells(CellRange.Create(infoTable, 0, 0, 0, 1));

                    // Tworzymy listę par etykieta/wartość zgodnie z danymi z okna
                    var rows = new List<(string Label, string Value)>
                    {
                        ("Jednostka projektowa", data.JednostkaProjektowa),
                        ("Inwestor", data.Inwestor),
                        ("Nazwa i adres obiektu", data.NazwaIAdresObiektu),
                        ("Tytuł rysunku", data.TytulRysunku),
                        ("Projektant", data.Projektant),
                        ("Sprawdzający", data.Sprawdzajacy),
                        ("Opracowujący", data.Opracowujacy),
                        ("Data", data.Data),
                        ("Skala", data.Skala),
                        ("Numer rysunku", data.NumerRysunku)
                    };

                    // Wypełniamy komórki metryki od wiersza 1 do 10
                    for (int i = 0; i < rows.Count; i++)
                    {
                        // Etykieta w kolumnie 0
                        infoTable.Cells[i + 1, 0].TextString = rows[i].Label;
                        // Wartość w kolumnie 1 (gdy pusta — wstawiamy "-")
                        infoTable.Cells[i + 1, 1].TextString =
                            string.IsNullOrWhiteSpace(rows[i].Value) ? "-" : rows[i].Value;
                    }

                    // Generujemy finalny układ tabeli metryki (geometria, wymiary)
                    infoTable.GenerateLayout();
                    // Dołączamy tabelę metryki do ModelSpace
                    btr.AppendEntity(infoTable);
                    // Rejestrujemy nowy obiekt w transakcji
                    tr.AddNewlyCreatedDBObject(infoTable, true);

                    // ----------------- TABELA LEGENDY -----------------

                    // Ustalamy odstęp pionowy między metryką a legendą (w jednostkach rysunku)
                    var gap = 15.0;
                    // Wyliczamy pozycję górnego-lewego narożnika tabeli legendy — pod metryką
                    var legendPos = new Point3d(basePoint.X, basePoint.Y - infoTable.Height - gap, 0);

                    // Tworzymy obiekt tabeli dla legendy
                    var legendTable = new Table();
                    // Styl tabeli jak w metryce
                    legendTable.TableStyle = db.Tablestyle;
                    // Rozmiar legendy: 1 nagłówek + liczba wybranych warstw; 3 kolumny
                    legendTable.SetSize(1 + data.SelectedLayers.Count, 3);
                    // Wysokość wiersza spójna z metryką
                    legendTable.SetRowHeight(8);
                    // Szerokości kolumn: nazwa, opis, kolor RGB (tekst)
                    legendTable.Columns[0].Width = 80;
                    legendTable.Columns[1].Width = 120;
                    legendTable.Columns[2].Width = 40;
                    // Pozycja górnego-lewego narożnika tabeli legendy
                    legendTable.Position = legendPos;
                    // Jednolita wysokość tekstu we wszystkich komórkach legendy
                    SetUniformTextHeight(legendTable, 3.0);

                    // Nagłówki kolumn
                    legendTable.Cells[0, 0].TextString = "Nazwa warstwy";
                    legendTable.Cells[0, 1].TextString = "Opis";
                    legendTable.Cells[0, 2].TextString = "Kolor RGB";

                    // Wypełnianie wierszy legendy danymi wybranych warstw
                    for (int i = 0; i < data.SelectedLayers.Count; i++)
                    {
                        // Warstwa bieżąca
                        var li = data.SelectedLayers[i];
                        // Numer wiersza w tabeli (nagłówek to wiersz 0)
                        int row = i + 1;
                        // Nazwa warstwy
                        legendTable.Cells[row, 0].TextString = li.Name;
                        // Opis lub zastępczy napis
                        legendTable.Cells[row, 1].TextString =
                            string.IsNullOrWhiteSpace(li.Description) ? "(brak opisu)" : li.Description;
                        // RGB z koloru AutoCAD → System.Drawing.Color
                        System.Drawing.Color rgb = li.AcadColor.ColorValue;
                        // Wpis tekstowy RGB
                        legendTable.Cells[row, 2].TextString = $"{rgb.R},{rgb.G},{rgb.B}";
                    }

                    // Generujemy układ tabeli legendy
                    legendTable.GenerateLayout();
                    // Dołączamy tabelę legendy do ModelSpace
                    btr.AppendEntity(legendTable);
                    // Rejestrujemy w transakcji
                    tr.AddNewlyCreatedDBObject(legendTable, true);

                    // Zatwierdzamy całą transakcję (obie tabele zapisane w rysunku)
                    tr.Commit();

                    // Informujemy użytkownika o sukcesie
                    ed.WriteMessage("\nWstawiono tabelę metryki oraz tabelę legendy.");
                }
            }
        }

        // Pomocnicza metoda ustawiająca jednolitą wysokość tekstu we wszystkich komórkach tabeli
        private static void SetUniformTextHeight(Table table, double height)
        {
            // Pętla po wszystkich wierszach tabeli (getter: Rows.Count — zgodnie z nowszym API)
            for (int r = 0; r < table.Rows.Count; r++)
            {
                // Pętla po wszystkich kolumnach tabeli
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    // Ustawiamy wysokość tekstu w komórce (r,c) metodą SetTextHeight
                    table.SetTextHeight(r, c, height);
                }
            }
        }
    }
}
