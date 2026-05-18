using Microsoft.Extensions.DependencyInjection;
using JanaApi.Dapper;
using JanaApi.Model;
using JanaApi.Service;
using JanaApi.Utilities;

namespace TestProject1
{
    [TestClass]
    public class TestAuth
    {
        public static string databaseName = "database-auth.sqlite";
        public static string connectionStringTest = "Data Source=" + databaseName;

        private static IUsersService _users;

        [ClassInitialize]
        public static void StartContainer(TestContext context)
        {
            File.Delete(databaseName);
            DbInit.LanciaMigrazione(connectionStringTest);

            var services = new ServiceCollection();
            services.AddScoped<IDapperContext, DapperContext>((_) => new DapperContext(connectionStringTest));
            services.AddLogging();
            services.AddScoped<IUsersService, UsersService>();
            var serviceProvider = services.BuildServiceProvider();
            _users = serviceProvider.GetService<IUsersService>();
        }

        //[TestInitialize]
        //public void TestInitialize()
        //{
        //    DapperContext dapper = new DapperContext(connectionStringTest);
        //    string passwordHash = PasswordHelper.ComputeSha256Hash("admin");
        //    string sql = "UPDATE Users SET password = @password WHERE username = 'admin'";
        //    dapper.ExecuteAsync(sql, new { password = passwordHash }).GetAwaiter().GetResult();
        //}

        [TestMethod]
        public async Task TestAdminUserCreatedInMigration()
        {
            DapperContext dapper = new DapperContext(connectionStringTest);

            // Check if admin user exists
            string sql = "SELECT * FROM Users WHERE username = 'admin'";
            var adminUser = await dapper.GetObjectAsync<User>(sql);

            Assert.IsNotNull(adminUser);
            Assert.AreEqual("admin", adminUser.username);
            Assert.AreEqual(0, adminUser.id);
            Assert.AreEqual(1, adminUser.admin);
            Assert.AreEqual(1, adminUser.active);

            // Verify password is hashed correctly
            string expectedHash = PasswordHelper.ComputeSha256Hash("admin");
            Assert.AreEqual(expectedHash, adminUser.password);
        }

        [TestMethod]
        public async Task TestAuthenticateWithValidCredentials()
        {
            var user = await _users.AuthenticateAsync("admin", "admin");

            Assert.IsNotNull(user);
            Assert.AreEqual("admin", user.username);
            Assert.AreEqual(0, user.id);
            Assert.AreEqual(1, user.admin);
            Assert.AreEqual(1, user.active);
        }

        [TestMethod]
        public async Task TestAuthenticateWithInvalidPassword()
        {
            var user = await _users.AuthenticateAsync("admin", "wrongpassword");

            Assert.IsNull(user);
        }

        [TestMethod]
        public async Task TestAuthenticateWithInvalidUsername()
        {
            var user = await _users.AuthenticateAsync("nonexistent", "password");

            Assert.IsNull(user);
        }

        [TestMethod]
        public async Task TestChangePasswordWithValidOldPassword()
        {
            // First, change the admin password
            bool result = await _users.ChangePasswordAsync(0, "admin", "newpassword");

            Assert.IsTrue(result);

            // Verify we can authenticate with new password
            var user = await _users.AuthenticateAsync("admin", "newpassword");
            Assert.IsNotNull(user);

            // Verify we cannot authenticate with old password
            user = await _users.AuthenticateAsync("admin", "password");
            Assert.IsNull(user);

            // Reset password for other tests
            await _users.ChangePasswordAsync(0, "newpassword", "admin");
        }

        [TestMethod]
        public async Task TestChangePasswordWithInvalidOldPassword()
        {
            bool result = await _users.ChangePasswordAsync(0, "wrongpassword", "newpassword");

            Assert.IsFalse(result);

            // Verify original password still works
            var user = await _users.AuthenticateAsync("admin", "admin");
            Assert.IsNotNull(user);
        }

        [TestMethod]
        public async Task TestChangePasswordForNonExistentUser()
        {
            bool result = await _users.ChangePasswordAsync(999, "password", "newpassword");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestUpdateAdminPassword()
        {
            bool result = await _users.UpdateAdminPasswordAsync("environmentpassword");

            Assert.IsTrue(result);

            // Verify we can authenticate with new password
            var user = await _users.AuthenticateAsync("admin", "environmentpassword");
            Assert.IsNotNull(user);

            // Reset password for other tests
            await _users.UpdateAdminPasswordAsync("admin");
        }

        [TestMethod]
        public async Task TestGetUserById()
        {
            var user = await _users.GetUserByIdAsync(0);

            Assert.IsNotNull(user);
            Assert.AreEqual("admin", user.username);
            Assert.AreEqual(0, user.id);
        }

        [TestMethod]
        public async Task TestGetUserByUsername()
        {
            var user = await _users.GetUserByUsernameAsync("admin");

            Assert.IsNotNull(user);
            Assert.AreEqual("admin", user.username);
            Assert.AreEqual(0, user.id);
        }

        [TestMethod]
        public async Task TestPasswordHashingIsConsistent()
        {
            string password = "testpassword123";
            string hash1 = PasswordHelper.ComputeSha256Hash(password);
            string hash2 = PasswordHelper.ComputeSha256Hash(password);

            Assert.AreEqual(hash1, hash2);
            Assert.IsTrue(PasswordHelper.VerifyPassword(password, hash1));
            Assert.IsFalse(PasswordHelper.VerifyPassword("wrongpassword", hash1));
        }

        [TestMethod]
        public async Task TestGetUsers()
        {
            var users = await _users.GetAllUsersAsync();
            Assert.IsNotNull(users);
            DapperContext dapper = new DapperContext(connectionStringTest);

            string sql = "INSERT INTO Users (id, username, name, active, admin, created, password) VALUES (10, 'user1', 'User One', 1, 0, datetime('now'), 'a')";
            await dapper.ExecuteAsync(sql);
            users = await _users.GetAllUsersAsync();
            Assert.IsNotNull(users);
            Assert.AreEqual(users.Count >= 1, true);
            Assert.AreEqual(users.Any(x => x.username == "user1"), true);

        }

        [TestMethod]
        public async Task TestInsertUser()
        {
            var users = await _users.GetAllUsersAsync();
            Assert.IsNotNull(users);
            int numUsersBefore = users.Count;

            User userMinimal = new User
            {
                username = "newuser",
                name = "New User",
                password = "newpassword",
                admin = 1
            };

            var test = await _users.AddUserAsync(userMinimal);
            Assert.AreEqual(test.success, true);
            Assert.AreEqual(test.id != 0, true);

            int idUser = test.id;

            users = await _users.GetAllUsersAsync();
            Assert.IsNotNull(users);
            Assert.AreEqual(numUsersBefore + 1, users.Count);

            DapperContext dapper = new DapperContext(connectionStringTest);
            string sql = "SELECT * FROM Users WHERE id = @id";
            var userDb = await dapper.GetObjectAsync<User>(sql, new { id = idUser });

            Assert.AreEqual("newuser", userDb.username);
            Assert.AreEqual("New User", userDb.name);
            Assert.AreEqual(1, userDb.admin);
            Assert.AreEqual(1, userDb.active);

            //check password
            var userLogin = await _users.AuthenticateAsync("newuser", "newpassword");
            Assert.IsNotNull(userLogin);


        }
    }
}