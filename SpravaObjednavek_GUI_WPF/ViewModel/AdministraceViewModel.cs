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
        public SpravaObjednavekViewModel SpravaObjednavekVM { get; set; }
        public CenikViewModel CenikVM { get; set; }
        public SpravaAdresViewModel SpravaAdresVM { get; set; }
        public SpravaLicenciViewModel SpravaLicenciVM { get; set; }
        public SpravaAlergenuViewModel SpravaAlergenuVM { get; set; }
        public SpravaObrazkuViewModel SpravaObrazkuVM { get; set; }
        public HierarchieViewModel HierarchieVM { get; set; }
        public SpravaUzivateluViewModel SpravaUzivateluVM { get; set; }

        public AdministraceViewModel()
        {
            RegistraceVM = new RegistraceViewModel();
            SpravaMenuVM = new SpravaMenuViewModel();
            SpravaObjednavekVM = new SpravaObjednavekViewModel();
            CenikVM = new CenikViewModel();
            SpravaAdresVM = new SpravaAdresViewModel();
            SpravaLicenciVM = new SpravaLicenciViewModel();
            SpravaAlergenuVM = new SpravaAlergenuViewModel();
            SpravaObrazkuVM = new SpravaObrazkuViewModel();
            HierarchieVM = new HierarchieViewModel();
            SpravaUzivateluVM = new SpravaUzivateluViewModel();
        }
    }
}
