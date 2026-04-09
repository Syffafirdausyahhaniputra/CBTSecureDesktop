namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents a student in the system
    /// </summary>
    public class Mahasiswa
    {
        public long MahasiswaId { get; set; }
        public long KelasId { get; set; }
        public string Nim { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Kelas? Kelas { get; set; }
    }
}
