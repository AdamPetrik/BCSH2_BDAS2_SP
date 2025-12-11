using SpravaObjednavek_GUI_WPF.ViewModel;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF
{
    public class MainViewModel : ObservableObject
    {
        // Tato vlastnost drží aktuálně zobrazený ViewModel (stránku)
        private object _currentView;
        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        // NOVÉ: Místo tří příkazů máme jen jeden univerzální
        public ICommand UpdateViewCommand { get; set; }

        public MainViewModel()
        {
            // Inicializace příkazu
            // 'parameter' je hodnota z CommandParameter v XAML (např. "Kasa", "Objednavky")
            UpdateViewCommand = new RelayCommand(parameter =>
            {
                // Převedeme parametr na string a rozhodneme, kam přepnout
                switch (parameter?.ToString())
                {
                    case "Kasa":
                        CurrentView = new KasaViewModel();
                        break;

                    case "Alergeny":
                        CurrentView = new AlergenyViewModel();
                        break;

                    case "Objednavky":
                        CurrentView = new ObjednavkyViewModel();
                        break;

                    case "Administrace":
                        CurrentView = new AdministraceViewModel();
                        break;

                    default:
                        // Volitelně: co dělat, když přijde neznámý parametr (např. nic)
                        break;
                }
            });

            // Výchozí stránka po spuštění
            CurrentView = new KasaViewModel();
        }
    }
}