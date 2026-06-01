using CBTSecureDesktop.Data;
using CBTSecureDesktop.Models;

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
        public int? SelectedAnswer { get; set; }
        public List<int> SelectedAnswers { get; set; } = new();
        public bool IsMultiAnswer { get; set; } = false;
        public List<string> Images { get; set; } = new();
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

        public ExamService()
        {
            _databaseService = new DatabaseService();
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
        /// Gets all image IDs for a specific exam to be pre-downloaded.
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
                        if (!string.IsNullOrEmpty(img.File)) // Not really used inside ImageService since ImageService uses ImageId
                        {
                            // we just need the IDs for GetImageAsync
                        }
                        string imgId = img.GambarSoalId.ToString();
                        // Alternatively, if the ImageService takes a URL piece, but it takes imageId.
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
        /// Loads the questions for a specific exam from database.
        /// </summary>
        public async Task<List<ExamQuestion>> LoadExamQuestionsAsync(long ujianId, long mahasiswaId)
        {
            try
            {
                _currentUjianId = ujianId;
                _currentMahasiswaId = mahasiswaId;

                // Start exam session
                _currentExamSessionId = await _databaseService.StartExamSessionAsync(ujianId, mahasiswaId);

                // Get questions from database
                var soalList = await _databaseService.GetExamQuestionsAsync(ujianId);

                // Prepare blank answers rows for this student to ensure they are recorded in database in the correct display sequence
                await _databaseService.InitializeStudentAnswersAsync(ujianId, mahasiswaId, soalList.Select(s => s.SoalId).ToList());

                // Convert to ExamQuestion view model
                _currentExamQuestions = new List<ExamQuestion>();
                int questionNumber = 1;

                foreach (var soal in soalList)
                {
                    bool isMulti = soal.OpsiJawaban.Count(o => o.Nilai == 1) > 1;
                    var examQuestion = new ExamQuestion
                    {
                        SoalId = soal.SoalId,
                        QuestionNumber = questionNumber++,
                        QuestionText = soal.Pertanyaan,
                        Options = soal.OpsiJawaban.Select(o => o.Jawaban).ToList(),
                        OptionIds = soal.OpsiJawaban.Select(o => o.OpsiJawabanId).ToList(),
                        IsMultiAnswer = isMulti,
                        Images = soal.GambarSoal.Select(g => g.GambarSoalId.ToString()).ToList()
                    };
                    _currentExamQuestions.Add(examQuestion);
                }

                // Load existing answers if any
                var existingAnswers = await _databaseService.GetStudentAnswersAsync(ujianId, mahasiswaId);
                foreach (var answer in existingAnswers)
                {
                    if (answer.OpsiJawabanId.HasValue)
                    {
                        var question = _currentExamQuestions.FirstOrDefault(q => q.SoalId == answer.SoalId);
                        if (question != null)
                        {
                            int optionIndex = question.OptionIds.IndexOf(answer.OpsiJawabanId.Value);
                            if (optionIndex >= 0)
                            {
                                if (question.IsMultiAnswer)
                                {
                                    if (!question.SelectedAnswers.Contains(optionIndex))
                                        question.SelectedAnswers.Add(optionIndex);
                                }
                                else
                                {
                                    question.SelectedAnswer = optionIndex;
                                }
                            }
                        }
                    }
                }

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
        /// Saves the student's answer for a specific question to database.
        /// </summary>
        public async Task<bool> SaveAnswerAsync(int questionNumber, int answerIndex)
        {
            try
            {
                var question = _currentExamQuestions.FirstOrDefault(q => q.QuestionNumber == questionNumber);
                if (question != null && !question.IsMultiAnswer && answerIndex >= 0 && answerIndex < question.OptionIds.Count)
                {
                    question.SelectedAnswer = answerIndex;
                    long opsiJawabanId = question.OptionIds[answerIndex];

                    // Save to database
                    return await _databaseService.SaveStudentAnswerAsync(
                        question.SoalId,
                        _currentMahasiswaId,
                        opsiJawabanId
                    );
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save answer error: {ex.Message}");
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
                var question = _currentExamQuestions.FirstOrDefault(q => q.QuestionNumber == questionNumber);
                if (question != null && question.IsMultiAnswer)
                {
                    question.SelectedAnswers = answerIndices.ToList();
                    var opsiJawabanIds = answerIndices.Where(i => i >= 0 && i < question.OptionIds.Count)
                                                      .Select(i => question.OptionIds[i]).ToList();

                    // Save to database
                    return await _databaseService.SaveStudentAnswersAsync(
                        question.SoalId,
                        _currentMahasiswaId,
                        opsiJawabanIds
                    );
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save answers error: {ex.Message}");
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
                // End exam session and calculate score
                return await _databaseService.EndExamSessionAsync(ujianId, mahasiswaId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Submit exam error: {ex.Message}");
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
                return await _databaseService.SaveForceStopExamAsync(ujianId, mahasiswaId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Submit force stopped exam error: {ex.Message}");
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
    }
}

