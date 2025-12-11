namespace SpravaObjednavek_GUI_WPF.Model
{
    public class StatistikaItem
    {
        public string Nazev { get; set; }
        public int Pocet { get; set; }

        // Pomocná vlastnost pro grafiku (výška obdélníku v pixelech)
        public double VyskaSloupce { get; set; }
    }
}