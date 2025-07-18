﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Zakupniczek
{
    public class ZakupowaListaForm : Form
    {
        private readonly string _nazwaListy;
        private readonly string _sciezka;
        private ListView _lista;

        public ZakupowaListaForm(string nazwa, string sciezkaPliku)
        {
            _nazwaListy = nazwa;
            _sciezka = sciezkaPliku;

            Text = $"Lista: {_nazwaListy}";
            Width = 400;
            Height = 500;
            StartPosition = FormStartPosition.CenterParent;

            _lista = new ListView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = true,
                View = View.List
            };
            _lista.ItemChecked += (s, e) => Zapisz();

            Controls.Add(_lista);

            Wczytaj();
        }

        private void Wczytaj()
        {
            if (!File.Exists(_sciezka))
                return;

            var linie = File.ReadAllLines(_sciezka);

            foreach (var linia in linie)
            {
                var czesci = linia.Split(new[] { "||" }, StringSplitOptions.None);
                if (czesci.Length >= 2)
                {
                    var item = new ListViewItem(czesci[1]) { Tag = czesci[0] };
                    _lista.Items.Add(item);
                }
                else
                {
                    _lista.Items.Add(new ListViewItem(linia));
                }
            }
        }

        private void Zapisz()
        {
            var dane = _lista.Items
                .Cast<ListViewItem>()
                .Select(i => $"{(string?)i.Tag ?? "Ogólne"}||{i.Text}");

            File.WriteAllLines(_sciezka, dane);
        }
    }
}