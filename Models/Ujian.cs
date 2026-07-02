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

        public System.Windows.Visibility PersiapanVisibility 
        {
            get
            {
                var now = DateTime.Now;
                string statusGlobal = (Status ?? string.Empty).Trim().ToLower();
                string statusMahasiswa = (StatusMahasiswa ?? string.Empty).Trim().ToLower();

                // Hide preparation if the exam is globally finished/expired,
                // or if the student has already finished/stopped their session.
                if (statusGlobal == "selesai" || now > ActualEndTime || statusMahasiswa == "selesai" || statusMahasiswa == "dihentikan")
                    return System.Windows.Visibility.Collapsed;

                return System.Windows.Visibility.Visible;
            }
        }

        public string ActionText
        {
            get
            {
                var now = DateTime.Now;

                string statusMhs = (StatusMahasiswa ?? string.Empty).Trim().ToLower();
                string statusGlobal = (Status ?? string.Empty).Trim().ToLower();

                // If global status is finished or time expired, hide the action button by returning empty text
                if (statusGlobal == "selesai" || now > ActualEndTime)
                {
                    return string.Empty; // no button shown
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
                    return string.Empty;
                }

                if (statusMhs == "dihentikan")
                {
                    return "Akses Ditutup";
                }

                return "Tidak Tersedia";
            }
        }

        public System.Windows.Visibility ActionVisibility
        {
            get
            {
                var now = DateTime.Now;
                string statusGlobal = (Status ?? string.Empty).Trim().ToLower();

                if (statusGlobal == "selesai" || now > ActualEndTime)
                    return System.Windows.Visibility.Collapsed;

                // also hide if there is no action text
                if (string.IsNullOrEmpty(ActionText))
                    return System.Windows.Visibility.Collapsed;

                return System.Windows.Visibility.Visible;
            }
        }

        public bool IsActionEnabled
        {
            get
            {
                var now = DateTime.Now;

                // If exam finished globally or time expired, hide/disable action button entirely
                if (Status == "selesai" || now > ActualEndTime)
                {
                    return false;
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
