using MySqlConnector;
using CBTSecureDesktop.Models;
using System.Data;

namespace CBTSecureDesktop.Data
{
    /// <summary>
    /// Provides database operations for the CBT application
    /// Uses Repository pattern for data access
    /// </summary>
    public class DatabaseService
    {
        private readonly DatabaseConnection _dbConnection;

        public DatabaseService()
        {
            _dbConnection = DatabaseConnection.Instance;
        }

        #region User & Authentication

        /// <summary>
        /// Authenticates a user by username and password
        /// </summary>
        public async Task<User?> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                string query = @"SELECT user_id, dosen_id, mahasiswa_id, username, password, level, created_at, updated_at 
                                FROM t_user 
                                WHERE username = @username";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", username);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var user = new User
                    {
                        UserId = reader.GetInt64("user_id"),
                        DosenId = reader.IsDBNull("dosen_id") ? null : reader.GetInt64("dosen_id"),
                        MahasiswaId = reader.IsDBNull("mahasiswa_id") ? null : reader.GetInt64("mahasiswa_id"),
                        Username = reader.GetString("username"),
                        Password = reader.GetString("password"),
                        Level = reader.GetString("level"),
                        CreatedAt = reader.IsDBNull("created_at") ? null : reader.GetDateTime("created_at"),
                        UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at")
                    };

