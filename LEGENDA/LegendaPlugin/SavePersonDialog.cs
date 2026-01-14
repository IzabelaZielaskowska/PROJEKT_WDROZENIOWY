using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace LegendPlugin
{
    public class SavePersonsDialog : Form
    {
        private FlowLayoutPanel flowPanel;
        private Dictionary<string, CheckBox> checkBoxes = new Dictionary<string, CheckBox>();

        public bool ShouldSave(string key) => checkBoxes.ContainsKey(key) && checkBoxes[key].Checked;

        public SavePersonsDialog(Dictionary<string, string> valuesToSave)
        {
            this.Text = "Czy chcesz zapisać nowe dane w pamięci?";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new Size(450, 400);

            flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 330,
                AutoScroll = true,
                Padding = new Padding(10),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            foreach (var item in valuesToSave)
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    var cb = new CheckBox
                    {
                        Text = $"Zapisz {item.Key}: {item.Value}",
                        Width = 400,
                        Checked = true,
                        Margin = new Padding(0, 5, 0, 5)
                    };
                    checkBoxes.Add(item.Key, cb);
                    flowPanel.Controls.Add(cb);
                }
            }

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            var btnOK = new Button { Text = "OK", Left = 250, Top = 10, DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "Anuluj", Left = 340, Top = 10, DialogResult = DialogResult.Cancel };

            btnPanel.Controls.Add(btnOK);
            btnPanel.Controls.Add(btnCancel);

            this.Controls.Add(flowPanel);
            this.Controls.Add(btnPanel);
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}