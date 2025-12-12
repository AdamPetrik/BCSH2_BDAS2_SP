using System;
using System.Windows.Media.Imaging;

namespace SpravaObjednavek_GUI_WPF.Model
{
    // Pro seznam v tabulce (rychlé načtení)
    public class ObrazekMeta
    {
        public int Id { get; set; }
        public string NazevSouboru { get; set; }
        public string Pripona { get; set; }
        public string Autor { get; set; }
        public DateTime NahranoKdy { get; set; }
    }
}