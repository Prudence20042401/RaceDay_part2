using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.API.Tests
{
    public class CategoryTests : ApiTestBase
    {
        public CategoryTests(CustomWebApplicationFactory factory) : base(factory) { }

        [Fact]
        public async Task Participant_CannotCreateCategory()
        {
            await LoginAsNewOrganiser();
            var eventId = await CreateEvent();
            await Logout();

            await LoginAsNewParticipant();
            var response = await Client.PostAsJsonAsync("/api/Categories", new
            {
                eventID = eventId,
                name = "Sneaky Category"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UnauthenticatedUser_CannotCreateCategory()
        {
            var response = await Client.PostAsJsonAsync("/api/Categories", new
            {
                eventID = 1,
                name = "Anonymous Category"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Organiser_CannotAddCategoryToAnotherOrganisersEvent()
        {
            await LoginAsNewOrganiser();
            var eventId = await CreateEvent();
            await Logout();

            await LoginAsNewOrganiser();
            var response = await Client.PostAsJsonAsync("/api/Categories", new
            {
                eventID = eventId,
                name = "Not Mine"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Anyone_CanListCategoriesForAnEvent()
        {
            await LoginAsNewOrganiser();
            var eventId = await CreateEvent();
            await CreateCategory(eventId, "5km");
            await CreateCategory(eventId, "10km");
            await Logout();

            var response = await Client.GetAsync("/api/Categories/event/" + eventId);
            var categories = await response.Content.ReadFromJsonAsync<List<IdResponse>>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, categories!.Count);
        }

        [Fact]
        public async Task Organiser_CannotDeleteCategoryThatHasEnrolments()
        {
            var (organiserEmail, eventId, categoryId) = await OrganiserWithEventAndCategory();
            await Logout();

            await LoginAsNewParticipant();
            Assert.Equal(HttpStatusCode.OK, (await Enrol(eventId, categoryId)).StatusCode);
            await Logout();

            await Login(organiserEmail);
            var response = await Client.DeleteAsync("/api/Categories/" + categoryId);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }
}
