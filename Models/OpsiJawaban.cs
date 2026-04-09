namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents an answer option for a question
    /// </summary>
    public class OpsiJawaban
    {
        public long OpsiJawabanId { get; set; }
        public long SoalId { get; set; }
        public string Jawaban { get; set; } = string.Empty;
        public int Nilai { get; set; } // 1 = correct, 0 = incorrect
        public string? File { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
