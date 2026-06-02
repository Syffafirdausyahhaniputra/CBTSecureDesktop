using System;
using System.Collections.Generic;

namespace CBTSecureDesktop.Services
{
    public enum PendingEntryType
    {
        SingleAnswer,
        MultipleAnswers,
        SubmitExam,
        ForceStop
    }

    public class PendingAnswer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public PendingEntryType Type { get; set; }
        public long UjianId { get; set; }
        public long MahasiswaId { get; set; }

        // For answers
        public long SoalId { get; set; }
        public long? OpsiJawabanId { get; set; }
        public List<long>? OpsiJawabanIds { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int Attempts { get; set; } = 0;
    }
}