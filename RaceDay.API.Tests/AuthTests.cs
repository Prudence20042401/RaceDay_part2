using System.Net;
using System.Net.Http.Json;
using Xunit;


namespace RaceDay.API.Tests
{
    public class AuthTests : ApiTestBase
    {
        public AuthTests(CustomWebApplicationFactory factory) : base(factory) { }

        public class SessionResponse
        {
            public int UserID { get; set; }
            public string Role { get; set; } = string.Empty;
        }

        [Fact]
        public async Task RegisterParticipant_WithValidDetails_ReturnsOk()
        {
            var response = await RegisterParticipant(UniqueEmail("p"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RegisterParticipant_WithoutDateOfBirth_ReturnsBadRequest()
        {
            var response = await Client.PostAsJsonAsync("/api/Auth/register", new
            {
                email = UniqueEmail("nodob"),
                password = Password,
                firstName = "Test",
                lastName = "Participant",
                role = "Participant"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RegisterOrganiser_WithValidInviteCode_ReturnsOk()
        {
            var response = await RegisterOrganiser(UniqueEmail("o"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RegisterOrganiser_WithWrongInviteCode_ReturnsForbidden()
        {
            var response = await RegisterOrganiserWithCode(UniqueEmail("o"), "WRONG-CODE");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RegisterOrganiser_WithoutInviteCode_ReturnsForbidden()
        {
            var response = await RegisterOrganiserWithCode(UniqueEmail("o"), null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RegisterSameEmailTwice_ReturnsConflict()
        {
            var email = UniqueEmail("duplicate");
            await RegisterParticipant(email);

            var response = await RegisterParticipant(email);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithCorrectCredentials_ReturnsOk()
        {
            var email = UniqueEmail("login");
            await RegisterParticipant(email);

            var response = await Client.PostAsJsonAsync("/api/Auth/login", new
            {
                email,
                password = Password
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithWrongPassword_ReturnsUnauthorized()
        {
            var email = UniqueEmail("wrongpassword");
            await RegisterParticipant(email);

            var response = await Client.PostAsJsonAsync("/api/Auth/login", new
            {
                email,
                password = "WrongPassword123!"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
        {
            var response = await Client.PostAsJsonAsync("/api/Auth/login", new
            {
                email = UniqueEmail("nobody"),
                password = Password
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Me_AfterLogin_ReturnsRoleStoredInSession()
        {
            await LoginAsNewOrganiser();

            var response = await Client.GetAsync("/api/Auth/me");
            var session = await response.Content.ReadFromJsonAsync<SessionResponse>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(session);
            Assert.Equal("Organiser", session!.Role);
            Assert.True(session.UserID > 0);
        }

        [Fact]
        public async Task Me_WithoutLogin_ReturnsUnauthorized()
        {
            var response = await Client.GetAsync("/api/Auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Logout_EndsSession()
        {
            await LoginAsNewParticipant();
            await Logout();

            var response = await Client.GetAsync("/api/Auth/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
