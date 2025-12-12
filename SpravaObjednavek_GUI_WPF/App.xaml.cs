using SpravaObjednavek_GUI_WPF.Services;
using System.Configuration;
using System.Data;
using System.Windows;

namespace SpravaObjednavek_GUI_WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Kritická chyba aplikace:\n" + e.Exception.Message + "\n\n" + e.Exception.InnerException?.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Zabrání pádu aplikace
        }

        // Zde si globálně uložíme ID uživatele, jakmile se přihlásí
        public static int? PrihlasenyUzivatelId { get; set; }
        public static string PrihlasenyJmeno { get; set; }

        public static string PrihlasenaRole { get; set; } // "ADMINISTRATOR" nebo "USER"
        public static bool JeHost { get; set; } = false;

        // Tato metoda se spustí automaticky, když se aplikace zavírá (křížkem, Alt+F4...)
        protected override void OnExit(ExitEventArgs e)
        {
            if (PrihlasenyUzivatelId != null)
            {
                // Vytvoříme instanci služby jen pro tento úkon
                DataService service = new DataService();

                try
                {
                    // Zavoláme odhlášení v DB
                    service.OdhlasitUzivatele(PrihlasenyUzivatelId.Value);
                }
                catch
                {
                    // Při vypínání chyby ignorujeme (uživatele nechceme zdržovat)
                }
            }

            base.OnExit(e);
        }
    }

}
