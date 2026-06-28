namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents a course/subject
    /// </summary>
    public class Matakuliah
    {
        public long MatakuliahId { get; set; }
        public long ProdiId { get; set; }
        public long TahunAjaranId { get; set; }
        public string Nama { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
