using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Xunit;
using Zakupniczek;

namespace Water_141677.Tests
{
    public class Form1Tests
    {
        [Fact]
        public void Save_and_load_roundtrip_preserves_items()
        {
            // Arrange
            var form = new Form1();
            string listName = $"TestList_{Guid.NewGuid():N}";
            var tab = new TabPage(listName);
            var listView = new ListView { Dock = DockStyle.Fill };
            tab.Controls.Add(listView);
            var group = new ListViewGroup("Og\u00f3lne", HorizontalAlignment.Left);
            listView.Groups.Add(group);
            listView.Items.Add(new ListViewItem("Item1") { Group = group });
            listView.Items.Add(new ListViewItem("Item2") { Group = group });

            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "listy");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, listName + ".json");

            // Act - save
            var save = typeof(Form1).GetMethod("ZapiszListeZZakladki", BindingFlags.Instance | BindingFlags.NonPublic);
            save.Invoke(form, new object[] { tab });

            Assert.True(File.Exists(path));

            // Act - load
            var loadTab = new TabPage(listName);
            var load = typeof(Form1).GetMethod("WczytajListeDoZakladki", BindingFlags.Instance | BindingFlags.NonPublic);
            load.Invoke(form, new object[] { loadTab });

            // Assert
            var loadedList = loadTab.Controls.OfType<ListView>().FirstOrDefault();
            Assert.NotNull(loadedList);
            Assert.Equal(listView.Items.Count, loadedList.Items.Count);
            Assert.Equal(listView.Items[0].Text, loadedList.Items[0].Text);
            Assert.Equal(listView.Items[1].Text, loadedList.Items[1].Text);

            // Cleanup
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
