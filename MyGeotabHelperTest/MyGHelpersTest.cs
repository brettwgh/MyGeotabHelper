using Geotab.Checkmate;
using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Logging;
using MyGeotabHelper;

namespace MyGeotabHelperTest
{
    public class MyGHelpersTest
    {

        private API _api;
        private string serviceAccount = "brettkelley@geotab.com";
        private string sessionId;
        private string database = "brettk";

        public MyGHelpersTest()
        {
            Mock<ILogger<MyGHelpersTest>> logger = new Mock<ILogger<MyGHelpersTest>>();
            MyGHelpers.AttachLogger(logger.Object);
            sessionId = Creds.sessionId;
            _api = new API(serviceAccount, null, sessionId, database);
        }

        [Fact]
        private async Task AddGroupsToDeviceAsyncTest()
        {
            List<string> groupNames = new List<string>() { "Off Road" };
            List<Group> groupList = await MyGHelpers.GetGroupListByNamesAsync(_api, groupNames);
            Device device = await MyGHelpers.GetDeviceByIdAsync(_api, Id.Create("b19"));
            try
            {
                await MyGHelpers.AddGroupsToDeviceAsync(_api, device, groupList);
            }
            catch (GroupRelationViolatedException grve)
            {
                Console.WriteLine(grve.Message);
            }
        }
    }
}
