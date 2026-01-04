// Komentarz: Importujemy biblioteki AutoCAD-a do obsługi bazy danych rysunku.
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
// Komentarz: Importujemy biblioteki do obsługi edytora i interakcji z użytkownikiem.
using Autodesk.AutoCAD.EditorInput;
// Komentarz: Importujemy biblioteki geometrii (punkty, wektory).
using Autodesk.AutoCAD.Geometry;
// Komentarz: Importujemy biblioteki uruchomieniowe AutoCAD-a.
using Autodesk.AutoCAD.Runtime;
// Komentarz: Standardowe biblioteki systemowe .NET.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
// Komentarz: Alias dla aplikacji AutoCAD.
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
// Komentarz: Alias dla kolorów AutoCAD-a.
using AcColor = Autodesk.AutoCAD.Colors.Color;

namespace LegendPlugin
{
    // Komentarz: Klasa danych (bez zmian).
    public class LegendData
    {
        public List<string> SelectedLayers { get; set; } = new List<string>();
        public string JednostkaProjektowa { get; set; }
        public string Inwestor { get; set; }
        public string NazwaAdresObiektu { get; set; }
        public string TytulRysunku { get; set; }
        public string Projektant { get; set; }
        public string Sprawdzajacy { get; set; }
        public string Opracowujacy { get; set; }
        public string Data { get; set; }
        public string Skala { get; set; }
        public string NumerRysunku { get; set; }
    }

    // Komentarz: Główna klasa komendy.
    public class LegendCommand
    {
        // =============================================================
        // STAŁE WYMIAROWE
        // =============================================================

        // Komentarz: Szerokość tabelki (49 mm).
        private const double TotalWidth = 49.0;
        // Komentarz: Całkowita wysokość ramki (141.5 mm).
        private const double TotalHeight = 141.5;

        // Komentarz: Wysokości wierszy metryczki.
        private const double RowHeight_Signatures = 5.5;
        private const double RowHeight_SignHeader = 3.0;
        private const double RowHeight_Title = 5.0;
        private const double RowHeight_Object = 8.5;
        private const double RowHeight_Investor = 8.0;
        private const double RowHeight_Unit = 7.0;
        private const double RowHeight_Footer = 4.0;

        // Komentarz: Stała szerokość pierwszej kolumny (Funkcja/Skala).
        private const double WidthCol_SignFunction = 7.5;

        // =============================================================
        // WIELKOŚCI CZCIONEK
        // =============================================================

        private const double FontH_Label = 0.8;
        private const double FontH_Content = 1.2;
        private const double FontH_Title = 1.5;
        private const double FontH_SigContent = 0.75; // Mała czcionka w tabeli podpisów
        private const double FontH_FooterLabel = 0.7; // Etykiety w stopce

        // Komentarz: Parametry legendy (górnej części).
        private const double LegendRowHeight = 5.0;
        private const double IconSize = 3.0;

        [CommandMethod("LEGENDA")]
        public void RunLegend()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            LegendData data = null;

            // Komentarz: Uruchomienie formularza.
            using (var form = new LegendForm(GetLayerInfos()))
            {
                if (AcApp.ShowModalDialog(form) == DialogResult.OK)
                    data = form.GetData();
                else
                    return;
            }

            var ppr = ed.GetPoint("\nWskaż lewy-dolny narożnik ramki: ");
            if (ppr.Status != PromptStatus.OK) return;
            var basePt = ppr.Value;

            var db = doc.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                // 1. RAMKA ZEWNĘTRZNA
                var frame = new Polyline();
                frame.AddVertexAt(0, new Point2d(basePt.X, basePt.Y), 0, 0, 0);
                frame.AddVertexAt(1, new Point2d(basePt.X + TotalWidth, basePt.Y), 0, 0, 0);
                frame.AddVertexAt(2, new Point2d(basePt.X + TotalWidth, basePt.Y + TotalHeight), 0, 0, 0);
                frame.AddVertexAt(3, new Point2d(basePt.X, basePt.Y + TotalHeight), 0, 0, 0);
                frame.Closed = true;
                btr.AppendEntity(frame); tr.AddNewlyCreatedDBObject(frame, true);

