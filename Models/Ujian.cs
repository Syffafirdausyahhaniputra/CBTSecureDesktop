namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents an exam in the system
    /// </summary>
    public class Ujian
    {
        public long UjianId { get; set; }
        public long MatakuliahId { get; set; }
        public long TahunAjaranId { get; set; }
        public long ProdiId { get; set; }
        public string KodeUjian { get; set; } = string.Empty;
        public string NamaUjian { get; set; } = string.Empty;
        public string Status { get; set; } = "menunggu"; // menunggu, dimulai, selesai
        public int ShuffleSoal { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Matakuliah? Matakuliah { get; set; }
        public Prodi? Prodi { get; set; }
    }
}
