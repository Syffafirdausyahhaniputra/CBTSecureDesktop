using MySqlConnector;
using CBTSecureDesktop.Models;
using System.Data;

namespace CBTSecureDesktop.Data
{
    /// <summary>
    /// Result of a per-save device binding validation (State-Reconciliation check).
    /// </summary>
    public enum DeviceCheckResult
    {
        Safe,         // active_device_id matches current machine — proceed normally
        AutoRelocked, // was NULL (admin accidentally reset) — silently re-locked, proceed
        Breach        // different device in DB — abort save and force logout
    }

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
                return null;
            }
        }

        /// <summary>
        /// Authenticates a mahasiswa and enforces Single Active Session (device binding).
        /// Uses SELECT … FOR UPDATE inside a transaction to prevent TOCTOU race conditions.
        /// Returns (Success, Message, User):
        ///   Message == null  → user was not found as a mahasiswa (caller should try other auth paths)
        ///   Message != null  → user exists; Success == false means wrong password or device conflict
        /// </summary>
        public async Task<(bool Success, string? Message, User? User)> LoginMahasiswaAsync(
            string username, string password)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    const string selectSql = @"
                        SELECT user_id, mahasiswa_id, username, password, level, active_device_id
                        FROM t_user
                        WHERE username = @username AND level = 'mahasiswa'
                        FOR UPDATE";

                    using var cmd = new MySqlCommand(selectSql, connection, transaction);
                    cmd.Parameters.AddWithValue("@username", username);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        await reader.CloseAsync();
                        await transaction.RollbackAsync();
                        return (false, null, null); // not a mahasiswa — caller falls back
                    }

                    var user = new User
                    {
                        UserId       = reader.GetInt64("user_id"),
                        MahasiswaId  = reader.IsDBNull("mahasiswa_id") ? null : reader.GetInt64("mahasiswa_id"),
                        Username     = reader.GetString("username"),
                        Password     = reader.GetString("password"),
                        Level        = reader.GetString("level"),
                        ActiveDeviceId = reader.IsDBNull("active_device_id") ? null : reader.GetString("active_device_id")
                    };
                    await reader.CloseAsync();

                    // --- Password verification ---
                    if (!VerifyPassword(password, user.Password))
                    {
                        await transaction.RollbackAsync();
                        return (false, "Username atau Password salah", null);
                    }

                    // --- Device binding check ---
                    string currentDevice = Environment.MachineName;
                    if (user.ActiveDeviceId != null &&
                        !string.Equals(user.ActiveDeviceId, currentDevice, StringComparison.OrdinalIgnoreCase))
                    {
                        await transaction.RollbackAsync();
                        return (false, "Akun sedang digunakan di perangkat lain.", null);
                    }

                    // --- Bind this device (covers NULL and same-machine re-login after crash) ---
                    const string updateSql = @"
                        UPDATE t_user
                        SET active_device_id = @deviceId
                        WHERE username = @username2";

                    using var updateCmd = new MySqlCommand(updateSql, connection, transaction);
                    updateCmd.Parameters.AddWithValue("@deviceId",   currentDevice);
                    updateCmd.Parameters.AddWithValue("@username2",  username);
                    await updateCmd.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();

                    user.ActiveDeviceId = currentDevice;
                    return (true, string.Empty, user);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoginMahasiswaAsync error: {ex.Message}");
                return (false, "Terjadi kesalahan saat login. Silakan coba lagi.", null);
            }
        }

        /// <summary>
        /// Releases the device binding for a mahasiswa on logout or exam completion,
        /// so the account can be used from another machine afterwards.
        /// </summary>
        public async Task<bool> LogoutMahasiswaAsync(string username)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE t_user
                    SET active_device_id = NULL
                    WHERE username = @username AND level = 'mahasiswa'";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@username", username);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LogoutMahasiswaAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// State-Reconciliation (Self-Healing) check called before every answer save.
        /// Scenario A — active_device_id is NULL (admin accidentally reset): silently re-locks
        ///              to the current machine and returns AutoRelocked so the save continues.
        /// Scenario B — active_device_id is set to a DIFFERENT machine: returns Breach so the
        ///              caller aborts the save and triggers a forced logout.
        /// Scenario C — active_device_id matches the current machine: returns Safe.
        /// On any DB/network error the method returns Safe to avoid interrupting the exam.
        /// </summary>
        public async Task<DeviceCheckResult> ValidateAndReconcileDeviceAsync(long mahasiswaId)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                const string selectSql = @"
                    SELECT active_device_id
                    FROM t_user
                    WHERE mahasiswa_id = @mahasiswaId AND level = 'mahasiswa'";

                using var selectCmd = new MySqlCommand(selectSql, connection);
                selectCmd.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                var rawResult = await selectCmd.ExecuteScalarAsync();

                // No matching row — cannot validate; treat as safe so exam is not interrupted
                if (rawResult == null)
                    return DeviceCheckResult.Safe;

                // Column value is SQL NULL → admin reset the binding
                string? storedDevice = rawResult == DBNull.Value ? null : rawResult as string;
                string currentDevice = Environment.MachineName;

                // --- Scenario A: NULL → Auto Re-Lock silently ---
                if (string.IsNullOrEmpty(storedDevice))
                {
                    const string relockSql = @"
                        UPDATE t_user
                        SET active_device_id = @deviceId
                        WHERE mahasiswa_id = @mahasiswaId2 AND level = 'mahasiswa'";

                    using var relockCmd = new MySqlCommand(relockSql, connection);
                    relockCmd.Parameters.AddWithValue("@deviceId",    currentDevice);
                    relockCmd.Parameters.AddWithValue("@mahasiswaId2", mahasiswaId);
                    await relockCmd.ExecuteNonQueryAsync();

                    System.Diagnostics.Debug.WriteLine(
                        $"[StateReconciliation] Auto Re-Lock: mahasiswaId={mahasiswaId}, device={currentDevice}");
                    return DeviceCheckResult.AutoRelocked;
                }

                // --- Scenario B: different device → Breach ---
                if (!string.Equals(storedDevice, currentDevice, StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[StateReconciliation] BREACH: stored={storedDevice}, current={currentDevice}, mahasiswaId={mahasiswaId}");
                    return DeviceCheckResult.Breach;
                }

                // --- Scenario C: same device → Safe ---
                return DeviceCheckResult.Safe;
            }
            catch (Exception ex)
            {
                // Do not interrupt the exam for transient DB/network errors
                System.Diagnostics.Debug.WriteLine($"ValidateAndReconcileDeviceAsync error: {ex.Message}");
                return DeviceCheckResult.Safe;
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
                return null;
            }
        }

        /// <summary>
        /// Changes a user's password after verifying their current password.
        /// </summary>
        public async Task<(bool Success, string Message)> ChangeUserPasswordAsync(long userId, string currentPassword, string newPassword)
        {
            if (userId <= 0)
                return (false, "User tidak valid.");

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return (false, "Password saat ini dan password baru wajib diisi.");

            if (newPassword.Length < 6)
                return (false, "Password baru minimal 6 karakter.");

            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                string selectQuery = @"SELECT password FROM t_user WHERE user_id = @userId LIMIT 1";
                using var selectCommand = new MySqlCommand(selectQuery, connection);
                selectCommand.Parameters.AddWithValue("@userId", userId);

                var storedPassword = await selectCommand.ExecuteScalarAsync();
                if (storedPassword == null)
                    return (false, "User tidak ditemukan.");

                var storedPasswordText = storedPassword.ToString() ?? string.Empty;
                if (!VerifyPassword(currentPassword, storedPasswordText) && !string.Equals(currentPassword, storedPasswordText, StringComparison.Ordinal))
                    return (false, "Password saat ini salah.");

                string newPasswordHash = HashPassword(newPassword);

                string updateQuery = @"UPDATE t_user 
                                       SET password = @password, updated_at = NOW() 
                                       WHERE user_id = @userId";
                using var updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@password", newPasswordHash);
                updateCommand.Parameters.AddWithValue("@userId", userId);

                int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                    return (true, "Password berhasil diperbarui.");

                return (false, "Password gagal diperbarui.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Change password error: {ex.Message}");
                return (false, "Terjadi kesalahan saat mengubah password.");
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
                           um.status as status_mahasiswa, um.nilai as nilai_mahasiswa,
                           um.extendtime as extendtime
                    FROM t_ujian u
                    INNER JOIN t_ujian_kelas uk ON u.ujian_id = uk.ujian_id
                    INNER JOIN t_kelas_mahasiswa km ON uk.kelas_id = km.kelas_id
                    INNER JOIN t_matakuliah m ON u.matakuliah_id = m.matakuliah_id
                    INNER JOIN t_prodi p ON u.prodi_id = p.prodi_id
                    LEFT JOIN t_ujian_mahasiswa um ON u.ujian_id = um.ujian_id AND um.mahasiswa_id = @mahasiswaId
                    WHERE km.mahasiswa_id = @mahasiswaId
                    AND u.status IN ('menunggu', 'dimulai', 'selesai', 'dihentikan')
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
                        StatusMahasiswa = reader.IsDBNull("status_mahasiswa") ? "none" : reader.GetString("status_mahasiswa"),
                        Nilai = reader.IsDBNull("nilai_mahasiswa") ? null : reader.GetDouble("nilai_mahasiswa"),
                        ExtendTimeMinutes = reader.IsDBNull("extendtime") ? 0 : reader.GetInt32("extendtime")
                    };
                            exams.Add(ujian);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Get exams error: {ex.Message}");
                        }
                        return exams;
                    }

                    /// <summary>
                    /// Gets exam (t_ujian) record by ujian_id and student-specific extend time from t_ujian_mahasiswa.
                    /// </summary>
                    public async Task<Ujian?> GetExamByIdAsync(long ujianId, long mahasiswaId)
                    {
                        try
                        {
                            using var connection = _dbConnection.GetConnection();
                            await connection.OpenAsync();

                            string query = @"SELECT u.ujian_id, u.matakuliah_id, u.tahun_ajaran_id, u.prodi_id, u.kode_ujian, u.nama_ujian, u.status, u.shufflesoal, u.starttime, u.endtime, u.created_at, u.updated_at,
                                                   um.extendtime as extendtime
                                            FROM t_ujian u
                                            LEFT JOIN t_ujian_mahasiswa um ON u.ujian_id = um.ujian_id AND um.mahasiswa_id = @mahasiswaId
                                            WHERE u.ujian_id = @ujianId
                                            ORDER BY um.ujianmahasiswa_id DESC
                                            LIMIT 1";

                            using var command = new MySqlCommand(query, connection);
                            command.Parameters.AddWithValue("@ujianId", ujianId);
                            command.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                            using var reader = await command.ExecuteReaderAsync();
                            if (await reader.ReadAsync())
                            {
                                var ujian = new Ujian
                                {
                                    UjianId = reader.GetInt64("ujian_id"),
                                    MatakuliahId = reader.GetInt64("matakuliah_id"),
                                    TahunAjaranId = reader.GetInt64("tahun_ajaran_id"),
                                    ProdiId = reader.GetInt64("prodi_id"),
                                    KodeUjian = reader.IsDBNull("kode_ujian") ? string.Empty : reader.GetString("kode_ujian"),
                                    NamaUjian = reader.IsDBNull("nama_ujian") ? string.Empty : reader.GetString("nama_ujian"),
                                    Status = reader.IsDBNull("status") ? "menunggu" : reader.GetString("status"),
                                    ShuffleSoal = reader.IsDBNull("shufflesoal") ? 0 : reader.GetInt32("shufflesoal"),
                                    StartTime = reader.IsDBNull("starttime") ? DateTime.MinValue : reader.GetDateTime("starttime"),
                                    EndTime = reader.IsDBNull("endtime") ? DateTime.MinValue : reader.GetDateTime("endtime"),
                                    CreatedAt = reader.IsDBNull("created_at") ? null : reader.GetDateTime("created_at"),
                                    UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at"),
                                    ExtendTimeMinutes = reader.IsDBNull("extendtime") ? 0 : reader.GetInt32("extendtime")
                                };

                                return ujian;
                            }

                            return null;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Get exam by id error: {ex.Message}");
                            return null;
                        }
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
            }
            return images;
        }

        #endregion

        #region Student Answers

        /// <summary>
        /// Saves a student's answer to a question (Multiple choices)
        /// </summary>
        public async Task<bool> SaveStudentAnswersAsync(long soalId, long mahasiswaId, List<long> opsiJawabanIds)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                // Delete existing answers to replace them
                string deleteQuery = @"DELETE FROM t_soal_mahasiswa 
                                       WHERE soal_id = @soalId AND mahasiswa_id = @mahasiswaId";

                using var deleteCommand = new MySqlCommand(deleteQuery, connection);
                deleteCommand.Parameters.AddWithValue("@soalId", soalId);
                deleteCommand.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);
                await deleteCommand.ExecuteNonQueryAsync();

                if (opsiJawabanIds.Count == 0)
                {
                    string insertEmptyQuery = @"INSERT INTO t_soal_mahasiswa (soal_id, mahasiswa_id, opsi_jawaban_id, created_at) 
                                                VALUES (@soalId, @mahasiswaId, NULL, NOW())";
                    using var emptyCmd = new MySqlCommand(insertEmptyQuery, connection);
                    emptyCmd.Parameters.AddWithValue("@soalId", soalId);
                    emptyCmd.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);
                    await emptyCmd.ExecuteNonQueryAsync();
                    return true;
                }

                int rowsAffected = 0;
                foreach (var opsiId in opsiJawabanIds)
                {
                    string insertQuery = @"INSERT INTO t_soal_mahasiswa (soal_id, mahasiswa_id, opsi_jawaban_id, created_at) 
                                           VALUES (@soalId, @mahasiswaId, @opsiJawabanId, NOW())";
                    using var insertCommand = new MySqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@soalId", soalId);
                    insertCommand.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);
                    insertCommand.Parameters.AddWithValue("@opsiJawabanId", opsiId);
                    rowsAffected += await insertCommand.ExecuteNonQueryAsync();
                }

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save answers error: {ex.Message}");
                return false;
            }
        }

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
                System.Diagnostics.Debug.WriteLine($"SaveStudentAnswerAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Seeds all empty answers into t_soal_mahasiswa so they can be tracked from the start
        /// Ordered properly by iterating over the list provided by questions retrieval
        /// </summary>
        public async Task InitializeStudentAnswersAsync(long ujianId, long mahasiswaId, List<long> orderedSoalIds)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                foreach (var soalId in orderedSoalIds)
                {
                    string query = @"
                        INSERT IGNORE INTO t_soal_mahasiswa (soal_id, mahasiswa_id, opsi_jawaban_id, created_at)
                        VALUES (@soalId, @mahasiswaId, NULL, NOW())";

                    using var command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@soalId", soalId);
                    command.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initialize student answers error: {ex.Message}");
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
                        OpsiJawabanId = reader.IsDBNull("opsi_jawaban_id") ? null : reader.GetInt64("opsi_jawaban_id"),
                        CreatedAt = reader.IsDBNull("created_at") ? null : reader.GetDateTime("created_at"),
                        UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at")
                    };
                    answers.Add(answer);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get student answers error: {ex.Message}");
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
                double score = await CalculateExamScoreAsync(connection, ujianId, mahasiswaId);

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
                return false;
            }
        }

        /// <summary>
        /// Calculates the exam score for a student
        /// </summary>
        private async Task<double> CalculateExamScoreAsync(MySqlConnection connection, long ujianId, long mahasiswaId)
        {
            try
            {
                // 1. Dapatkan total skor mentah mahasiswa dengan memperhitungkan soal yang memiliki lebih dari satu jawaban benar
                string rawScoreQuery = @"
                    SELECT COALESCE(SUM(
                        CASE 
                            WHEN q_correct.total_correct > 0 AND sm_option.nilai = 1 THEN (1.0 / q_correct.total_correct)
                            ELSE 0 
                        END
                    ), 0) as total_raw_score
                    FROM t_soal_mahasiswa sm
                    INNER JOIN t_soal s ON sm.soal_id = s.soal_id
                    LEFT JOIN t_opsi_jawaban sm_option ON sm.opsi_jawaban_id = sm_option.opsi_jawaban_id
                    LEFT JOIN (
                        SELECT soal_id, SUM(nilai) as total_correct 
                        FROM t_opsi_jawaban 
                        GROUP BY soal_id
                    ) q_correct ON s.soal_id = q_correct.soal_id
                    WHERE s.ujian_id = @ujianId AND sm.mahasiswa_id = @mahasiswaId";

                using var rawScoreCommand = new MySqlCommand(rawScoreQuery, connection);
                rawScoreCommand.Parameters.AddWithValue("@ujianId", ujianId);
                rawScoreCommand.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                double rawScore = Convert.ToDouble(await rawScoreCommand.ExecuteScalarAsync());

                // 2. Dapatkan total keseluruhan soal pada ujian tersebut
                string totalQuestionsQuery = "SELECT COUNT(*) FROM t_soal WHERE ujian_id = @ujianId";
                using var totalQuestionsCommand = new MySqlCommand(totalQuestionsQuery, connection);
                totalQuestionsCommand.Parameters.AddWithValue("@ujianId", ujianId);

                int totalQuestions = Convert.ToInt32(await totalQuestionsCommand.ExecuteScalarAsync());

                // 3. Kalkulasi nilai akhir: (Skor Mentah / Total Soal) * 100
                if (totalQuestions == 0) return 0; // Menghindari pembagian dengan nol

                double finalScore = (rawScore / totalQuestions) * 100.0;

                // Pembulatan ke 2 angka di belakang koma
                return Math.Round(finalScore, 2, MidpointRounding.AwayFromZero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Calculate score error: {ex.Message}");
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
                        Nilai = reader.IsDBNull("nilai") ? null : reader.GetDouble("nilai"),
                        CreatedAt = reader.IsDBNull("created_at") ? null : reader.GetDateTime("created_at"),
                        UpdatedAt = reader.IsDBNull("updated_at") ? null : reader.GetDateTime("updated_at")
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get exam result error: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> GetStudentExamStatusAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                string query = @"SELECT status 
                                FROM t_ujian_mahasiswa 
                                WHERE ujian_id = @ujianId AND mahasiswa_id = @mahasiswaId 
                                ORDER BY ujianmahasiswa_id DESC LIMIT 1";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@ujianId", ujianId);
                command.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                var result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get exam status error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves force stopped exam and calculates the score while keeping status 'dihentikan'
        /// </summary>
        public async Task<bool> SaveForceStopExamAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                // Calculate score
                double score = await CalculateExamScoreAsync(connection, ujianId, mahasiswaId);

                // Update exam session
                string query = @"UPDATE t_ujian_mahasiswa 
                                SET endtime = @endTime, nilai = @nilai, updated_at = NOW() 
                                WHERE ujian_id = @ujianId AND mahasiswa_id = @mahasiswaId 
                                AND status = 'dihentikan'
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
                System.Diagnostics.Debug.WriteLine($"Save force stop exam error: {ex.Message}");
                // No message box here because it's called silently in the background
                return false;
            }
        }

        /// <summary>
        /// Records an automatic breach-triggered termination in t_ujian_mahasiswa.
        /// Unlike SaveForceStopExamAsync (which requires the admin to set status first),
        /// this method sets status = 'dihentikan' itself, calculates the score, stamps
        /// endtime, and writes the supplied breach reason into the keterangan column.
        /// </summary>
        public async Task<bool> SaveBreachTerminationAsync(long ujianId, long mahasiswaId, string keterangan)
        {
            try
            {
                using var connection = _dbConnection.GetConnection();
                await connection.OpenAsync();

                double score = await CalculateExamScoreAsync(connection, ujianId, mahasiswaId);

                const string sql = @"
                    UPDATE t_ujian_mahasiswa
                    SET status      = 'dihentikan',
                        endtime     = @endTime,
                        nilai       = @nilai,
                        keterangan  = @keterangan,
                        updated_at  = NOW()
                    WHERE ujian_id     = @ujianId
                      AND mahasiswa_id = @mahasiswaId
                      AND status       = 'dimulai'
                    ORDER BY ujianmahasiswa_id DESC
                    LIMIT 1";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@endTime",    DateTime.Now);
                cmd.Parameters.AddWithValue("@nilai",      score);
                cmd.Parameters.AddWithValue("@keterangan", keterangan);
                cmd.Parameters.AddWithValue("@ujianId",    ujianId);
                cmd.Parameters.AddWithValue("@mahasiswaId", mahasiswaId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveBreachTerminationAsync error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Hashes a password for secure storage.
        /// </summary>
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

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
