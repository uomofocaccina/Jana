using Microsoft.Extensions.DependencyInjection;
using JanaApi.Dapper;
using JanaApi.Model;
using JanaApi.Service;

namespace TestProject1
{
    [TestClass]
    public class TestFolder
    {
        public static string databaseName = "database-test-folder.sqlite";
        public static string connectionStringTest = "Data Source=" + databaseName;

        //dichiarare il servizio
        private static IFolderService _folder;

        [ClassInitialize]
        public static void StartContainer(TestContext context)
        {
            File.Delete(databaseName);
            DbInit.LanciaMigrazione(connectionStringTest);

            var services = new ServiceCollection();
            //inetta i servizi che usano db con connectionstring
            services.AddScoped<IDapperContext, DapperContext>((_) => new DapperContext(connectionStringTest));
            services.AddLogging();
            services.AddScoped<IFolderService, FolderService>();
            var serviceProvider = services.BuildServiceProvider();
            //istanza   il servizio
            _folder = serviceProvider.GetService<IFolderService>();
        }

        //[TestMethod]
        //public async Task TestServiceDirectoryNote()
        //{
        //    Assert.AreEqual(true, true);
        //}

        [TestMethod]
        public async Task TestWriteUpdateDeleteFolder()
        {
            DapperContext dapper = new DapperContext(connectionStringTest);
            await dapper.DeleteAsync("delete from Folder where user_id = 666");

            Folder folder = new Folder
            {
                id = Guid.NewGuid().ToString(),
                text = "test folder",
                parent = null,
                active = 0,
                user_id = 666
            };

            var test = await _folder.AddFolderAsync(folder);

            Assert.AreEqual(test.success, true);

            string sql = "select * from Folder where user_id = 666";
            long timeStampRead = 0;
            try
            {
                var testRead = await dapper.GetObjectAsync<Folder>(sql);

                Assert.AreEqual(testRead.text == folder.text, true);
                Assert.AreEqual(testRead.id == folder.id, true);
                Assert.AreEqual(testRead.user_id == folder.user_id, true);
                Assert.AreEqual(testRead.timestamp != 0, true);
                Assert.AreEqual(testRead.active == folder.active, true);
                Assert.AreEqual(testRead.parent == folder.parent, true);
                timeStampRead = testRead.timestamp;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }

            folder.text = "updated folder note";
            folder.active = 1;
            folder.timestamp = 0;
            //wait 1 second to ensure timestamp changes
            await Task.Delay(1000);

            try
            {
                await _folder.UpdateFolderAsync(folder);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Update failed with exception: {ex.Message}");
            }

            var testRead2 = await dapper.GetObjectAsync<Folder>(sql);

            Assert.AreEqual(testRead2.text == folder.text, true);
            Assert.AreEqual(testRead2.id == folder.id, true);
            Assert.AreEqual(testRead2.user_id == folder.user_id, true);
            Assert.AreEqual(testRead2.timestamp != timeStampRead, true);
            Assert.AreEqual(testRead2.active == 1, true); //fixed for next test
            Assert.AreEqual(testRead2.parent == folder.parent, true);

            //test change parent
            Guid newParent = Guid.NewGuid();
            await _folder.UpdateFolderParentAsync(folder.id, newParent.ToString(), null, folder.user_id);
            var testRead3 = await dapper.GetObjectAsync<Folder>(sql);
            Assert.AreEqual(testRead3.parent == newParent.ToString(), true);
            Assert.AreEqual(testRead3.timestamp != testRead2.timestamp, true);
            var timestampRead = testRead3.timestamp;

            await _folder.UpdateFolderParentAsync(folder.id, null, null, folder.user_id);
            testRead3 = await dapper.GetObjectAsync<Folder>(sql);
            Assert.AreEqual(testRead3.parent == null, true);
            Assert.AreEqual(testRead3.timestamp != timestampRead, true);
            timestampRead = testRead3.timestamp;

            //test delete
            await _folder.DeleteFolderAsync(folder.id, null, folder.user_id);
            var testRead4 = await dapper.GetObjectAsync<Folder>(sql);
            Assert.AreEqual(testRead4.active == 0, true);
            Assert.AreEqual(testRead4.timestamp != timestampRead, true);
        }

        [TestMethod]
        public async Task TestReadFolder()
        {
            long timestamp = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeMilliseconds();

            DapperContext dapper = new DapperContext(connectionStringTest);
            await dapper.DeleteAsync("delete from Folder where user_id = 667");

            Folder folder = new Folder
            {
                id = Guid.NewGuid().ToString(),
                text = "test Folder",
                parent = null,
                active = 0,
                user_id = 667
            };

            var testInsert = await _folder.AddFolderAsync(folder);
            Assert.AreEqual(testInsert.success, true);

            Folder folder2 = new Folder
            {
                id = Guid.NewGuid().ToString(),
                text = "test Folder note2",
                parent = Guid.NewGuid().ToString(),
                active = 1,
                user_id = 667
            };

            testInsert = await _folder.AddFolderAsync(folder2);
            Assert.AreEqual(testInsert.success, true);

            var testRead = await _folder.GetFolderAsync(667, timestamp, 20, 0);
            Assert.AreEqual(testRead.Count == 2, true);
            Assert.AreEqual(testRead[0].text == folder.text, true);
            Assert.AreEqual(testRead[0].id == folder.id, true);
            Assert.AreEqual(testRead[0].user_id == folder.user_id, true);
            Assert.AreEqual(testRead[0].active == folder.active, true);
            Assert.AreEqual(testRead[0].parent == folder.parent, true);
            Assert.AreEqual(testRead[1].text == folder2.text, true);
            Assert.AreEqual(testRead[1].id == folder2.id, true);
            Assert.AreEqual(testRead[1].user_id == folder2.user_id, true);
            Assert.AreEqual(testRead[1].active == folder2.active, true);
            Assert.AreEqual(testRead[1].parent == folder2.parent, true);

            var count = await _folder.GetCountFolderAsync(667, timestamp);
            Assert.AreEqual(count == 2, true);

            await Task.Delay(100);
            var testRead2 = await _folder.GetFolderAsync(667, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 20, 0);
            Assert.AreEqual(testRead2.Count == 0, true);

            var count2 = await _folder.GetCountFolderAsync(667, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            Assert.AreEqual(count2 == 0, true);


        }

    }
}
