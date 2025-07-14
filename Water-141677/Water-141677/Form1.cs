using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Zakupniczek
{
    public class Form1 : Form
    {
        private TabControl _zakladki;
        private ContextMenuStrip _menuUstawienia;
        private Button _btnUstawienia;
        private Button _btnDodaj;
        private TextBox _txtProdukt;
        private ComboBox _cmbKategoria;
        private Label _lblStatystyki;
        private bool _ciemnyTryb = false;
        private Button _btnZaznaczWszystko;
        private Button _btnUsunZaznaczone;
        private TrackBar _sliderSzerokosci;

        
        private string FolderList => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "listy");
        private string SciezkaPliku(string nazwaListy) => Path.Combine(FolderList, nazwaListy + ".json");

        public Form1()
        {
            InicjalizujKomponenty();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Directory.CreateDirectory(FolderList);
            ZaladujIstniejaceListy();
        }

        private void InicjalizujKomponenty()
        {
            MinimumSize = new Size(900, 600);
            Text = "Zakupniczek";
            StartPosition = FormStartPosition.CenterScreen;

            _txtProdukt = new TextBox { Location = new Point(10, 10), Width = 180 };
            _cmbKategoria = new ComboBox { Location = new Point(200, 10), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbKategoria.Items.AddRange(new[] { "Ogólne", "Spożywcze", "Chemia", "Inne" });
            _cmbKategoria.SelectedIndex = 0;

            _btnDodaj = new Button { Text = "Dodaj", Location = new Point(330, 10), Width = 70 };
            _btnDodaj.Click += (s, e) => DodajProdukt();

            _lblStatystyki = new Label { Location = new Point(410, 14), AutoSize = true, Font = new Font(Font, FontStyle.Italic) };

            _zakladki = new TabControl
            {
                Location = new Point(10, 45),
                Size = new Size(ClientSize.Width - 20, ClientSize.Height - 70),
                DrawMode = TabDrawMode.OwnerDrawFixed,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(180, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _zakladki.DrawItem += Zakladki_DrawItem;
            _zakladki.MouseDown += Zakladki_MouseDown;
            _zakladki.SelectedIndexChanged += (s, e) => AktualizujStatystyki();


            _menuUstawienia = new ContextMenuStrip();
            _menuUstawienia.Items.Add("Dodaj nową listę", null, (s, e) => DodajNowaListe());
            _menuUstawienia.Items.Add("Usuń listę", null, (s, e) => UsunListe());
            _menuUstawienia.Items.Add("Zmień nazwę listy", null, (s, e) => ZmienNazweListy());
            _menuUstawienia.Items.Add("Tryb jasny/ciemny", null, (s, e) => PrzelaczTryb());
            _menuUstawienia.Items.Add("Otwórz listę w osobnym oknie", null, (s, e) => OtworzListeWOknie());
            _menuUstawienia.Items.Add("Dodaj własną kategorię", null, (s, e) => DodajWlasnaKategorie());
            _menuUstawienia.Items.Add("Usuń kategorię", null, (s, e) => UsunKategorie());


            _sliderSzerokosci = new TrackBar
            {
                Minimum = 80,
                Maximum = 300,
                Value = 140,
                TickFrequency = 20,
                SmallChange = 10,
                LargeChange = 20,
                Width = 120,
                Location = new Point(10, ClientSize.Height - 45),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            
            _btnZaznaczWszystko = new Button
            {
                Text = "Zaznacz wszystko",
                Width = 120,
                Location = new Point(ClientSize.Width - 310, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _btnZaznaczWszystko.Click += (s, e) =>
            {
                var lista = _zakladki.SelectedTab?.Controls.OfType<ListView>().FirstOrDefault();
                if (lista != null)
                    foreach (ListViewItem item in lista.Items)
                        item.Checked = true;
            };

            _btnUsunZaznaczone = new Button
            {
                Text = "Usuń zaznaczone",
                Width = 120,
                Location = new Point(ClientSize.Width - 180, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _btnUsunZaznaczone.Click += (s, e) =>
            {
                var lista = _zakladki.SelectedTab?.Controls.OfType<ListView>().FirstOrDefault();
                if (lista == null) return;

                var toRemove = lista.CheckedItems.Cast<ListViewItem>().ToList();
                if (toRemove.Count == 0) return;

                var confirm = MessageBox.Show(
                    $"Usunąć {toRemove.Count} zaznaczonych pozycji?",
                    "Potwierdź",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirm != DialogResult.Yes) return;

                foreach (var item in toRemove)
                    lista.Items.Remove(item);

                ZapiszListeZZakladki(_zakladki.SelectedTab);
                AktualizujStatystyki();
            };
            _btnUstawienia = new Button
            {
                Text = "⚙",
                Width = 30,
                Height = 25,
                Location = new Point(Width - 60, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnUstawienia.Click += (s, e) => _menuUstawienia.Show(_btnUstawienia, 0, _btnUstawienia.Height);

            Controls.AddRange(new Control[]
            {
                _txtProdukt,
                _cmbKategoria,
                _btnDodaj,
                _lblStatystyki,
                _zakladki,
                _btnUstawienia,
                _sliderSzerokosci,
                _btnZaznaczWszystko,
                _btnUsunZaznaczone
            });
        }
        private void DodajProdukt()
        {
            if (_zakladki.SelectedTab == null) return;

            string produkt = _txtProdukt.Text.Trim();
            if (string.IsNullOrWhiteSpace(produkt)) return;

            string kategoria = _cmbKategoria.SelectedItem?.ToString() ?? "Ogólne";

            var lista = _zakladki.SelectedTab.Controls.OfType<ListView>().FirstOrDefault();
            if (lista != null)
            {
                var grupa = lista.Groups.Cast<ListViewGroup>().FirstOrDefault(g => g.Header == kategoria);
                if (grupa == null)
                {
                    grupa = new ListViewGroup(kategoria, HorizontalAlignment.Left);
                    lista.Groups.Add(grupa);
                }

                var item = new ListViewItem(produkt) { Group = grupa };
                lista.Items.Add(item);

                _txtProdukt.Clear();
                _txtProdukt.Focus();
                ZapiszListeZZakladki(_zakladki.SelectedTab);
                AktualizujStatystyki();
            }
        }
        private void AktualizujStatystyki()
        {
            if (_zakladki.SelectedTab == null)
            {
                _lblStatystyki.Text = "";
                return;
            }

            var lista = _zakladki.SelectedTab.Controls.OfType<ListView>().FirstOrDefault();
            if (lista == null)
            {
                _lblStatystyki.Text = "";
                return;
            }

            int zaznaczone = lista.CheckedItems.Count;
            int wszystkie = lista.Items.Count;
            _lblStatystyki.Text = $"Zaznaczone: {zaznaczone} / {wszystkie}";
        }
        private void PrzelaczTryb()
        {
            _ciemnyTryb = !_ciemnyTryb;

            var tlo = _ciemnyTryb ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
            var tekst = _ciemnyTryb ? Color.White : SystemColors.ControlText;
            var tloListy = _ciemnyTryb ? Color.FromArgb(20, 20, 20) : Color.White;

            BackColor = tlo;
            ForeColor = tekst;

            foreach (Control ctrl in Controls)
            {
                if (ctrl is TabControl) continue;
                ctrl.BackColor = tlo;
                ctrl.ForeColor = tekst;
            }

            foreach (TabPage tab in _zakladki.TabPages)
            {
                foreach (ListView lista in tab.Controls.OfType<ListView>())
                {
                    lista.BackColor = tloListy;
                    lista.ForeColor = tekst;
                }
            }
        }
        private void ZaladujIstniejaceListy()
        {
            foreach (var plik in Directory.GetFiles(FolderList, "*.json"))
            {
                string nazwa = Path.GetFileNameWithoutExtension(plik);
                var tab = new TabPage(nazwa) { Name = nazwa };
                _zakladki.TabPages.Add(tab);
                WczytajListeDoZakladki(tab);
            }
            if (_zakladki.TabCount == 0)
                DodajNowaListe();
        }
        private void DodajNowaListe()
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Podaj nazwę nowej listy:", "Nowa lista zakupów", "Moja lista");
            if (string.IsNullOrWhiteSpace(input)) return;

            string nowa = input.Trim();
            if (ListaIstnieje(nowa)) {
                MessageBox.Show("Lista o takiej nazwie już istnieje.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var tab = new TabPage(nowa) { Name = nowa };
            _zakladki.TabPages.Add(tab);
            _zakladki.SelectedTab = tab;

            WczytajListeDoZakladki(tab);
        }
        private bool ListaIstnieje(string nazwa)
        {
            foreach (TabPage tab in _zakladki.TabPages)
                if (tab.Text.Equals(nazwa, StringComparison.OrdinalIgnoreCase))
                    return true;
            return File.Exists(SciezkaPliku(nazwa));
        }
        private void WczytajListeDoZakladki(TabPage tab)
        {
            ListView lista = new ListView
            {
                CheckBoxes = true,
                View = View.Details,
                Dock = DockStyle.Fill,
                FullRowSelect = true
            };
            var menu = new ContextMenuStrip();
            menu.Items.Add("Usuń zaznaczone", null, (s, e) =>
            {
                foreach (ListViewItem item in lista.SelectedItems)
                    lista.Items.Remove(item);

                ZapiszListeZZakladki(tab);
                AktualizujStatystyki();
            });
            lista.ContextMenuStrip = menu;
            lista.Columns.Clear();
            lista.Columns.Add("Produkt", lista.ClientSize.Width - 4);
            lista.HeaderStyle = ColumnHeaderStyle.None;

            tab.Controls.Add(lista);
            lista.ItemChecked += (s, e) => {
                ZapiszListeZZakladki(tab);
                AktualizujStatystyki();
            };
            lista.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && lista.SelectedItems.Count > 0)
                {
                    foreach (ListViewItem item in lista.SelectedItems)
                        lista.Items.Remove(item);

                    ZapiszListeZZakladki(tab);
                    AktualizujStatystyki();
                }
            };
            string path = SciezkaPliku(tab.Text);
            if (File.Exists(path))
            {
                foreach (var linia in File.ReadAllLines(path))
                {
                    string[] czesci = linia.Split(new[] { "||" }, StringSplitOptions.None);
                    if (czesci.Length < 2) continue;

                    string kategoria = czesci[0];
                    string produkt = czesci[1];

                    var grupa = lista.Groups.Cast<ListViewGroup>().FirstOrDefault(g => g.Header == kategoria);
                    if (grupa == null)
                    {
                        grupa = new ListViewGroup(kategoria, HorizontalAlignment.Left);
                        lista.Groups.Add(grupa);
                    }

                    var item = new ListViewItem(produkt) { Group = grupa };
                    lista.Items.Add(item);
                }
            }
        }
        private void ZapiszListeZZakladki(TabPage tab)
        {
            var lista = tab.Controls.OfType<ListView>().FirstOrDefault();
            if (lista == null) return;

            var dane = lista.Items.Cast<ListViewItem>()
                .Select(i => $"{i.Group.Header}||{i.Text}")
                .ToArray();

            File.WriteAllLines(SciezkaPliku(tab.Text), dane);
        }
        private void Zakladki_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tab = _zakladki.TabPages[e.Index];
            Rectangle r = _zakladki.GetTabRect(e.Index);
            StringFormat sf = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

            int xWidth = 18;
            int margin = 6;

            Rectangle textRect = new Rectangle(r.X + margin, r.Y + 4, r.Width - xWidth - 2 * margin, r.Height - 4);
            Rectangle closeRect = new Rectangle(r.Right - xWidth, r.Y + (r.Height - 12) / 2, 12, 12);

            var background = (e.State & DrawItemState.Selected) != 0 ? SystemBrushes.ControlLightLight : SystemBrushes.Control;
            e.Graphics.FillRectangle(background, r);
            e.Graphics.DrawString(tab.Text, Font, Brushes.Black, textRect, sf);
            e.Graphics.DrawString("✖", Font, Brushes.DarkRed, closeRect);
        }

        private void Zakladki_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < _zakladki.TabPages.Count; i++)
            {
                var r = _zakladki.GetTabRect(i);
                Rectangle closeRect = new Rectangle(r.Right - 20, r.Top + 5, 16, 16);
                if (closeRect.Contains(e.Location))
                {
                    var nazwa = _zakladki.TabPages[i].Text;
                    var confirm = MessageBox.Show($"Usunąć listę „{nazwa}”?", "Potwierdź", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (confirm == DialogResult.Yes)
                    {
                        string sciezka = SciezkaPliku(nazwa);
                        if (File.Exists(sciezka)) File.Delete(sciezka);
                        _zakladki.TabPages.RemoveAt(i);
                    }
                    return;
                }
            }
        }
        private void DodajWlasnaKategorie()
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Podaj nazwę nowej kategorii:", "Dodaj kategorię", "Nowa kategoria");
            if (string.IsNullOrWhiteSpace(input)) return;

            string nowa = input.Trim();
            if (!_cmbKategoria.Items.Contains(nowa))
            {
                _cmbKategoria.Items.Add(nowa);
                _cmbKategoria.SelectedItem = nowa;
            }
            else
            {
                MessageBox.Show("Taka kategoria już istnieje.", "Uwaga", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _cmbKategoria.SelectedItem = nowa;
            }
        }
        private void UsunKategorie()
        {
            if (_cmbKategoria.Items.Count == 0)
            {
                MessageBox.Show("Brak kategorii do usunięcia.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string aktualna = _cmbKategoria.SelectedItem?.ToString();
            var lista = _cmbKategoria.Items.Cast<string>().ToList();

            string wybierz = Microsoft.VisualBasic.Interaction.InputBox(
                "Wpisz nazwę kategorii do usunięcia:",
                "Usuń kategorię",
                aktualna
            );

            if (string.IsNullOrWhiteSpace(wybierz)) return;

            if (!_cmbKategoria.Items.Contains(wybierz))
            {
                MessageBox.Show("Taka kategoria nie istnieje.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Czy na pewno usunąć kategorię „{wybierz}” z listy?",
                "Potwierdzenie",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm == DialogResult.Yes)
            {
                _cmbKategoria.Items.Remove(wybierz);

                if (_cmbKategoria.Items.Count > 0)
                    _cmbKategoria.SelectedIndex = 0;
            }
        }
        private void UsunListe()
        {
            if (_zakladki.SelectedTab == null) return;

            var nazwa = _zakladki.SelectedTab.Text;
            var confirm = MessageBox.Show($"Na pewno usunąć listę „{nazwa}”?", "Potwierdź", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            string path = SciezkaPliku(nazwa);
            if (File.Exists(path)) File.Delete(path);

            _zakladki.TabPages.Remove(_zakladki.SelectedTab);
        }
        private void ZmienNazweListy()
        {
            if (_zakladki.SelectedTab == null) return;

            var stara = _zakladki.SelectedTab.Text;
            var input = Microsoft.VisualBasic.Interaction.InputBox("Nowa nazwa listy:", "Zmień nazwę", stara);
            if (string.IsNullOrWhiteSpace(input)) return;

            string nowa = input.Trim();
            if (nowa == stara) return;

            if (ListaIstnieje(nowa))
            {
                MessageBox.Show("Lista o takiej nazwie już istnieje.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string staryPlik = SciezkaPliku(stara);
            string nowyPlik = SciezkaPliku(nowa);

            if (File.Exists(staryPlik))
                File.Move(staryPlik, nowyPlik);

            _zakladki.SelectedTab.Text = nowa;
            _zakladki.SelectedTab.Name = nowa;
        }
        private void OtworzListeWOknie()
        {
            if (_zakladki.SelectedTab == null) return;

            string nazwa = _zakladki.SelectedTab.Text;
            string path = SciezkaPliku(nazwa);
            new ZakupowaListaForm(nazwa, path).Show();
        }
        private void Zakladki_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            var tab = _zakladki.TabPages[e.Index];
            int padding = 35; 
            using (Graphics g = _zakladki.CreateGraphics())
            {
                SizeF textSize = g.MeasureString(tab.Text, Font);
                e.ItemWidth = (int)textSize.Width + padding;
                e.ItemHeight = 30;
            }
        }
    }
}
