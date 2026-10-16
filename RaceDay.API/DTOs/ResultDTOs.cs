using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    /// <summary>What an Organiser sends to record a result.</summary>
    public class ResultDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "EnrolmentID is required.")]
        public int EnrolmentID { get; set; }

        /// <summary>Finish time in the format hh:mm:ss, for example 01:23:45.</summary>
        [Required]
        public TimeSpan? FinishTime { get; set; }

        /// <summary>Finishing position, 1 for first place.</summary>
        [Required]
        [Range(1, 1000000, ErrorMessage = "FinishingPosition must be 1 or more.")]
        public int? FinishingPosition { get; set; }
    }

    /// <summary>What an Organiser sends to correct an existing result.</summary>
    public class UpdateResultDTO
    {
        /// <summary>Finish time in the format hh:mm:ss.</summary>
        [Required]
        public TimeSpan? FinishTime { get; set; }

        [Required]
        [Range(1, 1000000, ErrorMessage = "FinishingPosition must be 1 or more.")]
        public int? FinishingPosition { get; set; }
    }

    /// <summary>A result as returned by the API.</summary>
    public class ResultResponseDTO
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string ParticipantName { get; set; } = string.Empty;
        public TimeSpan? FinishTime { get; set; }
        public int? FinishingPosition { get; set; }

        /// <summary>How many finishers have a recorded result for the event (the "out of" number).</summary>
        public int TotalFinishers { get; set; }

        public DateTime RecordedAt { get; set; }
    }
}
