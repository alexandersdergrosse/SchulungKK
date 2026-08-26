namespace SchulungKK.Models
{
    public class QuizDaten
    {
        public List<QuizFrage> Fragen { get; set; } = new();
    }

    public class QuizFrage
    {
        public string Text { get; set; } = string.Empty;

        public List<string> Antworten { get; set; } = new();

        public int RichtigeAntwortIndex { get; set; }
    }
}