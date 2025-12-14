// Komentarz: Importujemy przestrzeń nazw z obiektami dokumentu AutoCAD (Application, Document).
using Autodesk.AutoCAD.ApplicationServices;
// Komentarz: Importujemy API bazy danych AutoCAD (Database, Transaction, encje rysunkowe).
using Autodesk.AutoCAD.DatabaseServices;
// Komentarz: Importujemy API edytora (konsoli) – do komunikatów i pobierania punktów.
using Autodesk.AutoCAD.EditorInput;
// Komentarz: Importujemy typy geometryczne (Point2d/3d, Vector3d).
using Autodesk.AutoCAD.Geometry;
// Komentarz: Importujemy atrybuty środowiska uruchomieniowego (CommandMethod).
using Autodesk.AutoCAD.Runtime;
// Komentarz: Importy podstawowe .NET.
using System;
// Komentarz: Kolekcje – będziemy używać List<>.
using System.Collections.Generic;
// Komentarz: LINQ – do sortowania warstw.
using System.Linq;
// Komentarz: WinForms – aby pokazać formularz.
using System.Windows.Forms;
// Komentarz: Alias do AutoCAD-owego "Application", aby nie myliło się z System.Windows.Forms.Application.
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
// Komentarz: Alias do AutoCAD-owego koloru, aby nie myliło się z System.Drawing.Color.
using AcColor = Autodesk.AutoCAD.Colors.Color;

namespace LegendPlugin
{
    // Komentarz: Klasa z danymi wprowadzanymi w formularzu – przekażemy je do rysowania.
    public class LegendData
    {
        // Komentarz: Lista nazw warstw wybranych przez użytkownika do legendy.
        public List<string> SelectedLayers { get; set; } = new List<string>();
        // Komentarz: Jednostka projektowa – tekst do metryczki.
        public string JednostkaProjektowa { get; set; }
        // Komentarz: Inwestor – tekst do metryczki.
        public string Inwestor { get; set; }
        // Komentarz: Nazwa i adres obiektu – tekst do metryczki.
        public string NazwaAdresObiektu { get; set; }
        // Komentarz: Tytuł rysunku – tekst do metryczki (środkowe duże pole).
        public string TytulRysunku { get; set; }
        // Komentarz: Projektant – osoba w tabeli podpisów.
        public string Projektant { get; set; }
        // Komentarz: Sprawdzający – osoba w tabeli podpisów.
        public string Sprawdzajacy { get; set; }
        // Komentarz: Opracowujący – osoba w tabeli podpisów.
        public string Opracowujacy { get; set; }
        // Komentarz: Data – używana w wierszach podpisów i w polu "DATA" (jeśli dodasz).
        public string Data { get; set; }
        // Komentarz: Skala – trafi do paska "SKALA".
        public string Skala { get; set; }
        // Komentarz: Numer rysunku – trafi do paska "NR RYS.".
        public string NumerRysunku { get; set; }
    }

    // Komentarz: Główna klasa z komendą LEGENDA i funkcjami rysującymi układ.
    public class LegendCommand
    {
        // =============================
        // Komentarz: KONFIGURACJA UKŁADU (nowy wąski pion)
        // =============================

        // Komentarz: Szerokość całej ramki (np. A4 pion – 210).
        private const double FrameWidth = 210.0;
        // Komentarz: Wysokość całej ramki (np. A4 pion – 297).
        private const double FrameHeight = 297.0;

        // Komentarz: Margines wewnętrzny od zewnętrznej ramki.
        private const double Margin = 5.0;

        // Komentarz: Wysokość górnego panelu z legendą (nagłówek + wiersze).
        //private const double LegendPanelHeight = 80.0;
        // Komentarz: Wysokość dolnej metryczki (pól i tabelki).
        private const double TitleBlockHeight = 110.0;

        // Komentarz: Wysokości tekstów – dopasowane do wąskiego układu.
        private const double CaptionHeight = 3.0;      // Komentarz: małe napisy/etykiety.
        private const double TextHeight = 3.5;         // Komentarz: normalne wartości.
        private const double TitleTextHeight = 4.2;    // Komentarz: większe wartości (np. tytuł).
        private const double BigTitleHeight = 4.6;     // Komentarz: „PLAN SYTUACYJNY”.

