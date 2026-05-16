using Microsoft.Extensions.DependencyInjection;
using JanaApi.Dapper;
using JanaApi.Model;
using JanaApi.Service;


namespace TestProject1
{
    [TestClass]
    public class TestNote
    {
        public static string databaseName = "database-note.sqlite";
        public static string connectionStringTest = "Data Source=" + databaseName;

        //dichiarare il servizio
        private static INoteService _note;

        [ClassInitialize]
        public static void StartContainer(TestContext context)
        {
            File.Delete(databaseName);
            DbInit.LanciaMigrazione(connectionStringTest);

            var services = new ServiceCollection();
            //inetta i servizi che usano db con connectionstring
            services.AddScoped<IDapperContext, DapperContext>((_) => new DapperContext(connectionStringTest));
            services.AddLogging();
            services.AddScoped<INoteService, NoteService>();
            var serviceProvider = services.BuildServiceProvider();
            //istanza   il servizio
            _note = serviceProvider.GetService<INoteService>();
        }

        [TestMethod]
        public async Task TestWriteAndUpdateMoveDeleteNote()
        {
            DapperContext dapper = new DapperContext(connectionStringTest);
            await dapper.DeleteAsync("delete from Note where user_id = 666");

            Note note = new Note
            {
                id = Guid.NewGuid().ToString(),
                text = "test text note",
                title = "test title note",
                folder = Guid.NewGuid().ToString(),
                active = 0,
                user_id = 666
            };

            var test = await _note.AddNoteAsync(note);

            Assert.AreEqual(test.success, true);

            string sql = "select * from note where user_id = 666";
            long timeStampRead = 0;
            try
            {
                var testRead = await dapper.GetObjectAsync<Note>(sql);

                Assert.AreEqual(testRead.text == note.text, true);
                Assert.AreEqual(testRead.id == note.id, true);
                Assert.AreEqual(testRead.user_id == note.user_id, true);
                Assert.AreEqual(testRead.timestamp != 0, true);
                Assert.AreEqual(testRead.active == note.active, true);
                Assert.AreEqual(testRead.title == note.title, true);
                Assert.AreEqual(testRead.folder == note.folder, true);
                timeStampRead = testRead.timestamp;
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test failed with exception: {ex.Message}");
            }

            note.text = "updated text note";
            note.title = "updated title note";
            note.active = 1;
            note.timestamp = 0;
            //wait 100 ms to ensure timestamp changes
            await Task.Delay(100);

            try
            {
                await _note.UpdateNoteAsync(note);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Update failed with exception: {ex.Message}");
            }

            var testRead2 = await dapper.GetObjectAsync<Note>(sql);

            Assert.AreEqual(testRead2.text == note.text, true);
            Assert.AreEqual(testRead2.id == note.id, true);
            Assert.AreEqual(testRead2.user_id == note.user_id, true);
            Assert.AreEqual(testRead2.timestamp != timeStampRead, true);
            Assert.AreEqual(testRead2.active == note.active, true);
            Assert.AreEqual(testRead2.title == note.title, true);
            Assert.AreEqual(testRead2.folder == note.folder, true);

            string newFolderId = Guid.NewGuid().ToString();
            await _note.UpdateNoteFolderAsync(note.id, newFolderId, null, 666);
            var testRead3 = await dapper.GetObjectAsync<Note>(sql);
            Assert.AreEqual(testRead3.folder == newFolderId, true);
            Assert.AreEqual(testRead3.text == note.text, true);
            Assert.AreEqual(testRead3.id == note.id, true);
            Assert.AreEqual(testRead3.user_id == note.user_id, true);
            Assert.AreEqual(testRead3.timestamp != testRead2.timestamp, true);
            Assert.AreEqual(testRead3.active == note.active, true);
            Assert.AreEqual(testRead3.title == note.title, true);

        }

        [TestMethod]
        public async Task TestReadNote()
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            DapperContext dapper = new DapperContext(connectionStringTest);
            await dapper.DeleteAsync("delete from note where user_id = 667");

            Note note = new Note
            {
                id = Guid.NewGuid().ToString(),
                text = "test text note",
                title = "test title note",
                folder = Guid.NewGuid().ToString(),
                active = 0,
                user_id = 667
            };

            //wait 100 ms to ensure timestamp changes
            await Task.Delay(100);
            var testInsert = await _note.AddNoteAsync(note);
            Assert.AreEqual(testInsert.success, true);

            Note note2 = new Note
            {
                id = Guid.NewGuid().ToString(),
                text = "test text note2",
                title = "test title note2",
                folder = Guid.NewGuid().ToString(),
                active = 1,
                user_id = 667
            };

            //wait 100 ms to ensure timestamp changes
            await Task.Delay(100);
            testInsert = await _note.AddNoteAsync(note2);
            Assert.AreEqual(testInsert.success, true);

            var testRead = await _note.GetNotesAsync(667, timestamp, 20, 0);

            Assert.AreEqual(testRead.Count == 2, true);
            Assert.AreEqual(testRead[0].text == note.text, true);
            Assert.AreEqual(testRead[0].id == note.id, true);
            Assert.AreEqual(testRead[0].user_id == note.user_id, true);
            Assert.AreEqual(testRead[0].active == note.active, true);
            Assert.AreEqual(testRead[0].title == note.title, true);
            Assert.AreEqual(testRead[0].folder == note.folder, true);
            Assert.AreEqual(testRead[1].text == note2.text, true);
            Assert.AreEqual(testRead[1].id == note2.id, true);
            Assert.AreEqual(testRead[1].user_id == note2.user_id, true);
            Assert.AreEqual(testRead[1].active == note2.active, true);
            Assert.AreEqual(testRead[1].folder == note2.folder, true);
            Assert.AreEqual(testRead[1].title == note2.title, true);

            var count = await _note.GetCountNotesAsync(667, timestamp);
            Assert.AreEqual(count == 2, true);

            await Task.Delay(100);
            var testRead2 = await _note.GetNotesAsync(667, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 20, 0);
            Assert.AreEqual(testRead2.Count == 0, true);

            var count2 = await _note.GetCountNotesAsync(667, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            Assert.AreEqual(count2 == 0, true);


        }

    }
}
