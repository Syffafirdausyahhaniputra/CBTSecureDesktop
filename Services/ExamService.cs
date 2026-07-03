using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using CBTSecureDesktop.Data;
using CBTSecureDesktop.Models;
using System.Net.Http;
using System.Net.Http.Json;
using CBTSecureDesktop.Configuration;

namespace CBTSecureDesktop.Services
{
    /// <summary>
    /// View model for exam questions (bridges database model and UI)
    /// </summary>
    public class ExamQuestion
    {
        public long SoalId { get; set; }
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public List<long> OptionIds { get; set; } = new();
        public List<string?> OptionFiles { get; set; } = new();
        public int? SelectedAnswer { get; set; }
        public List<int> SelectedAnswers { get; set; } = new();
        public bool IsMultiAnswer { get; set; } = false;
        public bool IsDoubtful { get; set; } = false;
        public List<string> Images { get; set; } = new();
    }

    internal sealed class ExamOrderState
    {
        public List<long> QuestionOrder { get; set; } = new();
        public Dictionary<long, List<long>> OptionOrderBySoalId { get; set; } = new();
        public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Manages exam data and student responses using MySQL database.
    /// </summary>
    public class ExamService
    {
        private readonly DatabaseService _databaseService;
        private List<ExamQuestion> _currentExamQuestions = new();
        private long _currentUjianId;
        private long _currentMahasiswaId;
        private long _currentExamSessionId;

        // Pending queue file and sync controls
        private readonly string _pendingFilePath;
        private readonly string _appDataDirectory;
        private readonly SemaphoreSlim _pendingLock = new(1, 1);
        private readonly SemaphoreSlim _doubtLock = new(1, 1);
        private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(30);
        private CancellationTokenSource? _flushCts;

        // Pending notifications
        public event Action<int>? PendingCountChanged;

        // State-Reconciliation: check throttle — avoids one SELECT per keystroke
        private DateTime _lastDeviceCheckUtc = DateTime.MinValue;
        private static readonly TimeSpan DeviceCheckInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Fires when a session takeover (Breach) is detected during an active exam.
        /// The ExamWindow handler shows a warning and performs a forced logout.
        /// </summary>
        public event Action? SecurityBreachDetected;

        public ExamService()
        {
            _databaseService = new DatabaseService();

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _appDataDirectory = Path.Combine(appData, "CBTSecureDesktop");
            if (!Directory.Exists(_appDataDirectory)) Directory.CreateDirectory(_appDataDirectory);
            _pendingFilePath = Path.Combine(_appDataDirectory, "PendingAnswers.json");

            // Start background flush loop
            _flushCts = new CancellationTokenSource();
            _ = Task.Run(() => FlushLoopAsync(_flushCts.Token));
        }

        // Public helper to get current pending count
        public async Task<int> GetPendingCountAsync(long? ujianId = null, long? mahasiswaId = null)
        {
            var list = await ReadPendingAsync(ujianId, mahasiswaId);
            return list.Count;
        }

        // Public helper to get list of pending question IDs (soalId) for offline preview indicator
        public async Task<List<long>> GetPendingSoalIdsAsync(long? ujianId = null, long? mahasiswaId = null)
        {
            var list = await ReadPendingAsync(ujianId, mahasiswaId);
            return list.Where(p => p.Type == PendingEntryType.SingleAnswer || p.Type == PendingEntryType.MultipleAnswers)
                       .Select(p => p.SoalId)
                       .Distinct()
                       .ToList();
        }

        public Task<bool> TestConnectionAsync()
        {
            return DatabaseConnection.Instance.TestConnectionAsync();
        }

