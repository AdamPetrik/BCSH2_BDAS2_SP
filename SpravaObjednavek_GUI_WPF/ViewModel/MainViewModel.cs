using SpravaObjednavek_GUI_WPF.ViewModel;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF
{
    public class MainViewModel : ObservableObject
    {
        public bool JePlnyPristup { get; set; }
        // 1. Pro Kasu a Objednávky (Musí být aspoň USER, ne HOST)
        public bool JeRegistrovany { get; set; }

        // 2. Pro Statistiky a Administraci (Musí být ADMINISTRATOR)
        public bool JeAdmin { get; set; }

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

        public ICommand UpdateViewCommand { get; set; }

        private string _prihlasenyJmeno;
        public string PrihlasenyJmeno
        {
            get => _prihlasenyJmeno;
            set { _prihlasenyJmeno = value; OnPropertyChanged(); }
        }

        private string _prihlasenyRole;
        public string PrihlasenyRole
        {
            get => _prihlasenyRole;
            set { _prihlasenyRole = value; OnPropertyChanged(); }
        }

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

                    case "Galerie":
                        CurrentView = new GalerieViewModel();
                        break;

                    case "Statistiky":
                        CurrentView = new StatistikyViewModel();
                        break;

                    case "Administrace":
                        CurrentView = new AdministraceViewModel();
                        break;

                    default:
                        // Volitelně: co dělat, když přijde neznámý parametr (např. nic)
                        break;
                }
            });

            // Nastavení oprávnění
            JeRegistrovany = !App.JeHost;

            // Admin je ten, kdo není host A ZÁROVEŇ má roli ADMINISTRATOR
            JeAdmin = !App.JeHost && App.PrihlasenaRole == "ADMINISTRATOR";

            // Výchozí pohled
            if (JeAdmin || JeRegistrovany)
            {
                CurrentView = new KasaViewModel();
            }
            else
            {
                CurrentView = new AlergenyViewModel(); // Host začíná zde
            }

            NacistUzivatele();
        }

        private void NacistUzivatele()
        {
            // Podíváme se do globální proměnné v App.xaml.cs
            if (App.PrihlasenyUzivatelId != null)
            {
                var service = new Services.DataService();
                var info = service.ZiskatDetailUzivatele(App.PrihlasenyUzivatelId.Value);

                PrihlasenyJmeno = info.Jmeno;

                // Můžeme roli trochu přeložit, pokud je v DB anglicky/zkratkou
                PrihlasenyRole = info.Role;
            }
            else
            {
                // Pokud je ID null, je to host
                PrihlasenyJmeno = "Host";
                PrihlasenyRole = "Neregistrovaný";
            }
        }
    }
}