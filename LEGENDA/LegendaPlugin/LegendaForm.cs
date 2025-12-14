// Komentarz: .NET/WinForms.
using Autodesk.AutoCAD.DatabaseServices;
using LegendPlugin;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AcColor = Autodesk.AutoCAD.Colors.Color;        // Komentarz: Kolor AutoCAD-a (warstwy).
// Komentarz: >>> Aliasujemy typy koloru, aby uniknąć konfliktu nazw.
using SDColor = System.Drawing.Color;                  // Komentarz: Kolor do rysowania w WinForms.

namespace LegendPlugin
{
    public class LegendForm : Form
    {
        // Komentarz: Kontrolki.
        private CheckedListBox clbLayers;
        private TextBox tbJednostka, tbInwestor, tbObiekt, tbTytul, tbSkala, tbNrRys;
        private ComboBox cbProjektant, cbSprawdzajacy, cbOpracowujacy;  
        private LegendPlugin. CustomDatePicker dtpData;
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
            LoadPersonLists();
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
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true
            };
            clbLayers.DrawItem += ClbLayers_DrawItem;
            grpLayers.Controls.Add(clbLayers);

            var grpMeta = new GroupBox { Text = "Dane metryczki", Left = 450, Top = 10, Width = 430, Height = 520 };

            Label L(string t, int y) => new Label { Text = t, Left = 10, Top = y, Width = 200 };
            TextBox T(int y, bool wide = true) => new TextBox { Left = 10, Top = y, Width = wide ? 400 : 180 };
            ComboBox C(int y) => new ComboBox { Left = 10, Top = y, Width = 400, DropDownStyle = ComboBoxStyle.DropDown };

            int y0 = 25;
            int yRow = y0 + 410;
            grpMeta.Controls.Add(L("TYTUŁ RYSUNKU:", y0)); tbTytul = T(y0 + 25); tbTytul.MaxLength = 200; grpMeta.Controls.Add(tbTytul);
            grpMeta.Controls.Add(L("JEDNOSTKA PROJEKTOWA:", y0 + 50)); tbJednostka = T(y0 + 78); tbJednostka.MaxLength = 200; grpMeta.Controls.Add(tbJednostka);
            grpMeta.Controls.Add(L("INWESTOR:", y0 + 110)); tbInwestor = T(y0 + 138); tbInwestor.MaxLength = 200; grpMeta.Controls.Add(tbInwestor);
            grpMeta.Controls.Add(L("NAZWA I ADRES OBIEKTU:", y0 + 170)); tbObiekt = T(y0 + 198); tbObiekt.MaxLength = 200; grpMeta.Controls.Add(tbObiekt);
            
            grpMeta.Controls.Add(L("PROJEKTANT:", y0 + 230)); cbProjektant = C(y0 + 258); cbProjektant.MaxLength = 40; grpMeta.Controls.Add(cbProjektant);
            grpMeta.Controls.Add(L("SPRAWDZAJĄCY:", y0 + 290)); cbSprawdzajacy = C(y0 + 318); cbSprawdzajacy.MaxLength = 40; grpMeta.Controls.Add(cbSprawdzajacy);
            grpMeta.Controls.Add(L("OPRACOWAŁ(A):", y0 + 350)); cbOpracowujacy = C(y0 + 378); cbOpracowujacy.MaxLength = 40; grpMeta.Controls.Add(cbOpracowujacy);

            var labelSkala = L("SKALA:", yRow);
            labelSkala.Left = 10;
            labelSkala.Width = 120;
            grpMeta.Controls.Add(labelSkala);

            tbSkala = new TextBox
            {
                Left = 10,
                Top = yRow + 25,
                Width = 120,
                MaxLength = 20
            };
            grpMeta.Controls.Add(tbSkala);

            var labelNr = L("NR. RYSUNKU:", yRow);
            labelNr.Left = 150;
            labelNr.Width = 120;
            grpMeta.Controls.Add(labelNr);

            tbNrRys = new TextBox
            {
                Left = 150,
                Top = yRow + 25,
                Width = 120,
                MaxLength = 10
            };
            grpMeta.Controls.Add(tbNrRys);

            var labelData = L("DATA:", yRow);
            labelData.Left = 290;
            labelData.Width = 120;
            grpMeta.Controls.Add(labelData);

            dtpData = new CustomDatePicker
            {
                Left = 290,
                Top = yRow + 25,
                Width = 120
            };
            grpMeta.Controls.Add(dtpData);


            btnOk = new Button { Text = "OK", Left = 640, Top = 540, Width = 110, DialogResult = DialogResult.OK };
            btnOk.Click += BtnOk_Click; // Potrzebne do walidacji przed zamknięciem formularza.
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

        private void LoadPersonLists() // Tu się ładuje lista z ComboBox-ów z plików.
        {
            cbProjektant.Items.AddRange(PersonMemory.LoadProjektanci().ToArray());
            cbSprawdzajacy.Items.AddRange(PersonMemory.LoadSprawdzajacy().ToArray());
            cbOpracowujacy.Items.AddRange(PersonMemory.LoadOpracowujacy().ToArray());
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
                Projektant = cbProjektant.Text,
                Sprawdzajacy = cbSprawdzajacy.Text,
                Opracowujacy = cbOpracowujacy.Text,
                Skala = tbSkala.Text,
                NumerRysunku = tbNrRys.Text,
                Data = dtpData.Value.ToShortDateString(),

            };
            return d;

        }
        private void BtnOk_Click(object sender, EventArgs e)
        {
            // --- WALIDACJA ---
            if (string.IsNullOrWhiteSpace(tbTytul.Text))
            {
                MessageBox.Show("Pole 'Tytuł rysunku' jest wymagane.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbTytul.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            // Sprawdzamy, które osoby są nowe (nie ma ich jeszcze w pamięci)
            string projektant = cbProjektant.Text;
            string sprawdzajacy = cbSprawdzajacy.Text;
            string opracowujacy = cbOpracowujacy.Text;

            bool newProjektant = !string.IsNullOrWhiteSpace(projektant) && !PersonMemory.LoadProjektanci().Contains(projektant);
            bool newSprawdzajacy = !string.IsNullOrWhiteSpace(sprawdzajacy) && !PersonMemory.LoadSprawdzajacy().Contains(sprawdzajacy);
            bool newOpracowujacy = !string.IsNullOrWhiteSpace(opracowujacy) && !PersonMemory.LoadOpracowujacy().Contains(opracowujacy);

            // Jeśli nie ma żadnej nowej osoby → nie pokazujemy modala
            if (!newProjektant && !newSprawdzajacy && !newOpracowujacy)
            {
                this.DialogResult = DialogResult.OK;
                return;
            }

            // Tworzymy modal tylko z nowymi osobami
            var dlg = new SavePersonsDialog(
                newProjektant ? projektant : "",
                newSprawdzajacy ? sprawdzajacy : "",
                newOpracowujacy ? opracowujacy : ""
            );

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (dlg.SaveProjektant)
                    PersonMemory.SaveProjektant(projektant);

                if (dlg.SaveSprawdzajacy)
                    PersonMemory.SaveSprawdzajacy(sprawdzajacy);

                if (dlg.SaveOpracowujacy)
                    PersonMemory.SaveOpracowujacy(opracowujacy);

                this.DialogResult = DialogResult.OK;
            }
            else
            {
                this.DialogResult = DialogResult.None;
            }
        }
    }
}
