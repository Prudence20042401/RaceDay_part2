using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    /// <summary>The logged-in user's profile.</summary>
    public class ProfileResponseDTO
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        /// <summary>Organisers only.</summary>
        public string? OrganisationName { get; set; }

        /// <summary>Organisers only.</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>Participants only.</summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>Participants only.</summary>
        public string? ProfilePictureURL { get; set; }
    }

    /// <summary>Profile fields the user is allowed to change.</summary>
    public class UpdateProfileDTO
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>Organisers only. Ignored for Participants.</summary>
        [MaxLength(200)]
        public string? OrganisationName { get; set; }

        /// <summary>Organisers only. Ignored for Participants.</summary>
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        /// <summary>Participants only. Must be in the past. Ignored for Organisers.</summary>
        public DateTime? DateOfBirth { get; set; }
    }
}
