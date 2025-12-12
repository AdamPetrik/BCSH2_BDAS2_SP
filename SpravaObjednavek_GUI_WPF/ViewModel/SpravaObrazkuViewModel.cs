using Microsoft.Win32; // Pro OpenFileDialog
using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class SpravaObrazkuViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        public ObservableCollection<ObrazekMeta> SeznamObrazku { get; set; }

        // Editace
        public string EditNazev { get; set; }

        // Náhled obrázku
        private BitmapImage _nahled;
        public BitmapImage Nahled
        {
            get => _nahled;
            set { _nahled = value; OnPropertyChanged(); }
        }

        private ObrazekMeta _vybranyObrazek;
        public ObrazekMeta VybranyObrazek
        {
            get => _vybranyObrazek;
            set
            {
                _vybranyObrazek = value;
                OnPropertyChanged();

                if (_vybranyObrazek != null)
                {
                    EditNazev = _vybranyObrazek.NazevSouboru;
                    NacistNahled(_vybranyObrazek.Id); // Načteme BLOB až teď
                }
                else
                {
                    Nahled = null;
                    EditNazev = "";
                }
                OnPropertyChanged(nameof(EditNazev));
            }
        }

        public ICommand UlozitCommand { get; set; }
        public ICommand SmazatCommand { get; set; }
        public ICommand NahratNovyCommand { get; set; }

        public SpravaObrazkuViewModel()
        {
            _dataService = new DataService();
            SeznamObrazku = new ObservableCollection<ObrazekMeta>();

            UlozitCommand = new RelayCommand(Ulozit);
            SmazatCommand = new RelayCommand(Smazat);
            NahratNovyCommand = new RelayCommand(NahratNovy);

            NacistData();
        }

        private void NacistData()
        {
            var data = _dataService.NacistSeznamObrazku();
            SeznamObrazku.Clear();
            foreach (var item in data) SeznamObrazku.Add(item);
        }

        private void NacistNahled(int id)
        {
            try
            {
                byte[] data = _dataService.NacistDataObrazku(id);
                if (data != null && data.Length > 0)
                {
                    using (var ms = new MemoryStream(data))
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.DecodePixelWidth = 300; // Optimalizace paměti
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = ms;
                        image.EndInit();
                        image.Freeze();
                        Nahled = image;
                    }
                }
                else Nahled = null;
            }
            catch { Nahled = null; }
        }

        private void NahratNovy(object obj)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Obrázky|*.jpg;*.png;*.bmp";
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    // Použijeme existující metodu NahratObrazek z DataService
                    string autor = App.PrihlasenyJmeno ?? "Admin";
                    _dataService.NahratObrazek(dlg.FileName, autor);

                    MessageBox.Show("Obrázek nahrán.");
                    NacistData();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Chyba nahrávání: " + ex.Message);
                }
            }
        }

        private void Ulozit(object obj)
        {
            if (VybranyObrazek == null) return;

            try
            {
                _dataService.UpravitObrazek(VybranyObrazek.Id, EditNazev);
                NacistData();
                MessageBox.Show("Název upraven.");
                // Znovu vybereme, aby nezmizel náhled (zjednodušeně)
                // V praxi bychom jen aktualizovali objekt v kolekci
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
            }
        }

        private void Smazat(object obj)
        {
            if (VybranyObrazek == null) return;

            var res = MessageBox.Show("Opravdu smazat obrázek? Pokud je použit u jídla, akce se nemusí zdařit.", "Pozor", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.SmazatObrazek(VybranyObrazek.Id);
                    NacistData();
                    VybranyObrazek = null;
                    MessageBox.Show("Smazáno.");
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Nelze smazat (pravděpodobně je obrázek přiřazen k jídlu).\n" + ex.Message);
                }
            }
        }
    }
}