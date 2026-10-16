
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    [Table("Organisers")]
    public class Organiser
    {
        [Key]
        public int OrganiserID { get; set; }

        public int UserID { get; set; }

        [Required]
        [MaxLength(200)]
        public string OrganisationName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public User? User { get; set; }

        public ICollection<Event> Events { get; set; }
            = new List<Event>();
    }
}
