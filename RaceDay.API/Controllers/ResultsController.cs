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
    public class ResultsController : RaceDayControllerBase
    {
        public ResultsController(RaceDayContext context) : base(context)
        {
        }

        private static IQueryable<ResultResponseDTO> Project(IQueryable<Result> query) =>
            query.Select(r => new ResultResponseDTO
            {
                ResultID = r.ResultID,
                EnrolmentID = r.EnrolmentID,
                EventID = r.Enrolment!.EventID,
                EventName = r.Enrolment.Event!.Name,
                EventDate = r.Enrolment.Event.EventDate,
                CategoryName = r.Enrolment.Category!.Name,
                ParticipantName = r.Enrolment.Participant!.User!.FirstName + " " +
                                  r.Enrolment.Participant.User.LastName,
                FinishTime = r.FinishTime,
                FinishingPosition = r.FinishingPosition,
                TotalFinishers = r.Enrolment.Event.Enrolments.Count(en => en.Result != null),
                RecordedAt = r.RecordedAt
            });

        /// <summary>
        /// Records the finish time and finishing position for an enrolment.
        /// </summary>
        /// <remarks>Only the Organiser who owns the event can record results. FinishTime uses the format hh:mm:ss.</remarks>
        /// <response code="200">Result recorded.</response>
        /// <response code="400">Invalid details, or the enrolment was cancelled.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or the enrolment is for another Organiser's event.</response>
        /// <response code="404">Enrolment not found.</response>
        /// <response code="409">A result already exists for this enrolment.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        [ProducesResponseType(typeof(ResultResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RecordResult(ResultDTO dto)
        {
            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null)
                return Forbid();

            var enrolment = await Context.Enrolments
                .Include(e => e.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == dto.EnrolmentID);

            if (enrolment == null)
                return NotFound(new MessageDTO { Message = "Enrolment not found." });

            if (enrolment.Event!.OrganiserID != organiser.OrganiserID)
                return Forbid();

            if (enrolment.Status == "Cancelled")
                return BadRequest(new MessageDTO { Message = "A result cannot be recorded for a cancelled enrolment." });

            if (await Context.Results.AnyAsync(r => r.EnrolmentID == dto.EnrolmentID))
                return Conflict(new MessageDTO { Message = "A result already exists." });

            var result = new Result
            {
                EnrolmentID = dto.EnrolmentID,
                FinishTime = dto.FinishTime,
                FinishingPosition = dto.FinishingPosition
            };

            Context.Results.Add(result);
            await Context.SaveChangesAsync();

            return Ok(await Project(Context.Results.Where(r => r.ResultID == result.ResultID))
                .FirstAsync());
        }

        /// <summary>
        /// Lists the logged-in Participant's own results, including event, category,
        /// finish time, position and total number of finishers.
        /// </summary>
        /// <response code="200">The Participant's results.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Logged in as an Organiser.</response>
        /// <response code="404">Participant profile not found.</response>
        [Authorize(Roles = "Participant")]
        [HttpGet("my")]
        [ProducesResponseType(typeof(List<ResultResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyResults()
        {
            var participant = await GetCurrentParticipantAsync();

            if (participant == null)
                return NotFound();

            var results = await Project(
                    Context.Results
                        .Where(r => r.Enrolment!.ParticipantID == participant.ParticipantID)
                        .OrderByDescending(r => r.Enrolment!.Event!.EventDate))
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// Lists all results for one of the logged-in Organiser's own events, best position first.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <response code="200">The results for that event.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">Event not found.</response>
        [Authorize(Roles = "Organiser")]
        [HttpGet("event/{eventId:int}")]
        [ProducesResponseType(typeof(List<ResultResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByEvent(int eventId)
        {
            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null)
                return Forbid();

            var ev = await Context.Events.FirstOrDefaultAsync(e => e.EventID == eventId);

            if (ev == null)
                return NotFound();

            if (ev.OrganiserID != organiser.OrganiserID)
                return Forbid();

            var results = await Project(
                    Context.Results
                        .Where(r => r.Enrolment!.EventID == eventId)
                        .OrderBy(r => r.FinishingPosition))
                .ToListAsync();

            return Ok(results);
        }

        /// <summary>
        /// Corrects the finish time and position of an existing result.
        /// </summary>
        /// <param name="id">The result ID.</param>
        /// <response code="200">Result updated.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or the result is for another Organiser's event.</response>
        /// <response code="404">No result with that ID.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ResultResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateResult(int id, UpdateResultDTO dto)
        {
            var result = await Context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound();

            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null || result.Enrolment!.Event!.OrganiserID != organiser.OrganiserID)
                return Forbid();

            result.FinishTime = dto.FinishTime;
            result.FinishingPosition = dto.FinishingPosition;
            result.UpdatedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            return Ok(await Project(Context.Results.Where(r => r.ResultID == id))
                .FirstAsync());
        }

        /// <summary>
        /// Deletes a result.
        /// </summary>
        /// <param name="id">The result ID.</param>
        /// <response code="200">Result deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or the result is for another Organiser's event.</response>
        /// <response code="404">No result with that ID.</response>
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteResult(int id)
        {
            var result = await Context.Results
                .Include(r => r.Enrolment)
                    .ThenInclude(e => e!.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);

            if (result == null)
                return NotFound();

            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null || result.Enrolment!.Event!.OrganiserID != organiser.OrganiserID)
                return Forbid();

            Context.Results.Remove(result);
            await Context.SaveChangesAsync();

            return Ok(new MessageDTO { Message = "Result deleted." });
        }
    }
}