                // 2. METRYCZKA
                double currentY = basePt.Y;
                double leftX = basePt.X;
                double rightX = basePt.X + TotalWidth;

                // --- STOPKA (SKALA, NR RYS, ARKUSZ) ---
                double footerTopY = currentY + RowHeight_Footer;
                DrawLine(btr, tr, leftX, footerTopY, rightX, footerTopY);

                double widthArkusz = 5.0;
                double widthNr = 5.0;
                // Komentarz: Reszta szerokości to przestrzeń Skali (podzielona linią).
                double widthSkalaArea = TotalWidth - widthArkusz - widthNr;

                // Komentarz: Linie pionowe w stopce.
                // 1. Jawna linia z lewej strony (początek).
                DrawLine(btr, tr, leftX, currentY, leftX, footerTopY);

                // 2. Linia oddzielająca "SKALA RYS." (7.5mm) od reszty.
                DrawLine(btr, tr, leftX + WidthCol_SignFunction, currentY, leftX + WidthCol_SignFunction, footerTopY);

                // 3. Linia rozdzielająca obszar Skali od Nr Rys.
                DrawLine(btr, tr, leftX + widthSkalaArea, currentY, leftX + widthSkalaArea, footerTopY);

                // 4. Linia rozdzielająca Nr Rys od Arkusza.
                DrawLine(btr, tr, leftX + widthSkalaArea + widthNr, currentY, leftX + widthSkalaArea + widthNr, footerTopY);

                // Komentarz: Wypełnianie stopki.
                // WAŻNE: Używamy tutaj ostatniego parametru 'margin'. 
                // Ustawiamy go na 0.25 (zamiast domyślnego 1.0), żeby napisy w wąskich kolumnach (5mm) były dobrze wyjustowane i się mieściły.

                // Pole SKALA (etykieta w wąskiej kolumnie 7.5mm, wartość pusta lub obok - tutaj etykieta).
                DrawLabelAndValue(btr, tr, leftX, currentY, WidthCol_SignFunction, RowHeight_Footer, "SKALA RYS.", data.Skala, false, FontH_SigContent, FontH_FooterLabel, 0.25);

                // Pole NR RYS. (kolumna 5mm - mały margines konieczny).
                DrawLabelAndValue(btr, tr, leftX + widthSkalaArea, currentY, widthNr, RowHeight_Footer, "NR RYS.", data.NumerRysunku, false, FontH_SigContent, FontH_FooterLabel, 0.25);

                // Pole ARKUSZ (kolumna 5mm - mały margines konieczny).
                DrawLabelAndValue(btr, tr, leftX + widthSkalaArea + widthNr, currentY, widthArkusz, RowHeight_Footer, "ARKUSZ", "-", false, FontH_SigContent, FontH_FooterLabel, 0.25);

                currentY = footerTopY;

                // --- TABELA PODPISÓW ---
                double col1W = WidthCol_SignFunction;
                double col3W = 10.0;
                double col2W = TotalWidth - col1W - col3W;

                // Komentarz: Funkcja lokalna rysująca wiersz.
                void DrawSigRow(string t1, string t2, string t3, double h, bool isHeader)
                {
                    double top = currentY + h;
                    DrawLine(btr, tr, leftX, top, rightX, top);
                    DrawLine(btr, tr, leftX + col1W, currentY, leftX + col1W, top);
                    DrawLine(btr, tr, leftX + col1W + col2W, currentY, leftX + col1W + col2W, top);

                    DrawCellText(btr, tr, leftX, currentY, col1W, h, t1, isHeader, false);
                    DrawCellText(btr, tr, leftX + col1W, currentY, col2W, h, t2, isHeader, false);
                    DrawCellText(btr, tr, leftX + col1W + col2W, currentY, col3W, h, t3, isHeader, !isHeader);

                    currentY = top;
                }

                DrawSigRow("OPRACOWAŁ", data.Opracowujacy, data.Data, RowHeight_Signatures, false);
                DrawSigRow("SPRAWDZIŁ", data.Sprawdzajacy, data.Data, RowHeight_Signatures, false);
                DrawSigRow("PROJEKTANT", data.Projektant, data.Data, RowHeight_Signatures, false);
                DrawSigRow("FUNKCJA", "IMIĘ I NAZWISKO", "PODPIS", RowHeight_SignHeader, true);

