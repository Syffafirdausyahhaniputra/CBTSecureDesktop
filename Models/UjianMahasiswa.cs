namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents a student's exam session tracking
    /// </summary>
    public class UjianMahasiswa
    {
        public long UjianMahasiswaId { get; set; }
        public long UjianId { get; set; }
        public long MahasiswaId { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public TimeSpan? ExtendTime { get; set; }
        public DateTime? TanggalUjian { get; set; }
        public string Status { get; set; } = "menunggu"; // menunggu, dimulai, selesai, dihentikan
        public string? Keterangan { get; set; }
        public int? Nilai { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
