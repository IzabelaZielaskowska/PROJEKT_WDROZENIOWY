// Dołączamy WinForms – będziemy używać aliasu "WinForms", aby unikać konfliktów nazw (Label, Button, itp.)
using WinForms = System.Windows.Forms;
// Dołączamy klasy rysowania/bitmap do generowania ikon koloru (małe kwadraty 16x16)
using System.Drawing;
// Dołączamy kolekcje .NET (List, Dictionary) – do przechowywania warstw i mapowania po nazwie
using System.Collections.Generic;

namespace LegendaPlugin
{
    // Ta klasa reprezentuje okno, w którym użytkownik wybiera warstwy i wypełnia metrykę
    public class LegendaForm : WinForms.Form
    {
        // Prywatne pole: pełna lista wszystkich warstw dostępnych w rysunku (przekazana z komendy)
        private readonly List<LayerInfo> _allLayers;
        // Prywatny słownik: szybkie wyszukiwanie warstwy po nazwie (klucz: nazwa warstwy)
        private readonly Dictionary<string, LayerInfo> _byName = new Dictionary<string, LayerInfo>();
        // Obrazki 16x16 do „ikony” koloru warstwy (kolorowy kwadracik) – WinForms.ImageList
        private readonly WinForms.ImageList _images = new WinForms.ImageList();

        // Publiczna właściwość: wynik okna (wybrane warstwy + metryka), odczytywana przez komendę po OK
        public LegendaData ResultData { get; private set; } = new LegendaData();

        // Kontrolka: ComboBox z listą rozwijalną wszystkich warstw
        private WinForms.ComboBox cmbLayers;
        // Kontrolka: przycisk do dodawania wybranej warstwy do listy
        private WinForms.Button btnAddLayer;
        // Kontrolka: ListView z kolumnami (ikona, nazwa, opis) dla wybranych warstw
        private WinForms.ListView lvLayers;
        // Kontrolka: przycisk do usuwania pozycji z listy
        private WinForms.Button btnRemove;

        // Poniżej pola tekstowe dla metryki rysunku
        private WinForms.TextBox tbJednostka;
        private WinForms.TextBox tbInwestor;
        private WinForms.TextBox tbNazwaAdres;
        private WinForms.TextBox tbTytul;
        private WinForms.TextBox tbProjektant;
        private WinForms.TextBox tbSprawdzajacy;
        private WinForms.TextBox tbOpracowujacy;
        private WinForms.TextBox tbData;
        private WinForms.TextBox tbSkala;
        private WinForms.TextBox tbNumer;

        // Przyciski do zatwierdzenia/wyjścia z okna
        private WinForms.Button btnOk;
        private WinForms.Button btnCancel;

        // Konstruktor okna – przyjmuje listę wszystkich warstw, aby wypełnić ComboBox
        public LegendaForm(List<LayerInfo> allLayers)
        {
            // Zapamiętujemy listę warstw przekazaną z komendy
            _allLayers = allLayers;
            // Ustawiamy tytuł okna
            this.Text = "Legenda – wybór warstw i metryka";
            // Ustawiamy domyślną szerokość okna (px)
            this.Width = 900;
            // Ustawiamy domyślną wysokość okna (px)
            this.Height = 700;
            // Ustawiamy start pozycji – na środku ekranu, wygodnie dla użytkownika
            this.StartPosition = WinForms.FormStartPosition.CenterScreen;

            // Ustawiamy rozmiar obrazków w liście ikon (małe kwadraciki 16x16)
            _images.ImageSize = new Size(16, 16);

            // Budujemy mapę nazw → warstwa oraz generujemy bitmapy kolorów dla ikon
            foreach (var li in _allLayers)
            {
                // Dodajemy do słownika po nazwie, żeby łatwo było później pobrać pełny obiekt
                _byName[li.Name] = li;
                // Tworzymy małą bitmapę na ikonę koloru (16x16)
                var bmp = new Bitmap(16, 16);
                // Wyciągamy RGB z koloru AutoCAD (ColorValue zwraca System.Drawing.Color)
                var rgb = li.AcadColor.ColorValue;
                // Malujemy tło bitmapy na kolor warstwy i obrysowujemy czarną ramką
                using (var g = Graphics.FromImage(bmp))
                {
                    g.FillRectangle(new SolidBrush(rgb), 0, 0, 16, 16);
                    g.DrawRectangle(Pens.Black, 0, 0, 15, 15);
                }
                // Dodajemy bitmapę do ImageList pod kluczem nazwy warstwy (unikatowy identyfikator)
                _images.Images.Add(li.Name, bmp);
            }

            // Budujemy interfejs okna (kontrolki, rozmieszczenie)
            BuildUi();
            // Ładujemy nazwy warstw do listy rozwijalnej
            LoadLayersToCombo();
        }

