using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers
{
    /// <summary>
    /// Shared helpers for controllers that need to know who is logged in.
    /// </summary>
    public abstract class RaceDayControllerBase : ControllerBase
    {
        protected readonly RaceDayContext Context;

        protected RaceDayControllerBase(RaceDayContext context)
        {
            Context = context;
        }

        /// <summary>The ID of the logged-in user (from the authentication cookie).</summary>
        protected int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>The Organiser profile of the logged-in user, or null.</summary>
        protected Task<Organiser?> GetCurrentOrganiserAsync()
        {
            var userId = CurrentUserId;
            return Context.Organisers.FirstOrDefaultAsync(o => o.UserID == userId);
        }

        /// <summary>The Participant profile of the logged-in user, or null.</summary>
        protected Task<Participant?> GetCurrentParticipantAsync()
        {
            var userId = CurrentUserId;
            return Context.Participants.FirstOrDefaultAsync(p => p.UserID == userId);
        }
    }
}