        // Public trigger to request an immediate flush
        public Task TriggerFlushAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    await TryFlushOnceAsync();
                }
                catch { }
            });
        }

        /// <summary>
        /// Gets the list of available exams for the student from database.
        /// </summary>
        public async Task<List<Ujian>> GetAvailableExamsAsync(long mahasiswaId)
        {
            try
            {
                return await _databaseService.GetAvailableExamsForStudentAsync(mahasiswaId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get available exams error: {ex.Message}");
                return new List<Ujian>();
            }
        }

        /// <summary>
        /// Gets exam information for the active student session, including student-specific extend time.
        /// </summary>
        public async Task<Ujian?> GetExamByIdAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                return await _databaseService.GetExamByIdAsync(ujianId, mahasiswaId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get exam by id error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all question image IDs for a specific exam to be pre-downloaded.
        /// </summary>
        public async Task<List<string>> GetAllImageIdsForExamAsync(long ujianId)
        {
            try
            {
                var soalList = await _databaseService.GetExamQuestionsAsync(ujianId);
                var imageIds = new List<string>();

                foreach (var soal in soalList)
                {
                    foreach (var img in soal.GambarSoal)
                    {
                        string imgId = img.GambarSoalId.ToString();
                        if (!imageIds.Contains(imgId))
                        {
                            imageIds.Add(imgId);
                        }
                    }
                }

                return imageIds;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get image IDs error: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets all option image IDs for a specific exam to be pre-downloaded.
        /// </summary>
        public async Task<List<int>> GetAllOptionImageIdsForExamAsync(long ujianId)
        {
            try
            {
                var soalList = await _databaseService.GetExamQuestionsAsync(ujianId);
                var optionImageIds = new List<int>();

                foreach (var soal in soalList)
                {
                    foreach (var option in soal.OpsiJawaban)
                    {
                        if (!string.IsNullOrWhiteSpace(option.File) && option.OpsiJawabanId > 0 && !optionImageIds.Contains((int)option.OpsiJawabanId))
                        {
                            optionImageIds.Add((int)option.OpsiJawabanId);
                        }
                    }
                }

                return optionImageIds;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get option image IDs error: {ex.Message}");
                return new List<int>();
            }
        }

        /// <summary>
        /// Loads the questions for a specific exam from database.
        /// </summary>
        public async Task<List<ExamQuestion>> LoadExamQuestionsAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                _currentUjianId = ujianId;
                _currentMahasiswaId = mahasiswaId;

                // Start exam session once when exam window is opened
                _currentExamSessionId = await _databaseService.StartExamSessionAsync(ujianId, mahasiswaId);
                
                try
                {
                    await NotifyMonitoringAsync(ujianId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Notify monitoring error: {ex.Message}");
                }

                // Get latest questions from database and apply deterministic shuffle
                var soalList = await _databaseService.GetExamQuestionsAsync(ujianId);
                _currentExamQuestions = await BuildDeterministicExamQuestionsAsync(soalList, ujianId, mahasiswaId);

                var savedOrder = await ReadExamOrderStateAsync(ujianId, mahasiswaId);
                if (savedOrder != null)
                {
                    _currentExamQuestions = ApplyExamOrderState(_currentExamQuestions, savedOrder);
                }

                // Ensure order is always persisted in encrypted local file
                await WriteExamOrderStateAsync(ujianId, mahasiswaId, _currentExamQuestions);

                // Prepare blank answers rows in deterministic display sequence
                await _databaseService.InitializeStudentAnswersAsync(ujianId, mahasiswaId, _currentExamQuestions.Select(q => q.SoalId).ToList());

                return _currentExamQuestions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load exam questions error: {ex.Message}");
                System.Windows.MessageBox.Show($"Service Error (LoadExamQuestionsAsync): {ex.Message}");
                return new List<ExamQuestion>();
            }
        }

        /// <summary>
        /// Refreshes latest exam questions content from database while preserving deterministic display order.
        /// </summary>
        public async Task<List<ExamQuestion>> GetExamQuestionsAsync(long ujianId)
        {
            try
            {
                if (_currentMahasiswaId <= 0)
                {
                    return new List<ExamQuestion>();
                }

                _currentUjianId = ujianId;

                var soalList = await _databaseService.GetExamQuestionsAsync(ujianId);
                _currentExamQuestions = await BuildDeterministicExamQuestionsAsync(soalList, ujianId, _currentMahasiswaId);

                var savedOrder = await ReadExamOrderStateAsync(ujianId, _currentMahasiswaId);
                if (savedOrder != null)
                {
                    _currentExamQuestions = ApplyExamOrderState(_currentExamQuestions, savedOrder);
                }

                await WriteExamOrderStateAsync(ujianId, _currentMahasiswaId, _currentExamQuestions);
                return _currentExamQuestions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get exam questions error: {ex.Message}");
                return new List<ExamQuestion>();
            }
        }

        private async Task<List<ExamQuestion>> BuildDeterministicExamQuestionsAsync(List<Soal> soalList, long ujianId, long mahasiswaId)
        {
            // Normalize source order first so deterministic seed always generates the same question sequence.
            var normalizedQuestions = soalList.OrderBy(s => s.SoalId).ToList();

            var examQuestions = normalizedQuestions.Select(soal =>
            {
                bool isMulti = soal.OpsiJawaban.Count(o => o.Nilai == 1) > 1;
                return new ExamQuestion
                {
                    SoalId = soal.SoalId,
                    QuestionText = soal.Pertanyaan,
                    Options = soal.OpsiJawaban.Select(o => o.Jawaban).ToList(),
                    OptionIds = soal.OpsiJawaban.Select(o => o.OpsiJawabanId).ToList(),
                    OptionFiles = soal.OpsiJawaban.Select(o => o.File).ToList(),
                    IsMultiAnswer = isMulti,
                    Images = soal.GambarSoal.Select(g => g.GambarSoalId.ToString()).ToList()
                };
            }).ToList();

            int deterministicSeed = unchecked((int)((ujianId * 100000) + mahasiswaId));
            var rnd = new Random(deterministicSeed);
            examQuestions = examQuestions.OrderBy(q => rnd.Next()).ToList();

            for (int i = 0; i < examQuestions.Count; i++)
            {
                examQuestions[i].QuestionNumber = i + 1;
            }

            var existingAnswers = await _databaseService.GetStudentAnswersAsync(ujianId, mahasiswaId);
            foreach (var answer in existingAnswers)
            {
                if (!answer.OpsiJawabanId.HasValue)
                {
                    continue;
                }

                var question = examQuestions.FirstOrDefault(q => q.SoalId == answer.SoalId);
                if (question == null)
                {
                    continue;
                }

                int optionIndex = question.OptionIds.IndexOf(answer.OpsiJawabanId.Value);
                if (optionIndex < 0)
                {
                    continue;
                }

                if (question.IsMultiAnswer)
                {
                    if (!question.SelectedAnswers.Contains(optionIndex))
                    {
                        question.SelectedAnswers.Add(optionIndex);
                    }
                }
                else
                {
                    question.SelectedAnswer = optionIndex;
                }
            }

            var localDoubtStates = await ReadDoubtStatesAsync(ujianId, mahasiswaId);
            foreach (var question in examQuestions)
            {
                if (localDoubtStates.TryGetValue(question.SoalId, out var isDoubtful))
                {
                    question.IsDoubtful = isDoubtful;
                }
            }

            return examQuestions;
        }

        /// <summary>
        /// State-Reconciliation: runs a device binding check against the DB when the throttle
        /// window has expired (at most once every 30 s). Returns false and fires
        /// SecurityBreachDetected if a different device is found in the database.
        /// </summary>
        private async Task<bool> PerformDeviceCheckIfDueAsync()
        {
            if (DateTime.UtcNow - _lastDeviceCheckUtc < DeviceCheckInterval)
                return true; // Throttle: not due yet — skip DB roundtrip

            _lastDeviceCheckUtc = DateTime.UtcNow;

            var result = await _databaseService.ValidateAndReconcileDeviceAsync(_currentMahasiswaId);
            if (result == DeviceCheckResult.Breach)
            {
                SecurityBreachDetected?.Invoke();
                return false;
            }

            return true; // Safe or AutoRelocked — allow save to continue
        }

        /// <summary>
        /// Saves the student's answer for a specific question to database.
        /// </summary>
        public async Task<bool> SaveAnswerAsync(int questionNumber, int answerIndex)
        {
            try
            {
                // --- State-Reconciliation: validate device binding before writing ---
                if (!await PerformDeviceCheckIfDueAsync())
                    return false; // Breach detected — SecurityBreachDetected already fired

                var question = _currentExamQuestions.FirstOrDefault(q => q.QuestionNumber == questionNumber);
                if (question != null && !question.IsMultiAnswer && answerIndex >= 0 && answerIndex < question.OptionIds.Count)
                {
                    question.SelectedAnswer = answerIndex;
                    long opsiJawabanId = question.OptionIds[answerIndex];

                    // Try saving to database first
                    bool saved = await _databaseService.SaveStudentAnswerAsync(
                        question.SoalId,
                        _currentMahasiswaId,
                        opsiJawabanId
                    );

                    if (!saved)
                    {
                        // Persist to local pending queue for retry
                        var entry = new PendingAnswer
                        {
                            Type = PendingEntryType.SingleAnswer,
                            UjianId = _currentUjianId,
                            MahasiswaId = _currentMahasiswaId,
                            SoalId = question.SoalId,
                            OpsiJawabanId = opsiJawabanId
                        };
                        await EnqueuePendingAsync(entry);

                        // Return true to avoid spamming the UI with errors — student progress continues
                        return true;
                    }

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save answer error: {ex.Message}");
                // On exception, also enqueue pending so user doesn't lose work
                try
                {
                    var question = _currentExamQuestions.FirstOrDefault(q => q.QuestionNumber == questionNumber);
                    if (question != null && !question.IsMultiAnswer && answerIndex >= 0 && answerIndex < question.OptionIds.Count)
                    {
                        long opsiJawabanId = question.OptionIds[answerIndex];
                        var entry = new PendingAnswer
                        {
                            Type = PendingEntryType.SingleAnswer,
                            UjianId = _currentUjianId,
                            MahasiswaId = _currentMahasiswaId,
                            SoalId = question.SoalId,
                            OpsiJawabanId = opsiJawabanId
                        };
                        await EnqueuePendingAsync(entry);
                        return true;
                    }
                }
                catch { }
                return false;
            }
        }

        /// <summary>
        /// Saves the student's multiple answers for a specific question to database.
        /// </summary>
        public async Task<bool> SaveAnswersAsync(int questionNumber, List<int> answerIndices)
        {
            try
            {
                // --- State-Reconciliation: validate device binding before writing ---
                if (!await PerformDeviceCheckIfDueAsync())
                    return false; // Breach detected — SecurityBreachDetected already fired

                var question = _currentExamQuestions.FirstOrDefault(q => q.QuestionNumber == questionNumber);
                if (question != null && question.IsMultiAnswer)
                {
                    question.SelectedAnswers = answerIndices.ToList();
                    var opsiJawabanIds = answerIndices.Where(i => i >= 0 && i < question.OptionIds.Count)
                                                      .Select(i => question.OptionIds[i]).ToList();

                    // Try save to DB
                    bool saved = await _databaseService.SaveStudentAnswersAsync(
                        question.SoalId,
                        _currentMahasiswaId,
                        opsiJawabanIds
                    );

                    if (!saved)
                    {
                        var entry = new PendingAnswer
                        {
                            Type = PendingEntryType.MultipleAnswers,
                            UjianId = _currentUjianId,
                            MahasiswaId = _currentMahasiswaId,
                            SoalId = question.SoalId,
                            OpsiJawabanIds = opsiJawabanIds
                        };
                        await EnqueuePendingAsync(entry);
                        return true;
                    }

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save answers error: {ex.Message}");
                // On exception, also enqueue pending so user doesn't lose work
                try
                {
                    var question = _currentExamQuestions.FirstOrDefault(q => q.QuestionNumber == questionNumber);
                    if (question != null && question.IsMultiAnswer)
                    {
                        var opsiJawabanIds = answerIndices.Where(i => i >= 0 && i < question.OptionIds.Count)
                                                          .Select(i => question.OptionIds[i]).ToList();
                        if (opsiJawabanIds.Count > 0)
                        {
                            var entry = new PendingAnswer
                            {
                                Type = PendingEntryType.MultipleAnswers,
                                UjianId = _currentUjianId,
                                MahasiswaId = _currentMahasiswaId,
                                SoalId = question.SoalId,
                                OpsiJawabanIds = opsiJawabanIds
                            };
                            await EnqueuePendingAsync(entry);
                            return true;
                        }
                    }
                }
                catch { }
                return false;
            }
        }

        /// <summary>
        /// Submits the exam and calculates the final score.
        /// </summary>
        public async Task<bool> SubmitExamAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                if (await IsExamAlreadyFinalizedAsync(ujianId, mahasiswaId))
                {
                    await RemovePendingEntriesForExamAsync(ujianId, mahasiswaId);
                    return true;
                }

                // Try to end exam session on server
                bool ok = await _databaseService.EndExamSessionAsync(ujianId, mahasiswaId);
                if (ok)
                {
                    await RemovePendingEntriesForExamAsync(ujianId, mahasiswaId);
                    await NotifyMonitoringAsync(ujianId);
                    return true;
                }

                if (await IsExamAlreadyFinalizedAsync(ujianId, mahasiswaId))
                {
                    await RemovePendingEntriesForExamAsync(ujianId, mahasiswaId);
                    return true;
                }

                // Enqueue a pending submit so background will retry
                var entry = new PendingAnswer
                {
                    Type = PendingEntryType.SubmitExam,
                    UjianId = ujianId,
                    MahasiswaId = mahasiswaId
                };
                await EnqueuePendingAsync(entry);

                await NotifyMonitoringAsync(ujianId);
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Submit exam error: {ex.Message}");

                if (await IsExamAlreadyFinalizedAsync(ujianId, mahasiswaId))
                {
                    await RemovePendingEntriesForExamAsync(ujianId, mahasiswaId);
                    return true;
                }

                // enqueue pending submit
                var entry = new PendingAnswer
                {
                    Type = PendingEntryType.SubmitExam,
                    UjianId = ujianId,
                    MahasiswaId = mahasiswaId
                };
                await EnqueuePendingAsync(entry);
                return false;
            }
        }

        /// <summary>
        /// Silently saves the forced stop exam calculating the score.
        /// </summary>
        public async Task<bool> CalculateAndSaveForceStopScoreAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                bool ok = await _databaseService.SaveForceStopExamAsync(ujianId, mahasiswaId);
                if (!ok)
                {
                    if (await IsExamAlreadyFinalizedAsync(ujianId, mahasiswaId))
                    {
                        await RemovePendingEntriesForExamAsync(ujianId, mahasiswaId);
                        return true;
                    }

                    var entry = new PendingAnswer
                    {
                        Type = PendingEntryType.ForceStop,
                        UjianId = ujianId,
                        MahasiswaId = mahasiswaId
                    };
                    await EnqueuePendingAsync(entry);
                }
                else
                {
                    await RemovePendingEntriesForExamAsync(ujianId, mahasiswaId);
                }

                return ok;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Submit force stopped exam error: {ex.Message}");

                if (await IsExamAlreadyFinalizedAsync(ujianId, mahasiswaId))
                {
                    await RemovePendingEntriesForExamAsync(ujianId, mahasiswaId);
                    return true;
                }

                var entry = new PendingAnswer
                {
                    Type = PendingEntryType.ForceStop,
                    UjianId = ujianId,
                    MahasiswaId = mahasiswaId
                };
                await EnqueuePendingAsync(entry);
                return false;
            }
        }

        /// <summary>
        /// Records a device-binding breach termination in t_ujian_mahasiswa:
        /// sets status = 'dihentikan', stamps endtime, calculates and stores the final
        /// score, and writes the breach reason into the keterangan column so admins can
        /// see exactly why the session was terminated.
        /// Falls back to the local pending queue if the DB write fails.
        /// </summary>
        public async Task<bool> TerminateForBreachAsync(long ujianId, long mahasiswaId)
        {
            string keterangan = $"Dihentikan otomatis oleh sistem: terdeteksi perangkat lain aktif " +
                                $"pada akun mahasiswa saat ujian berlangsung " +
                                $"(perangkat ujian: {Environment.MachineName})";
            try
            {
                bool ok = await _databaseService.SaveBreachTerminationAsync(ujianId, mahasiswaId, keterangan);
                if (!ok)
                {
                    if (await IsExamAlreadyFinalizedAsync(ujianId, mahasiswaId))
                    {
                        await RemovePendingEntriesForExamAsync(ujianId, mahasiswaId);
                        return true;
                    }

                    var entry = new PendingAnswer
                    {
                        Type = PendingEntryType.BreachTermination,
                        UjianId = ujianId,
                        MahasiswaId = mahasiswaId,
                        Keterangan = keterangan
                    };
                    await EnqueuePendingAsync(entry);
                }
                else
                {
                    await RemovePendingEntriesForExamAsync(ujianId, mahasiswaId);
                }

                return ok;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TerminateForBreachAsync error: {ex.Message}");

                if (await IsExamAlreadyFinalizedAsync(ujianId, mahasiswaId))
                {
                    await RemovePendingEntriesForExamAsync(ujianId, mahasiswaId);
                    return true;
                }

                var entry = new PendingAnswer
                {
                    Type = PendingEntryType.BreachTermination,
                    UjianId = ujianId,
                    MahasiswaId = mahasiswaId,
                    Keterangan = keterangan
                };
                await EnqueuePendingAsync(entry);
                return false;
            }
        }

        /// <summary>
        /// Gets the student's exam result.
        /// </summary>
        public async Task<UjianMahasiswa?> GetExamResultAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                return await _databaseService.GetStudentExamResultAsync(ujianId, mahasiswaId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get exam result error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets current exam status to check if it's forcibly stopped by admin
        /// </summary>
        public async Task<string?> GetExamStatusAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                return await _databaseService.GetStudentExamStatusAsync(ujianId, mahasiswaId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get exam status error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the current exam questions.
        /// </summary>
        public List<ExamQuestion> GetCurrentQuestions() => _currentExamQuestions;

        public async Task SaveDoubtStateAsync(long soalId, bool isDoubtful)
        {
            if (_currentUjianId <= 0 || _currentMahasiswaId <= 0 || soalId <= 0)
            {
                return;
            }

            await _doubtLock.WaitAsync();
            try
            {
                var states = await ReadDoubtStatesAsync(_currentUjianId, _currentMahasiswaId);

                if (isDoubtful)
                {
                    states[soalId] = true;
                }
                else
                {
                    states.Remove(soalId);
                }

                await WriteDoubtStatesAsync(_currentUjianId, _currentMahasiswaId, states);
            }
            finally
            {
                _doubtLock.Release();
            }
        }

        public async Task ClearDoubtStatesAsync(long ujianId, long mahasiswaId)
        {
            if (ujianId <= 0 || mahasiswaId <= 0)
            {
                return;
            }

            await _doubtLock.WaitAsync();
            try
            {
                var path = GetDoubtStateFilePath(ujianId, mahasiswaId);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            finally
            {
                _doubtLock.Release();
            }
        }

        public Task ClearExamOrderStateAsync(long ujianId, long mahasiswaId)
        {
            if (ujianId <= 0 || mahasiswaId <= 0)
            {
                return Task.CompletedTask;
            }

            try
            {
                var path = GetExamOrderStateFilePath(ujianId, mahasiswaId);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear order state error: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public async Task ClearLocalExamStatesAsync(long ujianId, long mahasiswaId)
        {
            await ClearDoubtStatesAsync(ujianId, mahasiswaId);
            await ClearExamOrderStateAsync(ujianId, mahasiswaId);
        }

        private string GetDoubtStateFilePath(long ujianId, long mahasiswaId)
        {
            return Path.Combine(_appDataDirectory, $"DoubtStates_{ujianId}_{mahasiswaId}.json");
        }

        private string GetExamOrderStateFilePath(long ujianId, long mahasiswaId)
        {
            return Path.Combine(_appDataDirectory, $"OrderState_{ujianId}_{mahasiswaId}.dat");
        }

        private async Task<ExamOrderState?> ReadExamOrderStateAsync(long ujianId, long mahasiswaId)
        {
            var path = GetExamOrderStateFilePath(ujianId, mahasiswaId);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var text = await File.ReadAllTextAsync(path);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                if (EncryptionService.IsValidBase64(text))
                {
                    return EncryptionService.Decrypt<ExamOrderState>(text);
                }

                // Legacy/plain format fallback
                return JsonSerializer.Deserialize<ExamOrderState>(text);
            }
            catch
            {
                return null;
            }
        }

        private async Task WriteExamOrderStateAsync(long ujianId, long mahasiswaId, List<ExamQuestion> questions)
        {
            var state = new ExamOrderState
            {
                QuestionOrder = questions.Select(q => q.SoalId).ToList(),
                OptionOrderBySoalId = questions
                    .GroupBy(q => q.SoalId)
                    .ToDictionary(g => g.Key, g => g.First().OptionIds.ToList()),
                SavedAtUtc = DateTime.UtcNow
            };

            var path = GetExamOrderStateFilePath(ujianId, mahasiswaId);
            var tempPath = path + ".tmp";

            try
            {
                var encrypted = EncryptionService.Encrypt(state);
                await File.WriteAllTextAsync(tempPath, encrypted);
                File.Move(tempPath, path, true);
            }
            catch
            {
                var json = JsonSerializer.Serialize(state);
                await File.WriteAllTextAsync(tempPath, json);
                File.Move(tempPath, path, true);
            }
        }

        private static List<ExamQuestion> ApplyExamOrderState(List<ExamQuestion> source, ExamOrderState state)
        {
            if (source.Count == 0)
            {
                return source;
            }

            var ordered = new List<ExamQuestion>();
            var map = source.ToDictionary(q => q.SoalId, q => q);

            foreach (var soalId in state.QuestionOrder)
            {
                if (map.TryGetValue(soalId, out var question))
                {
                    ordered.Add(question);
                    map.Remove(soalId);
                }
            }

            if (map.Count > 0)
            {
                ordered.AddRange(map.Values.OrderBy(q => q.QuestionNumber));
            }

            foreach (var question in ordered)
            {
                if (!state.OptionOrderBySoalId.TryGetValue(question.SoalId, out var savedOptionOrder) || savedOptionOrder.Count == 0)
                {
                    continue;
                }

                ReorderQuestionOptions(question, savedOptionOrder);
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].QuestionNumber = i + 1;
            }

            return ordered;
        }

        private static void ReorderQuestionOptions(ExamQuestion question, List<long> savedOptionOrder)
        {
            var optionSnapshots = question.OptionIds
                .Select((id, index) => new
                {
                    OptionId = id,
                    OptionText = question.Options.ElementAtOrDefault(index) ?? string.Empty,
                    OptionFile = question.OptionFiles.ElementAtOrDefault(index)
                })
                .ToDictionary(x => x.OptionId, x => x);

            var selectedOptionIds = question.IsMultiAnswer
                ? question.SelectedAnswers
                    .Where(i => i >= 0 && i < question.OptionIds.Count)
                    .Select(i => question.OptionIds[i])
                    .Distinct()
                    .ToList()
                : (question.SelectedAnswer.HasValue && question.SelectedAnswer.Value >= 0 && question.SelectedAnswer.Value < question.OptionIds.Count
                    ? new List<long> { question.OptionIds[question.SelectedAnswer.Value] }
                    : new List<long>());

            var reorderedOptionIds = new List<long>();
            foreach (var optionId in savedOptionOrder)
            {
                if (optionSnapshots.ContainsKey(optionId))
                {
                    reorderedOptionIds.Add(optionId);
                }
            }

            foreach (var optionId in question.OptionIds)
            {
                if (!reorderedOptionIds.Contains(optionId))
                {
                    reorderedOptionIds.Add(optionId);
                }
            }

            question.OptionIds = reorderedOptionIds;
            question.Options = reorderedOptionIds.Select(id => optionSnapshots[id].OptionText).ToList();
            question.OptionFiles = reorderedOptionIds.Select(id => optionSnapshots[id].OptionFile).ToList();

            question.SelectedAnswers = selectedOptionIds
                .Select(id => question.OptionIds.IndexOf(id))
                .Where(i => i >= 0)
                .Distinct()
                .ToList();

            if (question.IsMultiAnswer)
            {
                question.SelectedAnswer = null;
            }
            else
            {
                question.SelectedAnswer = selectedOptionIds.Count > 0
                    ? question.OptionIds.IndexOf(selectedOptionIds[0])
                    : null;
            }
        }

        private async Task<Dictionary<long, bool>> ReadDoubtStatesAsync(long ujianId, long mahasiswaId)
        {
            var path = GetDoubtStateFilePath(ujianId, mahasiswaId);
            if (!File.Exists(path))
            {
                return new Dictionary<long, bool>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new Dictionary<long, bool>();
                }

                var states = JsonSerializer.Deserialize<Dictionary<long, bool>>(json);
                return states ?? new Dictionary<long, bool>();
            }
            catch
            {
                return new Dictionary<long, bool>();
            }
        }

        private async Task WriteDoubtStatesAsync(long ujianId, long mahasiswaId, Dictionary<long, bool> states)
        {
            var path = GetDoubtStateFilePath(ujianId, mahasiswaId);
            var tempPath = path + ".tmp";

            var json = JsonSerializer.Serialize(states);
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, path, true);
        }

        // -------------------- Pending Queue Helpers --------------------
        private async Task EnqueuePendingAsync(PendingAnswer entry)
        {
            await _pendingLock.WaitAsync();
            try
            {
                var list = await ReadPendingAsync();

                bool hasEquivalentTerminalEntry = list.Any(existing =>
                    existing.UjianId == entry.UjianId &&
                    existing.MahasiswaId == entry.MahasiswaId &&
                    ((existing.Type == PendingEntryType.SubmitExam && entry.Type == PendingEntryType.SubmitExam) ||
                     (existing.Type == PendingEntryType.ForceStop && entry.Type == PendingEntryType.ForceStop) ||
                     (existing.Type == PendingEntryType.BreachTermination && entry.Type == PendingEntryType.BreachTermination)));

                if (!hasEquivalentTerminalEntry)
                {
                    list.Add(entry);
                }

                // write atomically with encryption
                var tmp = _pendingFilePath + ".tmp";
                try
                {
                    var encrypted = EncryptionService.Encrypt(list);
                    await File.WriteAllTextAsync(tmp, encrypted);
                    File.Move(tmp, _pendingFilePath, true);
                }
                catch (Exception encryptEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Encryption failed in EnqueuePendingAsync: {encryptEx.Message}");
                    // If encryption fails, try to write as fallback (legacy format)
                    var json = System.Text.Json.JsonSerializer.Serialize(list);
                    await File.WriteAllTextAsync(tmp, json);
                    File.Move(tmp, _pendingFilePath, true);
                }

                PendingCountChanged?.Invoke(list.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnqueuePendingAsync error: {ex.Message}");
            }
            finally
            {
                _pendingLock.Release();
            }
        }

        private async Task<List<PendingAnswer>> ReadPendingAsync(long? ujianId = null, long? mahasiswaId = null)
        {
            if (!File.Exists(_pendingFilePath)) return new List<PendingAnswer>();
            try
            {
                var txt = await File.ReadAllTextAsync(_pendingFilePath);
                if (string.IsNullOrWhiteSpace(txt)) return new List<PendingAnswer>();

                List<PendingAnswer> list;

                // Check if file is encrypted (base64) or plain JSON (backward compatibility)
                if (EncryptionService.IsValidBase64(txt))
                {
                    try
                    {
                        // File is encrypted, decrypt it
                        list = EncryptionService.Decrypt<List<PendingAnswer>>(txt) ?? new List<PendingAnswer>();
                    }
                    catch (Exception decryptEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Decryption error (file may be corrupted): {decryptEx.Message}");
                        return new List<PendingAnswer>();
                    }
                }
                else
                {
                    // File is plain JSON (legacy format), try to read it
                    // After reading, we should re-encrypt it on next write for security
                    try
                    {
                        list = System.Text.Json.JsonSerializer.Deserialize<List<PendingAnswer>>(txt) ?? new List<PendingAnswer>();
                        System.Diagnostics.Debug.WriteLine("Legacy unencrypted pending answers file detected. Will be encrypted on next write.");
                    }
                    catch
                    {
                        return new List<PendingAnswer>();
                    }
                }

                if (ujianId.HasValue)
                {
                    list = list.Where(p => p.UjianId == ujianId.Value).ToList();
                }

                if (mahasiswaId.HasValue)
                {
                    list = list.Where(p => p.MahasiswaId == mahasiswaId.Value).ToList();
                }

                return list;
            }
            catch
            {
                return new List<PendingAnswer>();
            }
        }

        private async Task<bool> IsExamAlreadyFinalizedAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                var status = await _databaseService.GetStudentExamStatusAsync(ujianId, mahasiswaId);
                return string.Equals(status, "selesai", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(status, "dihentikan", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private async Task RemovePendingEntriesForExamAsync(long ujianId, long mahasiswaId)
        {
            await _pendingLock.WaitAsync();
            try
            {
                var list = await ReadPendingAsync();
                var remaining = list
                    .Where(x => x.UjianId != ujianId || x.MahasiswaId != mahasiswaId)
                    .ToList();

                if (remaining.Count == 0)
                {
                    try
                    {
                        if (File.Exists(_pendingFilePath))
                        {
                            File.Delete(_pendingFilePath);
                        }
                    }
                    catch (Exception deleteEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Delete pending file error: {deleteEx.Message}");
                    }

                    var tempPathIfExists = _pendingFilePath + ".tmp";
                    if (File.Exists(tempPathIfExists))
                    {
                        try
                        {
                            File.Delete(tempPathIfExists);
                        }
                        catch { }
                    }

                    PendingCountChanged?.Invoke(0);
                    return;
                }

                var tmp = _pendingFilePath + ".tmp";
                try
                {
                    var encrypted = EncryptionService.Encrypt(remaining);
                    await File.WriteAllTextAsync(tmp, encrypted);
                    File.Move(tmp, _pendingFilePath, true);
                }
                catch (Exception encryptEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Encryption failed in RemovePendingEntriesForExamAsync: {encryptEx.Message}");
                    var json = System.Text.Json.JsonSerializer.Serialize(remaining);
                    await File.WriteAllTextAsync(tmp, json);
                    File.Move(tmp, _pendingFilePath, true);
                }

                PendingCountChanged?.Invoke(remaining.Count);
            }
            finally
            {
                _pendingLock.Release();
            }
        }

        private async Task FlushLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await TryFlushOnceAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Flush loop error: {ex.Message}");
                }

                try
                {
                    await Task.Delay(_flushInterval, ct);
                }
                catch (TaskCanceledException) { break; }
            }
        }

        private async Task TryFlushOnceAsync()
        {
            await _pendingLock.WaitAsync();
            try
            {
                var list = await ReadPendingAsync();
                if (list.Count == 0) return;

                var succeeded = new List<Guid>();

                foreach (var entry in list.ToList())
                {
                    bool ok = false;
                    try
                    {
                        switch (entry.Type)
                        {
                            case PendingEntryType.SingleAnswer:
                                if (entry.OpsiJawabanId.HasValue)
                                {
                                    ok = await _databaseService.SaveStudentAnswerAsync(entry.SoalId, entry.MahasiswaId, entry.OpsiJawabanId.Value);
                                }
                                break;
                            case PendingEntryType.MultipleAnswers:
                                if (entry.OpsiJawabanIds != null)
                                {
                                    ok = await _databaseService.SaveStudentAnswersAsync(entry.SoalId, entry.MahasiswaId, entry.OpsiJawabanIds);
                                }
                                break;
                            case PendingEntryType.SubmitExam:
                                ok = await _databaseService.EndExamSessionAsync(entry.UjianId, entry.MahasiswaId);
                                break;
                            case PendingEntryType.ForceStop:
                                ok = await _databaseService.SaveForceStopExamAsync(entry.UjianId, entry.MahasiswaId);
                                break;
                            case PendingEntryType.BreachTermination:
                                ok = await _databaseService.SaveBreachTerminationAsync(
                                    entry.UjianId,
                                    entry.MahasiswaId,
                                    entry.Keterangan ?? "Dihentikan otomatis: pelanggaran keamanan sesi terdeteksi");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Flush entry failed: {ex.Message}");
                        ok = false;
                    }

                    if (ok)
                    {
                        succeeded.Add(entry.Id);
                    }
                    else
                    {
                        // increment attempts and keep entry; remove if attempts grow too large
                        entry.Attempts++;
                        if (entry.Attempts >= 10)
                        {
                            succeeded.Add(entry.Id); // drop it
                        }
                    }
                }

                if (succeeded.Count > 0)
                {
                    var remaining = list.Where(x => !succeeded.Contains(x.Id)).ToList();
                    var tmp = _pendingFilePath + ".tmp";
                    try
                    {
                        var encrypted = EncryptionService.Encrypt(remaining);
                        await File.WriteAllTextAsync(tmp, encrypted);
                        File.Move(tmp, _pendingFilePath, true);
                    }
                    catch (Exception encryptEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Encryption failed in TryFlushOnceAsync: {encryptEx.Message}");
                        // If encryption fails, try to write as fallback (legacy format)
                        var json = System.Text.Json.JsonSerializer.Serialize(remaining);
                        await File.WriteAllTextAsync(tmp, json);
                        File.Move(tmp, _pendingFilePath, true);
                    }

                    // notify listeners
                    PendingCountChanged?.Invoke(remaining.Count);
                }
                else
                {
                    // still notify to keep UI consistent
                    PendingCountChanged?.Invoke(list.Count);
                }
            }
            finally
            {
                _pendingLock.Release();
            }
        }

        // Dispose cancellation token when object is garbage collected
        ~ExamService()
        {
            try
            {
                _flushCts?.Cancel();
            }
            catch { }
        }

        private async Task NotifyMonitoringAsync(long ujianId)
        {
            using var client = new HttpClient();

            client.BaseAddress = new Uri(AppConfig.GetApiBaseUrl());

            var response = await client.PostAsync(
                $"/api/ujian/{ujianId}/monitoring", null);

            response.EnsureSuccessStatusCode();
        }
    }
}