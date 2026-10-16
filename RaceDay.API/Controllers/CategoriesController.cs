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
    public class CategoriesController : RaceDayControllerBase
    {
        public CategoriesController(RaceDayContext context) : base(context)
        {
        }

        private static IQueryable<CategoryResponseDTO> Project(IQueryable<Category> query) =>
            query.Select(c => new CategoryResponseDTO
            {
                CategoryID = c.CategoryID,
                EventID = c.EventID,
                Name = c.Name,
                Description = c.Description
            });

        /// <summary>
        /// Lists all categories for all events. Open to everyone.
        /// </summary>
        /// <response code="200">The list of categories.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<CategoryResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories()
        {
            return Ok(await Project(Context.Categories.OrderBy(c => c.EventID).ThenBy(c => c.Name))
                .ToListAsync());
        }


        /// <summary>
        /// Lists the categories of one event. Open to everyone.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <response code="200">The categories for that event (empty if none).</response>
        /// <response code="404">No event with that ID.</response>
        [HttpGet("event/{eventId:int}")]
        [ProducesResponseType(typeof(List<CategoryResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByEvent(int eventId)
        {
            if (!await Context.Events.AnyAsync(e => e.EventID == eventId))
                return NotFound();

            return Ok(await Project(
                    Context.Categories.Where(c => c.EventID == eventId).OrderBy(c => c.Name))
                .ToListAsync());
        }

        /// <summary>
        /// Adds a category (for example "10km" or "Under 20") to one of the Organiser's events.
        /// </summary>
        /// <response code="200">Category created.</response>
        /// <response code="400">Invalid details.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">Event does not exist.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        [ProducesResponseType(typeof(CategoryResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateCategory(CategoryDTO dto)
        {
            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null)
                return Forbid();

            var ev = await Context.Events.FirstOrDefaultAsync(e => e.EventID == dto.EventID);

            if (ev == null)
                return NotFound(new MessageDTO { Message = "Event not found." });

            if (ev.OrganiserID != organiser.OrganiserID)
                return Forbid();

            var category = new Category
            {
                EventID = dto.EventID,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim()
            };

            Context.Categories.Add(category);
            await Context.SaveChangesAsync();

            return Ok(await Project(Context.Categories.Where(c => c.CategoryID == category.CategoryID))
                .FirstAsync());
        }

        /// <summary>
        /// Updates a category on one of the Organiser's own events.
        /// </summary>
        /// <param name="id">The category ID.</param>
        /// <response code="200">Category updated.</response>
        /// <response code="400">Invalid details, or an attempt to move the category to another event.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No category with that ID.</response>
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(CategoryResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(int id, CategoryDTO dto)
        {
            var existing = await Context.Categories.FindAsync(id);

            if (existing == null)
                return NotFound();

            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null)
                return Forbid();

            var ownsEvent = await Context.Events.AnyAsync(e =>
                e.EventID == existing.EventID && e.OrganiserID == organiser.OrganiserID);

            if (!ownsEvent)
                return Forbid();

            if (dto.EventID != existing.EventID)
            {
                return BadRequest(new MessageDTO
                {
                    Message = "A category cannot be moved to a different event."
                });
            }

            existing.Name = dto.Name.Trim();
            existing.Description = dto.Description?.Trim();
            existing.UpdatedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            return Ok(await Project(Context.Categories.Where(c => c.CategoryID == id))
                .FirstAsync());
        }

        /// <summary>
        /// Deletes a category from one of the Organiser's own events.
        /// </summary>
        /// <param name="id">The category ID.</param>
        /// <response code="200">Category deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Participant, or the event belongs to another Organiser.</response>
        /// <response code="404">No category with that ID.</response>
        /// <response code="409">Participants are enrolled in this category, so it cannot be deleted.</response>
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await Context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            var organiser = await GetCurrentOrganiserAsync();

            if (organiser == null)
                return Forbid();

            var ownsEvent = await Context.Events.AnyAsync(e =>
                e.EventID == category.EventID && e.OrganiserID == organiser.OrganiserID);

            if (!ownsEvent)
                return Forbid();

            if (await Context.Enrolments.AnyAsync(e => e.CategoryID == id))
            {
                return Conflict(new MessageDTO
                {
                    Message = "This category cannot be deleted because participants are enrolled in it."
                });
            }

            Context.Categories.Remove(category);
            await Context.SaveChangesAsync();

            return Ok(new MessageDTO { Message = "Category deleted." });
        }
    }
}
