using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class RegistraceViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        // --- Původní vlastnosti ---
        public string Jmeno { get; set; } // + NotifyPropertyChanged (zkráceno pro přehlednost)
        public ObservableCollection<string> DostupneRole { get; set; }
        public string VybranaRole { get; set; }
        public string Poznamka { get; set; } // + NotifyPropertyChanged

        // --- ADRESY (Už máme hotové) ---
        public ObservableCollection<Adresa> SeznamAdres { get; set; }
        private Adresa _vybranaAdresa;
        public Adresa VybranaAdresa
        {
            get => _vybranaAdresa;
            set { _vybranaAdresa = value; OnPropertyChanged(); }
        }

        // --- NOVÉ: LICENCE (Místo stringu LicenseIdInput) ---
        public ObservableCollection<LicenceV2> SeznamLicenci { get; set; }

        private LicenceV2 _vybranaLicence;
        public LicenceV2 VybranaLicence
        {
            get => _vybranaLicence;
            set { _vybranaLicence = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Manazer> SeznamManazeru { get; set; }

        private Manazer _vybranyManazer;
        public Manazer VybranyManazer
        {
            get => _vybranyManazer;
            set { _vybranyManazer = value; OnPropertyChanged(); }
        }

        public ICommand RegistrovatCommand { get; set; }

        public RegistraceViewModel()
        {
            _dataService = new DataService();
            SeznamAdres = new ObservableCollection<Adresa>();
            SeznamLicenci = new ObservableCollection<LicenceV2>(); // Inicializace

            DostupneRole = new ObservableCollection<string> { "USER", "ADMINISTRATOR" };
            VybranaRole = "USER";

            SeznamManazeru = new ObservableCollection<Manazer>();

            RegistrovatCommand = new RelayCommand(Registrovat);

            // Načtení dat do ComboBoxů
            NacistCiselniky();
        }

        private void NacistCiselniky()
        {
            try
            {
                // 1. Adresy
                var adresy = _dataService.NacistAdresy();
                SeznamAdres.Clear();
                foreach (var adr in adresy) SeznamAdres.Add(adr);

                // 2. Licence (Voláme novou metodu)
                var licence = _dataService.NacistVsechnyLicence();
                SeznamLicenci.Clear();
                foreach (var lic in licence) SeznamLicenci.Add(lic);

                // 3. Manažeři
                var manazeri = _dataService.NacistVsechnyManazery();
                SeznamManazeru.Clear();
                foreach (var m in manazeri) SeznamManazeru.Add(m);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Chyba při načítání číselníků: " + ex.Message);
            }
        }

        private void Registrovat(object parameter)
        {
            var passwordBox = parameter as PasswordBox;
            string heslo = passwordBox?.Password;

            // Validace: Kontrolujeme, zda je vybrána Adresa I Licence
            if (string.IsNullOrWhiteSpace(Jmeno) || string.IsNullOrWhiteSpace(heslo) ||
                VybranaAdresa == null || VybranaLicence == null)
            {
                MessageBox.Show("Vyplňte jméno, heslo a vyberte Adresu i Licenci.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int? managerId = VybranaLicence != null ? VybranyManazer.Id : (int?)null;
                // ODESLÁNÍ DO DB:
                // Bereme VybranaLicence.Id a VybranaAdresa.Id
                _dataService.RegistrovatUzivatele(
                    Jmeno,
                    heslo,
                    VybranaRole,
                    VybranaLicence.Id,
                    VybranaAdresa.Id,
                    managerId,
                    Poznamka
                );

                MessageBox.Show($"Uživatel {Jmeno} registrován.", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reset formuláře
                Jmeno = string.Empty; OnPropertyChanged(nameof(Jmeno));
                Poznamka = string.Empty; OnPropertyChanged(nameof(Poznamka));
                VybranaAdresa = null;
                VybranaLicence = null; // Reset licence
                if (passwordBox != null) passwordBox.Password = string.Empty;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Chyba registrace: " + ex.Message);
            }
        }
    }
}