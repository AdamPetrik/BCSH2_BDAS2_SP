using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class ObjednavkyViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        // Seznam pro tabulku
        public ObservableCollection<ObjednavkaZaznam> SeznamObjednavek { get; set; }

        // Výběr v ComboBoxech
        public List<int> DostupneRoky { get; set; }
        public List<int> DostupneMesice { get; set; } = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

        private int _vybranyRok;
        public int VybranyRok
        {
            get => _vybranyRok;
            set { _vybranyRok = value; OnPropertyChanged(); }
        }

        private int _vybranyMesic;
        public int VybranyMesic
        {
            get => _vybranyMesic;
            set { _vybranyMesic = value; OnPropertyChanged(); }
        }

        // Tlačítko pro načtení
        public ICommand NacistCommand { get; set; }

        public ObjednavkyViewModel()
        {
            _dataService = new DataService();
            SeznamObjednavek = new ObservableCollection<ObjednavkaZaznam>();

            // Naplníme roky (např. od 2023 do dneška)
            DostupneRoky = new List<int>();
            int aktualniRok = DateTime.Now.Year;
            for (int i = 2023; i <= aktualniRok; i++)
            {
                DostupneRoky.Add(i);
            }

            // Výchozí hodnoty (dnešní datum)
            VybranyRok = DateTime.Now.Year;
            VybranyMesic = DateTime.Now.Month;

            NacistCommand = new RelayCommand(o => NacistData());

            // Načíst hned při startu
            NacistData();
        }

        private void NacistData()
        {
            var data = _dataService.NacistObjednavkyPodleData(VybranyMesic, VybranyRok);
            SeznamObjednavek.Clear();
            foreach (var item in data)
            {
                SeznamObjednavek.Add(item);
            }
        }
    }
}