namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents a study program
    /// </summary>
    public class Prodi
    {
        public long ProdiId { get; set; }
        public string KodeProdi { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;
        public string Jenjang { get; set; } = string.Empty; // D2, D3, D4
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
