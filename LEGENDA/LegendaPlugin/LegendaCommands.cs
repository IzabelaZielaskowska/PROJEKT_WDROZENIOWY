using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcColor = Autodesk.AutoCAD.Colors.Color;

namespace LegendPlugin
{
    public class LegendData
    {
        public List<string> SelectedLayers { get; set; } = new List<string>();
        public List<LegendCommand.LayerInfo> SelectedLayersInfo { get; set; } = new List<LegendCommand.LayerInfo>();
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

    public class LegendCommand
    {
        private const double TotalWidth = 49.0;
        private const double TotalHeight = 141.5;
        private const double RowHeight_Signatures = 5.5;
        private const double RowHeight_SignHeader = 3.0;
        private const double RowHeight_Title = 5.0;
        private const double RowHeight_Object = 8.5;
        private const double RowHeight_Investor = 8.0;
        private const double RowHeight_Unit = 7.0;
        private const double RowHeight_Footer = 4.0;
        private const double WidthCol_SignFunction = 7.5;

        private const double FontH_Label = 0.8;
        private const double FontH_Content = 1.2;
        private const double FontH_Title = 1.5;
        private const double FontH_SigContent = 0.75;
        private const double FontH_FooterLabel = 0.7;

        private const double LegendRowHeight = 5.5;
        private const double IconSize = 4.0;

        [CommandMethod("LEGENDA")]
        public void RunLegend()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            LegendData data = null;

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

                double currentY = basePt.Y;
                double leftX = basePt.X;
                double rightX = basePt.X + TotalWidth;

                double footerTopY = currentY + RowHeight_Footer;
                DrawLine(btr, tr, leftX, footerTopY, rightX, footerTopY);
                double widthArkusz = 5.0; double widthNr = 5.0;
                double widthSkalaArea = TotalWidth - widthArkusz - widthNr;
                DrawLine(btr, tr, leftX, currentY, leftX, footerTopY);
                DrawLine(btr, tr, leftX + WidthCol_SignFunction, currentY, leftX + WidthCol_SignFunction, footerTopY);
                DrawLine(btr, tr, leftX + widthSkalaArea, currentY, leftX + widthSkalaArea, footerTopY);
                DrawLine(btr, tr, leftX + widthSkalaArea + widthNr, currentY, leftX + widthSkalaArea + widthNr, footerTopY);
                DrawLabelAndValue(btr, tr, leftX, currentY, WidthCol_SignFunction, RowHeight_Footer, "SKALA RYS.", data.Skala, true, FontH_SigContent, FontH_FooterLabel, 0.25);
                DrawLabelAndValue(btr, tr, leftX + widthSkalaArea, currentY, widthNr, RowHeight_Footer, "NR RYS.", data.NumerRysunku, true, FontH_SigContent, FontH_FooterLabel, 0.25);
                DrawLabelAndValue(btr, tr, leftX + widthSkalaArea + widthNr, currentY, widthArkusz, RowHeight_Footer, "ARKUSZ", "", true, FontH_SigContent, FontH_FooterLabel, 0.25);
                currentY = footerTopY;

                double col1W = WidthCol_SignFunction; double col3W = 10.0; double col2W = TotalWidth - col1W - col3W;
                void DrawSigRow(string t1, string t2, string t3, double h, bool isHeader, bool isSign)
                {
                    double top = currentY + h;
                    DrawLine(btr, tr, leftX, top, rightX, top);
                    DrawLine(btr, tr, leftX + col1W, currentY, leftX + col1W, top);
                    DrawLine(btr, tr, leftX + col1W + col2W, currentY, leftX + col1W + col2W, top);
                    DrawCellText(btr, tr, leftX, currentY, col1W, h, t1, isHeader, !isSign, false);
                    DrawCellText(btr, tr, leftX + col1W, currentY, col2W, h, t2, isHeader, isSign, false);
                    DrawCellText(btr, tr, leftX + col1W + col2W, currentY, col3W, h, t3, isHeader, !isSign, !isHeader);
                    currentY = top;
                }
                DrawSigRow("OPRACOWAŁ", data.Opracowujacy, data.Data, RowHeight_Signatures, false, true);
                DrawSigRow("SPRAWDZIŁ", data.Sprawdzajacy, data.Data, RowHeight_Signatures, false, true);
                DrawSigRow("PROJEKTANT", data.Projektant, data.Data, RowHeight_Signatures, false, true);
                DrawSigRow("FUNKCJA", "IMIĘ I NAZWISKO", "PODPIS", RowHeight_SignHeader, true, false);

