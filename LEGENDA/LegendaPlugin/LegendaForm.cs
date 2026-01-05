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
        // Komentarz: Zmieniamy CheckedListBox na DataGridView, aby mieć kolumny.
        private DataGridView dgvLayers;

        private TextBox tbJednostka, tbInwestor, tbObiekt, tbTytul, tbSkala, tbNrRys;
        private ComboBox cbProjektant, cbSprawdzajacy, cbOpracowujacy;
        private LegendPlugin.CustomDatePicker dtpData;
        private Button btnOk, btnCancel;

        // Komentarz: Warstwy wejściowe (z kolorem AutoCAD).
        private readonly List<LegendCommand.LayerInfo> _layers;

        // Komentarz: Konstruktor – przekazujemy listę warstw.
        public LegendForm(List<LegendCommand.LayerInfo> layers)
        {
            _layers = layers ?? new List<LegendCommand.LayerInfo>();
            this.Text = "Legenda – wybór warstw i dane metryczki";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(950, 620); // Nieco szersze okno dla tabeli
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            BuildUi();
            LoadLayersToGrid(); // Zmiana nazwy metody ładowania
            LoadPersonLists();
        }

        // Komentarz: Budowa interfejsu.
        private void BuildUi()
        {
            // Grupa lewa: Warstwy
            var grpLayers = new GroupBox { Text = "Warstwy (zaznacz 'Kwadrat' dla hatch)", Left = 10, Top = 10, Width = 430, Height = 530 };

            // Komentarz: Tworzymy tabelę zamiast listy
            dgvLayers = new DataGridView
            {
                Left = 10,
                Top = 20,
                Width = 410,
                Height = 500,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Definicja kolumn
            var colCheck = new DataGridViewCheckBoxColumn { HeaderText = "Wybierz", Width = 50, Name = "colCheck" };
            var colName = new DataGridViewTextBoxColumn { HeaderText = "Nazwa Warstwy", Width = 307, Name = "colName", ReadOnly = true };
            var colIsHatch = new DataGridViewCheckBoxColumn { HeaderText = "Kwadrat", Width = 50, Name = "colIsHatch", ToolTipText = "Zaznacz, aby rysować wypełniony kwadrat (Solid Hatch)" };

            dgvLayers.Columns.AddRange(colCheck, colName, colIsHatch);
            grpLayers.Controls.Add(dgvLayers);

            // Grupa prawa: Metryczka (BEZ ZMIAN W UKŁADZIE)
            var grpMeta = new GroupBox { Text = "Dane metryczki", Left = 500, Top = 10, Width = 430, Height = 530 };

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

            // 2. JEDNOSTKA PROJEKTOWA
            int yJednostka = y0 + 60;
            grpMeta.Controls.Add(L("JEDNOSTKA PROJEKTOWA:", yJednostka));
            tbJednostka = new TextBox
            {
                Left = 10,
                Top = yJednostka + 25,
                Width = 400,
                Height = 45,
                Multiline = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical
            };
            grpMeta.Controls.Add(tbJednostka);

            // 3. INWESTOR
            int yInwestor = yJednostka + 80;
            grpMeta.Controls.Add(L("INWESTOR:", yInwestor));
            tbInwestor = new TextBox
            {
                Left = 10,
                Top = yInwestor + 25,
                Width = 400,
                Height = 45,
                Multiline = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical
            };
            grpMeta.Controls.Add(tbInwestor);

            // 4. NAZWA I ADRES OBIEKTU
            int yObiekt = yInwestor + 80;
            grpMeta.Controls.Add(L("NAZWA I ADRES OBIEKTU:", yObiekt));
            tbObiekt = T(yObiekt + 25);
            tbObiekt.MaxLength = 200;
            grpMeta.Controls.Add(tbObiekt);

            // 5. OSOBY
            int yProjektant = yObiekt + 60;
            grpMeta.Controls.Add(L("PROJEKTANT:", yProjektant));
            cbProjektant = C(yProjektant + 25);
            grpMeta.Controls.Add(cbProjektant);

            int ySprawdzajacy = yProjektant + 60;
            grpMeta.Controls.Add(L("SPRAWDZAJĄCY:", ySprawdzajacy));
            cbSprawdzajacy = C(ySprawdzajacy + 25);
            grpMeta.Controls.Add(cbSprawdzajacy);

            int yOpracowujacy = ySprawdzajacy + 60;
            grpMeta.Controls.Add(L("OPRACOWAŁ(A)::", yOpracowujacy));
            cbOpracowujacy = C(yOpracowujacy + 25);
            grpMeta.Controls.Add(cbOpracowujacy);

            // 6. STOPKA
            int yRow = yOpracowujacy + 60;

            var labelSkala = L("SKALA:", yRow); labelSkala.Left = 10; labelSkala.Width = 120; grpMeta.Controls.Add(labelSkala);
            tbSkala = new TextBox { Left = 10, Top = yRow + 25, Width = 120, MaxLength = 20 }; grpMeta.Controls.Add(tbSkala);

            var labelNr = L("NR. RYSUNKU:", yRow); labelNr.Left = 150; labelNr.Width = 120; grpMeta.Controls.Add(labelNr);
            tbNrRys = new TextBox { Left = 150, Top = yRow + 25, Width = 120, MaxLength = 10 }; grpMeta.Controls.Add(tbNrRys);

            var labelData = L("DATA:", yRow); labelData.Left = 290; labelData.Width = 120; grpMeta.Controls.Add(labelData);
            dtpData = new CustomDatePicker { Left = 290, Top = yRow + 25, Width = 120 }; grpMeta.Controls.Add(dtpData);

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

        // Komentarz: Załadowanie warstw do tabeli DataGridView
        private void LoadLayersToGrid()
        {
            dgvLayers.Rows.Clear();
            foreach (var l in _layers.OrderBy(x => x.Name))
            {
                // Tworzymy bitmapę z kolorem warstwy
                Bitmap bmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    SDColor c = l.Color != null ? l.Color.ColorValue : SDColor.Gray;
                    using (Brush b = new SolidBrush(c)) g.FillRectangle(b, 0, 0, 16, 16);
                    g.DrawRectangle(Pens.Black, 0, 0, 15, 15);
                }

                int idx = dgvLayers.Rows.Add();
                var row = dgvLayers.Rows[idx];

                row.Cells["colCheck"].Value = false; // Domyślnie odznaczone
                row.Cells["colName"].Value = l.Name;

                // Domyślnie zaznaczamy "Kwadrat" jeśli nazwa sugeruje hatch
                bool isHatchGuess = l.Name.ToLower().Contains("hatch") ||
                                    l.Name.ToLower().Contains("wypełnienie") ||
                                    l.Name.ToLower().Contains("kostka");
                row.Cells["colIsHatch"].Value = isHatchGuess;

                // Przechowujemy oryginalny obiekt w Tag
                row.Tag = l;
            }
        }

        private void LoadPersonLists()
        {
            cbProjektant.Items.AddRange(PersonMemory.LoadProjektanci().ToArray());
            cbSprawdzajacy.Items.AddRange(PersonMemory.LoadSprawdzajacy().ToArray());
            cbOpracowujacy.Items.AddRange(PersonMemory.LoadOpracowujacy().ToArray());
        }

        // Komentarz: Zbiór danych z formularza do obiektu LegendData.
        public LegendData GetData()
        {
            // Zbieramy informacje o zaznaczonych warstwach i ich trybie (Hatch/Linia)
            var selectedLayers = new List<LegendCommand.LayerInfo>();

            foreach (DataGridViewRow row in dgvLayers.Rows)
            {
                bool isChecked = Convert.ToBoolean(row.Cells["colCheck"].Value);
                if (isChecked)
                {
                    var originalInfo = row.Tag as LegendCommand.LayerInfo;
                    bool isHatch = Convert.ToBoolean(row.Cells["colIsHatch"].Value);

                    // Tworzymy nowy obiekt LayerInfo z ustawioną flagą IsHatch
                    selectedLayers.Add(new LegendCommand.LayerInfo
                    {
                        Name = originalInfo.Name,
                        Color = originalInfo.Color,
                        IsHatch = isHatch
                    });
                }
            }

            // UWAGA: Musisz upewnić się, że klasa LegendData w LegendaCommands.cs
            // posiada pole `SelectedLayersInfo` lub zaktualizować logikę jej użycia.
            var d = new LegendData
            {
                // Przekazujemy listę obiektów (wymaga zmiany w LegendaCommands.cs!)
                SelectedLayersInfo = selectedLayers,
                // Stara lista (dla kompatybilności, jeśli potrzebna)
                SelectedLayers = selectedLayers.Select(x => x.Name).ToList(),

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

            string projektant = cbProjektant.Text;
            string sprawdzajacy = cbSprawdzajacy.Text;
            string opracowujacy = cbOpracowujacy.Text;

            bool newProjektant = !string.IsNullOrWhiteSpace(projektant) && !PersonMemory.LoadProjektanci().Contains(projektant);
            bool newSprawdzajacy = !string.IsNullOrWhiteSpace(sprawdzajacy) && !PersonMemory.LoadSprawdzajacy().Contains(sprawdzajacy);
            bool newOpracowujacy = !string.IsNullOrWhiteSpace(opracowujacy) && !PersonMemory.LoadOpracowujacy().Contains(opracowujacy);

            if (!newProjektant && !newSprawdzajacy && !newOpracowujacy)
            {
                this.DialogResult = DialogResult.OK;
                return;
            }

            var dlg = new SavePersonsDialog(
                newProjektant ? projektant : "",
                newSprawdzajacy ? sprawdzajacy : "",
                newOpracowujacy ? opracowujacy : ""
            );

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (dlg.SaveProjektant) PersonMemory.SaveProjektant(projektant);
                if (dlg.SaveSprawdzajacy) PersonMemory.SaveSprawdzajacy(sprawdzajacy);
                if (dlg.SaveOpracowujacy) PersonMemory.SaveOpracowujacy(opracowujacy);

                this.DialogResult = DialogResult.OK;
            }
            else
            {
                this.DialogResult = DialogResult.None;
            }
        }
    }
}