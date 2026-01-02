namespace StopWatchTest
{
    using Moq;
    using NUnit.Framework;
    using RestSharp;
    using StopWatch;
    using System;

    [TestFixture]
    public class JiraApiRequestFactoryTest
    {
        private RestRequest lastCreatedRequest;
        private Mock<IRestRequestFactory> requestFactoryMock;

        private JiraApiRequestFactory jiraApiRequestFactory;

        [SetUp]
        public void Setup()
        {
            requestFactoryMock = new Mock<IRestRequestFactory>();
            requestFactoryMock
                .Setup(m => m.Create(It.IsAny<string>(), It.IsAny<Method>()))
                .Returns((string url, Method method) =>
                {
                    lastCreatedRequest = new RestRequest(url, method);
                    return lastCreatedRequest;
                });

            jiraApiRequestFactory = new JiraApiRequestFactory(requestFactoryMock.Object);
        }



        [Test]
        public void CreateValidateSessionRequest_CreatesValidRequest()
        {
            var request = jiraApiRequestFactory.CreateValidateSessionRequest();
            requestFactoryMock.Verify(m => m.Create("/rest/auth/1/session", Method.Get));
        }


        [Test]
        public void CreateGetFavoriteFiltersRequest_CreatesValidRequest()
        {
            var request = jiraApiRequestFactory.CreateGetFavoriteFiltersRequest();
            requestFactoryMock.Verify(m => m.Create("/rest/api/2/filter/favourite", Method.Get));
        }
        

        [Test]
        public void CreateGetIssuesByJQLRequest_CreatesValidRequest()
        {
            string jql = "status%3Dopen";
            var request = jiraApiRequestFactory.CreateGetIssuesByJQLRequest(jql);
            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/2/search?jql={0}&maxResults=200", jql), Method.Get));
        }


        [Test]
        public void CreateGetIssueSummaryRequest_CreatesValidRequest()
        {
            string key = "FOO-42";
            var request = jiraApiRequestFactory.CreateGetIssueSummaryRequest(key);
            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/2/issue/{0}", key), Method.Get));
        }


        [Test]
        public void CreateGetIssueSummaryRequest_RemoveLeadingAndTrailingSpacesFromIssueKey()
        {
            string key = "   FOO-42   ";
            var request = jiraApiRequestFactory.CreateGetIssueSummaryRequest(key);
            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/2/issue/{0}", key.Trim()), Method.Get));
        }

        [Test]
        public void CreateGetIssueTimetrackingRequestt_CreatesValidRequest()
        {
            string key = "FOO-42";
            var request = jiraApiRequestFactory.CreateGetIssueTimetrackingRequest(key);
            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/2/issue/{0}?fields=timetracking", key), Method.Get));
        }


        [Test]
        public void CreateGetIssueTimetrackingRequest_RemoveLeadingAndTrailingSpacesFromIssueKey()
        {
            string key = "   FOO-42   ";
            var request = jiraApiRequestFactory.CreateGetIssueTimetrackingRequest(key);
            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/2/issue/{0}?fields=timetracking", key.Trim()), Method.Get));
        }


        [Test]
        public void CreatePostWorklogRequest_CreatesValidRequest()
        {
            string key = "FOO-42";
            var started = new DateTimeOffset(2016, 07, 26, 1, 44, 15, TimeSpan.Zero);
            TimeSpan time = new TimeSpan(1, 2, 0);
            string comment = "Sorry for the inconvenience...";
            StopWatch.EstimateUpdateMethods adjusmentMethod = EstimateUpdateMethods.Auto;
            string adjustmentValue = "";
            var request = jiraApiRequestFactory.CreatePostWorklogRequest(key, started, time, comment, adjusmentMethod, adjustmentValue);

            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/2/issue/{0}/worklog", key), Method.Post));

            Assert.AreEqual(DataFormat.Json, lastCreatedRequest.RequestFormat);
        }


        [Test]
        public void CreatePostWorklogRequest_RemoveLeadingAndTrailingSpacesFromIssueKey()
        {
            string key = "   FOO-42   ";
            var started = new DateTimeOffset(2016, 07, 26, 1, 44, 15, TimeSpan.Zero);
            TimeSpan time = new TimeSpan(1, 2, 0);
            string comment = "Sorry for the inconvenience...";
            StopWatch.EstimateUpdateMethods adjusmentMethod = EstimateUpdateMethods.Auto;
            string adjustmentValue = "";
            var request = jiraApiRequestFactory.CreatePostWorklogRequest(key, started, time, comment, adjusmentMethod, adjustmentValue);

            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/2/issue/{0}/worklog", key.Trim()), Method.Post));
        }

        [Test]
        public void CreatePostCommentRequest_CreatesValidRequest()
        {
            string key = "FOO-42";
            string comment = "Sorry for the inconvenience...";
            var request = jiraApiRequestFactory.CreatePostCommentRequest(key, comment);

            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/2/issue/{0}/comment", key), Method.Post));

            Assert.AreEqual(DataFormat.Json, lastCreatedRequest.RequestFormat);
        }


        [Test]
        public void CreatePostCommentRequest_RemoveLeadingAndTrailingSpacesFromIssueKey()
        {
            string key = "   FOO-42   ";
            string comment = "Sorry for the inconvenience...";
            var request = jiraApiRequestFactory.CreatePostCommentRequest(key, comment);

            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/2/issue/{0}/comment", key.Trim()), Method.Post));
        }


        [Test]
        public void CreateGetAvailableTransitions_CreatesValidRequest()
        {
            string key = "TST-1";

            var request = jiraApiRequestFactory.CreateGetAvailableTransitions(key);

            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/2/issue/{0}/transitions", key), Method.Get));
        }


        [Test]
        public void CreateDoTransition_CreatesValidRequest()
        {
            string key = "TST-1";
            int transitionId = 5;

            var request = jiraApiRequestFactory.CreateDoTransition(key, transitionId);

            requestFactoryMock.Verify(m => m.Create(String.Format("/rest/api/2/issue/{0}/transitions", key), Method.Post));

            Assert.AreEqual(DataFormat.Json, lastCreatedRequest.RequestFormat);
        }

    }
}
