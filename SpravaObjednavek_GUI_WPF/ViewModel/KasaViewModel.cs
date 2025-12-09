using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class KasaViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        // Seznam tlačítek (Menu) - načte se z DB
        public ObservableCollection<PolozkaMenu> MenuPolozky { get; set; }

        // Seznam účtenky (Košík)
        public ObservableCollection<PolozkaKosiku> KosikPolozky { get; set; }

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
                        Nazev = vybranaPolozka.Nazev,
                        CenaZaKus = vybranaPolozka.Cena,
                        Pocet = 1
                    });
                }
                PrepocitatCelkem();
            }
        }

        private void PrepocitatCelkem()
        {
            CelkovaCena = KosikPolozky.Sum(p => p.CenaCelkem);
        }
    }
}