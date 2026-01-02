namespace StopWatchTest
{
    using Moq;
    using NUnit.Framework;
    using RestSharp;
    using StopWatch;

    internal class TestPocoClass { public string foo { get; set; } public string bar { get; set; } }

    [TestFixture]
    public class JiraApiRequesterTest
    {
        private Mock<IRestClientFactory> clientFactoryMock;

        private Mock<IJiraApiRequestFactory> jiraApiRequestFactoryMock;

        private JiraApiRequester jiraApiRequester;

        [SetUp]
        public void Setup()
        {
            clientFactoryMock = new Mock<IRestClientFactory>();
            // Return a RestClient pointing to an invalid base URL to avoid real HTTP calls
            clientFactoryMock.Setup(c => c.Create(It.IsAny<bool>()))
                .Returns(() => new RestClient(new RestClientOptions("https://example.invalid")));

            jiraApiRequestFactoryMock = new Mock<IJiraApiRequestFactory>();

            jiraApiRequester = new JiraApiRequester(clientFactoryMock.Object, jiraApiRequestFactoryMock.Object);
        }

        [Test, Description("DoAuthenticatedRequest: throws when username/token not set")]
        public void DoAuthenticatedRequest_WithoutCredentials_Throws()
        {
            var request = new RestRequest("/test", Method.Get);
            Assert.Throws<UsernameAndApiTokenNotSetException>(() =>
            {
                var _ = jiraApiRequester.DoAuthenticatedRequest<TestPocoClass>(request);
            });
        }

        [Test, Description("DoAuthenticatedRequest: with any credentials attempts request (may fail with denied)")]
        public void DoAuthenticatedRequest_WithCredentials_ThrowsDeniedOnInvalidEndpoint()
        {
            var request = new RestRequest("/test", Method.Get);
            jiraApiRequester.SetAuthentication("user", "token");
            Assert.Throws<RequestDeniedException>(() =>
            {
                var _ = jiraApiRequester.DoAuthenticatedRequest<TestPocoClass>(request);
            });
        }

    }
}
