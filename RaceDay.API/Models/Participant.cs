
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    [Table("Participants")]
    public class Participant
    {
        [Key]
        public int ParticipantID { get; set; }

        public int UserID { get; set; }

        public DateTime DateOfBirth { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureURL { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public User? User { get; set; }

        public ICollection<Enrolment> Enrolments { get; set; }
            = new List<Enrolment>();
    }
}
