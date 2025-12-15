using System;
using System.Collections.Generic;
using System.Text;

namespace SpravaObjednavek_GUI_WPF.Model
{
    public class LicenceV2
    {
        public int Id { get; set; }          // license_id
        public string TypLicence { get; set; } // type (z tabulky license_type)
        public DateTime PlatnostDo { get; set; } // valid_till

        // CelkovyPopis string pro potřebu ComboBoxu
        public string CelkovyPopis => $"{TypLicence} (do {PlatnostDo:dd.MM.yyyy})";
    }
}
