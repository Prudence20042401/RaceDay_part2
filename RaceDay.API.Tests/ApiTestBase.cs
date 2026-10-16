using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RaceDay.API.Tests
{
    /// <summary>
    /// Shared helpers used by all the test classes: register, log in, create events, and so on.
    /// Every test gets its own HttpClient (and therefore its own cookies / session).
    /// </summary>
    public abstract class ApiTestBase : IClassFixture<CustomWebApplicationFactory>
    {
        protected const string Password = "TestPassword123!";

        protected readonly HttpClient Client;
        protected readonly string ValidInviteCode;

        protected ApiTestBase(CustomWebApplicationFactory factory)
        {
            ValidInviteCode = factory.InviteCode;

            Client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true,
                AllowAutoRedirect = false
            });
        }

        // ---------- small response shapes used when reading JSON ----------

        public class IdResponse
        {
            public int EventID { get; set; }
            public int CategoryID { get; set; }
            public int EnrolmentID { get; set; }
            public int ResultID { get; set; }
        }

        public class EventInfo
        {
            public int EventID { get; set; }
            public string Name { get; set; } = string.Empty;
            public int EnrolmentCount { get; set; }
        }

        public class EnrolmentInfo
        {
            public int EnrolmentID { get; set; }
            public int EventID { get; set; }
            public int CategoryID { get; set; }
            public string EventName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        public class ResultInfo
        {
            public int ResultID { get; set; }
            public string EventName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public int? FinishingPosition { get; set; }
            public int TotalFinishers { get; set; }
        }

        // ---------- helpers ----------

        protected static string UniqueEmail(string prefix) =>
            $"{prefix}-{Guid.NewGuid():N}@example.com";

        protected Task<HttpResponseMessage> RegisterParticipant(string email) =>
            Client.PostAsJsonAsync("/api/Auth/register", new
            {
                email,
                password = Password,
                firstName = "Test",
                lastName = "Participant",
                role = "Participant",
                dateOfBirth = "2000-01-01"
            });

        protected Task<HttpResponseMessage> RegisterOrganiserWithCode(string email, string? inviteCode) =>
            Client.PostAsJsonAsync("/api/Auth/register", new
            {
                email,
                password = Password,
                firstName = "Test",
                lastName = "Organiser",
                role = "Organiser",
                organisationName = "RaceDay Test Club",
                phoneNumber = "+27123456789",
                inviteCode
            });

        protected Task<HttpResponseMessage> RegisterOrganiser(string email) =>
            RegisterOrganiserWithCode(email, ValidInviteCode);

        protected async Task Login(string email)
        {
            var response = await Client.PostAsJsonAsync("/api/Auth/login", new
            {
                email,
                password = Password
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        protected async Task Logout()
        {
            var response = await Client.PostAsync("/api/Auth/logout", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>Registers a brand-new Organiser, logs in, and returns the email.</summary>
        protected async Task<string> LoginAsNewOrganiser()
        {
            var email = UniqueEmail("organiser");
            Assert.Equal(HttpStatusCode.OK, (await RegisterOrganiser(email)).StatusCode);
            await Login(email);
            return email;
        }

        /// <summary>Registers a brand-new Participant, logs in, and returns the email.</summary>
        protected async Task<string> LoginAsNewParticipant()
        {
            var email = UniqueEmail("participant");
            Assert.Equal(HttpStatusCode.OK, (await RegisterParticipant(email)).StatusCode);
            await Login(email);
            return email;
        }

        protected static object NewEvent(string name = "Test Fun Run") => new
        {
            name,
            description = "A test running event",
            eventDate = "2030-06-01",
            location = "Johannesburg, South Africa",
            distance = 10.0,
            eventType = "Run"
        };

        /// <summary>Creates an event as the currently logged-in Organiser and returns its ID.</summary>
        protected async Task<int> CreateEvent(string name = "Test Fun Run")
        {
            var response = await Client.PostAsJsonAsync("/api/Events", NewEvent(name));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<IdResponse>();
            Assert.NotNull(body);
            return body!.EventID;
        }

        /// <summary>Creates a category as the currently logged-in Organiser and returns its ID.</summary>
        protected async Task<int> CreateCategory(int eventId, string name = "10km")
        {
            var response = await Client.PostAsJsonAsync("/api/Categories", new
            {
                eventID = eventId,
                name,
                description = name + " category"
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<IdResponse>();
            Assert.NotNull(body);
            return body!.CategoryID;
        }

        protected Task<HttpResponseMessage> Enrol(int eventId, int categoryId, string? status = null) =>
            Client.PostAsJsonAsync("/api/Enrolments", new
            {
                eventID = eventId,
                categoryID = categoryId,
                status
            });

        /// <summary>
        /// Logs in as a new Organiser who creates an event with one category.
        /// Returns the organiser email, event ID and category ID. Leaves the Organiser logged in.
        /// </summary>
        protected async Task<(string OrganiserEmail, int EventId, int CategoryId)> OrganiserWithEventAndCategory()
        {
            var email = await LoginAsNewOrganiser();
            var eventId = await CreateEvent();
            var categoryId = await CreateCategory(eventId);
            return (email, eventId, categoryId);
        }
    }
}
