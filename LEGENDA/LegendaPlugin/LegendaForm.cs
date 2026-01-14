using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LegendPlugin
{
    public class LegendForm : Form
    {
        private DataGridView dgvLayers;
        private ComboBox cbJednostka, cbInwestor, cbObiekt, cbTytul, cbSkala;
        private TextBox tbNrRys;
        private ComboBox cbProjektant, cbSprawdzajacy, cbOpracowujacy;
        private LegendPlugin.CustomDatePicker dtpData;
        private Button btnOk, btnCancel;

        private readonly List<LegendCommand.LayerInfo> _layers;

        public LegendForm(List<LegendCommand.LayerInfo> layers)
        {
            _layers = layers ?? new List<LegendCommand.LayerInfo>();
            this.Text = "Legenda – wybór warstw i dane metryczki";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(950, 650);
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            BuildUi();
            LoadLayersToGrid();
            LoadPersonLists();
        }

        private void BuildUi()
        {
            var grpLayers = new GroupBox { Text = "Warstwy (zaznacz 'Kwadrat' dla hatch)", Left = 10, Top = 10, Width = 430, Height = 530 };

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

            var colCheck = new DataGridViewCheckBoxColumn { HeaderText = "Wybierz", Width = 50, Name = "colCheck" };
            var colName = new DataGridViewTextBoxColumn { HeaderText = "Nazwa Warstwy", Width = 307, Name = "colName", ReadOnly = true };
            var colIsHatch = new DataGridViewCheckBoxColumn { HeaderText = "Kwadrat", Width = 50, Name = "colIsHatch" };

            dgvLayers.Columns.AddRange(colCheck, colName, colIsHatch);
            grpLayers.Controls.Add(dgvLayers);

            var grpMeta = new GroupBox { Text = "Dane metryczki", Left = 500, Top = 10, Width = 430, Height = 560 };

            Label L(string t, int y) => new Label { Text = t, Left = 10, Top = y, Width = 200 };
            ComboBox C(int y, bool multiLine = false) => new ComboBox
            {
                Left = 10,
                Top = y,
                Width = 400,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };

            int y0 = 25;

            grpMeta.Controls.Add(L("TYTUŁ RYSUNKU:", y0));
            cbTytul = C(y0 + 25);
            grpMeta.Controls.Add(cbTytul);

            int yJednostka = y0 + 60;
            grpMeta.Controls.Add(L("JEDNOSTKA PROJEKTOWA:", yJednostka));
            cbJednostka = C(yJednostka + 25);
            grpMeta.Controls.Add(cbJednostka);

            int yInwestor = yJednostka + 60;
            grpMeta.Controls.Add(L("INWESTOR:", yInwestor));
            cbInwestor = C(yInwestor + 25);
            grpMeta.Controls.Add(cbInwestor);

            int yObiekt = yInwestor + 60;
            grpMeta.Controls.Add(L("NAZWA I ADRES OBIEKTU:", yObiekt));
            cbObiekt = C(yObiekt + 25);
            grpMeta.Controls.Add(cbObiekt);

            int yProjektant = yObiekt + 60;
            grpMeta.Controls.Add(L("PROJEKTANT:", yProjektant));
            cbProjektant = C(yProjektant + 25);
            grpMeta.Controls.Add(cbProjektant);

            int ySprawdzajacy = yProjektant + 60;
            grpMeta.Controls.Add(L("SPRAWDZAJĄCY:", ySprawdzajacy));
            cbSprawdzajacy = C(ySprawdzajacy + 25);
            grpMeta.Controls.Add(cbSprawdzajacy);

            int yOpracowujacy = ySprawdzajacy + 60;
            grpMeta.Controls.Add(L("OPRACOWAŁ(A):", yOpracowujacy));
            cbOpracowujacy = C(yOpracowujacy + 25);
            grpMeta.Controls.Add(cbOpracowujacy);

            int yRow = yOpracowujacy + 60;
            var labelSkala = L("SKALA:", yRow); labelSkala.Left = 10; labelSkala.Width = 120; grpMeta.Controls.Add(labelSkala);
            cbSkala = new ComboBox { Left = 10, Top = yRow + 25, Width = 120, DropDownStyle = ComboBoxStyle.DropDown };
            grpMeta.Controls.Add(cbSkala);

            var labelNr = L("NR. RYSUNKU:", yRow); labelNr.Left = 150; labelNr.Width = 120; grpMeta.Controls.Add(labelNr);
            tbNrRys = new TextBox { Left = 150, Top = yRow + 25, Width = 120, MaxLength = 10 };
            grpMeta.Controls.Add(tbNrRys);

            var labelData = L("DATA:", yRow); labelData.Left = 290; labelData.Width = 120; grpMeta.Controls.Add(labelData);
            dtpData = new LegendPlugin.CustomDatePicker { Left = 290, Top = yRow + 25, Width = 120 };
            grpMeta.Controls.Add(dtpData);

            btnOk = new Button { Text = "OK", Left = 640, Top = 580, Width = 110 };
            btnOk.Click += BtnOk_Click;
            btnCancel = new Button { Text = "Anuluj", Left = 760, Top = 580, Width = 110, DialogResult = DialogResult.Cancel };

            this.Controls.Add(grpLayers);
            this.Controls.Add(grpMeta);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }

        private void LoadLayersToGrid()
        {
            dgvLayers.Rows.Clear();
            foreach (var l in _layers.OrderBy(x => x.Name))
            {
                int idx = dgvLayers.Rows.Add();
                var row = dgvLayers.Rows[idx];
                row.Cells["colCheck"].Value = false;
                row.Cells["colName"].Value = l.Name;
                bool isHatchGuess = l.Name.ToLower().Contains("hatch") || l.Name.ToLower().Contains("wypełnienie");
                row.Cells["colIsHatch"].Value = isHatchGuess;
                row.Tag = l;
            }
        }

        private void LoadPersonLists()
        {
            cbProjektant.Items.AddRange(PersonMemory.LoadProjektanci().ToArray());
            cbSprawdzajacy.Items.AddRange(PersonMemory.LoadSprawdzajacy().ToArray());
            cbOpracowujacy.Items.AddRange(PersonMemory.LoadOpracowujacy().ToArray());

            cbJednostka.Items.AddRange(PersonMemory.LoadJednostki().ToArray());
            cbInwestor.Items.AddRange(PersonMemory.LoadInwestorzy().ToArray());
            cbObiekt.Items.AddRange(PersonMemory.LoadObiekty().ToArray());
            cbTytul.Items.AddRange(PersonMemory.LoadTytuly().ToArray());
            cbSkala.Items.AddRange(PersonMemory.LoadSkale().ToArray());
        }

        public LegendData GetData()
        {
            var selectedLayers = new List<LegendCommand.LayerInfo>();
            foreach (DataGridViewRow row in dgvLayers.Rows)
            {
                if (Convert.ToBoolean(row.Cells["colCheck"].Value))
                {
                    var originalInfo = row.Tag as LegendCommand.LayerInfo;
                    selectedLayers.Add(new LegendCommand.LayerInfo
                    {
                        Name = originalInfo.Name,
                        Color = originalInfo.Color,
                        IsHatch = Convert.ToBoolean(row.Cells["colIsHatch"].Value)
                    });
                }
            }

            return new LegendData
            {
                SelectedLayersInfo = selectedLayers,
                SelectedLayers = selectedLayers.Select(x => x.Name).ToList(),
                JednostkaProjektowa = cbJednostka.Text,
                Inwestor = cbInwestor.Text,
                NazwaAdresObiektu = cbObiekt.Text,
                TytulRysunku = cbTytul.Text,
                Projektant = cbProjektant.Text,
                Sprawdzajacy = cbSprawdzajacy.Text,
                Opracowujacy = cbOpracowujacy.Text,
                Skala = cbSkala.Text,
                NumerRysunku = tbNrRys.Text,
                Data = dtpData.Value.ToShortDateString(),
            };
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cbTytul.Text))
            {
                MessageBox.Show("Pole 'Tytuł rysunku' jest wymagane.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbTytul.Focus();
                return;
            }

            var newEntries = new Dictionary<string, string>();

            CheckAndAdd(newEntries, "Projektant", cbProjektant.Text, PersonMemory.LoadProjektanci());
            CheckAndAdd(newEntries, "Sprawdzający", cbSprawdzajacy.Text, PersonMemory.LoadSprawdzajacy());
            CheckAndAdd(newEntries, "Opracowujący", cbOpracowujacy.Text, PersonMemory.LoadOpracowujacy());
            CheckAndAdd(newEntries, "Jednostka", cbJednostka.Text, PersonMemory.LoadJednostki());
            CheckAndAdd(newEntries, "Inwestor", cbInwestor.Text, PersonMemory.LoadInwestorzy());
            CheckAndAdd(newEntries, "Obiekt", cbObiekt.Text, PersonMemory.LoadObiekty());
            CheckAndAdd(newEntries, "Tytuł", cbTytul.Text, PersonMemory.LoadTytuly());
            CheckAndAdd(newEntries, "Skala", cbSkala.Text, PersonMemory.LoadSkale());

            if (newEntries.Count > 0)
            {
                var dlg = new SavePersonsDialog(newEntries);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (dlg.ShouldSave("Projektant")) PersonMemory.SaveProjektant(cbProjektant.Text);
                    if (dlg.ShouldSave("Sprawdzający")) PersonMemory.SaveSprawdzajacy(cbSprawdzajacy.Text);
                    if (dlg.ShouldSave("Opracowujący")) PersonMemory.SaveOpracowujacy(cbOpracowujacy.Text);
                    if (dlg.ShouldSave("Jednostka")) PersonMemory.SaveJednostka(cbJednostka.Text);
                    if (dlg.ShouldSave("Inwestor")) PersonMemory.SaveInwestor(cbInwestor.Text);
                    if (dlg.ShouldSave("Obiekt")) PersonMemory.SaveObiekt(cbObiekt.Text);
                    if (dlg.ShouldSave("Tytuł")) PersonMemory.SaveTytul(cbTytul.Text);
                    if (dlg.ShouldSave("Skala")) PersonMemory.SaveSkala(cbSkala.Text);

                    this.DialogResult = DialogResult.OK;
                }
                else {
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }
            else
            {
                this.DialogResult = DialogResult.OK;
            }
        }

        private void CheckAndAdd(Dictionary<string, string> dict, string key, string val, List<string> existing)
        {
            if (!string.IsNullOrWhiteSpace(val) && !existing.Contains(val.Trim()))
            {
                dict.Add(key, val.Trim());
            }
        }
    }
}