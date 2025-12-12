using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class SpravaAdresViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        public ObservableCollection<Adresa> SeznamAdres { get; set; }

        // Vlastnosti formuláře (Stringy, aby šly snadno mazat, převedeme je na int při uložení)
        public string EditUlice { get; set; }
        public string EditCisloPopisne { get; set; }
        public string EditKraj { get; set; }
        public string EditMesto { get; set; }
        public string EditPSC { get; set; }

        private Adresa _vybranaAdresa;
        public Adresa VybranaAdresa
        {
            get => _vybranaAdresa;
            set
            {
                _vybranaAdresa = value;
                OnPropertyChanged();

                // Pokud vybereme řádek, naplníme formulář
                if (_vybranaAdresa != null)
                {
                    EditUlice = _vybranaAdresa.Ulice;
                    EditCisloPopisne = _vybranaAdresa.CisloPopisne.ToString();
                    EditKraj = _vybranaAdresa.Kraj;
                    EditMesto = _vybranaAdresa.Mesto;
                    EditPSC = _vybranaAdresa.PSC.ToString();
                }
                // Notify property changed pro všechny properties (aby se aktualizoval formulář)
                OnPropertyChanged(nameof(EditUlice));
                OnPropertyChanged(nameof(EditCisloPopisne));
                OnPropertyChanged(nameof(EditKraj));
                OnPropertyChanged(nameof(EditMesto));
                OnPropertyChanged(nameof(EditPSC));
            }
        }

        public ICommand UlozitCommand { get; set; }
        public ICommand SmazatCommand { get; set; }
        public ICommand NovaAdresaCommand { get; set; }

        public SpravaAdresViewModel()
        {
            _dataService = new DataService();
            SeznamAdres = new ObservableCollection<Adresa>();

            UlozitCommand = new RelayCommand(Ulozit);
            SmazatCommand = new RelayCommand(Smazat);
            NovaAdresaCommand = new RelayCommand(VycistitFormular);

            NacistData();
        }

        private void NacistData()
        {
            var data = _dataService.NacistAdresy();
            SeznamAdres.Clear();
            foreach (var a in data) SeznamAdres.Add(a);
        }

        private void VycistitFormular(object obj)
        {
            VybranaAdresa = null; // Zruší výběr v tabulce
            EditUlice = "";
            EditCisloPopisne = "";
            EditKraj = "";
            EditMesto = "";
            EditPSC = "";

            // Aktualizace View
            OnPropertyChanged(nameof(EditUlice));
            OnPropertyChanged(nameof(EditCisloPopisne));
            OnPropertyChanged(nameof(EditKraj));
            OnPropertyChanged(nameof(EditMesto));
            OnPropertyChanged(nameof(EditPSC));
            OnPropertyChanged(nameof(VybranaAdresa));
        }

        private void Ulozit(object obj)
        {
            // Validace čísel
            if (!int.TryParse(EditCisloPopisne, out int cp) || !int.TryParse(EditPSC, out int psc))
            {
                MessageBox.Show("Číslo popisné a PSČ musí být čísla.");
                return;
            }

            var adresa = new Adresa
            {
                Ulice = EditUlice,
                CisloPopisne = cp,
                Kraj = EditKraj,
                Mesto = EditMesto,
                PSC = psc
            };

            try
            {
                if (VybranaAdresa == null)
                {
                    // INSERT
                    _dataService.VytvoritAdresu(adresa);
                }
                else
                {
                    // UPDATE
                    adresa.Id = VybranaAdresa.Id;
                    _dataService.UpravitAdresu(adresa);
                }

                NacistData();
                VycistitFormular(null);
                MessageBox.Show("Uloženo.");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
            }
        }

        private void Smazat(object obj)
        {
            if (VybranaAdresa == null) return;

            try
            {
                _dataService.SmazatAdresu(VybranaAdresa.Id);
                NacistData();
                VycistitFormular(null);
                MessageBox.Show("Smazáno.");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Nelze smazat (možná na adrese někdo bydlí?)\n" + ex.Message);
            }
        }
    }
}