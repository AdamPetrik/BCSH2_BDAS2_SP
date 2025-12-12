using SpravaObjednavek_GUI_WPF.Services;
using SpravaObjednavek_GUI_WPF.View; // <--- DŮLEŽITÉ: Abychom viděli MainView
using System;
using System.Windows;

namespace SpravaObjednavek_GUI_WPF
{
    public partial class MainWindow : Window
    {
        private DataService _dataService;

        public MainWindow()
        {
            InitializeComponent();
            _dataService = new DataService();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            usernameTb.Focus(); // Kurzor skočí do pole jméno
        }

        private void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            string jmeno = usernameTb.Text;
            string heslo = passwordTb.Password; // PasswordBox používá .Password

            if (string.IsNullOrWhiteSpace(jmeno) || string.IsNullOrWhiteSpace(heslo))
            {
                MessageBox.Show("Zadejte prosím jméno a heslo.", "Upozornění", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Voláme službu
                int? userId = _dataService.OveritUzivatele(jmeno, heslo);

                if (userId != null)
                {
                    App.PrihlasenyUzivatelId = userId.Value;
                    var detail = _dataService.ZiskatDetailUzivatele(userId.Value);
                    App.PrihlasenyJmeno = detail.Jmeno;
                    OtevritHlavniAplikaci();
                }
                else
                {
                    MessageBox.Show("Chybné jméno nebo heslo.", "Chyba přihlášení", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba připojení k databázi:\n" + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void guestBtn_Click(object sender, RoutedEventArgs e)
        {
            App.JeHost = true;
            App.PrihlasenyJmeno = "Návštěvník";
            App.PrihlasenyUzivatelId = null; // Nemá ID

            // Otevřeme aplikaci stejně jako při přihlášení
            OtevritHlavniAplikaci();
        }

        private void OtevritHlavniAplikaci()
        {
            // Vytvoříme instanci hlavního okna (MainView je ve složce View)
            MainView hlavniOkno = new MainView();

            // Zobrazíme ho
            hlavniOkno.Show();

            // Zavřeme toto přihlašovací okno
            this.Close();
        }
    }
}