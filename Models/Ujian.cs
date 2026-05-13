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
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Additional properties for displaying in dashboard
        public int? Nilai { get; set; }
        public string StatusMahasiswa { get; set; } = string.Empty; // Status of the student's exam session
        public string ActionText => (StatusMahasiswa == "selesai" || StatusMahasiswa == "dihentikan") ? "Review" : "Mulai"; // Text for the action button
        public string NilaiText => (StatusMahasiswa == "selesai" || StatusMahasiswa == "dihentikan") ? $"Nilai: {Nilai ?? 0}" : string.Empty;

        // Navigation properties
        public Matakuliah? Matakuliah { get; set; }
        public Prodi? Prodi { get; set; }
    }
}
