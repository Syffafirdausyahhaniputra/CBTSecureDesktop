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

        public int ExtendTimeMinutes { get; set; }
        public DateTime ActualEndTime => EndTime.AddMinutes(ExtendTimeMinutes);

        // Additional properties for displaying in dashboard
        public double? Nilai { get; set; }
        public string StatusMahasiswa { get; set; } = string.Empty; // Status of the student's exam session

        public string DisplayStatus
        {
            get
            {
                var now = DateTime.Now;

                string statusMhs = (StatusMahasiswa ?? string.Empty).Trim().ToLower();
                string statusGlobal = (Status ?? string.Empty).Trim().ToLower();

                // CEK STATUS GLOBAL DULU
                if (statusGlobal == "selesai")
                {
                    if (statusMhs == "selesai" || statusMhs == "dimulai")
                        return "Selesai";

                    if (statusMhs == "dihentikan")
                        return "Dihentikan";

                    return "Kedaluwarsa";
                }

                // 1. BEFORE EXAM
                if (now < StartTime)
                {
                    return "Menunggu";
                }

                // 3. AFTER EXAM (Waktu Habis)
                if (now > ActualEndTime)
                {
                    if (statusMhs == "selesai" || statusMhs == "dimulai")
                        return "Selesai";

                    if (statusMhs == "dihentikan")
                        return "Dihentikan";

                    return "Kedaluwarsa";
                }

                // 2. DURING EXAM (Ujian Berlangsung)
                // Menambahkan 'menunggu' sebagai indikasi mahasiswa belum menekan tombol mulai
                if (string.IsNullOrEmpty(statusMhs) || statusMhs == "none" || statusMhs == "menunggu")
                {
                    return "Tersedia";
                }

                if (statusMhs == "dimulai")
                {
                    return "Sedang Dikerjakan";
                }

                if (statusMhs == "selesai")
                {
                    return "Sudah Dikerjakan";
                }

                if (statusMhs == "dihentikan")
                {
                    return "Dihentikan";
                }

                return "Tidak Diketahui";
            }
        }

        public string PersiapanVisibility 
        {
            get
            {
                var now = DateTime.Now;
                string statusGlobal = (Status ?? string.Empty).Trim().ToLower();

                // Munculkan persiapan ujian hanya sebelum ujian dimulai (atau terserah logic sebelum waktu habis/review).
                if (statusGlobal == "selesai" || now > ActualEndTime)
                    return "Collapsed";

                return "Visible";
            }
        }

        public string ActionText
        {
            get
            {
                var now = DateTime.Now;

                string statusMhs = (StatusMahasiswa ?? string.Empty).Trim().ToLower();
                string statusGlobal = (Status ?? string.Empty).Trim().ToLower();

                // CEK STATUS GLOBAL / SETELAH WAKTU HABIS
                if (statusGlobal == "selesai" || now > ActualEndTime)
                {
                    if (statusMhs == "selesai" || statusMhs == "dimulai" || statusMhs == "dihentikan")
                        return "Review Ujian";

                    return "Berakhir";
                }

                // 1. BEFORE EXAM
                if (now < StartTime)
                {
                    return "Menunggu Ujian";
                }

                // 2. DURING EXAM
                if (string.IsNullOrEmpty(statusMhs) || statusMhs == "none" || statusMhs == "menunggu")
                {
                    return "Mulai Ujian";
                }

                if (statusMhs == "dimulai")
                {
                    return "Lanjutkan Ujian";
                }

                if (statusMhs == "selesai")
                {
                    return "Review Ujian";
                }

                if (statusMhs == "dihentikan")
                {
                    return "Akses Ditutup";
                }

                return "Tidak Tersedia";
            }
        }

        public bool IsActionEnabled
        {
            get
            {
                var now = DateTime.Now;

                // CEK STATUS GLOBAL / SETELAH WAKTU HABIS
                if (Status == "selesai" || now > ActualEndTime)
                {
                    // Tombol review aktif untuk mahasiswa yang ikut ujian (termasuk yang dihentikan)
                    return StatusMahasiswa == "selesai" || StatusMahasiswa == "dimulai" || StatusMahasiswa == "dihentikan";
                }

                // 1. BEFORE EXAM
                if (now < StartTime)
                {
                    return true; // Tombol Persiapan aktif
                }

                // 2. DURING EXAM
                if (StatusMahasiswa == "dihentikan")
                {
                    return false; // Pelanggaran saat ujian berlangsung, tombol mati sepenuhnya
                }

                return true;
            }
        }

        public string NilaiText => (StatusMahasiswa == "selesai" || StatusMahasiswa == "dihentikan") ? $"Nilai: {(Nilai ?? 0).ToString("F2")}" : string.Empty;

        // Navigation properties
        public Matakuliah? Matakuliah { get; set; }
        public Prodi? Prodi { get; set; }
    }
}
