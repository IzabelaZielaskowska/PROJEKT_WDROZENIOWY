using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegendPlugin
{
    public class SavePersonsDialog : Form
    {
        private CheckBox cbProj;
        private CheckBox cbSpr;
        private CheckBox cbOpr;

        public bool SaveProjektant => cbProj.Checked;
        public bool SaveSprawdzajacy => cbSpr.Checked;
        public bool SaveOpracowujacy => cbOpr.Checked;

        public SavePersonsDialog(string projektant, string sprawdzajacy, string opracowujacy)
        {
            this.Text = "Czy chcesz zapisać w pamięci osobę?";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(420, 170);

            cbProj = new CheckBox
            {
                Left = 20,
                Top = 20,
                Width = 380,
                Text = $"Zapisz projektanta: {projektant}",
                Visible = !string.IsNullOrWhiteSpace(projektant)
            };

            cbSpr = new CheckBox
            {
                Left = 20,
                Top = 50,
                Width = 380,
                Text = $"Zapisz sprawdzającego: {sprawdzajacy}",
                Visible = !string.IsNullOrWhiteSpace(sprawdzajacy)
            };

            cbOpr = new CheckBox
            {
                Left = 20,
                Top = 80,
                Width = 380,
                Text = $"Zapisz opracowującego: {opracowujacy}",
                Visible = !string.IsNullOrWhiteSpace(opracowujacy)
            };

            Controls.Add(cbProj);
            Controls.Add(cbSpr);
            Controls.Add(cbOpr);

            var btnOK = new Button
            {
                Text = "OK",
                Left = 220,
                Top = 130,
                Width = 80,
                DialogResult = DialogResult.OK
            };
            Controls.Add(btnOK);

            var btnCancel = new Button
            {
                Text = "Anuluj",
                Left = 310,
                Top = 130,
                Width = 80,
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}
