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
        private LegendPlugin.CustomDatePicker dtpData;
        private Button btnOk, btnCancel;

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // LegendForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "LegendForm";
            this.ResumeLayout(false);

        }

        // Komentarz: Warstwy wejściowe (z kolorem AutoCAD).
        private readonly List<LegendCommand.LayerInfo> _layers;

        // Komentarz: Konstruktor – przekazujemy listę warstw.
        public LegendForm(List<LegendCommand.LayerInfo> layers)
        {
            _layers = layers ?? new List<LegendCommand.LayerInfo>();
            this.Text = "Legenda – wybór warstw i dane metryczki";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(900, 620); // Rozmiar okna bez zmian
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            BuildUi();
            LoadLayers();
            LoadPersonLists();
        }

        // Komentarz: Budowa interfejsu.
        private void BuildUi()
        {
            // Grupa lewa: Warstwy
            var grpLayers = new GroupBox { Text = "Warstwy do legendy (z ikoną koloru)", Left = 10, Top = 10, Width = 430, Height = 530 };

            clbLayers = new CheckedListBox
            {
                Left = 10,
                Top = 20,
                Width = 410,
                Height = 500,
                DrawMode = DrawMode.OwnerDrawFixed,
                IntegralHeight = false,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true
            };
            clbLayers.DrawItem += ClbLayers_DrawItem;
            grpLayers.Controls.Add(clbLayers);

            // Grupa prawa: Metryczka
            var grpMeta = new GroupBox { Text = "Dane metryczki", Left = 450, Top = 10, Width = 430, Height = 530 };

            // Funkcje pomocnicze do tworzenia etykiet i prostych kontrolek
            Label L(string t, int y) => new Label { Text = t, Left = 10, Top = y, Width = 200 };
            TextBox T(int y, bool wide = true) => new TextBox { Left = 10, Top = y, Width = wide ? 400 : 180 };
            ComboBox C(int y) => new ComboBox { Left = 10, Top = y, Width = 400, DropDownStyle = ComboBoxStyle.DropDown };

            int y0 = 25; // Pozycja startowa Y

            // 1. TYTUŁ RYSUNKU
            grpMeta.Controls.Add(L("TYTUŁ RYSUNKU:", y0));
            tbTytul = T(y0 + 25);
            tbTytul.MaxLength = 200;
            grpMeta.Controls.Add(tbTytul);

            // 2. JEDNOSTKA PROJEKTOWA (Zmiana: Multiline)
            // Przesuwamy nieco w dół
            int yJednostka = y0 + 60;
            grpMeta.Controls.Add(L("JEDNOSTKA PROJEKTOWA:", yJednostka));
            // Tworzymy TextBox ręcznie, aby włączyć Multiline
            tbJednostka = new TextBox
            {
                Left = 10,
                Top = yJednostka + 25,
                Width = 400,
                Height = 45, // Wyższy, na ok. 2-3 linie
                Multiline = true,
                AcceptsReturn = true, // Obsługa entera
                ScrollBars = ScrollBars.Vertical
            };
            // Usuwam MaxLength (domyślnie 32767), żeby nie ograniczać
            grpMeta.Controls.Add(tbJednostka);

            // 3. INWESTOR (Zmiana: Multiline)
            int yInwestor = yJednostka + 80; // Większy odstęp ze względu na wysokość poprzedniego pola
            grpMeta.Controls.Add(L("INWESTOR:", yInwestor));
            tbInwestor = new TextBox
            {
                Left = 10,
                Top = yInwestor + 25,
                Width = 400,
                Height = 45, // Wyższy
                Multiline = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical
            };
            grpMeta.Controls.Add(tbInwestor);

            // 4. NAZWA I ADRES OBIEKTU (Standard)
            int yObiekt = yInwestor + 80;
            grpMeta.Controls.Add(L("NAZWA I ADRES OBIEKTU:", yObiekt));
            tbObiekt = T(yObiekt + 25);
            tbObiekt.MaxLength = 200;
            grpMeta.Controls.Add(tbObiekt);

            // 5. OSOBY (Zmiana: Usunięcie limitu znaków)
            int yProjektant = yObiekt + 60;
            grpMeta.Controls.Add(L("PROJEKTANT:", yProjektant));
            cbProjektant = C(yProjektant + 25);
            // Usunięto: cbProjektant.MaxLength = 40;
            grpMeta.Controls.Add(cbProjektant);

            int ySprawdzajacy = yProjektant + 60;
            grpMeta.Controls.Add(L("SPRAWDZAJĄCY:", ySprawdzajacy));
            cbSprawdzajacy = C(ySprawdzajacy + 25);
            // Usunięto: cbSprawdzajacy.MaxLength = 40;
            grpMeta.Controls.Add(cbSprawdzajacy);

            int yOpracowujacy = ySprawdzajacy + 60;
            grpMeta.Controls.Add(L("OPRACOWAŁ(A)::", yOpracowujacy));
            cbOpracowujacy = C(yOpracowujacy + 25);
            // Usunięto: cbOpracowujacy.MaxLength = 40;
            grpMeta.Controls.Add(cbOpracowujacy);

            // 6. STOPKA (Skala, Nr Rys, Data)
            int yRow = yOpracowujacy + 60; // Dolna linia formularza

            // Skala
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

            // Nr Rysunku
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

            // Data
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


            // Przyciski dolne
            btnOk = new Button { Text = "OK", Left = 640, Top = 550, Width = 110, DialogResult = DialogResult.OK };
            btnOk.Click += BtnOk_Click;
            btnCancel = new Button { Text = "Anuluj", Left = 760, Top = 550, Width = 110, DialogResult = DialogResult.Cancel };


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

        private void LoadPersonLists()
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