                    // For demo: check plain password or bcrypt hash
                    // In production, use proper password hashing verification
                    if (user.Password == password || VerifyPassword(password, user.Password))
                    {
                        return user;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (AuthenticateUserAsync): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets student information by student ID
        /// </summary>
        public async Task<Mahasiswa?> GetMahasiswaByIdAsync(long mahasiswaId)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                string query = @"SELECT m.mahasiswa_id, km.kelas_id, m.nim, m.nama, m.created_at, m.updated_at 
                                FROM t_mahasiswa m
                                LEFT JOIN t_kelas_mahasiswa km ON m.mahasiswa_id = km.mahasiswa_id
                                WHERE m.mahasiswa_id = @mahasiswaId LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Mahasiswa
                    {
                        MahasiswaId = reader.GetInt64("mahasiswa_id"),
                        KelasId = reader.IsDBNull("kelas_id") ? 0 : reader.GetInt64("kelas_id"),
                        Nim = reader.GetString("nim"),
                        Nama = reader.GetString("nama"),
                        CreatedAt = reader.IsDBNull("created_at") ? null : reader.GetDateTime("created_at"),
                        UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get mahasiswa error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (GetMahasiswaByIdAsync): {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Exams

        /// <summary>
        /// Gets all available exams for a student based on their class
        /// </summary>
        public async Task<List<Ujian>> GetAvailableExamsForStudentAsync(long mahasiswaId)
        {
            var exams = new List<Ujian>();
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                string query = @"
                    SELECT DISTINCT u.ujian_id, u.matakuliah_id, u.tahun_ajaran_id, u.prodi_id,
                           u.kode_ujian, u.nama_ujian, u.status, u.shufflesoal,
                           u.starttime, u.endtime, u.created_at, u.updated_at,
                           m.nama as matakuliah_nama, p.nama as prodi_nama,
                           um.status as status_mahasiswa, um.nilai as nilai_mahasiswa
                    FROM t_ujian u
                    INNER JOIN t_ujian_kelas uk ON u.ujian_id = uk.ujian_id
                    INNER JOIN t_kelas_mahasiswa km ON uk.kelas_id = km.kelas_id
                    INNER JOIN t_matakuliah m ON u.matakuliah_id = m.matakuliah_id
                    INNER JOIN t_prodi p ON u.prodi_id = p.prodi_id
                    LEFT JOIN t_ujian_mahasiswa um ON u.ujian_id = um.ujian_id AND um.mahasiswa_id = @mahasiswaId
                    WHERE km.mahasiswa_id = @mahasiswaId
                    AND u.status IN ('menunggu', 'dimulai', 'selesai')
                    ORDER BY u.created_at DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var ujian = new Ujian
                    {
                        UjianId = reader.GetInt64("ujian_id"),
                        MatakuliahId = reader.GetInt64("matakuliah_id"),
                        TahunAjaranId = reader.GetInt64("tahun_ajaran_id"),
                        ProdiId = reader.GetInt64("prodi_id"),
                        KodeUjian = reader.GetString("kode_ujian"),
                        NamaUjian = reader.GetString("nama_ujian"),
                        Status = reader.GetString("status"),
                        ShuffleSoal = reader.GetInt32("shufflesoal"),
                        StartTime = reader.GetDateTime("starttime"),
                        EndTime = reader.GetDateTime("endtime"),
                        CreatedAt = reader.IsDBNull("created_at") ? null : reader.GetDateTime("created_at"),
                        UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at"),
                        Matakuliah = new Matakuliah { Nama = reader.GetString("matakuliah_nama") },
                        Prodi = new Prodi { Nama = reader.GetString("prodi_nama") },
                        StatusMahasiswa = reader.IsDBNull("status_mahasiswa") ? "menunggu" : reader.GetString("status_mahasiswa"),
                        Nilai = reader.IsDBNull("nilai_mahasiswa") ? null : reader.GetInt32("nilai_mahasiswa")
                    };
                    exams.Add(ujian);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get exams error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (GetAvailableExamsForStudentAsync): {ex.Message}");
            }
            return exams;
        }

        /// <summary>
        /// Gets all questions for a specific exam
        /// </summary>
        public async Task<List<Soal>> GetExamQuestionsAsync(long ujianId)
        {
            var questions = new List<Soal>();
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                // Get shuffle setting
                string shuffleQuery = "SELECT shufflesoal FROM t_ujian WHERE ujian_id = @ujianId";
                using var shuffleCommand = new MySqlCommand(shuffleQuery, connection);
                shuffleCommand.Parameters.AddWithValue("@ujianId", ujianId);
                var doShuffle = Convert.ToInt32(await shuffleCommand.ExecuteScalarAsync());

                // Get questions
                string query = @"SELECT soal_id, ujian_id, nomer_soal, pertanyaan, created_at, updated_at 
                                FROM t_soal 
                                WHERE ujian_id = @ujianId 
                                ORDER BY nomer_soal";

                if (doShuffle == 1)
                {
                    query = @"SELECT soal_id, ujian_id, nomer_soal, pertanyaan, created_at, updated_at 
                            FROM t_soal 
                            WHERE ujian_id = @ujianId 
                            ORDER BY RAND()";
                }

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ujianId", ujianId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // The nomer_soal might be stored as Int32 in DB, so we'll read it safely
                    string kodeSoalString = "";
                    if (!reader.IsDBNull("nomer_soal"))
                    {
                        var nomerSoalValue = reader.GetValue("nomer_soal");
                        kodeSoalString = nomerSoalValue.ToString() ?? "";
                    }

                    var soal = new Soal
                    {
                        SoalId = reader.GetInt64("soal_id"),
                        UjianId = reader.GetInt64("ujian_id"),
                        KodeSoal = kodeSoalString,
                        Pertanyaan = reader.GetString("pertanyaan"),
                        CreatedAt = reader.IsDBNull("created_at") ? null : reader.GetDateTime("created_at"),
                        UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at")
                    };
                    questions.Add(soal);
                }
                await reader.CloseAsync();

                // Get answer options for each question
                foreach (var soal in questions)
                {
                    soal.OpsiJawaban = await GetAnswerOptionsAsync(connection, soal.SoalId, doShuffle);
                    soal.GambarSoal = await GetQuestionImagesAsync(connection, soal.SoalId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get questions error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (GetExamQuestionsAsync): {ex.Message}");
            }
            return questions;
        }

        /// <summary>
        /// Gets answer options for a specific question
        /// </summary>
        private async Task<List<OpsiJawaban>> GetAnswerOptionsAsync(MySqlConnection connection, long soalId, int doShuffle)
        {
            var options = new List<OpsiJawaban>();
            try
            {
                string query = @"SELECT opsi_jawaban_id, soal_id, jawaban, nilai, file, created_at, updated_at 
                                FROM t_opsi_jawaban 
                                WHERE soal_id = @soalId 
                                ORDER BY opsi_jawaban_id";

                if (doShuffle == 1)
                {
                    query = @"SELECT opsi_jawaban_id, soal_id, jawaban, nilai, file, created_at, updated_at 
                            FROM t_opsi_jawaban 
                            WHERE soal_id = @soalId 
                            ORDER BY RAND()";
                }

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@soalId", soalId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var option = new OpsiJawaban
                    {
                        OpsiJawabanId = reader.GetInt64("opsi_jawaban_id"),
                        SoalId = reader.GetInt64("soal_id"),
                        Jawaban = reader.GetString("jawaban"),
                        Nilai = reader.GetInt32("nilai"),
                        File = reader.IsDBNull("file") ? null : reader.GetString("file"),
                        CreatedAt = reader.IsDBNull("created_at") ? null : reader.GetDateTime("created_at"),
                        UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at")
                    };
                    options.Add(option);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get options error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (GetAnswerOptionsAsync): {ex.Message}");
            }
            return options;
        }

        /// <summary>
        /// Gets images for a specific question
        /// </summary>
        private async Task<List<GambarSoal>> GetQuestionImagesAsync(MySqlConnection connection, long soalId)
        {
            var images = new List<GambarSoal>();
            try
            {
                string query = @"SELECT gambar_soal_id, soal_id, file, created_at, updated_at 
                                FROM t_gambar_soal 
                                WHERE soal_id = @soalId";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@soalId", soalId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var image = new GambarSoal
                    {
                        GambarSoalId = reader.GetInt64("gambar_soal_id"),
                        SoalId = reader.GetInt64("soal_id"),
                        File = reader.GetString("file"),
                        CreatedAt = reader.IsDBNull("created_at") ? null : reader.GetDateTime("created_at"),
                        UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at")
                    };
                    images.Add(image);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get images error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (GetQuestionImagesAsync): {ex.Message}");
            }
            return images;
        }

        #endregion

        #region Student Answers

        /// <summary>
        /// Saves a student's answer to a question
        /// </summary>
        public async Task<bool> SaveStudentAnswerAsync(long soalId, long mahasiswaId, long opsiJawabanId)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                // Check if answer already exists
                string checkQuery = @"SELECT soal_mahasiswa_id FROM t_soal_mahasiswa 
                                     WHERE soal_id = @soalId AND mahasiswa_id = @mahasiswaId";

                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@soalId", soalId);
                checkCommand.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                var existingId = await checkCommand.ExecuteScalarAsync();

                string query;
                if (existingId != null)
                {
                    // Update existing answer
                    query = @"UPDATE t_soal_mahasiswa 
                             SET opsi_jawaban_id = @opsiJawabanId, updated_at = NOW() 
                             WHERE soal_id = @soalId AND mahasiswa_id = @mahasiswaId";
                }
                else
                {
                    // Insert new answer
                    query = @"INSERT INTO t_soal_mahasiswa (soal_id, mahasiswa_id, opsi_jawaban_id, created_at) 
                             VALUES (@soalId, @mahasiswaId, @opsiJawabanId, NOW())";
                }

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@soalId", soalId);
                command.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);
                command.Parameters.AddWithValue("@opsiJawabanId", opsiJawabanId);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save answer error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (SaveStudentAnswerAsync): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets a student's answers for an exam
        /// </summary>
        public async Task<List<SoalMahasiswa>> GetStudentAnswersAsync(long ujianId, long mahasiswaId)
        {
            var answers = new List<SoalMahasiswa>();
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                string query = @"
                    SELECT sm.soal_mahasiswa_id, sm.soal_id, sm.mahasiswa_id, sm.opsi_jawaban_id, 
                           sm.created_at, sm.updated_at
                    FROM t_soal_mahasiswa sm
                    INNER JOIN t_soal s ON sm.soal_id = s.soal_id
                    WHERE s.ujian_id = @ujianId AND sm.mahasiswa_id = @mahasiswaId";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ujianId", ujianId);
                command.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var answer = new SoalMahasiswa
                    {
                        SoalMahasiswaId = reader.GetInt64("soal_mahasiswa_id"),
                        SoalId = reader.GetInt64("soal_id"),
                        MahasiswaId = reader.GetInt64("mahasiswa_id"),
                        OpsiJawabanId = reader.GetInt64("opsi_jawaban_id"),
                        CreatedAt = reader.IsDBNull("created_at") ? null : reader.GetDateTime("created_at"),
                        UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at")
                    };
                    answers.Add(answer);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get student answers error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (GetStudentAnswersAsync): {ex.Message}");
            }
            return answers;
        }

        #endregion

        #region Exam Session Management

        /// <summary>
        /// Starts an exam session for a student
        /// </summary>
        public async Task<long> StartExamSessionAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                // First check if an existing session is there
                string checkQuery = @"SELECT ujianmahasiswa_id FROM t_ujian_mahasiswa 
                                      WHERE ujian_id = @ujianId AND mahasiswa_id = @mahasiswaId";

                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@ujianId", ujianId);
                checkCommand.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                var existingId = await checkCommand.ExecuteScalarAsync();

                if (existingId != null)
                {
                    // Update existing session to dimulai if needed
                    string updateQuery = @"UPDATE t_ujian_mahasiswa 
                                           SET status = 'dimulai', updated_at = NOW() 
                                           WHERE ujianmahasiswa_id = @id AND status = 'menunggu'";
                    using var updateCommand = new MySqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@id", Convert.ToInt64(existingId));
                    await updateCommand.ExecuteNonQueryAsync();

                    return Convert.ToInt64(existingId);
                }

                // If not found, insert
                string query = @"INSERT INTO t_ujian_mahasiswa 
                                (ujian_id, mahasiswa_id, starttime, tanggal_ujian, status, created_at) 
                                VALUES (@ujianId, @mahasiswaId, @startTime, @tanggalUjian, 'dimulai', NOW())";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ujianId", ujianId);
                command.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);
                command.Parameters.AddWithValue("@startTime", DateTime.Now);
                command.Parameters.AddWithValue("@tanggalUjian", DateTime.Now);

                await command.ExecuteNonQueryAsync();
                return command.LastInsertedId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Start exam session error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (StartExamSessionAsync): {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Ends an exam session and calculates the score
        /// </summary>
        public async Task<bool> EndExamSessionAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                // Calculate score
                int score = await CalculateExamScoreAsync(connection, ujianId, mahasiswaId);

                // Update exam session
                string query = @"UPDATE t_ujian_mahasiswa 
                                SET endtime = @endTime, status = 'selesai', nilai = @nilai, updated_at = NOW() 
                                WHERE ujian_id = @ujianId AND mahasiswa_id = @mahasiswaId 
                                AND status = 'dimulai'
                                ORDER BY ujianmahasiswa_id DESC LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@endTime", DateTime.Now);
                command.Parameters.AddWithValue("@nilai", score);
                command.Parameters.AddWithValue("@ujianId", ujianId);
                command.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"End exam session error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (EndExamSessionAsync): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Calculates the exam score for a student
        /// </summary>
        private async Task<int> CalculateExamScoreAsync(MySqlConnection connection, long ujianId, long mahasiswaId)
        {
            try
            {
                // Nilai = Sum of nilai from each options selected
                string query = @"
                    SELECT COALESCE(SUM(o.nilai), 0)
                    FROM t_soal_mahasiswa sm
                    INNER JOIN t_opsi_jawaban o ON sm.opsi_jawaban_id = o.opsi_jawaban_id
                    INNER JOIN t_soal s ON sm.soal_id = s.soal_id
                    WHERE s.ujian_id = @ujianId AND sm.mahasiswa_id = @mahasiswaId";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ujianId", ujianId);
                command.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Calculate score error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (CalculateExamScoreAsync): {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets student exam result
        /// </summary>
        public async Task<UjianMahasiswa?> GetStudentExamResultAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                string query = @"SELECT ujianmahasiswa_id, ujian_id, mahasiswa_id, starttime, endtime, 
                                       extendtime, tanggal_ujian, status, keterangan, nilai, created_at, updated_at 
                                FROM t_ujian_mahasiswa 
                                WHERE ujian_id = @ujianId AND mahasiswa_id = @mahasiswaId 
                                AND status = 'selesai'
                                ORDER BY ujianmahasiswa_id DESC LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ujianId", ujianId);
                command.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new UjianMahasiswa
                    {
                        UjianMahasiswaId = reader.GetInt64("ujianmahasiswa_id"),
                        UjianId = reader.GetInt64("ujian_id"),
                        MahasiswaId = reader.GetInt64("mahasiswa_id"),
                        
                        // Parse TIME/DATETIME types safely. Usually TIME comes as TimeSpan and DATETIME as DateTime.
                        StartTime = reader.IsDBNull("starttime") ? null : 
                                    (reader.GetFieldType(reader.GetOrdinal("starttime")) == typeof(TimeSpan) 
                                        ? DateTime.Today.Add(reader.GetTimeSpan("starttime")) 
                                        : reader.GetDateTime("starttime")),
                                        
                        EndTime = reader.IsDBNull("endtime") ? null : 
                                  (reader.GetFieldType(reader.GetOrdinal("endtime")) == typeof(TimeSpan) 
                                        ? DateTime.Today.Add(reader.GetTimeSpan("endtime")) 
                                        : reader.GetDateTime("endtime")),
                                        
                        ExtendTime = reader.IsDBNull("extendtime") ? null : 
                                     (reader.GetFieldType(reader.GetOrdinal("extendtime")) == typeof(TimeSpan) 
                                        ? DateTime.Today.Add(reader.GetTimeSpan("extendtime")) 
                                        : reader.GetDateTime("extendtime")),
                                        
                        TanggalUjian = reader.IsDBNull("tanggal_ujian") ? null : reader.GetDateTime("tanggal_ujian"),
                        Status = reader.GetString("status"),
                        Keterangan = reader.IsDBNull("keterangan") ? null : reader.GetString("keterangan"),
                        Nilai = reader.IsDBNull("nilai") ? null : reader.GetInt32("nilai"),
                        CreatedAt = reader.IsDBNull("created_at") ? null : reader.GetDateTime("created_at"),
                        UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get exam result error: {ex.Message}");
                System.Windows.MessageBox.Show($"DB Error (GetStudentExamResultAsync): {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Verifies a password against a hash (for bcrypt hashed passwords)
        /// </summary>
        private bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(password))
                return false;

            try
            {
                // Check if it's a bcrypt hash (starts with $2a$, $2b$, $2x$, or $2y$)
                if (hash.StartsWith("$2") && hash.Length >= 60)
                {
                    return BCrypt.Net.BCrypt.Verify(password, hash);
                }

                // Fallback for plain text comparison if no hash is used
                return password == hash;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
