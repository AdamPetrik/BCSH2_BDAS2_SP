using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class KasaViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        // Seznam tlačítek (Menu) - načte se z DB
        public ObservableCollection<PolozkaMenu> MenuPolozky { get; set; }

        // Seznam účtenky (Košík)
        public ObservableCollection<PolozkaKosiku> KosikPolozky { get; set; }
        public ICommand OdebratZKosikuCommand { get; set; }
        public ICommand ZaplatitCommand { get; set; }

        // Celková cena (musíme ji aktualizovat při každé změně)
        private decimal _celkovaCena;
        public decimal CelkovaCena
        {
            get => _celkovaCena;
            set { _celkovaCena = value; OnPropertyChanged(); }
        }

        // Příkazy
        public ICommand PridatDoKosikuCommand { get; set; }

        public KasaViewModel()
        {
            _dataService = new DataService();
            MenuPolozky = new ObservableCollection<PolozkaMenu>();
            KosikPolozky = new ObservableCollection<PolozkaKosiku>();

            // Načtení dat z DB hned při startu
            NacistData();

            // Inicializace příkazu
            PridatDoKosikuCommand = new RelayCommand(PridatPolozku);
            OdebratZKosikuCommand = new RelayCommand(OdebratPolozku);
            ZaplatitCommand = new RelayCommand(Zaplatit);
        }

        private void NacistData()
        {
            var data = _dataService.NacistMenuZDatabaze();
            MenuPolozky.Clear();
            foreach (var item in data)
            {
                MenuPolozky.Add(item);
            }
        }

        private void PridatPolozku(object parameter)
        {
            if (parameter is PolozkaMenu vybranaPolozka)
            {
                // Zkusíme najít, jestli už v košíku není
                var existujici = KosikPolozky.FirstOrDefault(p => p.Nazev == vybranaPolozka.Nazev);

                if (existujici != null)
                {
                    existujici.Pocet++; // Jen zvýšíme počet
                }
                else
                {
                    // Vytvoříme novou položku v košíku
                    KosikPolozky.Add(new PolozkaKosiku
                    {
                        Id = vybranaPolozka.Id,
                        Nazev = vybranaPolozka.Nazev,
                        CenaZaKus = vybranaPolozka.Cena,
                        Pocet = 1
                    });
                }
                PrepocitatCelkem();
            }
        }

        private void OdebratPolozku(object parameter)
        {
            if (parameter is PolozkaKosiku polozka)
            {
                // Odstraníme položku z kolekce
                KosikPolozky.Remove(polozka);

                // A musíme znovu přepočítat celkovou cenu
                PrepocitatCelkem();
            }
        }

        private void Zaplatit(object parameter)
        {
            // 1. Validace: Je košík prázdný?
            if (KosikPolozky.Count == 0)
            {
                MessageBox.Show("Košík je prázdný, nelze vytvořit objednávku.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Získání typu platby z parametru tlačítka (CASH nebo CARD)
            string typPlatby = parameter as string;

            // Pro jistotu ořízneme na 4 znaky, kdyby tam bylo něco delšího (DB má limit VARCHAR2(4))
            if (typPlatby == "HOTOVĚ") typPlatby = "CASH";
            if (typPlatby == "KARTOU") typPlatby = "CARD";

            try
            {
                // 3. Získání ID přihlášeného uživatele
                int userId = App.PrihlasenyUzivatelId ?? 0; // Pokud je null (Host), dáme 0 nebo jiné ID pro anonyma

                // 4. Volání služby
                _dataService.VytvoritObjednavku(userId, CelkovaCena, typPlatby, KosikPolozky);

                // 5. Úspěch -> Vyčistit košík
                KosikPolozky.Clear();
                PrepocitatCelkem(); // Nastaví cenu na 0

                MessageBox.Show("Objednávka byla úspěšně zaplacena a uložena.", "Hotovo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba při ukládání objednávky:\n" + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrepocitatCelkem()
        {
            CelkovaCena = KosikPolozky.Sum(p => p.CenaCelkem);
        }
    }
}