                double titleTopY = currentY + RowHeight_Title;
                DrawLine(btr, tr, leftX, titleTopY, rightX, titleTopY);
                DrawLabelAndValue(btr, tr, leftX, currentY, TotalWidth, RowHeight_Title, "TYTUŁ RYSUNKU", data.TytulRysunku, true, FontH_Title);
                currentY = titleTopY;

                double objTopY = currentY + RowHeight_Object;
                DrawLine(btr, tr, leftX, objTopY, rightX, objTopY);
                DrawLabelAndValue(btr, tr, leftX, currentY, TotalWidth, RowHeight_Object, "NAZWA I ADRES OBIEKTU BUDOWLANEGO", data.NazwaAdresObiektu, true, FontH_Content);
                currentY = objTopY;

                double invTopY = currentY + RowHeight_Investor;
                DrawLine(btr, tr, leftX, invTopY, rightX, invTopY);
                DrawLabelAndValue(btr, tr, leftX, currentY, TotalWidth, RowHeight_Investor, "INWESTOR", data.Inwestor, false, FontH_Content);
                currentY = invTopY;

                double unitTopY = currentY + RowHeight_Unit;
                DrawLine(btr, tr, leftX, unitTopY, rightX, unitTopY);
                DrawLabelAndValue(btr, tr, leftX, currentY, TotalWidth, RowHeight_Unit, "JEDNOSTKA PROJEKTOWA", data.JednostkaProjektowa, false, FontH_Content);
                currentY = unitTopY;

                double legendTopY = basePt.Y + TotalHeight;
                int columnIndex = 0;
                double limitY = currentY;

                void DrawColumnFrame(double x)
                {
                    var poly = new Polyline();
                    poly.AddVertexAt(0, new Point2d(x, basePt.Y), 0, 0, 0);
                    poly.AddVertexAt(1, new Point2d(x + TotalWidth, basePt.Y), 0, 0, 0);
                    poly.AddVertexAt(2, new Point2d(x + TotalWidth, legendTopY), 0, 0, 0);
                    poly.AddVertexAt(3, new Point2d(x, legendTopY), 0, 0, 0);
                    poly.Closed = true;
                    btr.AppendEntity(poly); tr.AddNewlyCreatedDBObject(poly, true);
                }

                DrawColumnFrame(basePt.X);
                double currentLegendX = basePt.X + 2.5;

                var headerTxt = new DBText();
                headerTxt.Position = new Point3d(currentLegendX, legendTopY - 3.5, 0);
                headerTxt.Height = 2.0; headerTxt.TextString = "LEGENDA:";
                btr.AppendEntity(headerTxt); tr.AddNewlyCreatedDBObject(headerTxt, true);

                DrawLine(btr, tr, basePt.X, legendTopY - 5.0, basePt.X + TotalWidth, legendTopY - 5.0);

                double rowY = legendTopY - 11.0;

                foreach (var info in data.SelectedLayersInfo)
                {
                    if (rowY < limitY + 2.0)
                    {
                        columnIndex++;
                        double nextX = basePt.X + (columnIndex * TotalWidth);
                        DrawColumnFrame(nextX);

                        limitY = basePt.Y + 2.0;
                        currentLegendX = nextX + 2.5;
                        rowY = legendTopY - 5.0;
                    }

                    AcColor layerColor = AcColor.FromColorIndex(ColorMethod.ByAci, 7);
                    ObjectId ltId = ObjectId.Null; LineWeight lw = LineWeight.ByLineWeightDefault;
                    Transparency trans = new Transparency((byte)255);

                    using (var trL = db.TransactionManager.StartTransaction())
                    {
                        var lt = (LayerTable)trL.GetObject(db.LayerTableId, OpenMode.ForRead);
                        if (lt.Has(info.Name))
                        {
                            var ltr = (LayerTableRecord)trL.GetObject(lt[info.Name], OpenMode.ForRead);
                            layerColor = ltr.Color; ltId = ltr.LinetypeObjectId; lw = ltr.LineWeight; trans = ltr.Transparency;
                        }
                        trL.Commit();
                    }

                    double icL = currentLegendX; double icMidY = rowY + 1.0 + (IconSize / 2.0);

                    if (info.IsHatch)
                    {
                        var loop = new Polyline();
                        loop.AddVertexAt(0, new Point2d(icL, rowY + 1.0), 0, 0, 0);
                        loop.AddVertexAt(1, new Point2d(icL + IconSize, rowY + 1.0), 0, 0, 0);
                        loop.AddVertexAt(2, new Point2d(icL + IconSize, rowY + 1.0 + IconSize), 0, 0, 0);
                        loop.AddVertexAt(3, new Point2d(icL, rowY + 1.0 + IconSize), 0, 0, 0);
                        loop.Closed = true; loop.Color = layerColor; loop.Transparency = trans;
                        btr.AppendEntity(loop); tr.AddNewlyCreatedDBObject(loop, true);
                        var h = new Hatch(); h.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
                        h.Color = layerColor; h.Transparency = trans;
                        btr.AppendEntity(h); tr.AddNewlyCreatedDBObject(h, true);
                        h.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection { loop.ObjectId }); h.EvaluateHatch(true);
                    }
                    else
                    {
                        var ln = new Line(new Point3d(icL, icMidY, 0), new Point3d(icL + IconSize, icMidY, 0));
                        ln.Color = layerColor; ln.LineWeight = lw; ln.Transparency = trans;
                        if (ltId != ObjectId.Null) ln.LinetypeId = ltId;
                        btr.AppendEntity(ln); tr.AddNewlyCreatedDBObject(ln, true);
                    }

