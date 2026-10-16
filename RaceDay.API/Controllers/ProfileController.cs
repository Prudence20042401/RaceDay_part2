using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ProfileController : RaceDayControllerBase
    {
        public ProfileController(RaceDayContext context) : base(context)
        {
        }

        /// <summary>
        /// Returns the profile of the logged-in user (Organiser or Participant).
        /// </summary>
        /// <response code="200">The profile.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="404">User no longer exists.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ProfileResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = CurrentUserId;

            var user = await Context.Users
                .Include(u => u.Organiser)
                .Include(u => u.Participant)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return NotFound();

            return Ok(new ProfileResponseDTO
            {
                UserID = user.UserID,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                OrganisationName = user.Organiser?.OrganisationName,
                PhoneNumber = user.Organiser?.PhoneNumber,
                DateOfBirth = user.Participant?.DateOfBirth,
                ProfilePictureURL = user.Participant?.ProfilePictureURL
            });
        }

        /// <summary>
        /// Updates the logged-in user's own profile.
        /// </summary>
        /// <remarks>
        /// Everyone can change their first and last name. Organisers can also change
        /// organisation name and phone number. Participants can also change their date of birth.
        /// </remarks>
        /// <response code="200">Profile updated.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="404">User no longer exists.</response>
        [HttpPut]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
        {
            var userId = CurrentUserId;

            var user = await Context.Users
                .Include(u => u.Organiser)
                .Include(u => u.Participant)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(dto.FirstName) ||
                string.IsNullOrWhiteSpace(dto.LastName))
            {
                return BadRequest(new MessageDTO
                {
                    Message = "First name and last name are required."
                });
            }

            var now = DateTime.UtcNow;

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.UpdatedAt = now;

            if (user.Organiser != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.OrganisationName))
                    user.Organiser.OrganisationName = dto.OrganisationName.Trim();

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                    user.Organiser.PhoneNumber = dto.PhoneNumber.Trim();

                user.Organiser.UpdatedAt = now;
            }

            if (user.Participant != null && dto.DateOfBirth.HasValue)
            {
                if (dto.DateOfBirth.Value.Date >= now.Date)
                {
                    return BadRequest(new MessageDTO
                    {
                        Message = "Date of birth must be in the past."
                    });
                }

                user.Participant.DateOfBirth = dto.DateOfBirth.Value.Date;
                user.Participant.UpdatedAt = now;
            }

            await Context.SaveChangesAsync();

            return Ok(new MessageDTO { Message = "Profile updated successfully." });
        }
    }
}
