
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    [Table("Enrolments")]
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        public int ParticipantID { get; set; }
        public int EventID { get; set; }
        public int CategoryID { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime? UpdatedAt { get; set; }

        public Participant? Participant { get; set; }
        public Event? Event { get; set; }
        public Category? Category { get; set; }

        public Result? Result { get; set; }
    }
}
