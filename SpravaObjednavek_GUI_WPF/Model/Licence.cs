using System;

namespace SpravaObjednavek_GUI_WPF.Model
{
    public class Licence
    {
        public int Id { get; set; }
        public int TypId { get; set; } // 1, 2, 3
        public DateTime PlatnostOd { get; set; }
        public DateTime PlatnostDo { get; set; }

        // Pomocná vlastnost pouze pro zobrazení v DataGridu (neukládá se do DB)
        public string NazevTypu
        {
            get
            {
                switch (TypId)
                {
                    case 1: return "LITE";
                    case 2: return "STANDARD";
                    case 3: return "PREMIUM";
                    default: return "Neznámý";
                }
            }
        }
    }
}