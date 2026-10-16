using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class AuthController : ControllerBase
    {
        private const string SessionUserIdKey = "UserID";
        private const string SessionRoleKey = "Role";

        private readonly RaceDayContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<User> _hasher = new();

        public AuthController(RaceDayContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Registers a new account as an Organiser or a Participant.
        /// Participants must supply a date of birth in the past.
        /// Organisers must supply an organisation name, a phone number and the
        /// organiser invite code (so that not just anyone can become an Organiser).
        /// The password is hashed before it is stored.
        /// <response code="200">Account created.</response>
        /// <response code="400">Missing or invalid details.</response>
        /// <response code="403">Organiser invite code missing or wrong.</response>
        /// <response code="409">Email address already registered.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserSummaryDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterDTO dto)
        {
            var email = dto.Email.Trim().ToLower();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Conflict(new MessageDTO { Message = "Email is already registered." });

            var role = dto.Role.Trim();

            if (role != "Organiser" && role != "Participant")
                return BadRequest(new MessageDTO { Message = "Invalid role." });

            if (role == "Organiser")
            {
                if (!InviteCodeIsValid(dto.InviteCode))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new MessageDTO
                    {
                        Message = "A valid organiser invite code is required to register as an Organiser."
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.OrganisationName) ||
                    string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    return BadRequest(new MessageDTO
                    {
                        Message = "Organisers must provide an organisation name and phone number."
                    });
                }
            }

            if (role == "Participant" &&
                (!dto.DateOfBirth.HasValue ||
                 dto.DateOfBirth.Value.Date >= DateTime.UtcNow.Date))
            {
                return BadRequest(new MessageDTO
                {
                    Message = "Participants must provide a valid date of birth in the past."
                });
            }

            var user = new User
            {
                Email = email,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Role = role
            };

            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (role == "Organiser")
            {
                _context.Organisers.Add(new Organiser
                {
                    UserID = user.UserID,
                    OrganisationName = dto.OrganisationName!.Trim(),
                    PhoneNumber = dto.PhoneNumber!.Trim()
                });
            }
            else
            {
                _context.Participants.Add(new Participant
                {
                    UserID = user.UserID,
                    DateOfBirth = dto.DateOfBirth!.Value.Date
                });
            }

            await _context.SaveChangesAsync();

            return Ok(ToSummary(user, "Registration successful."));
        }

        /// <summary>
        /// Logs in and starts a server-side session holding the user's ID and role.
        ///
        /// </summary>
        /// <response code="200">Logged in. The session cookie is set.</response>
        /// <response code="401">Email or password is wrong.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(UserSummaryDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            var email = dto.Email.Trim().ToLower();

            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return Unauthorized(new MessageDTO { Message = "Invalid email or password." });

            var result = _hasher.VerifyHashedPassword(
                user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new MessageDTO { Message = "Invalid email or password." });

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Name, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            // Server-side session: user ID and role are kept on the server.
            HttpContext.Session.SetInt32(SessionUserIdKey, user.UserID);
            HttpContext.Session.SetString(SessionRoleKey, user.Role);

            return Ok(ToSummary(user, "Login successful."));
        }

        /// <summary>
        /// Returns the user ID and role stored in the current server-side session.
        /// </summary>
        /// <response code="200">The session details.</response>
        /// <response code="401">Not logged in, or the session has expired.</response>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(SessionInfoDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Me()
        {
            var userId = HttpContext.Session.GetInt32(SessionUserIdKey);
            var role = HttpContext.Session.GetString(SessionRoleKey);

            if (userId == null || string.IsNullOrEmpty(role))
                return Unauthorized(new MessageDTO { Message = "Session expired. Please log in again." });

            return Ok(new SessionInfoDTO { UserID = userId.Value, Role = role });
        }

        /// <summary>
        /// Logs out and clears the server-side session.
        /// </summary>
        /// <response code="200">Logged out.</response>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(MessageDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new MessageDTO { Message = "Logged out successfully." });
        }

        private bool InviteCodeIsValid(string? supplied)
        {
            var expected = _configuration["OrganiserInviteCode"];

            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(supplied))
                return false;

            // Fixed-time comparison so the code cannot be guessed by timing.
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(supplied.Trim()),
                Encoding.UTF8.GetBytes(expected.Trim()));
        }

        private static UserSummaryDTO ToSummary(User user, string message) => new()
        {
            Message = message,
            UserID = user.UserID,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role
        };
    }
}
