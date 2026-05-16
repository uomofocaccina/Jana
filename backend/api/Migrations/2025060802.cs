using FluentMigrator;
using System.Security.Cryptography;
using System.Text;

namespace JanaApi.Migrations
{
    [Migration(2025060802)]
    public class AddAdminColumnAndDefaultUser : Migration
    {
        public override void Up()
        {
            // Add admin column to Users table
            Alter.Table("Users")
                .AddColumn("admin").AsInt16().WithDefaultValue(0);

            // Fix the index that was incorrectly pointing to Folder table
            Delete.Index("IX_Users_UserId_Active").OnTable("Folder");
            
            Create.Index("IX_Users_Username_Active")
                .OnTable("Users")
                .OnColumn("username").Ascending()
                .OnColumn("active").Ascending();

            // Create default admin user with SHA256 hashed password
            string hashedPassword = ComputeSha256Hash("admin");

            Execute.Sql($@"
                INSERT INTO Users (id, username, password, name, active, admin, created)
                VALUES (0, 'admin', '{hashedPassword}', 'Administrator', 1, 1, datetime('now'))
            ");
        }

        public override void Down()
        {
            // Migration not reversible for safety
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}