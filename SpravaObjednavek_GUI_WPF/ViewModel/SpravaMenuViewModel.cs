using Microsoft.Win32; // Pro OpenFileDialog
using SpravaObjednavek_GUI_WPF.Services;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging; // Pro obrázky

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class SpravaMenuViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        // Vlastnosti pro formulář
        public string NazevPolozky { get; set; }
        public string CenaPolozkyInput { get; set; } // String, aby se lépe mazalo

        // Cesta k vybranému souboru (jen pro info)
        public string CestaKObrazku { get; set; }

        // Náhled obrázku pro UI
        private BitmapImage _nahledObrazku;
        public BitmapImage NahledObrazku
        {
            get => _nahledObrazku;
            set { _nahledObrazku = value; OnPropertyChanged(); }
        }

        // Příkazy
        public ICommand VybratObrazekCommand { get; set; }
        public ICommand UlozitPolozkuCommand { get; set; }

        public SpravaMenuViewModel()
        {
            _dataService = new DataService();
            VybratObrazekCommand = new RelayCommand(VybratObrazek);
            UlozitPolozkuCommand = new RelayCommand(UlozitPolozku);
        }

        private void VybratObrazek(object obj)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Obrázky|*.jpg;*.jpeg;*.png;*.bmp"; // Filtr souborů

            if (dlg.ShowDialog() == true)
            {
                CestaKObrazku = dlg.FileName;
                OnPropertyChanged(nameof(CestaKObrazku));

                // Zobrazíme náhled v aplikaci
                NahledObrazku = new BitmapImage(new Uri(CestaKObrazku));
            }
        }

        private void UlozitPolozku(object obj)
        {
            // 1. Validace
            if (string.IsNullOrWhiteSpace(NazevPolozky) || string.IsNullOrWhiteSpace(CenaPolozkyInput))
            {
                MessageBox.Show("Zadejte název a cenu.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(CenaPolozkyInput, out decimal cena))
            {
                MessageBox.Show("Cena musí být číslo.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int? imageId = null;

                // 1. Nahrání obrázku (pokud existuje)
                if (!string.IsNullOrEmpty(CestaKObrazku))
                {
                    string autor = App.PrihlasenyJmeno ?? "Admin";
                    imageId = _dataService.NahratObrazek(CestaKObrazku, autor);
                }

                // 2. Vytvoření položky v tabulce ITEM - TOTO JSME PŘIDALI
                _dataService.VytvoritPolozkuMenu(NazevPolozky, cena, imageId);

                MessageBox.Show("Položka uložena do menu.", "Hotovo");

                // Vyčistit formulář
                NazevPolozky = "";
                CenaPolozkyInput = "";
                CestaKObrazku = "";
                NahledObrazku = null;

                // Aktualizace UI (NotifyPropertyChanged)
                OnPropertyChanged(nameof(NazevPolozky));
                OnPropertyChanged(nameof(CenaPolozkyInput));
                OnPropertyChanged(nameof(CestaKObrazku));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
            }
        }
    }
}