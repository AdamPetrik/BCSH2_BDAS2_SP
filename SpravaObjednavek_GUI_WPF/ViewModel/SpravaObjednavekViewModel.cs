using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class SpravaObjednavekViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        public ObservableCollection<ObjednavkaZaznam> SeznamObjednavek { get; set; }

        // Filtrování
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

        // Příkazy
        public ICommand NacistCommand { get; set; }
        public ICommand SmazatObjednavkuCommand { get; set; } // <--- NOVÉ

        public SpravaObjednavekViewModel()
        {
            _dataService = new DataService();
            SeznamObjednavek = new ObservableCollection<ObjednavkaZaznam>();

            // Naplnění roků
            DostupneRoky = new List<int>();
            for (int i = 2023; i <= DateTime.Now.Year; i++) DostupneRoky.Add(i);

            VybranyRok = DateTime.Now.Year;
            VybranyMesic = DateTime.Now.Month;

            NacistCommand = new RelayCommand(o => NacistData());
            SmazatObjednavkuCommand = new RelayCommand(SmazatObjednavku);

            NacistData();
        }

        private void NacistData()
        {
            var data = _dataService.NacistObjednavkyPodleData(VybranyMesic, VybranyRok);
            SeznamObjednavek.Clear();
            foreach (var item in data) SeznamObjednavek.Add(item);
        }

        private void SmazatObjednavku(object parameter)
        {
            if (parameter is ObjednavkaZaznam objednavka)
            {
                var result = MessageBox.Show($"Opravdu chcete smazat objednávku č. {objednavka.Id}?",
                                             "Potvrzení", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _dataService.SmazatObjednavku(objednavka.Id);
                        SeznamObjednavek.Remove(objednavka); // Smažeme ji rovnou ze seznamu
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Chyba při mazání: " + ex.Message);
                    }
                }
            }
        }
    }
}