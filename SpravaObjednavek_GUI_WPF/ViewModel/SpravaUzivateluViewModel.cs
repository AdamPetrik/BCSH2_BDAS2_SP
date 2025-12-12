using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class SpravaUzivateluViewModel : ObservableObject
    {
        private DataService _dataService;
        public ObservableCollection<UzivatelPrehled> SeznamUzivatelu { get; set; }

        public ICommand EmulovatCommand { get; set; }

        public SpravaUzivateluViewModel()
        {
            _dataService = new DataService();
            SeznamUzivatelu = new ObservableCollection<UzivatelPrehled>();
            EmulovatCommand = new RelayCommand(Emulovat);
            NacistData();
        }

        private void NacistData()
        {
            var data = _dataService.NacistVsechnyUzivatele();
            SeznamUzivatelu.Clear();
            foreach (var u in data) SeznamUzivatelu.Add(u);
        }

        private void Emulovat(object parameter)
        {
            if (parameter is UzivatelPrehled uzivatel)
            {
                var res = MessageBox.Show($"Opravdu se chcete přihlásit jako '{uzivatel.Jmeno}'?",
                                          "Emulace uživatele", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 1. Změna v DB
                        if (_dataService == null) _dataService = new DataService(); // Pojistka
                        _dataService.EmulovatUzivatele(uzivatel.Id);

                        // 2. Změna globálních proměnných
                        App.PrihlasenyUzivatelId = uzivatel.Id;
                        App.PrihlasenyJmeno = uzivatel.Jmeno;
                        App.JeHost = false;

                        App.PrihlasenaRole = uzivatel.Role;

                        // 3. NAJDI AKTIVNÍ OKNO (Bezpečnější metoda)
                        // Místo Application.Current.MainWindow projdeme všechna otevřená okna
                        Window ciloveOkno = null;

                        foreach (Window w in Application.Current.Windows)
                        {
                            // Hledáme okno, které je typu MainWindow a je viditelné
                            if (w.GetType().Name == "MainWindow" && w.IsVisible)
                            {
                                ciloveOkno = w;
                                break;
                            }
                        }

                        // Pokud jsme ho nenašli podle jména, vezmeme prostě to, které je aktivní
                        if (ciloveOkno == null)
                        {
                            foreach (Window w in Application.Current.Windows)
                            {
                                if (w.IsActive)
                                {
                                    ciloveOkno = w;
                                    break;
                                }
                            }
                        }

                        if (ciloveOkno == null) throw new Exception("Nepodařilo se najít otevřené okno aplikace.");

                        // 4. Vytvoření nového ViewModelu (Hot Swap)
                        // Zde se znovu načtou oprávnění pro nového uživatele
                        var newMainVM = new MainViewModel();

                        // 5. Výměna mozku
                        ciloveOkno.DataContext = newMainVM;

                        MessageBox.Show($"Nyní jste přihlášen jako: {uzivatel.Jmeno}", "Úspěch");
                    }
                    catch (Exception ex)
                    {
                        // Detailní výpis chyby
                        MessageBox.Show($"Chyba při přepínání: {ex.Message}\nZdroj: {ex.Source}\n{ex.StackTrace}");
                    }
                }
            }
        }
    }
}