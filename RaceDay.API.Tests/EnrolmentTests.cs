using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.API.Tests
{
    public class EnrolmentTests : ApiTestBase
    {
        public EnrolmentTests(CustomWebApplicationFactory factory) : base(factory) { }

        [Fact]
        public async Task Participant_CanEnrol_AndEnrolmentIsRecordedCorrectly()
        {
            var (_, eventId, categoryId) = await OrganiserWithEventAndCategory();
            await Logout();
            await LoginAsNewParticipant();

            var enrolResponse = await Enrol(eventId, categoryId);
            var myResponse = await Client.GetAsync("/api/Enrolments/my");
            var mine = await myResponse.Content.ReadFromJsonAsync<List<EnrolmentInfo>>();

            Assert.Equal(HttpStatusCode.OK, enrolResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, myResponse.StatusCode);
            var saved = Assert.Single(mine!);
            Assert.Equal(eventId, saved.EventID);
            Assert.Equal(categoryId, saved.CategoryID);
            Assert.Equal("10km", saved.CategoryName);
            Assert.Equal("Pending", saved.Status);
        }

        [Fact]
        public async Task Participant_CannotSelfConfirmEnrolment()
        {
            var (_, eventId, categoryId) = await OrganiserWithEventAndCategory();
            await Logout();
            await LoginAsNewParticipant();

            await Enrol(eventId, categoryId, status: "Confirmed");
            var mine = await Client.GetFromJsonAsync<List<EnrolmentInfo>>("/api/Enrolments/my");

            Assert.Equal("Pending", Assert.Single(mine!).Status);
        }

        [Fact]
        public async Task Participant_CannotEnrolTwiceInSameEvent()
        {
            var (_, eventId, categoryId) = await OrganiserWithEventAndCategory();
            await Logout();
            await LoginAsNewParticipant();

            await Enrol(eventId, categoryId);
            var second = await Enrol(eventId, categoryId);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task Enrol_WithCategoryFromDifferentEvent_ReturnsBadRequest()
        {
            var (_, eventId, _) = await OrganiserWithEventAndCategory();
            var otherEventId = await CreateEvent("Other Event");
            var otherCategoryId = await CreateCategory(otherEventId, "21km");
            await Logout();
            await LoginAsNewParticipant();

            var response = await Enrol(eventId, otherCategoryId);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotEnrol()
        {
            var (_, eventId, categoryId) = await OrganiserWithEventAndCategory();

            var response = await Enrol(eventId, categoryId);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UnauthenticatedUser_CannotEnrol()
        {
            var response = await Enrol(1, 1);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CanViewEnrolmentsForOwnEvent()
        {
            var (organiserEmail, eventId, categoryId) = await OrganiserWithEventAndCategory();
            await Logout();
            await LoginAsNewParticipant();
            await Enrol(eventId, categoryId);
            await Logout();
            await Login(organiserEmail);

            var response = await Client.GetAsync("/api/Enrolments/event/" + eventId);
            var enrolments = await response.Content.ReadFromJsonAsync<List<EnrolmentInfo>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(enrolments!);
        }

        [Fact]
        public async Task Organiser_CannotViewEnrolmentsForAnotherOrganisersEvent()
        {
            var (_, eventId, _) = await OrganiserWithEventAndCategory();
            await Logout();
            await LoginAsNewOrganiser();

            var response = await Client.GetAsync("/api/Enrolments/event/" + eventId);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CannotViewEventEnrolments()
        {
            var (_, eventId, _) = await OrganiserWithEventAndCategory();
            await Logout();
            await LoginAsNewParticipant();

            var response = await Client.GetAsync("/api/Enrolments/event/" + eventId);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CanConfirmEnrolment()
        {
            var (organiserEmail, eventId, categoryId) = await OrganiserWithEventAndCategory();
            await Logout();
            await LoginAsNewParticipant();
            var enrolled = await (await Enrol(eventId, categoryId)).Content.ReadFromJsonAsync<EnrolmentInfo>();
            await Logout();
            await Login(organiserEmail);

            var response = await Client.PutAsJsonAsync(
                $"/api/Enrolments/{enrolled!.EnrolmentID}/status", new { status = "Confirmed" });
            var updated = await response.Content.ReadFromJsonAsync<EnrolmentInfo>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Confirmed", updated!.Status);
        }

        [Fact]
        public async Task Participant_CannotChangeEnrolmentStatus()
        {
            var (_, eventId, categoryId) = await OrganiserWithEventAndCategory();
            await Logout();
            await LoginAsNewParticipant();
            var enrolled = await (await Enrol(eventId, categoryId)).Content.ReadFromJsonAsync<EnrolmentInfo>();

            var response = await Client.PutAsJsonAsync(
                $"/api/Enrolments/{enrolled!.EnrolmentID}/status", new { status = "Confirmed" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
