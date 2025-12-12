using SpravaObjednavek_GUI_WPF.Model;
using SpravaObjednavek_GUI_WPF.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class StatistikyViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        // Data pro první graf (Kebaby 6-15)
        public ObservableCollection<StatistikaItem> DataKebaby { get; set; }

        // Data pro druhý graf (Omáčky 16-18)
        public ObservableCollection<StatistikaItem> DataOmacky { get; set; }

        public ObservableCollection<DenniTrzba> SeznamTrzeb { get; set; }
        public ObservableCollection<VykonObsluhy> SeznamVykonu { get; set; }

        public StatistikyViewModel()
        {
            _dataService = new DataService();
            DataKebaby = new ObservableCollection<StatistikaItem>();
            DataOmacky = new ObservableCollection<StatistikaItem>();

            SeznamTrzeb = new ObservableCollection<DenniTrzba>();
            SeznamVykonu = new ObservableCollection<VykonObsluhy>();

            NacistData();
        }

        private void NacistData()
        {
            // 1. Načíst Kebaby (ID 6 až 15)
            var kebaby = _dataService.ZiskatStatistiku(6, 15);
            PrepocitatVyskuGrafu(kebaby, 200); // Max výška sloupce 200px

            DataKebaby.Clear();
            foreach (var k in kebaby) DataKebaby.Add(k);

            // 2. Načíst Omáčky (ID 16 až 18)
            var omacky = _dataService.ZiskatStatistiku(16, 18);
            PrepocitatVyskuGrafu(omacky, 200);

            DataOmacky.Clear();
            foreach (var o in omacky) DataOmacky.Add(o);

            // 1. Denní tržby
            var trzby = _dataService.NacistDenniTrzby();
            SeznamTrzeb.Clear();
            foreach (var t in trzby) SeznamTrzeb.Add(t);

            // 2. Výkon obsluhy
            var vykony = _dataService.NacistVykonObsluhy();
            SeznamVykonu.Clear();
            foreach (var v in vykony) SeznamVykonu.Add(v);
        }

        private void PrepocitatVyskuGrafu(List<StatistikaItem> data, double maxVyskaPixelu)
        {
            if (data.Count == 0) return;

            // Najdeme nejvyšší hodnotu v seznamu (např. nejprodávanější kebab má 100 ks)
            double maxPocet = data.Max(x => x.Pocet);
            if (maxPocet == 0) maxPocet = 1; // Abychom nedělili nulou

            foreach (var item in data)
            {
                // Trojčlenka: (Aktuální / Max) * VýškaGrafu
                // Příklad: (50 / 100) * 200 = 100px
                item.VyskaSloupce = (item.Pocet / maxPocet) * maxVyskaPixelu;

                // Zajistíme, aby i nulový prodej měl aspoň 1px čárku (aby byl vidět v grafu)
                if (item.VyskaSloupce < 2) item.VyskaSloupce = 2;
            }
        }
    }
}