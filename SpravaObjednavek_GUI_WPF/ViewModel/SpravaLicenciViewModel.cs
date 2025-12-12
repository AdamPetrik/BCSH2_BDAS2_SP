using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    // Pomocná třída pro ComboBox
    public class TypLicencePolozka
    {
        public int Id { get; set; }
        public string Nazev { get; set; }
    }

    public class SpravaLicenciViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        public ObservableCollection<Licence> SeznamLicenci { get; set; }

        // Data pro ComboBox (LITE, STANDARD, PREMIUM)
        public List<TypLicencePolozka> TypyLicenci { get; set; }

        // --- VLASTNOSTI FORMULÁŘE ---
        // Vybraný typ v ComboBoxu
        private TypLicencePolozka _vybranyTyp;
        public TypLicencePolozka VybranyTyp
        {
            get => _vybranyTyp;
            set { _vybranyTyp = value; OnPropertyChanged(); }
        }

        // Datumy (Nullable, aby šlo detekovat nevyplnění)
        private DateTime? _editOd;
        public DateTime? EditOd
        {
            get => _editOd;
            set { _editOd = value; OnPropertyChanged(); }
        }

        private DateTime? _editDo;
        public DateTime? EditDo
        {
            get => _editDo;
            set { _editDo = value; OnPropertyChanged(); }
        }

        // Vybraný řádek v tabulce
        private Licence _vybranaLicence;
        public Licence VybranaLicence
        {
            get => _vybranaLicence;
            set
            {
                _vybranaLicence = value;
                OnPropertyChanged();

                if (_vybranaLicence != null)
                {
                    // Naplnění formuláře při kliknutí
                    EditOd = _vybranaLicence.PlatnostOd;
                    EditDo = _vybranaLicence.PlatnostDo;
                    // Najdeme správný typ v seznamu pro ComboBox
                    VybranyTyp = TypyLicenci.Find(t => t.Id == _vybranaLicence.TypId);
                }
            }
        }

        public ICommand UlozitCommand { get; set; }
        public ICommand SmazatCommand { get; set; }
        public ICommand NovaCommand { get; set; }

        public SpravaLicenciViewModel()
        {
            _dataService = new DataService();
            SeznamLicenci = new ObservableCollection<Licence>();

            // Inicializace číselníku (nemusíme tahat z DB, zadání je fixní)
            TypyLicenci = new List<TypLicencePolozka>
            {
                new TypLicencePolozka { Id = 1, Nazev = "LITE" },
                new TypLicencePolozka { Id = 2, Nazev = "STANDARD" },
                new TypLicencePolozka { Id = 3, Nazev = "PREMIUM" }
            };

            UlozitCommand = new RelayCommand(Ulozit);
            SmazatCommand = new RelayCommand(Smazat);
            NovaCommand = new RelayCommand(Vycistit);

            NacistData();
        }

        private void NacistData()
        {
            var data = _dataService.NacistLicence();
            SeznamLicenci.Clear();
            foreach (var l in data) SeznamLicenci.Add(l);
        }

        private void Vycistit(object obj)
        {
            VybranaLicence = null;
            EditOd = DateTime.Now; // Předvyplníme dnešek
            EditDo = DateTime.Now.AddYears(1); // Předvyplníme rok dopředu
            VybranyTyp = TypyLicenci[0]; // Předvyplníme LITE

            OnPropertyChanged(nameof(VybranaLicence)); // Refresh
        }

        private void Ulozit(object obj)
        {
            if (VybranyTyp == null || EditOd == null || EditDo == null)
            {
                MessageBox.Show("Vyplňte typ a datumy platnosti.");
                return;
            }

            if (EditDo < EditOd)
            {
                MessageBox.Show("Datum 'Do' nesmí být menší než 'Od'.");
                return;
            }

            try
            {
                if (VybranaLicence == null)
                {
                    // INSERT
                    _dataService.VytvoritLicenci(VybranyTyp.Id, EditOd.Value, EditDo.Value);
                }
                else
                {
                    // UPDATE
                    _dataService.UpravitLicenci(VybranaLicence.Id, VybranyTyp.Id, EditOd.Value, EditDo.Value);
                }

                NacistData();
                Vycistit(null);
                MessageBox.Show("Uloženo.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba: " + ex.Message);
            }
        }

        private void Smazat(object obj)
        {
            if (VybranaLicence == null) return;

            try
            {
                _dataService.SmazatLicenci(VybranaLicence.Id);
                NacistData();
                Vycistit(null);
                MessageBox.Show("Smazáno.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nelze smazat:\n" + ex.Message);
            }
        }
    }
}