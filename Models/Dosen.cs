namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents a lecturer
    /// </summary>
    public class Dosen
    {
        public long DosenId { get; set; }
        public string Nip { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
