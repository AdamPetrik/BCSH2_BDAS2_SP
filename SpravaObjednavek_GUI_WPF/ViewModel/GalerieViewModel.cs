using Microsoft.Win32; // Pro SaveFileDialog
using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class GalerieViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        public ObservableCollection<PolozkaGalerie> Polozky { get; set; }

        public ICommand StahnoutCommand { get; set; }

        public GalerieViewModel()
        {
            _dataService = new DataService();
            Polozky = new ObservableCollection<PolozkaGalerie>();
            StahnoutCommand = new RelayCommand(StahnoutObrazek);

            _ = NacistDataAsync();
        }

        private async Task NacistDataAsync()
        {
            try
            {
                // 1. TĚŽKÁ PRÁCE NA POZADÍ (Stahování MB dat z DB)
                var data = await Task.Run(() => _dataService.NacistGalerii());

                // 2. LEHKÁ PRÁCE NA HLAVNÍM VLÁKNĚ (Vytvoření obrázků)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Polozky.Clear();
                    foreach (var item in data)
                    {
                        // Pokud má data, vytvoříme z nich obrázek TEĎ a TADY (bezpečně)
                        if (item.ObrazekData != null && item.ObrazekData.Length > 0)
                        {
                            item.ObrazekSource = VytvoritObrazekZByte(item.ObrazekData);
                        }

                        Polozky.Add(item);
                    }
                });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Chyba: {ex.Message}");
            }
        }

        // Pomocná metoda pro převod (běží na UI vlákně)
        private BitmapImage VytvoritObrazekZByte(byte[] data)
        {
            try
            {
                using (var ms = new MemoryStream(data))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.DecodePixelWidth = 200; // Šetříme paměť (náhled)
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    return image;
                }
            }
            catch
            {
                return null; // Když je obrázek vadný, vrátíme nic
            }
        }

        private void StahnoutObrazek(object parameter)
        {
            if (parameter is PolozkaGalerie polozka && polozka.MaObrazek)
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.FileName = polozka.NazevSouboru; // Předvyplní název z DB
                dialog.DefaultExt = "." + polozka.Pripona;
                dialog.Filter = $"Obrázky|*.{polozka.Pripona}|Všechny soubory|*.*";

                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        // Zápis surových dat (BLOB) na disk
                        File.WriteAllBytes(dialog.FileName, polozka.ObrazekData);
                        MessageBox.Show("Obrázek byl stažen.", "Hotovo");
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("Chyba při ukládání: " + ex.Message);
                    }
                }
            }
        }
    }
}