using System;

namespace SpravaObjednavek_GUI_WPF.Model
{
    public class ObjednavkaZaznam
    {
        public int Id { get; set; }
        public DateTime Datum { get; set; }
        public decimal Cena { get; set; }
        public string ZpusobPlatby { get; set; }
        public string Obsluha { get; set; }
    }
}