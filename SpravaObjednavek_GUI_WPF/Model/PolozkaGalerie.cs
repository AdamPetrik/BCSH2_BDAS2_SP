using System.Windows.Media.Imaging;

namespace SpravaObjednavek_GUI_WPF.Model
{
    public class PolozkaGalerie
    {
        public string NazevJidla { get; set; }

        // Pro zobrazení v aplikaci
        public BitmapImage ObrazekSource { get; set; }

        // Pro stažení na disk (surová data)
        public byte[] ObrazekData { get; set; }

        // Metadata pro uložení
        public string NazevSouboru { get; set; }     // např. "kebab.jpg"
        public string Pripona { get; set; }          // např. "jpg"

        // Pomocná vlastnost: Má položka obrázek? (pro skrývání prvků v XAML)
        public bool MaObrazek => ObrazekSource != null;
    }
}