using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegendPlugin
{
    // Elegancki własny selektor daty z klasyczną ikoną kalendarza
    public class CustomDatePicker : UserControl
    {
        private TextBox txtDate;
        private Button btnCalendar;
        private MonthCalendar calendar;
        private Form popupForm;

        public DateTime Value { get; private set; } = DateTime.Today;

        public CustomDatePicker()
        {
            // Pole tekstowe
            txtDate = new TextBox
            {
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Text = Value.ToString("dd.MM.yyyy"),
                TextAlign = HorizontalAlignment.Left,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Przycisk z własną ikoną
            btnCalendar = new Button
            {
                Dock = DockStyle.Right,
                Width = 28,
                FlatStyle = FlatStyle.Flat
            };
            btnCalendar.FlatAppearance.BorderSize = 0;
            btnCalendar.BackColor = SystemColors.ControlLight;
            btnCalendar.Paint += BtnCalendar_Paint;
            btnCalendar.Click += (s, e) => ShowCalendar();

            txtDate.Click += (s, e) => ShowCalendar();

            this.Controls.Add(txtDate);
            this.Controls.Add(btnCalendar);

            this.Height = txtDate.Height;
            this.MinimumSize = new Size(120, txtDate.Height);
        }

        // Rysowanie małej ikonki kalendarza
        private void BtnCalendar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = new Rectangle(7, 6, 14, 12);

            using (var pen = new Pen(Color.Gray))
            {
                // Ramka
                g.DrawRectangle(pen, rect);
                // Linie symbolizujące kratki
                g.DrawLine(pen, rect.Left, rect.Top + 4, rect.Right, rect.Top + 4);
                g.DrawLine(pen, rect.Left, rect.Top + 8, rect.Right, rect.Top + 8);
                g.DrawLine(pen, rect.Left + 4, rect.Top + 4, rect.Left + 4, rect.Bottom);
                g.DrawLine(pen, rect.Left + 9, rect.Top + 4, rect.Left + 9, rect.Bottom);
                // Górne "kółka"
                g.FillEllipse(Brushes.Gray, rect.Left + 2, rect.Top - 3, 3, 3);
                g.FillEllipse(Brushes.Gray, rect.Right - 5, rect.Top - 3, 3, 3);
            }
        }

        private void ShowCalendar()
        {
            if (popupForm != null) return;

            popupForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.White,
                TopMost = true,
                AutoSize = true,
                MaximumSize = new Size(800, 200),
            };

            calendar = new MonthCalendar
            {
                MaxSelectionCount = 1,
                ShowToday = false,
                ShowTodayCircle = false,
                
            };

            calendar.DateSelected += (s, e) =>
            {
                Value = e.Start;
                txtDate.Text = Value.ToString("dd.MM.yyyy");
                popupForm.Close();
                popupForm = null;
            };

            // Ustawiamy kalendarz w formie w naturalnym rozmiarze
            //calendar.Location = new Point(0, 0); 
            popupForm.Controls.Add(calendar);
            //popupForm.ClientSize = calendar.PreferredSize; // forma dokładnie dopasowana do kalendarza
            calendar.AutoSize = true;
            calendar.Dock = DockStyle.Fill; // Dopasowujemy formularz do kalendarza
            var screenPoint = this.PointToScreen(new Point(0, this.Height));
            popupForm.Location = screenPoint;


            // Pozycja popup pod kontrolką
            //var screenPoint = this.PointToScreen(new Point(0, this.Height));
            //popupForm.Location = screenPoint;

            popupForm.Deactivate += (s, e) =>
            {
                popupForm.Close();
                popupForm = null;
            };

            popupForm.Show();

        }

        public override string Text
        {
            get => txtDate.Text;
            set => txtDate.Text = value;
        }
    }
}