        // Komentarz: Parametry wierszy legendy (ramki na pozycje).
        private const double LegendRowHeight = 10.0;   // Komentarz: wysokość pojedynczego wiersza.
        private const double IconSize = 6.0;           // Komentarz: wielkość kwadratu koloru.
        private const double LegendInnerPad = 2.5;     // Komentarz: wewnętrzne marginesy w panelu legendy.

        // Komentarz: Wymiary miejsca na logo w metryczce (prawy górny narożnik metryczki).
        private const double LogoBoxWidth = 26.0;
        private const double LogoBoxHeight = 20.0;

        // Komentarz: Rejestrujemy komendę LEGENDA, aby można ją było wywołać w konsoli AutoCAD.
        [CommandMethod("LEGENDA")]
        public void RunLegend()
        {
            // Komentarz: Pobieramy bieżący dokument AutoCAD – potrzebny do transakcji i edytora.
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            // Komentarz: Pobieramy edytor (konsolę) – do pytań i komunikatów.
            var ed = doc.Editor;

            // Komentarz: Zmienna na dane z formularza.
            LegendData data = null;
            // Komentarz: Tworzymy i pokazujemy formularz z warstwami i polami metryczki.
            using (var form = new LegendForm(GetLayerInfos()))
            {
                // Komentarz: Pokazujemy formularz modalnie przez AutoCAD-owe Application (nie WinForms.Application).
                var result = AcApp.ShowModalDialog(form);
                // Komentarz: Jeśli OK – pobieramy dane; jeśli Anuluj – kończymy komendę.
                if (result == DialogResult.OK)
                    data = form.GetData();
                else
                {
                    // Komentarz: Komunikat o przerwaniu.
                    ed.WriteMessage("\nPrzerwano przez użytkownika.");
                    // Komentarz: Wychodzimy z metody.
                    return;
                }
            }

            // Komentarz: Pytamy użytkownika o lewy-dolny narożnik całej ramki.
            var ppr = ed.GetPoint("\nWskaż lewy-dolny narożnik ramki: ");
            // Komentarz: Jeśli nie wskazano poprawnie – kończymy.
            if (ppr.Status != PromptStatus.OK) return;
            // Komentarz: Zapamiętujemy wybrany punkt.
            var basePt = ppr.Value;

            // Komentarz: Rozpoczynamy transakcję – wszystkie obiekty narysujemy „atomowo”.
            var db = doc.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Komentarz: Otwieramy bieżącą przestrzeń (Model lub PaperSpace) do zapisu.
                var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                // ===============================
                // Komentarz: 1) Zewnętrzna ramka
                // ===============================

                // Komentarz: Tworzymy prostokąt LWPolyline całej ramki używając helpera z Point2d (unikamy CS1503).
                var frame = MakeRectLW(new Point2d(basePt.X, basePt.Y), FrameWidth, FrameHeight);
                // Komentarz: Dodajemy ramkę do rysunku.
                btr.AppendEntity(frame); tr.AddNewlyCreatedDBObject(frame, true);

                // Komentarz: Obliczamy obszar wewnętrzny po odjęciu marginesów.
                var innerX = basePt.X + Margin;
                var innerY = basePt.Y + Margin;
                var innerW = FrameWidth - 2 * Margin;
                var innerH = FrameHeight - 2 * Margin;

                // ==============================================
                // Komentarz: 2) Górny panel – LEGENDA (z wierszami)
                // ==============================================
                var legendRowCount = data.SelectedLayers.Count; // tu liczy ilosc wierszy
                var legH = CaptionHeight + 2 * LegendInnerPad + legendRowCount * LegendRowHeight; // dynamicznie zmieniająca się legenda
                // Komentarz: Wyznaczamy lewy-dolny punkt i rozmiar panelu legendy.
                var legX = innerX;
                var legY = innerY + innerH - legH;
                    //LegendPanelHeight;
                var legW = innerW;
                //var legendRowCount = data.SelectedLayers.Count; // tu liczy ilosc wierszy
                //var legH = CaptionHeight + 2 * LegendInnerPad + legendRowCount * LegendRowHeight; // dynamicznie zmieniająca się legenda

                //var legH = LegendPanelHeight;

                // Komentarz: Rysujemy obwiednię panelu legendy.
                var legendBox = MakeRectLW(new Point2d(legX, legY), legW, legH);
                // Komentarz: Dodajemy panel do rysunku.
                btr.AppendEntity(legendBox); tr.AddNewlyCreatedDBObject(legendBox, true);

                // Komentarz: Rysujemy napis „LEGENDA:” w lewym górnym rogu panelu.
                var legendCaption = new DBText
                {
                    // Komentarz: Ustawiamy pozycję napisu z niewielkim odsunięciem.
                    Position = new Point3d(legX + LegendInnerPad, legY + legH - CaptionHeight - LegendInnerPad, 0),
                    // Komentarz: Ustawiamy wysokość czcionki dla etykiety.
                    Height = CaptionHeight,
                    // Komentarz: Treść etykiety.
                    TextString = "LEGENDA:"
                };
                // Komentarz: Dodajemy napis do rysunku.
                btr.AppendEntity(legendCaption); tr.AddNewlyCreatedDBObject(legendCaption, true);

                // Komentarz: Wyznaczamy Y górnej krawędzi pierwszego wiersza pod napisem.
                double currentY = legY + legH - CaptionHeight - 2 * LegendInnerPad;
                // Komentarz: Wyliczamy maksymalną liczbę wierszy, które zmieszczą się w panelu.
                int maxRows = (int)Math.Floor((currentY - (legY + LegendInnerPad)) / LegendRowHeight);
                // Komentarz: Przycinamy listę warstw do liczby mieszczących się wierszy.
                // var legendRows = data.SelectedLayers.Take(Math.Max(0, maxRows)).ToList();
                var legendRows = data.SelectedLayers.ToList();
                // Komentarz: Iterujemy po warstwach do narysowania pozycji legendy.
                foreach (var layerName in legendRows)
                {
                    // Komentarz: Wyznaczamy spód bieżącego wiersza (wysokość LegendRowHeight).
                    double rowBottom = currentY - LegendRowHeight;
                    // Komentarz: Rysujemy ramkę wiersza (mały prostokąt na całą szerokość panelu z marginesami).
                    var rowRect = MakeRectLW(
                        new Point2d(legX + LegendInnerPad, rowBottom),
                        legW - 2 * LegendInnerPad,
                        LegendRowHeight);
                    // Komentarz: Dodajemy ramkę wiersza do rysunku.
                    btr.AppendEntity(rowRect); tr.AddNewlyCreatedDBObject(rowRect, true);

                    // Komentarz: Wyznaczamy bazę ikonki (kwadrat koloru) – centrowana w pionie w wierszu.
                    var iconX = legX + LegendInnerPad + 2.0;
                    var iconY = rowBottom + (LegendRowHeight - IconSize) / 2.0;
                    // Komentarz: Rysujemy kwadrat koloru warstwy.
                    DrawColorIcon(btr, tr, new Point3d(iconX, iconY, 0), layerName);

                    // Komentarz: Rysujemy krótki odcinek BYLAYER obok kwadratu jako próbkę typu linii.
                    var line = new Line(
                        // Komentarz: Punkt początkowy – na środku wysokości ikonki.
                        new Point3d(iconX + IconSize + 2.0, iconY + IconSize / 2.0, 0),
                        // Komentarz: Punkt końcowy – 18 jednostek w prawo.
                        new Point3d(iconX + IconSize + 2.0 + 18.0, iconY + IconSize / 2.0, 0));
                    // Komentarz: Przypisujemy odcinek do warstwy – odziedziczy kolor/typ/ciężar.
                    line.Layer = layerName;
                    // Komentarz: Dodajemy odcinek do rysunku.
                    btr.AppendEntity(line); tr.AddNewlyCreatedDBObject(line, true);

                    // Komentarz: Tworzymy napis z nazwą warstwy po prawej od próbki linii.
                    var rowText = new DBText
                    {
                        // Komentarz: Ustawiamy pozycję napisu (X po prawej od próbki, Y w środku wiersza).
                        Position = new Point3d(iconX + IconSize + 2.0 + 20.0, rowBottom + (LegendRowHeight - TextHeight) / 2.0, 0),
                        // Komentarz: Ustawiamy wysokość czcionki.
                        Height = TextHeight,
                        // Komentarz: Wpisujemy nazwę warstwy.
                        TextString = layerName
                    };
                    // Komentarz: Dodajemy napis do rysunku.
                    btr.AppendEntity(rowText); tr.AddNewlyCreatedDBObject(rowText, true);

                    // Komentarz: Przesuwamy kursor na kolejny wiersz (niżej).
                    currentY = rowBottom;
                }

                // ====================================
                // Komentarz: 3) Dolny panel – METRYCZKA
                // ====================================

                // Komentarz: Położenie i rozmiar metryczki (u dołu obszaru wewnętrznego).
                var tbX = innerX;
                var tbY = innerY;
                var tbW = innerW;
                var tbH = TitleBlockHeight;

                // Komentarz: Rysujemy obwiednię metryczki jako LWPolyline.
                var tbRect = MakeRectLW(new Point2d(tbX, tbY), tbW, tbH);
                // Komentarz: Dodajemy obwiednię do rysunku.
                btr.AppendEntity(tbRect); tr.AddNewlyCreatedDBObject(tbRect, true);

                // Komentarz: Górna krawędź metryczki – potrzebna do ustalania pól.
                var tbTop = tbY + tbH;

                // Komentarz: Wyznaczamy prostokąt na LOGO (prawy górny narożnik metryczki).
                var logoX = tbX + tbW - LogoBoxWidth - 1.0;
                var logoY = tbTop - LogoBoxHeight - 1.0;
                // Komentarz: Rysujemy ramkę na LOGO.
                var logoRect = MakeRectLW(new Point2d(logoX, logoY), LogoBoxWidth, LogoBoxHeight);
                // Komentarz: Dodajemy ramkę LOGO do rysunku.
                btr.AppendEntity(logoRect); tr.AddNewlyCreatedDBObject(logoRect, true);
                // Komentarz: Umieszczamy napis „LOGO” jako placeholder (możesz później wczytać obraz).
                var logoText = new DBText
                {
                    // Komentarz: Pozycja napisu wewnątrz pola LOGO z lekkim marginesem.
                    Position = new Point3d(logoX + 3.0, logoY + LogoBoxHeight / 2.5, 0),
                    // Komentarz: Wysokość etykiety.
                    Height = CaptionHeight,
                    // Komentarz: Treść napisu.
                    TextString = "LOGO"
                };
                // Komentarz: Dodajemy napis „LOGO”.
                btr.AppendEntity(logoText); tr.AddNewlyCreatedDBObject(logoText, true);

                // Komentarz: Lewy górny blok metryczki – „JEDNOSTKA PROJEKTOWA”.
                var infoWidth = tbW - LogoBoxWidth - 2.0;
                // Komentarz: Startowy Y pierwszego pola (pod górną krawędzią metryczki).
                var infoTop = tbTop - 3.0;

                // Komentarz: Rysujemy etykietowaną ramkę „JEDNOSTKA PROJEKTOWA”.
                infoTop = DrawLabeledBox(btr, tr,
                    // Komentarz: Bazowy punkt – lewy górny narożnik pola (liczymy w naszej metodzie od tego punktu w dół).
                    new Point3d(tbX + 1.5, infoTop, 0),
                    // Komentarz: Szerokość – cała lewa strefa (bez LOGO).
                    infoWidth - 3.0,
                    // Komentarz: Wysokość pola – 16 jednostek.
                    16.0,
                    // Komentarz: Napis etykiety.
                    "JEDNOSTKA PROJEKTOWA",
                    // Komentarz: Wartość wpisana w formularzu.
                    data.JednostkaProjektowa);

                // Komentarz: Rysujemy etykietowaną ramkę „INWESTOR”.
                infoTop = DrawLabeledBox(btr, tr,
                    // Komentarz: Bazowy punkt – nieco niżej niż poprzednie pole.
                    new Point3d(tbX + 1.5, infoTop - 2.0, 0),
                    // Komentarz: Szerokość – ta sama lewa strefa.
                    infoWidth - 3.0,
                    // Komentarz: Wysokość – 16 jednostek.
                    16.0,
                    // Komentarz: Etykieta.
                    "INWESTOR",
                    // Komentarz: Treść z formularza.
                    data.Inwestor);

                // Komentarz: Rysujemy szerokie pole „TYTUŁ” (środek metryczki).
                var afterTitleTop = DrawLabeledBox(btr, tr,
                    // Komentarz: Bazowy punkt – poniżej pola INWESTOR.
                    new Point3d(tbX + 1.5, infoTop - 2.0, 0),
                    // Komentarz: Szerokość – cała metryczka (bez 3 mm marginesu po bokach).
                    tbW - 3.0,
                    // Komentarz: Wysokość – 22 jednostki.
                    22.0,
                    // Komentarz: Etykieta nad polem.
                    "TYTUŁ",
                    // Komentarz: Wartość – jeśli tytuł pusty, użyj nazwy/adresu obiektu.
                    string.IsNullOrWhiteSpace(data.TytulRysunku) ? data.NazwaAdresObiektu : data.TytulRysunku,
                    // Komentarz: Wyśrodkowujemy wartość (zgodnie z wzorem).
                    centerValue: true,
                    // Komentarz: Trochę większa wysokość liter dla tytułu.
                    valueHeight: TitleTextHeight);

                // Komentarz: Rysujemy ramkę pod napis „PLAN SYTUACYJNY”.
                var planH = 12.0;
                // Komentarz: Dolna krawędź pola „TYTUŁ” to 'afterTitleTop'; ustawiamy nowe pole tuż pod nim.
                var planY = afterTitleTop - 2.0 - planH;
                // Komentarz: Rysujemy ramkę na napis „PLAN SYTUACYJNY”.
                var planRect = MakeRectLW(new Point2d(tbX + 1.5, planY), tbW - 3.0, planH);
                // Komentarz: Dodajemy ramkę do rysunku.
                btr.AppendEntity(planRect); tr.AddNewlyCreatedDBObject(planRect, true);
                // Komentarz: Tworzymy MText z wyrównaniem do środka (AttachmentPoint.MiddleCenter).
                var planText = new MText
                {
                    // Komentarz: Ustawiamy pozycję w geometrycznym środku pola.
                    Location = new Point3d(tbX + (tbW / 2.0), planY + planH / 2.0, 0),
                    // Komentarz: Szerokość – na tyle duża, żeby działało wyśrodkowanie (używana tylko informacyjnie).
                    Width = tbW - 6.0,
                    // Komentarz: Wysokość znaków – większa, jak w wzorze.
                    TextHeight = BigTitleHeight,
                    // Komentarz: Treść napisu.
                    Contents = "PLAN SYTUACYJNY",
                    // Komentarz: Wyrównanie – środek.
                    Attachment = AttachmentPoint.MiddleCenter
                };
                // Komentarz: Dodajemy MText do rysunku.
                btr.AppendEntity(planText); tr.AddNewlyCreatedDBObject(planText, true);

                // Komentarz: Wyznaczamy górę tabeli podpisów (3 wiersze) – tuż pod „PLAN SYTUACYJNY”.
                var tableTop = planY - 2.0;
                // Komentarz: Wysokość jednego wiersza tabeli podpisów.
                var rowH = 10.0;
                // Komentarz: Szerokości 3 kolumn – etykieta, nazwisko/imie, data.
                var col1W = tbW * 0.25;
                var col2W = tbW * 0.55;
                var col3W = tbW - col1W - col2W - 3.0;

                // Komentarz: Funkcja lokalna rysująca jeden wiersz tabeli podpisów.
                void SignRow(string label, string value, string date, double topY)
                {
                    // Komentarz: Rysujemy 3 przyległe prostokąty (kolumny).
                    var r1 = MakeRectLW(new Point2d(tbX + 1.5, topY - rowH), col1W, rowH);
                    var r2 = MakeRectLW(new Point2d(tbX + 1.5 + col1W, topY - rowH), col2W, rowH);
                    var r3 = MakeRectLW(new Point2d(tbX + 1.5 + col1W + col2W, topY - rowH), col3W, rowH);
                    // Komentarz: Dodajemy je do rysunku.
                    btr.AppendEntity(r1); tr.AddNewlyCreatedDBObject(r1, true);
                    btr.AppendEntity(r2); tr.AddNewlyCreatedDBObject(r2, true);
                    btr.AppendEntity(r3); tr.AddNewlyCreatedDBObject(r3, true);

                    // Komentarz: Dodajemy teksty do każdej kolumny (z marginesem ~1.5).
                    var t1 = new DBText { Position = new Point3d(tbX + 1.5 + 1.5, (topY - rowH) + (rowH - CaptionHeight) / 2.5, 0), Height = CaptionHeight, TextString = label };
                    var t2 = new DBText { Position = new Point3d(tbX + 1.5 + col1W + 1.5, (topY - rowH) + (rowH - TextHeight) / 2.5, 0), Height = TextHeight, TextString = string.IsNullOrWhiteSpace(value) ? "-" : value };
                    var t3 = new DBText { Position = new Point3d(tbX + 1.5 + col1W + col2W + 1.5, (topY - rowH) + (rowH - TextHeight) / 2.5, 0), Height = TextHeight, TextString = string.IsNullOrWhiteSpace(date) ? "-" : date };
                    // Komentarz: Dodajemy napisy do rysunku.
                    btr.AppendEntity(t1); tr.AddNewlyCreatedDBObject(t1, true);
                    btr.AppendEntity(t2); tr.AddNewlyCreatedDBObject(t2, true);
                    btr.AppendEntity(t3); tr.AddNewlyCreatedDBObject(t3, true);
                }

                // Komentarz: Rysujemy 3 wiersze podpisów (wszystkie z tą samą datą z formularza).
                SignRow("OPRACOWAŁ(A)", data.Opracowujacy, data.Data, tableTop);
                SignRow("PROJEKTANT", data.Projektant, data.Data, tableTop - rowH);
                SignRow("SPRAWDZAJĄCY", data.Sprawdzajacy, data.Data, tableTop - 2 * rowH);

                // Komentarz: Pasek na dole metryczki – SKALA (lewa) / NR RYS. (prawa).
                var bottomBarH = 10.0;
                // Komentarz: Rysujemy prostokąt całego paska.
                var barRect = MakeRectLW(new Point2d(tbX + 1.5, tbY + 1.5), tbW - 3.0, bottomBarH);
                // Komentarz: Dodajemy pasek.
                btr.AppendEntity(barRect); tr.AddNewlyCreatedDBObject(barRect, true);
                // Komentarz: Dzielimy pasek na dwie połowy (lewa/prawa).
                var half = (tbW - 3.0) / 2.0;
                // Komentarz: Lewa połówka – SKALA.
                var skRect = MakeRectLW(new Point2d(tbX + 1.5, tbY + 1.5), half, bottomBarH);
                // Komentarz: Prawa połówka – NR RYS.
                var nrRect = MakeRectLW(new Point2d(tbX + 1.5 + half, tbY + 1.5), half, bottomBarH);
                // Komentarz: Dodajemy obie połowy do rysunku.
                btr.AppendEntity(skRect); tr.AddNewlyCreatedDBObject(skRect, true);
                btr.AppendEntity(nrRect); tr.AddNewlyCreatedDBObject(nrRect, true);
                // Komentarz: Napis po lewej – „SKALA: …”.
                var tSk = new DBText
                {
                    // Komentarz: Pozycja tekstu – wewnątrz lewego pola z lekkim marginesem.
                    Position = new Point3d(tbX + 1.5 + 1.5, tbY + 1.5 + (bottomBarH - CaptionHeight) / 2.5, 0),
                    // Komentarz: Wysokość czcionki – etykieta.
                    Height = CaptionHeight,
                    // Komentarz: Treść wraz z wartością skali.
                    TextString = $"SKALA: {(string.IsNullOrWhiteSpace(data.Skala) ? "-" : data.Skala)}"
                };
                // Komentarz: Napis po prawej – „NR RYS.: …”.
                var tNr = new DBText
                {
                    // Komentarz: Pozycja tekstu – wewnątrz prawego pola z marginesem.
                    Position = new Point3d(tbX + 1.5 + half + 1.5, tbY + 1.5 + (bottomBarH - CaptionHeight) / 2.5, 0),
                    // Komentarz: Wysokość czcionki – etykieta.
                    Height = CaptionHeight,
                    // Komentarz: Treść z numerem rysunku.
                    TextString = $"NR RYS.: {(string.IsNullOrWhiteSpace(data.NumerRysunku) ? "-" : data.NumerRysunku)}"
                };
                // Komentarz: Dodajemy oba napisy do rysunku.
                btr.AppendEntity(tSk); tr.AddNewlyCreatedDBObject(tSk, true);
                btr.AppendEntity(tNr); tr.AddNewlyCreatedDBObject(tNr, true);

                // Komentarz: Zatwierdzamy wszystkie dodane obiekty.
                tr.Commit();
            }