        // Metoda pomocnicza – tworzy i rozmieszcza wszystkie kontrolki okna
        private void BuildUi()
        {
            // Tworzymy etykietę informacyjną nad listą rozwijalną warstw
            var lblLayer = new WinForms.Label();
            // Tekst etykiety instruujący użytkownika
            lblLayer.Text = "Wybierz warstwę z listy i dodaj do legendy:";
            // Pozycja X etykiety (px)
            lblLayer.Left = 20;
            // Pozycja Y etykiety (px)
            lblLayer.Top = 20;
            // Dodajemy etykietę do okna
            this.Controls.Add(lblLayer);

            // Tworzymy ComboBox (lista rozwijalna) dla nazw wszystkich warstw
            cmbLayers = new WinForms.ComboBox();
            // Szerokość ComboBoxa (px)
            cmbLayers.Width = 400;
            // Pozycja X ComboBoxa (px)
            cmbLayers.Left = 20;
            // Pozycja Y ComboBoxa (px) – tuż pod etykietą
            cmbLayers.Top = 45;
            // Tryb DropDownList – użytkownik wybiera tylko z listy, nie wpisuje dowolnego tekstu
            cmbLayers.DropDownStyle = WinForms.ComboBoxStyle.DropDownList;
            // Dodajemy ComboBox do okna
            this.Controls.Add(cmbLayers);

            // Tworzymy przycisk „Dodaj” – dodaje wybraną warstwę do listy
            btnAddLayer = new WinForms.Button();
            // Tekst przycisku
            btnAddLayer.Text = "Dodaj";
            // Szerokość przycisku (px)
            btnAddLayer.Width = 100;
            // Pozycja X przycisku (px) – obok ComboBoxa
            btnAddLayer.Left = cmbLayers.Right + 10;
            // Pozycja Y przycisku (px) – wyrównana do ComboBoxa
            btnAddLayer.Top = cmbLayers.Top;
            // Podpinamy zdarzenie kliknięcia – wywoła metodę dodającą warstwę do listy
            btnAddLayer.Click += (s, e) => AddSelectedLayer();
            // Dodajemy przycisk do okna
            this.Controls.Add(btnAddLayer);

            // Tworzymy ListView – lista wybranych warstw z ikoną koloru, nazwą i opisem
            lvLayers = new WinForms.ListView();
            // Ustawiamy widok „Szczegóły” – kolumny
            lvLayers.View = WinForms.View.Details;
            // Przypinamy listę małych ikon (nasze kolorowe kwadraty)
            lvLayers.SmallImageList = _images;
            // Zaznaczanie całych wierszy – wygodniejsze usuwanie
            lvLayers.FullRowSelect = true;
            // Dodajemy kolumny: (pusta na ikonę), „Warstwa”, „Opis”
            lvLayers.Columns.Add(" ", 30);
            lvLayers.Columns.Add("Warstwa", 200);
            lvLayers.Columns.Add("Opis", 400);
            // Ustawiamy pozycję X listy (px)
            lvLayers.Left = 20;
            // Ustawiamy pozycję Y listy (px) – poniżej ComboBoxa
            lvLayers.Top = cmbLayers.Bottom + 15;
            // Szerokość listy (px)
            lvLayers.Width = 700;
            // Wysokość listy (px)
            lvLayers.Height = 200;
            // Dodajemy listę do okna
            this.Controls.Add(lvLayers);

            // Tworzymy przycisk usuwania zaznaczonej warstwy z listy
            btnRemove = new WinForms.Button();
            // Tekst przycisku
            btnRemove.Text = "Usuń zaznaczoną";
            // Szerokość przycisku (px)
            btnRemove.Width = 150;
            // Pozycja X przycisku – obok listy
            btnRemove.Left = lvLayers.Right + 10;
            // Pozycja Y przycisku – wyrównana do górnej krawędzi listy
            btnRemove.Top = lvLayers.Top;
            // Podpinamy zdarzenie kliknięcia – usunie wybraną pozycję z listy
            btnRemove.Click += (s, e) => RemoveSelectedLayer();
            // Dodajemy przycisk do okna
            this.Controls.Add(btnRemove);

            // Tworzymy grupę pól metryki dla czytelności
            var grp = new WinForms.GroupBox();
            // Tekst nagłówka grupy
            grp.Text = "Metryka rysunku";
            // Pozycja X grupy (px)
            grp.Left = 20;
            // Pozycja Y grupy (px) – poniżej listy warstw
            grp.Top = lvLayers.Bottom + 15;
            // Szerokość grupy (px)
            grp.Width = 820;
            // Wysokość grupy (px)
            grp.Height = 300;
            // Dodajemy grupę do okna
            this.Controls.Add(grp);

            // Funkcja lokalna: tworzy etykietę WinForms na zadanej pozycji
            WinForms.Label MakeLabel(string text, int x, int y)
            {
                // Tworzymy etykietę
                var l = new WinForms.Label();
                // Ustawiamy tekst etykiety
                l.Text = text;
                // Ustawiamy pozycję X/Y w obrębie grupy
                l.Left = x; l.Top = y;
                // Szerokość etykiety (px) – żeby tekst się mieścił
                l.Width = 160;
                // Zwracamy gotową etykietę
                return l;
            }

            // Funkcja lokalna: tworzy pole tekstowe WinForms o zadanej pozycji i szerokości
            WinForms.TextBox MakeTextBox(int x, int y, int w = 600)
            {
                // Tworzymy pole tekstowe
                var t = new WinForms.TextBox();
                // Ustawiamy pozycję X/Y
                t.Left = x; t.Top = y;
                // Ustawiamy szerokość (px)
                t.Width = w;
                // Zwracamy gotowe pole
                return t;
            }

            // Ustalamy „siatkę” rozmieszczenia wierszy pól metryki
            int xLabel = 20, xField = 180, rowH = 28, top = 30;

            // Dodajemy etykiety i pola do grupy – po kolei wszystkie wymagane pozycje
            var l1 = MakeLabel("Jednostka projektowa:", xLabel, top + rowH * 0); grp.Controls.Add(l1);
            tbJednostka = MakeTextBox(xField, top + rowH * 0); grp.Controls.Add(tbJednostka);

            var l2 = MakeLabel("Inwestor:", xLabel, top + rowH * 1); grp.Controls.Add(l2);
            tbInwestor = MakeTextBox(xField, top + rowH * 1); grp.Controls.Add(tbInwestor);

            var l3 = MakeLabel("Nazwa i adres obiektu:", xLabel, top + rowH * 2); grp.Controls.Add(l3);
            tbNazwaAdres = MakeTextBox(xField, top + rowH * 2); grp.Controls.Add(tbNazwaAdres);

            var l4 = MakeLabel("Tytuł rysunku:", xLabel, top + rowH * 3); grp.Controls.Add(l4);
            tbTytul = MakeTextBox(xField, top + rowH * 3); grp.Controls.Add(tbTytul);

            var l5 = MakeLabel("Projektant:", xLabel, top + rowH * 4); grp.Controls.Add(l5);
            tbProjektant = MakeTextBox(xField, top + rowH * 4); grp.Controls.Add(tbProjektant);

            var l6 = MakeLabel("Sprawdzający:", xLabel, top + rowH * 5); grp.Controls.Add(l6);
            tbSprawdzajacy = MakeTextBox(xField, top + rowH * 5); grp.Controls.Add(tbSprawdzajacy);

            var l7 = MakeLabel("Opracowujący:", xLabel, top + rowH * 6); grp.Controls.Add(l7);
            tbOpracowujacy = MakeTextBox(xField, top + rowH * 6); grp.Controls.Add(tbOpracowujacy);

            var l8 = MakeLabel("Data:", xLabel, top + rowH * 7); grp.Controls.Add(l8);
            tbData = MakeTextBox(xField, top + rowH * 7, 200); grp.Controls.Add(tbData);

            var l9 = MakeLabel("Skala:", xLabel, top + rowH * 8); grp.Controls.Add(l9);
            tbSkala = MakeTextBox(xField, top + rowH * 8, 200); grp.Controls.Add(tbSkala);

            var l10 = MakeLabel("Numer rysunku:", xLabel, top + rowH * 9); grp.Controls.Add(l10);
            tbNumer = MakeTextBox(xField, top + rowH * 9, 200); grp.Controls.Add(tbNumer);

            // Tworzymy przycisk OK (zatwierdza wybór i zamyka okno)
            btnOk = new WinForms.Button();
            // Tekst na przycisku
            btnOk.Text = "OK – wstaw do rysunku";
            // Szerokość przycisku (px)
            btnOk.Width = 200;
            // Pozycja X przycisku – prawa część okna
            btnOk.Left = this.Width - 260;
            // Pozycja Y przycisku – na dole okna
            btnOk.Top = this.Height - 100;
            // Po kliknięciu zbieramy dane i zamykamy okno z DialogResult.OK
            btnOk.Click += (s, e) => OnOk();
            // Dodajemy przycisk OK do okna
            this.Controls.Add(btnOk);

            // Tworzymy przycisk Anuluj (zamyka okno bez zmian)
            btnCancel = new WinForms.Button();
            // Tekst na przycisku
            btnCancel.Text = "Anuluj";
            // Szerokość przycisku (px)
            btnCancel.Width = 120;
            // Pozycja X przycisku – na lewo od OK
            btnCancel.Left = btnOk.Left - 140;
            // Pozycja Y przycisku – wyrównana do OK
            btnCancel.Top = btnOk.Top;
            // Po kliknięciu ustawiamy wynik dialogu na Cancel i zamykamy
            btnCancel.Click += (s, e) => { this.DialogResult = WinForms.DialogResult.Cancel; this.Close(); };
            // Dodajemy przycisk Anuluj do okna
            this.Controls.Add(btnCancel);
        }

