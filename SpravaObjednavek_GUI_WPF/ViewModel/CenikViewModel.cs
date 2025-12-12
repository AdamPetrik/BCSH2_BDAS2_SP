using SpravaObjednavek_GUI_WPF.Model; // Zde musí být vaše třída PolozkaMenu/PolozkaKosiku
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class CenikViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        // Seznam všech jídel
        public ObservableCollection<PolozkaMenu> SeznamPolozek { get; set; }
        public ICommand ZdrazitCommand { get; set; }

        // Právě vybraná položka v tabulce
        private PolozkaMenu _vybranaPolozka;
        public PolozkaMenu VybranaPolozka
        {
            get => _vybranaPolozka;
            set
            {
                _vybranaPolozka = value;
                OnPropertyChanged();
                // Když se změní výběr, přepíšeme hodnoty do editačních políček
                if (_vybranaPolozka != null)
                {
                    EditNazev = _vybranaPolozka.Nazev;
                    EditCena = _vybranaPolozka.Cena.ToString();
                }
            }
        }

        // Pomocné vlastnosti pro editaci (aby se změna neprojevila hned v tabulce, dokud neuložíme)
        private string _editNazev;
        public string EditNazev
        {
            get => _editNazev;
            set { _editNazev = value; OnPropertyChanged(); }
        }

        private string _editCena;
        public string EditCena
        {
            get => _editCena;
            set { _editCena = value; OnPropertyChanged(); }
        }

        public ICommand UlozitZmenyCommand { get; set; }
        public ICommand SmazatPolozkuCommand { get; set; }
        public ICommand NacistCommand { get; set; }

        public CenikViewModel()
        {
            _dataService = new DataService();
            SeznamPolozek = new ObservableCollection<PolozkaMenu>();

            UlozitZmenyCommand = new RelayCommand(UlozitZmeny);
            SmazatPolozkuCommand = new RelayCommand(SmazatPolozku);
            NacistCommand = new RelayCommand(o => NacistData());
            ZdrazitCommand = new RelayCommand(o => {
                _dataService.ZdrazitLevnaJidla(10);
                MessageBox.Show("Levná jídla byla zdražena o 10%.");
                NacistData();
            });

            NacistData();
        }

        private void NacistData()
        {
            // Využijeme existující metodu pro načtení menu
            var data = _dataService.NacistMenuZDatabaze();
            SeznamPolozek.Clear();
            foreach (var item in data) SeznamPolozek.Add(item);
        }

        private void UlozitZmeny(object obj)
        {
            if (VybranaPolozka == null) return;

            if (decimal.TryParse(EditCena, out decimal novaCena))
            {
                try
                {
                    _dataService.UpravitPolozku(VybranaPolozka.Id, EditNazev, novaCena);
                    MessageBox.Show("Cena byla upravena.", "Hotovo");
                    NacistData(); // Obnovit seznam
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Chyba: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Cena musí být číslo.");
            }
        }

        private void SmazatPolozku(object obj)
        {
            if (VybranaPolozka == null) return;

            var res = MessageBox.Show($"Smazat {VybranaPolozka.Nazev}?", "Pozor", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.SmazatPolozku(VybranaPolozka.Id);
                    SeznamPolozek.Remove(VybranaPolozka);
                    EditNazev = "";
                    EditCena = "";
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Nelze smazat položku, která je součástí objednávek.\n(Chyba: ORA-02292)", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}