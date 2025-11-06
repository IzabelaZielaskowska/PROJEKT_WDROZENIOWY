// Komentarz: .NET/WinForms.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// Komentarz: >>> Aliasujemy typy koloru, aby uniknąć konfliktu nazw.
using SDColor = System.Drawing.Color;                  // Komentarz: Kolor do rysowania w WinForms.
using AcColor = Autodesk.AutoCAD.Colors.Color;        // Komentarz: Kolor AutoCAD-a (warstwy).

namespace LegendPlugin
{
    public class LegendForm : Form
    {
        // Komentarz: Kontrolki.
        private CheckedListBox clbLayers;
        private TextBox tbJednostka, tbInwestor, tbObiekt, tbTytul, tbProjektant, tbSprawdzajacy, tbOpracowujacy, tbSkala, tbNrRys;
        DateTimePicker dtpData;
        private Button btnOk, btnCancel;

        // Komentarz: Warstwy wejściowe (z kolorem AutoCAD).
        private readonly List<LegendCommand.LayerInfo> _layers;

        // Komentarz: Konstruktor – przekazujemy listę warstw.
        public LegendForm(List<LegendCommand.LayerInfo> layers)
        {
            _layers = layers ?? new List<LegendCommand.LayerInfo>();
            this.Text = "Legenda – wybór warstw i dane metryczki";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(900, 620);
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            BuildUi();
            LoadLayers();
        }

        // Komentarz: Budowa interfejsu.
        private void BuildUi()
        {
            var grpLayers = new GroupBox { Text = "Warstwy do legendy (z ikoną koloru)", Left = 10, Top = 10, Width = 430, Height = 520 };

            clbLayers = new CheckedListBox
            {
                Left = 10,
                Top = 20,
                Width = 410,
                Height = 490,
                DrawMode = DrawMode.OwnerDrawFixed,
                IntegralHeight = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            clbLayers.DrawItem += ClbLayers_DrawItem;
            grpLayers.Controls.Add(clbLayers);

            var grpMeta = new GroupBox { Text = "Dane metryczki", Left = 450, Top = 10, Width = 430, Height = 520 };

            Label L(string t, int y) => new Label { Text = t, Left = 10, Top = y, Width = 200 };
            TextBox T(int y, bool wide = true) => new TextBox { Left = 10, Top = y, Width = wide ? 400 : 180 };

            int y0 = 25;
            grpMeta.Controls.Add(L("TYTUŁ RYSUNKU:", y0)); tbTytul = T(y0 + 18); grpMeta.Controls.Add(tbTytul);
            grpMeta.Controls.Add(L("JEDNOSTKA PROJEKTOWA:", y0 + 50)); tbJednostka = T(y0 + 78); grpMeta.Controls.Add(tbJednostka);
            grpMeta.Controls.Add(L("INWESTOR:", y0 + 110)); tbInwestor = T(y0 + 138); grpMeta.Controls.Add(tbInwestor);
            grpMeta.Controls.Add(L("NAZWA I ADRES OBIEKTU:", y0 + 170)); tbObiekt = T(y0 + 198); grpMeta.Controls.Add(tbObiekt);
            grpMeta.Controls.Add(L("PROJEKTANT:", y0 + 230)); tbProjektant = T(y0 + 258); grpMeta.Controls.Add(tbProjektant);
            grpMeta.Controls.Add(L("SPRAWDZAJĄCY:", y0 + 290)); tbSprawdzajacy = T(y0 + 318); grpMeta.Controls.Add(tbSprawdzajacy);
            grpMeta.Controls.Add(L("OPRACOWAŁ(A):", y0 + 350)); tbOpracowujacy = T(y0 + 378); grpMeta.Controls.Add(tbOpracowujacy);
            grpMeta.Controls.Add(L("DATA:", y0 + 410)); 
            var dtpData = new DateTimePicker
            {
                Left = 10,
                Top = y0 + 438,
                Width = 180,
                Format = DateTimePickerFormat.Short,  // format: dd.MM.yyyy
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            grpMeta.Controls.Add(dtpData);
            grpMeta.Controls.Add(L("SKALA:", y0 + 410)); tbSkala = new TextBox { Left = 210, Top = y0 + 438, Width = 90 }; grpMeta.Controls.Add(tbSkala);
            grpMeta.Controls.Add(L("NR RYS.:", y0 + 410)); tbNrRys = new TextBox { Left = 310, Top = y0 + 438, Width = 100 }; grpMeta.Controls.Add(tbNrRys);

            btnOk = new Button { Text = "OK", Left = 640, Top = 540, Width = 110, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Anuluj", Left = 760, Top = 540, Width = 110, DialogResult = DialogResult.Cancel };

            this.Controls.Add(grpLayers);
            this.Controls.Add(grpMeta);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        // Komentarz: Załadowanie listy warstw do CheckedListBox.
        private void LoadLayers()
        {
            clbLayers.Items.Clear();
            foreach (var l in _layers.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
                clbLayers.Items.Add(l, false);
        }

        // Komentarz: Rysujemy wiersz listy z ikonką koloru (System.Drawing).
        private void ClbLayers_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var item = clbLayers.Items[e.Index] as LegendCommand.LayerInfo;
            e.DrawBackground();

            var rect = new Rectangle(e.Bounds.Left + 22, e.Bounds.Top + 4, 18, e.Bounds.Height - 8);

            // Komentarz: Bazowy kolor: szary; potem podmienimy na kolor z AutoCAD.
            SDColor color = SDColor.Gray;

            // Komentarz: Jeżeli LayerInfo ma ustawiony AutoCAD-owy kolor (AcColor) – używamy jego wartości RGB.
            if (item?.Color != null)
            {
                // Komentarz: AcColor.ColorValue to już System.Drawing.Color – używamy bezpośrednio.
                color = item.Color.ColorValue;
            }

            using (var br = new SolidBrush(color))
            using (var pen = new Pen(SDColor.Black))
            {
                e.Graphics.FillRectangle(br, rect);
                e.Graphics.DrawRectangle(pen, rect);
            }

            var textX = rect.Right + 8;
            TextRenderer.DrawText(e.Graphics, item?.Name ?? "(warstwa)", e.Font, new Point(textX, e.Bounds.Top + 4), e.ForeColor);

            e.DrawFocusRectangle();
        }

        // Komentarz: Zbiór danych z formularza do obiektu LegendData.
        public LegendData GetData()
        {
            var d = new LegendData
            {
                SelectedLayers = clbLayers.CheckedItems.Cast<LegendCommand.LayerInfo>().Select(x => x.Name).ToList(),
                JednostkaProjektowa = tbJednostka.Text,
                Inwestor = tbInwestor.Text,
                NazwaAdresObiektu = tbObiekt.Text,
                TytulRysunku = tbTytul.Text,
                Projektant = tbProjektant.Text,
                Sprawdzajacy = tbSprawdzajacy.Text,
                Opracowujacy = tbOpracowujacy.Text,
                Data = dtpData.Value.ToShortDateString(),
                Skala = tbSkala.Text,
                NumerRysunku = tbNrRys.Text
            };
            return d;

        }
    }
}
