using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class LogViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        public ObservableCollection<LogZaznam> SeznamLogu { get; set; }

        public LogViewModel()
        {
            _dataService = new DataService();
            SeznamLogu = new ObservableCollection<LogZaznam>();
            NacistData();
        }

        private void NacistData()
        {
            try
            {
                var data = _dataService.NacistSystemoveLogy();
                SeznamLogu.Clear();
                foreach (var log in data)
                {
                    SeznamLogu.Add(log);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Chyba při načítání logů: " + ex.Message);
            }
        }
    }
}