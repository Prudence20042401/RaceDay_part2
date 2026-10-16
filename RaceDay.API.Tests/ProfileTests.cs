using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.API.Tests
{
    public class ProfileTests : ApiTestBase
    {
        public ProfileTests(CustomWebApplicationFactory factory) : base(factory) { }

        public class ProfileInfo
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string? OrganisationName { get; set; }
            public DateTime? DateOfBirth { get; set; }
        }

        [Fact]
        public async Task UnauthenticatedUser_CannotViewProfile()
        {
            var response = await Client.GetAsync("/api/Profile");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CanViewAndUpdateOwnProfile()
        {
            await LoginAsNewParticipant();

            var updateResponse = await Client.PutAsJsonAsync("/api/Profile", new
            {
                firstName = "Thabo",
                lastName = "Mokoena",
                dateOfBirth = "1995-04-12"
            });
            var profile = await Client.GetFromJsonAsync<ProfileInfo>("/api/Profile");

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            Assert.Equal("Thabo", profile!.FirstName);
            Assert.Equal("Mokoena", profile.LastName);
            Assert.Equal("Participant", profile.Role);
            Assert.Equal(new DateTime(1995, 4, 12), profile.DateOfBirth);
        }

        [Fact]
        public async Task Organiser_CanViewAndUpdateOwnProfile()
        {
            await LoginAsNewOrganiser();

            var updateResponse = await Client.PutAsJsonAsync("/api/Profile", new
            {
                firstName = "Lerato",
                lastName = "Dlamini",
                organisationName = "Joburg Road Runners",
                phoneNumber = "+27119998888"
            });
            var profile = await Client.GetFromJsonAsync<ProfileInfo>("/api/Profile");

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            Assert.Equal("Organiser", profile!.Role);
            Assert.Equal("Joburg Road Runners", profile.OrganisationName);
        }

        [Fact]
        public async Task UpdateProfile_WithBlankName_ReturnsBadRequest()
        {
            await LoginAsNewParticipant();

            var response = await Client.PutAsJsonAsync("/api/Profile", new
            {
                firstName = "",
                lastName = ""
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