                    var mt = new MText();
                    mt.Contents = info.Name; mt.TextHeight = FontH_Content;
                    mt.Width = TotalWidth - IconSize - 6.0;
                    mt.Attachment = AttachmentPoint.MiddleLeft;
                    mt.Location = new Point3d(currentLegendX + IconSize + 2.0, icMidY, 0);
                    btr.AppendEntity(mt); tr.AddNewlyCreatedDBObject(mt, true);

                    rowY -= Math.Max(LegendRowHeight, mt.ActualHeight + 1.0);
                }
                tr.Commit();
            }
            ed.WriteMessage("\nLegenda wygenerowana z kreską tylko w pierwszej kolumnie.");
        }

        private void DrawLine(BlockTableRecord btr, Transaction tr, double x1, double y1, double x2, double y2)
        {
            var line = new Line(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0));
            btr.AppendEntity(line); tr.AddNewlyCreatedDBObject(line, true);
        }
        private void DrawLabelAndValue(BlockTableRecord btr, Transaction tr, double x, double bottomY, double w, double h, string label, string value, bool centerValue, double customH, double customLabelH = FontH_Label, double margin = 1.0)
        {
            double topY = bottomY + h;
            var lbl = new DBText(); lbl.Position = new Point3d(x + margin, topY - customLabelH - 0.5, 0);
            lbl.Height = customLabelH; lbl.TextString = label; lbl.ColorIndex = 8;
            btr.AppendEntity(lbl); tr.AddNewlyCreatedDBObject(lbl, true);
            var mt = new MText(); mt.Contents = value; mt.TextHeight = customH; mt.Width = w - (2 * margin);
            if (centerValue) { mt.Attachment = AttachmentPoint.MiddleCenter; mt.Location = new Point3d(x + w / 2.0, (bottomY + h / 2.0) - 0.5, 0); }
            else { mt.Attachment = AttachmentPoint.TopLeft; mt.Location = new Point3d(x + margin, topY - (customLabelH + 1.2), 0); }
            btr.AppendEntity(mt); tr.AddNewlyCreatedDBObject(mt, true);
        }
        private void DrawCellText(BlockTableRecord btr, Transaction tr, double x, double bottomY, double w, double h, string text, bool isHeader, bool isSign, bool alignBottom)
        {
            var mt = new MText(); mt.Contents = text; mt.TextHeight = isHeader ? FontH_Label : FontH_SigContent; mt.Width = w - 0.5;
            if (alignBottom) { mt.Attachment = AttachmentPoint.BottomCenter; mt.Location = new Point3d(x + w / 2.0, bottomY + 0.5, 0); }
            else { mt.Attachment = AttachmentPoint.MiddleCenter; mt.Location = new Point3d(x + w / 2.0, bottomY + h / 2.0, 0); }
            if (isHeader) mt.ColorIndex = 8; if (isSign) mt.TextHeight = 0.8;
            btr.AppendEntity(mt); tr.AddNewlyCreatedDBObject(mt, true);
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
                    if (!ltr.IsDependent) list.Add(new LayerInfo { Name = ltr.Name, Color = ltr.Color });
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
            public bool IsHatch { get; set; }
        }
    }
}