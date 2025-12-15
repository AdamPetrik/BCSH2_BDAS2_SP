using System;

namespace SpravaObjednavek_GUI_WPF.Model
{
    public class LogZaznam
    {
        public int Id { get; set; }
        public DateTime Cas { get; set; }
        public string Akce { get; set; }
        public string Tabulka { get; set; }
        public string Uzivatel { get; set; }
    }
}