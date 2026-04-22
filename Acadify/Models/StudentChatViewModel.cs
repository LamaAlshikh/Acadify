namespace Acadify.Models
{
    public class StudentChatViewModel
    {
        public string AdvisorName { get; set; } = "";
        public string StudentName { get; set; } = "";

        public bool IsRecordingStarted { get; set; } = false;

        public List<ChatMessageVM> Messages { get; set; } = new();
    }

    public class ChatMessageVM
    {
        public string SenderName { get; set; } = "";
        public string Text { get; set; } = "";
        public bool IsFromStudent { get; set; } // true = right, false = left
        public string TimeText { get; set; } = ""; // optional
        public bool IsRecorded { get; set; } = false;
    }



}