                // --- TYTUŁ RYSUNKU ---
                double titleTopY = currentY + RowHeight_Title;
                DrawLine(btr, tr, leftX, titleTopY, rightX, titleTopY);
                DrawLabelAndValue(btr, tr, leftX, currentY, TotalWidth, RowHeight_Title, "TYTUŁ RYSUNKU", data.TytulRysunku, true, FontH_Title);
                currentY = titleTopY;

                // --- NAZWA OBIEKTU ---
                double objTopY = currentY + RowHeight_Object;
                DrawLine(btr, tr, leftX, objTopY, rightX, objTopY);
                DrawLabelAndValue(btr, tr, leftX, currentY, TotalWidth, RowHeight_Object, "NAZWA I ADRES OBIEKTU BUDOWLANEGO", data.NazwaAdresObiektu, false, FontH_Content);
                currentY = objTopY;

                // --- INWESTOR ---
                double invTopY = currentY + RowHeight_Investor;
                DrawLine(btr, tr, leftX, invTopY, rightX, invTopY);
                DrawLabelAndValue(btr, tr, leftX, currentY, TotalWidth, RowHeight_Investor, "INWESTOR", data.Inwestor, false, FontH_Content);
                currentY = invTopY;

                // --- JEDNOSTKA PROJEKTOWA ---
                double unitTopY = currentY + RowHeight_Unit;
                DrawLine(btr, tr, leftX, unitTopY, rightX, unitTopY);
                DrawLabelAndValue(btr, tr, leftX, currentY, TotalWidth, RowHeight_Unit, "JEDNOSTKA PROJEKTOWA", data.JednostkaProjektowa, false, FontH_Content);

                // Komentarz: >>> USUNIĘTO SEKCJĘ Z LOGO "VA" ZGODNIE Z PROŚBĄ <<<

                currentY = unitTopY;

                // 3. LEGENDA
                double legendTopY = basePt.Y + TotalHeight;
                double legendX = basePt.X + 2.5;

                // Komentarz: Nagłówek "LEGENDA:".
                var headerTxt = new DBText();
                headerTxt.Position = new Point3d(legendX, legendTopY - 3.5, 0);
                headerTxt.Height = 2.0;
                headerTxt.TextString = "LEGENDA:";
                btr.AppendEntity(headerTxt); tr.AddNewlyCreatedDBObject(headerTxt, true);

                // Komentarz: NOWOŚĆ - Pozioma linia pod słowem LEGENDA.
                // Linia na wysokości legendTopY - 5.0 (lekko pod tekstem). Długość ok. 25mm.
                double lineY = legendTopY - 5.0;
                DrawLine(btr, tr, legendX, lineY, legendX + 25.0, lineY);

                // Komentarz: Przesuwamy start rysowania elementów legendy niżej (pod linię).
                double rowY = legendTopY - 8.0;

                foreach (var layerName in data.SelectedLayers)
                {
                    if (rowY < currentY + 2.0) break;

                    // Ikonka
                    var icon = new Polyline();
                    double icL = legendX;
                    double icB = rowY + 1.0;
                    icon.AddVertexAt(0, new Point2d(icL, icB), 0, 0, 0);
                    icon.AddVertexAt(1, new Point2d(icL + IconSize, icB), 0, 0, 0);
                    icon.AddVertexAt(2, new Point2d(icL + IconSize, icB + IconSize), 0, 0, 0);
                    icon.AddVertexAt(3, new Point2d(icL, icB + IconSize), 0, 0, 0);
                    icon.Closed = true;

                    var layerColor = GetLayerColor(layerName);
                    if (layerColor != null) icon.Color = layerColor;

                    btr.AppendEntity(icon); tr.AddNewlyCreatedDBObject(icon, true);

                    // Opis
                    var descTxt = new DBText();
                    descTxt.Position = new Point3d(legendX + IconSize + 2.0, rowY + 1.5, 0);
                    descTxt.Height = FontH_Content;
                    descTxt.TextString = layerName;

                    btr.AppendEntity(descTxt); tr.AddNewlyCreatedDBObject(descTxt, true);

                    rowY -= LegendRowHeight;
                }

