using System;
using System.Collections.Generic;
using System.Text;

namespace SpravaObjednavek_GUI_WPF.Model
{
    public class Adresa
    {
        public int Id { get; set; }
        public string Ulice { get; set; }
        public int CisloPopisne { get; set; }
        public string Kraj { get; set; }
        public string Mesto { get; set; }
        public int PSC { get; set; }
        public string CelaAdresa => $"{Ulice} {CisloPopisne}, {Mesto}";
    }
}