        // Metoda pomocnicza – wypełnia ComboBox listą nazw wszystkich warstw
        private void LoadLayersToCombo()
        {
            // Czyścimy ewentualną starą zawartość
            cmbLayers.Items.Clear();
            // Dodajemy nazwy warstw z listy wejściowej
            foreach (var li in _allLayers)
            {
                // Dodajemy nazwę warstwy jako pozycję w ComboBox
                cmbLayers.Items.Add(li.Name);
            }
            // Jeśli są pozycje – ustawiamy wybór na pierwszą, by ułatwić szybkie dodanie
            if (cmbLayers.Items.Count > 0)
                cmbLayers.SelectedIndex = 0;
        }

        // Obsługa przycisku „Dodaj” – przerzuca wybraną warstwę z ComboBoxa do ListView
        private void AddSelectedLayer()
        {
            // Jeśli nic nie wybrano w ComboBox – kończymy bez akcji
            if (cmbLayers.SelectedItem == null) return;

            // Odczytujemy nazwę wybranej warstwy
            string name = cmbLayers.SelectedItem.ToString();

            // Sprawdzamy, czy już jest na liście – unikamy duplikatów
            foreach (WinForms.ListViewItem it in lvLayers.Items)
            {
                // Kolumna 1 (druga) zawiera nazwę warstwy
                if (it.SubItems[1].Text == name)
                    return;
            }

            // Pobieramy pełny obiekt warstwy z mapy po nazwie
            var li = _byName[name];

            // Tworzymy nowy wiersz ListView
            var item = new WinForms.ListViewItem();
            // Ustawiamy ikonę (kolorowy kwadracik) – kluczem jest nazwa warstwy
            item.ImageKey = name;
            // Druga kolumna: nazwa warstwy
            item.SubItems.Add(li.Name);
            // Trzecia kolumna: opis lub „(brak opisu)”, gdy pusty
            item.SubItems.Add(string.IsNullOrWhiteSpace(li.Description) ? "(brak opisu)" : li.Description);

            // Dodajemy gotowy wiersz do listy
            lvLayers.Items.Add(item);
        }

