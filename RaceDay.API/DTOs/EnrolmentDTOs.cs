using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    /// <summary>What a Participant sends to enter an event.</summary>
    public class EnrolDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "EventID is required.")]
        public int EventID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CategoryID is required.")]
        public int CategoryID { get; set; }
    }

    /// <summary>What an Organiser sends to change an enrolment's status.</summary>
    public class UpdateEnrolmentStatusDTO
    {
        /// <summary>Pending, Confirmed or Cancelled.</summary>
        [Required]
        [RegularExpression("^(Pending|Confirmed|Cancelled)$",
            ErrorMessage = "Status must be Pending, Confirmed or Cancelled.")]
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>An enrolment as returned by the API.</summary>
    public class EnrolmentResponseDTO
    {
        public int EnrolmentID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ParticipantID { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
    }
}
