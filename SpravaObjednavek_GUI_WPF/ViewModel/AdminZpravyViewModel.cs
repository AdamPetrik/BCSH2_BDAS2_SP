using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class AdminZpravyViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        public ObservableCollection<AdminZprava> SeznamZprav { get; set; }
        public ICommand SmazatZpravuCommand { get; set; }

        public AdminZpravyViewModel()
        {
            _dataService = new DataService();
            SeznamZprav = new ObservableCollection<AdminZprava>();
            SmazatZpravuCommand = new RelayCommand(SmazatZpravu);
            NacistZpravy();
        }

        private void NacistZpravy()
        {
            try
            {
                var data = _dataService.NacistVsechnyZpravyAdmin();
                SeznamZprav.Clear();
                foreach (var zprava in data)
                {
                    SeznamZprav.Add(zprava);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Chyba při načítání zpráv: " + ex.Message);
            }
        }

        private void SmazatZpravu(object parameter)
        {
            if (parameter is AdminZprava zprava)
            {
                var res = MessageBox.Show("Opravdu chcete smazat tuto zprávu? Záznam o smazání zůstane v LOGu.",
                                          "Potvrzení", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 1. Smazat z DB
                        _dataService.SmazatZpravu(zprava.Id);

                        // 2. Smazat z tabulky na obrazovce
                        SeznamZprav.Remove(zprava);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("Chyba při mazání: " + ex.Message);
                    }
                }
            }
        }
    }
}