                tr.Commit();
            }
            ed.WriteMessage("\nWygenerowano legendę (usunięto VA, wyjustowano stopkę, linia pod nagłówkiem).");
        }

        // =============================================================
        // METODY POMOCNICZE
        // =============================================================

        private void DrawLine(BlockTableRecord btr, Transaction tr, double x1, double y1, double x2, double y2)
        {
            var line = new Line(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0));
            btr.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);
        }

        // Komentarz: Zmodyfikowana metoda - dodano parametr 'margin' z wartością domyślną 1.0.
        // Dzięki temu dla stopki możemy podać mniejszy margines (np. 0.25), żeby tekst się mieścił.
        private void DrawLabelAndValue(BlockTableRecord btr, Transaction tr, double x, double bottomY, double w, double h, string label, string value, bool centerValue, double customH, double customLabelH = FontH_Label, double margin = 1.0)
        {
            double topY = bottomY + h;

            // Etykieta
            var lbl = new DBText();
            // Komentarz: Używamy przekazanego marginesu do pozycjonowania X.
            lbl.Position = new Point3d(x + margin, topY - customLabelH - 0.5, 0);
            lbl.Height = customLabelH;
            lbl.TextString = label;
            lbl.ColorIndex = 8;
            btr.AppendEntity(lbl); tr.AddNewlyCreatedDBObject(lbl, true);

            // Wartość
            var mt = new MText();
            mt.Contents = string.IsNullOrWhiteSpace(value) ? "-" : value;
            mt.TextHeight = customH;
            mt.Width = w - (2 * margin);

            if (centerValue)
            {
                mt.Attachment = AttachmentPoint.MiddleCenter;
                mt.Location = new Point3d(x + w / 2.0, bottomY + h / 2.0, 0);
            }
            else
            {
                mt.Attachment = AttachmentPoint.TopLeft;
                // Komentarz: Przesuwamy wartość poniżej etykiety.
                mt.Location = new Point3d(x + margin, topY - (customLabelH + 1.2), 0);
            }

            btr.AppendEntity(mt); tr.AddNewlyCreatedDBObject(mt, true);
        }

        private void DrawCellText(BlockTableRecord btr, Transaction tr, double x, double bottomY, double w, double h, string text, bool isHeader, bool alignBottom)
        {
            var mt = new MText();
            mt.Contents = text;
            mt.TextHeight = isHeader ? FontH_Label : FontH_SigContent;
            mt.Width = w - 0.5;

            // Komentarz: Logika pozycjonowania.
            if (alignBottom)
            {
                mt.Attachment = AttachmentPoint.BottomCenter;
                mt.Location = new Point3d(x + w / 2.0, bottomY + 0.5, 0);
            }
            else
            {
                mt.Attachment = AttachmentPoint.MiddleCenter;
                mt.Location = new Point3d(x + w / 2.0, bottomY + h / 2.0, 0);
            }

            if (isHeader) mt.ColorIndex = 8;

            btr.AppendEntity(mt); tr.AddNewlyCreatedDBObject(mt, true);
        }

        private AcColor GetLayerColor(string layerName)
        {
            var db = AcApp.DocumentManager.MdiActiveDocument.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (lt.Has(layerName))
                {
                    var ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForRead);
                    return ltr.Color;
                }
            }
            return null;
        }

        private List<LayerInfo> GetLayerInfos()
        {
            var list = new List<LayerInfo>();
            var db = AcApp.DocumentManager.MdiActiveDocument.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId id in lt)
                {
                    var ltr = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                    if (!ltr.IsDependent)
                        list.Add(new LayerInfo { Name = ltr.Name, Color = ltr.Color });
                }
                tr.Commit();
            }
            return list.OrderBy(x => x.Name).ToList();
        }

        public class LayerInfo
        {
            public string Name { get; set; }
            public AcColor Color { get; set; }
            public override string ToString() => Name;
        }
    }
}