            // Komentarz: Informujemy użytkownika w konsoli o zakończeniu komendy.
            ed.WriteMessage("\nLegenda + metryczka zostały narysowane (układ pionowy).");
        }

        // ==========================================
        // Komentarz: POMOCNICZE – warstwy i rysowanie
        // ==========================================

        // Komentarz: Pobiera listę dostępnych warstw (nazwa + kolor) do formularza wyboru.
        private List<LayerInfo> GetLayerInfos()
        {
            // Komentarz: Przygotowujemy pustą listę.
            var list = new List<LayerInfo>();
            // Komentarz: Bierzemy bazę danych bieżącego dokumentu.
            var db = AcApp.DocumentManager.MdiActiveDocument.Database;
            // Komentarz: Startujemy transakcję odczytową.
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Komentarz: Otwieramy tabelę warstw do odczytu.
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                // Komentarz: Iterujemy po wszystkich identyfikatorach warstw.
                foreach (ObjectId id in lt)
                {
                    // Komentarz: Otwieramy rekord warstwy.
                    var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    // Komentarz: Pomijamy warstwy zależne (np. XREF), by nie zaśmiecać listy.
                    if (ltr.IsDependent) continue;
                    // Komentarz: Dodajemy warstwę do listy (nazwa + kolor AutoCAD).
                    list.Add(new LayerInfo { Name = ltr.Name, Color = ltr.Color });
                }
                // Komentarz: Kończymy transakcję odczytu.
                tr.Commit();
            }
            // Komentarz: Sortujemy alfabetycznie i zwracamy.
            return list.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        // Komentarz: Prosty pojemnik na informacje o warstwie (nazwa + kolor AutoCAD).
        public class LayerInfo
        {
            // Komentarz: Nazwa warstwy (wyświetlana na liście i w legendzie).
            public string Name { get; set; }
            // Komentarz: Kolor warstwy (typ AutoCAD AcColor – unika konfliktu z System.Drawing.Color).
            public AcColor Color { get; set; }
            // Komentarz: Dla wygody – ToString zwróci nazwę.
            public override string ToString() => Name;
        }

