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

        // --- Existující vlastnosti ---
        public string Jmeno { get; set; }
        public ObservableCollection<string> DostupneRole { get; set; }
        public string VybranaRole { get; set; }

        // --- NOVÉ VLASTNOSTI ---
        // Používám string pro jednodušší binding v TextBoxu (aby tam nebylo defaultně 0)
        public string LicenseIdInput { get; set; }
        public string AddressIdInput { get; set; }
        public string Poznamka { get; set; }
        // -----------------------

        public ICommand RegistrovatCommand { get; set; }

        public RegistraceViewModel()
        {
            _dataService = new DataService();

            DostupneRole = new ObservableCollection<string>
            {
                "USER",
                "ADMINISTRATOR"
            };

            VybranaRole = "USER";
            
            RegistrovatCommand = new RelayCommand(Registrovat);
        }

        private void Registrovat(object parameter)
        {
            var passwordBox = parameter as PasswordBox;
            string heslo = passwordBox?.Password;

            // 1. Validace povinných polí
            if (string.IsNullOrWhiteSpace(Jmeno) || string.IsNullOrWhiteSpace(heslo) ||
                string.IsNullOrWhiteSpace(LicenseIdInput) || string.IsNullOrWhiteSpace(AddressIdInput))
            {
                MessageBox.Show("Vyplňte všechna povinná pole (Jméno, Heslo, License ID, Address ID).", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Převod čísel
            if (!int.TryParse(LicenseIdInput, out int licenseId) || !int.TryParse(AddressIdInput, out int addressId))
            {
                MessageBox.Show("License ID a Address ID musí být čísla.", "Chyba formátu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Volání služby s novými parametry
                _dataService.RegistrovatUzivatele(Jmeno, heslo, VybranaRole, licenseId, addressId, Poznamka);

                MessageBox.Show($"Uživatel {Jmeno} byl úspěšně registrován.", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);

                // Vyčištění formuláře
                Jmeno = string.Empty;
                OnPropertyChanged(nameof(Jmeno));

                LicenseIdInput = string.Empty;
                OnPropertyChanged(nameof(LicenseIdInput));

                AddressIdInput = string.Empty;
                OnPropertyChanged(nameof(AddressIdInput));

                Poznamka = string.Empty;
                OnPropertyChanged(nameof(Poznamka));

                passwordBox.Password = string.Empty;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Chyba při registraci: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}