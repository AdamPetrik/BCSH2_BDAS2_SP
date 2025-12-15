using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class ZpravyViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        // Seznam lidí na levé straně
        public ObservableCollection<UzivatelPrehled> SeznamLidi { get; set; }

        // Zprávy v pravé části
        public ObservableCollection<Zprava> HistorieZprav { get; set; }

        private UzivatelPrehled _vybranyUzivatel;
        public UzivatelPrehled VybranyUzivatel
        {
            get => _vybranyUzivatel;
            set
            {
                _vybranyUzivatel = value;
                OnPropertyChanged();
                NacistChat(); // Jakmile kliknu na člověka, načtu zprávy
            }
        }

        private string _textZpravy;
        public string TextZpravy
        {
            get => _textZpravy;
            set { _textZpravy = value; OnPropertyChanged(); }
        }

        public ICommand OdeslatCommand { get; }

        public ZpravyViewModel()
        {
            _dataService = new DataService();
            SeznamLidi = new ObservableCollection<UzivatelPrehled>();
            HistorieZprav = new ObservableCollection<Zprava>();
            OdeslatCommand = new RelayCommand(Odeslat);

            NacistLidi();
        }

        private void NacistLidi()
        {
            if (App.PrihlasenyUzivatelId == null) return;
            var lidi = _dataService.NacistUzivateleProChat(App.PrihlasenyUzivatelId.Value);
            SeznamLidi.Clear();
            foreach (var clovek in lidi) SeznamLidi.Add(clovek);
        }

        private void NacistChat()
        {
            if (VybranyUzivatel == null || App.PrihlasenyUzivatelId == null) return;

            var zpravy = _dataService.NacistHistoriiChatu(App.PrihlasenyUzivatelId.Value, VybranyUzivatel.Id);
            HistorieZprav.Clear();
            foreach (var z in zpravy) HistorieZprav.Add(z);
        }

        private void Odeslat(object obj)
        {
            if (string.IsNullOrWhiteSpace(TextZpravy) || VybranyUzivatel == null) return;

            try
            {
                _dataService.OdeslatZpravu(App.PrihlasenyUzivatelId.Value, VybranyUzivatel.Id, TextZpravy);

                // Přidáme zprávu rovnou do seznamu, ať se zobrazí hned
                HistorieZprav.Add(new Zprava
                {
                    Content = TextZpravy,
                    SentAt = System.DateTime.Now,
                    SenderId = App.PrihlasenyUzivatelId.Value,
                    ReceiverId = VybranyUzivatel.Id
                });

                TextZpravy = ""; // Vyčistit pole
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Chyba při odesílání: " + ex.Message);
            }
        }
    }
}