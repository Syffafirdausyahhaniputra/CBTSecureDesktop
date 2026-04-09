namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents a student's answer to a question
    /// </summary>
    public class SoalMahasiswa
    {
        public long SoalMahasiswaId { get; set; }
        public long SoalId { get; set; }
        public long MahasiswaId { get; set; }
        public long OpsiJawabanId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
