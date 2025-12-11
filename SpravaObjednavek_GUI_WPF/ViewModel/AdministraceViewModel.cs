using System;
using System.Collections.Generic;
using System.Text;

namespace SpravaObjednavek_GUI_WPF.ViewModel
{
    public class AdministraceViewModel : ObservableObject
    {
        // Vlastnost, na kterou se váže View
        public RegistraceViewModel RegistraceVM { get; set; }
        public SpravaMenuViewModel SpravaMenuVM { get; set; }

        public AdministraceViewModel()
        {
            // !!! TENTO ŘÁDEK TAM MUSÍ BÝT !!!
            // Pokud tu chybí, ComboBox bude prázdný
            RegistraceVM = new RegistraceViewModel();
            SpravaMenuVM = new SpravaMenuViewModel();
        }
    }
}
