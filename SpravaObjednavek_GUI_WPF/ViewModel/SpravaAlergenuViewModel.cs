using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class SpravaAlergenuViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        public ObservableCollection<Alergen> SeznamAlergenu { get; set; }

        // Vlastnost formuláře
        public string EditNazev { get; set; }

        private Alergen _vybranyAlergen;
        public Alergen VybranyAlergen
        {
            get => _vybranyAlergen;
            set
            {
                _vybranyAlergen = value;
                OnPropertyChanged();

                if (_vybranyAlergen != null)
                {
                    EditNazev = _vybranyAlergen.Nazev;
                }
                OnPropertyChanged(nameof(EditNazev));
            }
        }

        public ICommand UlozitCommand { get; set; }
        public ICommand SmazatCommand { get; set; }
        public ICommand NovyCommand { get; set; }

        public SpravaAlergenuViewModel()
        {
            _dataService = new DataService();
            SeznamAlergenu = new ObservableCollection<Alergen>();

            UlozitCommand = new RelayCommand(Ulozit);
            SmazatCommand = new RelayCommand(Smazat);
            NovyCommand = new RelayCommand(Vycistit);

            NacistData();
        }

        private void NacistData()
        {
            var data = _dataService.NacistVsechnyAlergeny();
            SeznamAlergenu.Clear();
            foreach (var a in data) SeznamAlergenu.Add(a);
        }

        private void Vycistit(object obj)
        {
            VybranyAlergen = null;
            EditNazev = "";
            OnPropertyChanged(nameof(EditNazev));
            OnPropertyChanged(nameof(VybranyAlergen));
        }

        private void Ulozit(object obj)
        {
            if (string.IsNullOrWhiteSpace(EditNazev))
            {
                MessageBox.Show("Zadejte název alergenu.");
                return;
            }

            try
            {
                if (VybranyAlergen == null)
                {
                    // INSERT
                    _dataService.VytvoritAlergen(EditNazev);
                }
                else
                {
                    // UPDATE
                    _dataService.UpravitAlergen(VybranyAlergen.Id, EditNazev);
                }

                NacistData();
                Vycistit(null);
                MessageBox.Show("Uloženo.");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
            }
        }

        private void Smazat(object obj)
        {
            if (VybranyAlergen == null) return;

            try
            {
                _dataService.SmazatAlergen(VybranyAlergen.Id);
                NacistData();
                Vycistit(null);
                MessageBox.Show("Smazáno.");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Nelze smazat (pravděpodobně je alergen přiřazen k nějakému jídlu).\n" + ex.Message);
            }
        }
    }
}