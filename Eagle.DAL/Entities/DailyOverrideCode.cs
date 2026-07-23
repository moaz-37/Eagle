namespace Eagle.DAL.Entities
{
    public class DailyOverrideCode
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } // UTC date, no time component
        public string Code { get; set; } = string.Empty;
    }
}