namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents an image attached to a question
    /// </summary>
    public class GambarSoal
    {
        public long GambarSoalId { get; set; }
        public long SoalId { get; set; }
        public string File { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