        // Komentarz: Helper budujący LWPolyline (Polyline) jako prostokąt – używa ściśle Point2d (eliminuje CS1503).
        private Polyline MakeRectLW(Point2d basePt2d, double width, double height)
        {
            // Komentarz: Tworzymy nową LWPolyline.
            var pl = new Polyline();
            // Komentarz: Dodajemy 4 wierzchołki – zgodnie z kolejnością dookoła.
            pl.AddVertexAt(0, new Point2d(basePt2d.X, basePt2d.Y), 0.0, 0.0, 0.0);
            // Komentarz: Prawy-dolny.
            pl.AddVertexAt(1, new Point2d(basePt2d.X + width, basePt2d.Y), 0.0, 0.0, 0.0);
            // Komentarz: Prawy-górny.
            pl.AddVertexAt(2, new Point2d(basePt2d.X + width, basePt2d.Y + height), 0.0, 0.0, 0.0);
            // Komentarz: Lewy-górny.
            pl.AddVertexAt(3, new Point2d(basePt2d.X, basePt2d.Y + height), 0.0, 0.0, 0.0);
            // Komentarz: Zamykanie polilinii.
            pl.Closed = true;
            // Komentarz: Zwracamy gotowy prostokąt.
            return pl;
        }

        // Komentarz: Rysuje „ikonkę” koloru warstwy – kwadrat w kolorze danej warstwy.
        private void DrawColorIcon(BlockTableRecord btr, Transaction tr, Point3d iconBase, string layerName)
        {
            // Komentarz: Pobieramy kolor warstwy według nazwy.
            var color = GetLayerColor(layerName);
            // Komentarz: Tworzymy kwadrat jako LWPolyline (bez wypełnienia – sam obrys w kolorze).
            var sq = MakeRectLW(new Point2d(iconBase.X, iconBase.Y), IconSize, IconSize);
            // Komentarz: Jeśli znaleziono kolor warstwy – przypisz go do kwadratu.
            if (color != null) sq.Color = color;
            // Komentarz: Dodajemy kwadrat do rysunku.
            btr.AppendEntity(sq); tr.AddNewlyCreatedDBObject(sq, true);
        }

