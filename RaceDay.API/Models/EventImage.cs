
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.API.Models
{
    [Table("EventImages")]
    public class EventImage
    {
        [Key]
        public int ImageID { get; set; }

        public int EventID { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImageURL { get; set; } = string.Empty;

        public bool IsMain { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public Event? Event { get; set; }
    }
}
