using System;

namespace SpravaObjednavek_GUI_WPF.Model
{
    public class AdminZprava
    {
        public int Id { get; set; }
        public DateTime Cas { get; set; }
        public string Odesilatel { get; set; }
        public string Prijemce { get; set; }
        public string Obsah { get; set; }
    }
}