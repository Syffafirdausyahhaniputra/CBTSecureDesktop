namespace CBTSecureDesktop.Models
{
    /// <summary>
    /// Represents a user in the system (Student, Lecturer, or Committee)
    /// </summary>
    public class User
    {
        public long UserId { get; set; }
        public long? DosenId { get; set; }
        public long? MahasiswaId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty; // mahasiswa, dosen, panitia
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
