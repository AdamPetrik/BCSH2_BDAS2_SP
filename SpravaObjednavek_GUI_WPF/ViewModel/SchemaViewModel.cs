using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class SchemaViewModel
    {
        private readonly DataService _dataService;
        public ObservableCollection<DatabazovyObjekt> SeznamObjektu { get; set; }

        public SchemaViewModel()
        {
            _dataService = new DataService();
            SeznamObjektu = new ObservableCollection<DatabazovyObjekt>();
            NacistData();
        }

        private void NacistData()
        {
            var data = _dataService.NacistSchéma();
            SeznamObjektu.Clear();
            foreach (var item in data) SeznamObjektu.Add(item);
        }
    }
}
