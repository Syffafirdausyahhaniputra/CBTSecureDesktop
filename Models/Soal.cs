namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents a question in an exam
    /// </summary>
    public class Soal
    {
        public long SoalId { get; set; }
        public long UjianId { get; set; }
        public string KodeSoal { get; set; } = string.Empty; // Originally nomer_soal
        public string Pertanyaan { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public List<OpsiJawaban> OpsiJawaban { get; set; } = new();
        public List<GambarSoal> GambarSoal { get; set; } = new();
    }
}