        // Obsługa przycisku „Usuń zaznaczoną” – usuwa wybraną pozycję z ListView
        private void RemoveSelectedLayer()
        {
            // Jeśli nie ma zaznaczenia – nic nie robimy
            if (lvLayers.SelectedItems.Count == 0) return;
            // Usuwamy pierwszy (jeden) zaznaczony element
            lvLayers.Items.Remove(lvLayers.SelectedItems[0]);
        }

        // Zbieramy wszystkie dane z okna i zwracamy je do komendy (DialogResult.OK)
        private void OnOk()
        {
            // Resetujemy wynik (na wypadek ponownego uruchomienia okna)
            ResultData = new LegendaData();

            // Zbieramy wybrane warstwy z ListView
            foreach (WinForms.ListViewItem it in lvLayers.Items)
            {
                // Nazwa warstwy znajduje się w drugiej kolumnie (SubItems[1])
                string name = it.SubItems[1].Text;
                // Pobieramy oryginalny obiekt LayerInfo po nazwie
                var li = _byName[name];
                // Dodajemy do listy wybranych w wyniku
                ResultData.SelectedLayers.Add(li);
            }

            // Zbieramy uzupełnione pola metryki z TextBoxów
            ResultData.JednostkaProjektowa = tbJednostka.Text;
            ResultData.Inwestor = tbInwestor.Text;
            ResultData.NazwaIAdresObiektu = tbNazwaAdres.Text;
            ResultData.TytulRysunku = tbTytul.Text;
            ResultData.Projektant = tbProjektant.Text;
            ResultData.Sprawdzajacy = tbSprawdzajacy.Text;
            ResultData.Opracowujacy = tbOpracowujacy.Text;
            ResultData.Data = tbData.Text;
            ResultData.Skala = tbSkala.Text;
            ResultData.NumerRysunku = tbNumer.Text;

            // Ustawiamy wynik dialogu na OK – komenda wstawi tabele
            this.DialogResult = WinForms.DialogResult.OK;
            // Zamykamy okno
            this.Close();
        }
    }
}
