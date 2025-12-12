namespace SpravaObjednavek_GUI_WPF.Model
{
    public class UzivatelPrehled
    {
        public int Id { get; set; }
        public string Jmeno { get; set; }
        public string Role { get; set; } // USER / ADMINISTRATOR

        // Pomocná vlastnost pro zobrazení
        public string Info => $"{Jmeno} ({Role})";
    }
}