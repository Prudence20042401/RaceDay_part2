using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.API.Tests
{
    public class ResultTests : ApiTestBase
    {
        public ResultTests(CustomWebApplicationFactory factory) : base(factory) { }

        /// <summary>
        /// Builds a full scenario: Organiser creates event + category, a Participant enrols.
        /// Leaves the Organiser logged in. Returns what the tests need.
        /// </summary>
        private async Task<(string OrganiserEmail, string ParticipantEmail, int EventId, int EnrolmentId)> EnrolledScenario()
        {
            var (organiserEmail, eventId, categoryId) = await OrganiserWithEventAndCategory();
            await Logout();

            var participantEmail = await LoginAsNewParticipant();
            var enrolled = await (await Enrol(eventId, categoryId)).Content.ReadFromJsonAsync<EnrolmentInfo>();
            await Logout();

            await Login(organiserEmail);
            return (organiserEmail, participantEmail, eventId, enrolled!.EnrolmentID);
        }

        private Task<HttpResponseMessage> PostResult(int enrolmentId, int position = 47) =>
            Client.PostAsJsonAsync("/api/Results", new
            {
                enrolmentID = enrolmentId,
                finishTime = "01:23:45",
                finishingPosition = position
            });

        [Fact]
        public async Task Organiser_CanRecordResult_AndParticipantSeesItInMyResults()
        {
            var (_, participantEmail, _, enrolmentId) = await EnrolledScenario();

            var recordResponse = await PostResult(enrolmentId);
            await Logout();
            await Login(participantEmail);
            var myResponse = await Client.GetAsync("/api/Results/my");
            var mine = await myResponse.Content.ReadFromJsonAsync<List<ResultInfo>>();

            Assert.Equal(HttpStatusCode.OK, recordResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, myResponse.StatusCode);
            var result = Assert.Single(mine!);
            Assert.Equal("Test Fun Run", result.EventName);
            Assert.Equal("10km", result.CategoryName);
            Assert.Equal(47, result.FinishingPosition);
            Assert.Equal(1, result.TotalFinishers);
        }

        [Fact]
        public async Task Participant_CannotRecordResult()
        {
            var (_, participantEmail, _, enrolmentId) = await EnrolledScenario();
            await Logout();
            await Login(participantEmail);

            var response = await PostResult(enrolmentId);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UnauthenticatedUser_CannotRecordResult()
        {
            var response = await PostResult(1);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotRecordResultForAnotherOrganisersEvent()
        {
            var (_, _, _, enrolmentId) = await EnrolledScenario();
            await Logout();
            await LoginAsNewOrganiser();

            var response = await PostResult(enrolmentId);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RecordingTwoResultsForSameEnrolment_ReturnsConflict()
        {
            var (_, _, _, enrolmentId) = await EnrolledScenario();

            await PostResult(enrolmentId);
            var second = await PostResult(enrolmentId, position: 12);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task RecordResult_WithInvalidPosition_ReturnsBadRequest()
        {
            var (_, _, _, enrolmentId) = await EnrolledScenario();

            var response = await PostResult(enrolmentId, position: 0);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotUseParticipantMyResultsEndpoint()
        {
            await LoginAsNewOrganiser();

            var response = await Client.GetAsync("/api/Results/my");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CanListAndUpdateResultsForOwnEvent()
        {
            var (_, _, eventId, enrolmentId) = await EnrolledScenario();
            var created = await (await PostResult(enrolmentId)).Content.ReadFromJsonAsync<ResultInfo>();

            var listResponse = await Client.GetAsync("/api/Results/event/" + eventId);
            var list = await listResponse.Content.ReadFromJsonAsync<List<ResultInfo>>();
            var updateResponse = await Client.PutAsJsonAsync("/api/Results/" + created!.ResultID, new
            {
                finishTime = "01:20:00",
                finishingPosition = 5
            });
            var updated = await updateResponse.Content.ReadFromJsonAsync<ResultInfo>();

            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            Assert.Single(list!);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            Assert.Equal(5, updated!.FinishingPosition);
        }

        [Fact]
        public async Task Organiser_CanDeleteResult()
        {
            var (_, _, _, enrolmentId) = await EnrolledScenario();
            var created = await (await PostResult(enrolmentId)).Content.ReadFromJsonAsync<ResultInfo>();

            var response = await Client.DeleteAsync("/api/Results/" + created!.ResultID);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
