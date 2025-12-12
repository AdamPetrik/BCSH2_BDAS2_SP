using System;

namespace SpravaObjednavek_GUI_WPF.Model
{
    // Třída pro řádek z V_DENNI_TRZBY
    public class DenniTrzba
    {
        public DateTime Den { get; set; }
        public int PocetObjednavek { get; set; }
        public decimal CelkovaTrzba { get; set; }
    }

    // Třída pro řádek z V_VYKON_OBSLUHY
    public class VykonObsluhy
    {
        public string Obsluha { get; set; }
        public int PocetObjednavek { get; set; }
        public decimal CelkovaTrzba { get; set; }
    }
}