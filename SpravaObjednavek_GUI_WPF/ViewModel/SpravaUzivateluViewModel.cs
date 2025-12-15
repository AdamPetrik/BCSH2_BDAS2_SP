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
        public ICommand SmazatCommand { get; set; }

        public SpravaUzivateluViewModel()
        {
            _dataService = new DataService();
            SeznamUzivatelu = new ObservableCollection<UzivatelPrehled>();
            EmulovatCommand = new RelayCommand(Emulovat);
            SmazatCommand = new RelayCommand(Smazat);
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

                        // 3. NAJDI AKTIVNÍ OKNO
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

                        // 5. Výměna contextu
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

        private void Smazat(object parameter)
        {
            if (parameter is UzivatelPrehled uzivatel)
            {
                // Kontrola, abych si nesmazal sám sebe
                if (uzivatel.Id == App.PrihlasenyUzivatelId)
                {
                    MessageBox.Show("Nemůžete smazat svůj vlastní účet, když jste přihlášen!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var res = MessageBox.Show($"Opravdu chcete smazat uživatele '{uzivatel.Jmeno}'?\n\nTato akce je nevratná a odstraní i přihlašovací údaje.",
                                          "Smazat uživatele", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 1. Smazání z Databáze
                        _dataService.SmazatUzivatele(uzivatel.Id);

                        // 2. Smazání z UI (DataGridu)
                        SeznamUzivatelu.Remove(uzivatel);

                        MessageBox.Show("Uživatel byl úspěšně smazán.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Chyba při mazání: {ex.Message}\n\n(Pokud má uživatel již objednávky, nelze ho smazat kvůli historii dat.)", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}