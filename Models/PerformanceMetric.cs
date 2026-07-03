using System;

namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Mewakili snapshot metrik performa aplikasi CBT pada suatu waktu tertentu.
    /// Digunakan untuk tracking & analyzing performa exam application.
    /// </summary>
    public class PerformanceMetric
    {
        private static readonly TimeZoneInfo _appTimeZone = ResolveAppTimeZone();

        /// <summary>
        /// Timestamp ketika metrik di-collect (UTC+07:00).
        /// </summary>
        public DateTime Timestamp { get; set; } = GetLocalTimestampNow();

        public static DateTime GetLocalTimestampNow()
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _appTimeZone);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private static TimeZoneInfo ResolveAppTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }

        /// <summary>
        /// CPU usage aplikasi CBT dalam persen (0-100%).
        /// </summary>
        public double CpuPercentage { get; set; }

        /// <summary>
        /// Memory usage aplikasi CBT dalam megabytes (MB).
        /// </summary>
        public double MemoryMb { get; set; }

        /// <summary>
        /// Total memory yang tersedia di sistem dalam megabytes.
        /// </summary>
        public double TotalSystemMemoryMb { get; set; }

        /// <summary>
        /// Memory available di sistem dalam megabytes.
        /// </summary>
        public double AvailableSystemMemoryMb { get; set; }

        /// <summary>
        /// Jumlah thread yang berjalan di aplikasi CBT.
        /// </summary>
        public int ThreadCount { get; set; }

        /// <summary>
        /// Jumlah handle (file, registry, dll) yang digunakan aplikasi.
        /// </summary>
        public int HandleCount { get; set; }

        /// <summary>
        /// Jumlah proses yang sedang berjalan di sistem (snapshot).
        /// </summary>
        public int RunningProcessCount { get; set; }

        /// <summary>
        /// Virtual memory yang digunakan aplikasi dalam megabytes.
        /// </summary>
        public double VirtualMemoryMb { get; set; }

        /// <summary>
        /// Working set private memory dalam megabytes (private pages).
        /// </summary>
        public double PrivateMemoryMb { get; set; }

        /// <summary>
        /// Jumlah page faults yang terjadi (swapping ke disk).
        /// </summary>
        public long PageFaults { get; set; }

        /// <summary>
        /// Uptime aplikasi dalam detik sejak start.
        /// </summary>
        public long UptimeSeconds { get; set; }

        /// <summary>
        /// ID proses CBT (PID).
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// Jumlah data yang diterima aplikasi via network (bytes).
        /// </summary>
        public long NetworkBytesReceived { get; set; }

        /// <summary>
        /// Jumlah data yang dikirim aplikasi via network (bytes).
        /// </summary>
        public long NetworkBytesSent { get; set; }

        /// <summary>
        /// Total network I/O dalam megabytes.
        /// </summary>
        public double NetworkIOMb { get; set; }

        /// <summary>
        /// Jumlah GC collections yang terjadi (Gen 0, Gen 1, Gen 2).
        /// Format: "Gen0:X Gen1:Y Gen2:Z"
        /// </summary>
        public string? GarbageCollectionStats { get; set; }

        /// <summary>
        /// Status user saat ini (di mana, aktivitas apa).
        /// </summary>
        public string? UserStatus { get; set; }

        /// <summary>
        /// Catatan khusus tentang kondisi sistem.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Format metric sebagai string yang mudah dibaca untuk logging.
        /// </summary>
        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}] " +
                   $"CPU: {CpuPercentage:F1}% | " +
                   $"Memory: {MemoryMb:F1}MB/{TotalSystemMemoryMb:F0}MB ({(MemoryMb / TotalSystemMemoryMb * 100):F1}%) | " +
                   $"Threads: {ThreadCount} | " +
                   $"Handles: {HandleCount} | " +
                   $"Processes: {RunningProcessCount} | " +
                   $"Network: {NetworkIOMb:F1}MB | " +
                   $"Uptime: {UptimeSeconds}s";
        }

        /// <summary>
        /// Format lengkap dengan semua detail untuk file logging.
        /// </summary>
        public string ToDetailedString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss zzz}]\n" +
                   $"  CPU: {CpuPercentage:F2}%\n" +
                   $"  Memory (CBT): {MemoryMb:F1}MB\n" +
                   $"  Memory (System): Total={TotalSystemMemoryMb:F0}MB, Available={AvailableSystemMemoryMb:F0}MB\n" +
                   $"  Virtual Memory: {VirtualMemoryMb:F1}MB\n" +
                   $"  Private Memory: {PrivateMemoryMb:F1}MB\n" +
                   $"  Threads: {ThreadCount}\n" +
                   $"  Handles: {HandleCount}\n" +
                   $"  Page Faults: {PageFaults}\n" +
                   $"  Network I/O: {NetworkIOMb:F1}MB (Sent: {NetworkBytesSent / (1024d * 1024d):F1}MB, Recv: {NetworkBytesReceived / (1024d * 1024d):F1}MB)\n" +
                   $"  Running Processes: {RunningProcessCount}\n" +
                   $"  Uptime: {UptimeSeconds}s ({TimeSpan.FromSeconds(UptimeSeconds):hh\\:mm\\:ss})\n" +
                   $"  Process ID: {ProcessId}\n" +
                   (GarbageCollectionStats != null ? $"  GC Stats: {GarbageCollectionStats}\n" : "") +
                   (UserStatus != null ? $"  User Status: {UserStatus}\n" : "") +
                   (Notes != null ? $"  Notes: {Notes}\n" : "");
        }

        /// <summary>
        /// CSV format untuk import ke analytics tools.
        /// </summary>
        public string ToCSV()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss zzz}," +
                   $"{CpuPercentage:F2}," +
                   $"{MemoryMb:F1}," +
                   $"{TotalSystemMemoryMb:F0}," +
                   $"{AvailableSystemMemoryMb:F0}," +
                   $"{ThreadCount}," +
                   $"{HandleCount}," +
                   $"{RunningProcessCount}," +
                   $"{VirtualMemoryMb:F1}," +
                   $"{PrivateMemoryMb:F1}," +
                   $"{PageFaults}," +
                   $"{NetworkIOMb:F1}," +
                   $"{UptimeSeconds}";
        }

        /// <summary>
        /// CSV header untuk analytics.
        /// </summary>
        public static string GetCSVHeader()
        {
            return "TimestampWithOffset,CPU%,MemoryMB,TotalSystemMemory,AvailableSystemMemory,Threads,Handles,RunningProcesses,VirtualMemory,PrivateMemory,PageFaults,NetworkIOMB,UptimeSeconds";
        }

        /// <summary>
        /// Detect apakah metrik menunjukkan performa masalah.
        /// </summary>
        public bool IsPerformanceIssueDetected()
        {
            // High CPU usage
            if (CpuPercentage > 90) return true;

            // Memory leak indicator (high private memory)
            if (PrivateMemoryMb > 500) return true;

            // Too many threads
            if (ThreadCount > 500) return true;

            // Too many handles
            if (HandleCount > 5000) return true;

            // Critical memory pressure
            if (AvailableSystemMemoryMb < 100) return true;

            return false;
        }

        /// <summary>
        /// Diagnosa performa issue berdasarkan metrik.
        /// </summary>
        public string? DiagnosePerformanceIssue()
        {
            if (CpuPercentage > 90)
                return "HIGH_CPU: Aplikasi menggunakan CPU >90%, mungkin ada infinite loop atau computation berat.";

            if (PrivateMemoryMb > 500)
                return "MEMORY_LEAK: Memory pribadi aplikasi sangat besar (>500MB), indikasi memory leak.";

            if (ThreadCount > 500)
                return "THREAD_LEAK: Terlalu banyak thread (>500), mungkin thread tidak di-cleanup dengan proper.";

            if (HandleCount > 5000)
                return "HANDLE_LEAK: Terlalu banyak handle (>5000), mungkin file/resource tidak di-close.";

            if (AvailableSystemMemoryMb < 100)
                return "MEMORY_PRESSURE: Sistem memory pressure tinggi (<100MB tersedia), bisa cause slowdown.";

            return null;
        }
    }
}
