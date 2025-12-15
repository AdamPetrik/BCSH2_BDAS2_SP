using System;

namespace SpravaObjednavek_GUI_WPF.Model
{
    public class Zprava
    {
        public int MessageId { get; set; }
        public DateTime SentAt { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; }
        public bool JeMoje => SenderId == App.PrihlasenyUzivatelId;
    }
}