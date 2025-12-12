using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class HierarchieViewModel : ObservableObject
    {
        private readonly DataService _dataService;
        public ObservableCollection<HierarchiePolozka> Struktura { get; set; }

        public HierarchieViewModel()
        {
            _dataService = new DataService();
            Struktura = new ObservableCollection<HierarchiePolozka>();

            NacistData();
        }

        private void NacistData()
        {
            var data = _dataService.NacistHierarchii();
            Struktura.Clear();
            foreach (var item in data) Struktura.Add(item);
            MessageBox.Show("Načteno položek: " + Struktura.Count);
        }
    }
}