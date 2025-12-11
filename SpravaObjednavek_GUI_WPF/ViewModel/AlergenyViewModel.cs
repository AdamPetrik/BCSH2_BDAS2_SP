using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class AlergenyViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        // Seznam pro zobrazení v tabulce
        public ObservableCollection<PolozkaAlergen> SeznamAlergenu { get; set; }

        public AlergenyViewModel()
        {
            _dataService = new DataService();
            SeznamAlergenu = new ObservableCollection<PolozkaAlergen>();

            NacistData();
        }

        private void NacistData()
        {
            var data = _dataService.NacistAlergeny();
            SeznamAlergenu.Clear();
            foreach (var item in data)
            {
                SeznamAlergenu.Add(item);
            }
        }
    }
}