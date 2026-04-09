namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents a class
    /// </summary>
    public class Kelas
    {
        public long KelasId { get; set; }
        public long ProdiId { get; set; }
        public long TahunAjaranId { get; set; }
        public string NamaKelas { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
