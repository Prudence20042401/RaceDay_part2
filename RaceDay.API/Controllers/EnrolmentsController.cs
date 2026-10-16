using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EnrolmentsController : RaceDayControllerBase
    {
        public EnrolmentsController(RaceDayContext context) : base(context)
        {
        }

        private static IQueryable<EnrolmentResponseDTO> Project(IQueryable<Enrolment> query) =>
            query.Select(e => new EnrolmentResponseDTO
            {
                EnrolmentID = e.EnrolmentID,
                EventID = e.EventID,
                EventName = e.Event!.Name,
                EventDate = e.Event.EventDate,
                CategoryID = e.CategoryID,
                CategoryName = e.Category!.Name,
                ParticipantID = e.ParticipantID,
                ParticipantName = e.Participant!.User!.FirstName + " " + e.Participant.User.LastName,
                Status = e.Status,
                EnrolmentDate = e.EnrolmentDate
            });

        /// <summary>
        /// Enters the logged-in Participant into an event in the chosen category.
        /// </summary>
        /// <remarks>
        /// The enrolment always starts as "Pending"; only an Organiser can confirm it.
        /// A Participant can enter each event only once, and cannot enter an event
        /// that has already taken place.
        /// </remarks>
        /// <response code="200">Enrolment recorded.</response>
        /// <response code="400">Event or category is invalid, or the event has already taken place.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        /// <response code="409">Already enrolled in this event.</response>
        [Authorize(Roles = "Participant")]
        [HttpPost]
        [ProducesResponseType(typeof(EnrolmentResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enrol(EnrolDTO dto)
        {
            var participant = await GetCurrentParticipantAsync();

            if (participant == null)
                return BadRequest(new MessageDTO { Message = "Participant profile not found." });

            var category = await Context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == dto.CategoryID);

            if (category == null || category.EventID != dto.EventID)
                return BadRequest(new MessageDTO { Message = "Invalid event or category." });

            if (category.Event!.EventDate.Date < DateTime.UtcNow.Date)
                return BadRequest(new MessageDTO { Message = "This event has already taken place." });

            bool alreadyEnrolled = await Context.Enrolments.AnyAsync(e =>
                e.ParticipantID == participant.ParticipantID &&
                e.EventID == dto.EventID);

            if (alreadyEnrolled)
                return Conflict(new MessageDTO { Message = "Already enrolled in this event." });

            var enrolment = new Enrolment
            {
                ParticipantID = participant.ParticipantID,
                EventID = dto.EventID,
                CategoryID = dto.CategoryID,
                Status = "Pending"
            };

            Context.Enrolments.Add(enrolment);
            await Context.SaveChangesAsync();

            return Ok(await Project(
                    Context.Enrolments.Where(e => e.EnrolmentID == enrolment.EnrolmentID))
                .FirstAsync());
        }

        /// <summary>
        /// Lists the logged-in Participant's own enrolments with event and category details.
        /// </summary>
        /// <response code="200">The Participant's enrolments.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        /// <response code="404">Participant profile not found.</response>
        [Authorize(Roles = "Participant")]
        [HttpGet("my")]
        [ProducesResponseType(typeof(List<EnrolmentResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MyEnrolments()
        {
            var participant = await GetCurrentParticipantAsync();

            if (participant == null)
                return NotFound();

            var enrolments = await Project(
                    Context.Enrolments
                        .Where(e => e.ParticipantID == participant.ParticipantID)
                        .OrderBy(e => e.Event!.EventDate))
                .ToListAsync();

            return Ok(enrolments);
        }

        /// <summary>
        /// Lists every enrolment for one of the logged-in Organiser's own events.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <response code="200">The enrolments for that event.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">Organiser profile not found.</response>
        [Authorize(Roles = "Organiser")]
        [HttpGet("event/{eventId:int}")]
        [ProducesResponseType(typeof(List<EnrolmentResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EventEnrolments(int eventId)
        {
            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null)
                return NotFound();

            bool ownsEvent = await Context.Events.AnyAsync(e =>
                e.EventID == eventId && e.OrganiserID == organiser.OrganiserID);

            if (!ownsEvent)
                return Forbid();

            var enrolments = await Project(
                    Context.Enrolments
                        .Where(e => e.EventID == eventId)
                        .OrderBy(e => e.EnrolmentDate))
                .ToListAsync();

            return Ok(enrolments);
        }

        /// <summary>
        /// Changes the status of an enrolment (Pending, Confirmed or Cancelled).
        /// </summary>
        /// <param name="id">The enrolment ID.</param>
        /// <response code="200">Status updated.</response>
        /// <response code="400">Invalid status.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or the enrolment is for another Organiser's event.</response>
        /// <response code="404">No enrolment with that ID.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}/status")]
        [ProducesResponseType(typeof(EnrolmentResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(int id, UpdateEnrolmentStatusDTO dto)
        {
            var enrolment = await Context.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (enrolment == null)
                return NotFound();

            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null || enrolment.Event!.OrganiserID != organiser.OrganiserID)
                return Forbid();

            enrolment.Status = dto.Status;
            enrolment.UpdatedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            return Ok(await Project(Context.Enrolments.Where(e => e.EnrolmentID == id))
                .FirstAsync());
        }
    }
}
