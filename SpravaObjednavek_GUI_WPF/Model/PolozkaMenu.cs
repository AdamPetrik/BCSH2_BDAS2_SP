namespace SpravaObjednavek_GUI_WPF.Model
{
    // Tato třída odpovídá řádku v tabulce ITEM
    public class PolozkaMenu
    {
        public int Id { get; set; }         // Sloupec ITEM_ID
        public string Nazev { get; set; }   // Sloupec NAME
        public decimal Cena { get; set; }   // Sloupec PRICE
    }

    // Pomocná třída pro položku v pravém seznamu (košíku)
    public class PolozkaKosiku : ObservableObject
    {
        public int Id { get; set; }
        public string Nazev { get; set; }
        public decimal CenaZaKus { get; set; }

        private int _pocet;
        public int Pocet
        {
            get => _pocet;
            set
            {
                _pocet = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CenaCelkem)); // Přepočítat celkovou cenu
            }
        }

        // Vypočítaná vlastnost (nepotřebuje set)
        public decimal CenaCelkem => CenaZaKus * Pocet;
    }
}