        // Komentarz: Zwraca AutoCAD-owy kolor warstwy na podstawie nazwy.
        private AcColor GetLayerColor(string layerName)
        {
            // Komentarz: Pobieramy bieżącą bazę danych.
            var db = AcApp.DocumentManager.MdiActiveDocument.Database;
            // Komentarz: Otwieramy transakcję odczytową.
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Komentarz: Otwieramy tabelę warstw.
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                // Komentarz: Sprawdzamy czy istnieje warstwa o tej nazwie.
                if (lt.Has(layerName))
                {
                    // Komentarz: Pobieramy rekord warstwy.
                    var ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForRead);
                    // Komentarz: Zwracamy jej kolor.
                    return ltr.Color;
                }
                // Komentarz: Jeśli nie znaleziono – zwracamy null.
                return null;
            }
        }

        // Komentarz: Rysuje „etykietowane” pole (ramka + mała etykieta + wartość jako MText).
        // Komentarz: Zwraca Y górnej krawędzi kolejnego obszaru (czyli dolny Y narysowanego pola) – ułatwia układanie pól pod sobą.
        private double DrawLabeledBox(BlockTableRecord btr, Transaction tr,
                                      Point3d baseTopLeft, double width, double height,
                                      string label, string value,
                                      bool centerValue = false, double valueHeight = TextHeight)
        {
            // Komentarz: Wyznaczamy rzeczywisty lewy-dolny punkt prostokąta (bazowy jest lewym-górnym).
            var minX = baseTopLeft.X;
            var minY = baseTopLeft.Y - height;
            // Komentarz: Rysujemy ramkę prostokątną.
            var rect = MakeRectLW(new Point2d(minX, minY), width, height);
            // Komentarz: Dodajemy ramkę do rysunku.
            btr.AppendEntity(rect); tr.AddNewlyCreatedDBObject(rect, true);

            // Komentarz: Tworzymy małą etykietę w lewym górnym rogu pola.
            var lab = new DBText
            {
                // Komentarz: Pozycja etykiety z marginesem 1.5.
                Position = new Point3d(minX + 1.5, minY + height - CaptionHeight - 1.5, 0),
                // Komentarz: Wysokość znaków – mała, jak w legendzie wzorcowej.
                Height = CaptionHeight,
                // Komentarz: Treść etykiety (np. „JEDNOSTKA PROJEKTOWA”).
                TextString = label
            };
            // Komentarz: Dodajemy etykietę do rysunku.
            btr.AppendEntity(lab); tr.AddNewlyCreatedDBObject(lab, true);

            // Komentarz: Tworzymy MText z wartością (pozwala zawijać, lepszy do długich opisów).
            var mt = new MText
            {
                // Komentarz: Jeśli wyśrodkowujemy – ustawimy później Attachment i Location.
                Location = new Point3d(minX + 1.5, minY + 1.5, 0),
                // Komentarz: Szerokość okna MText – prawie cała szerokość pola (zostawiamy 3 mm).
                Width = width - 3.0,
                // Komentarz: Wysokość znaków (domyślnie TextHeight lub większa, jeśli przekazano).
                TextHeight = valueHeight,
                // Komentarz: Treść – jeśli brak, wstawiamy myślnik.
                Contents = string.IsNullOrWhiteSpace(value) ? "-" : value
            };

            // Komentarz: Jeśli mamy wycentrować wartość – zmieniamy wyrównanie i punkt.
            if (centerValue)
            {
                // Komentarz: Ustawiamy wyrównanie do środka.
                mt.Attachment = AttachmentPoint.MiddleCenter;
                // Komentarz: Lokalizujemy MText w środku prostokąta (X i Y).
                mt.Location = new Point3d(minX + width / 2.0, minY + height / 2.0, 0);
            }

            // Komentarz: Dodajemy MText do rysunku.
            btr.AppendEntity(mt); tr.AddNewlyCreatedDBObject(mt, true);

            // Komentarz: Zwracamy Y dolnej krawędzi tego pola – to będzie „górny Y” dla następnego pola niżej.
            return minY;
        }
    }
}
