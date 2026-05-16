
using FluentMigrator;

namespace JanaApi.Migrations
{

    [Migration(2025060801)]
    public class PrimaMigrazione : Migration
    {
        public override void Up()
        {

            Create.Table("Folder")
                .WithColumn("id").AsString(50).PrimaryKey()
                .WithColumn("text").AsString(int.MaxValue).Nullable()
                .WithColumn("parent").AsString(50).Nullable()
                .WithColumn("timestamp").AsInt64().WithDefaultValue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                .WithColumn("created").AsDateTime().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("active").AsInt16().WithDefaultValue(1)
                .WithColumn("user_id").AsInt32();

            Create.Index("IX_Folder_UserId_Active")
                .OnTable("Folder")
                .OnColumn("user_id").Ascending()
                .OnColumn("active").Ascending()
                .OnColumn("timestamp").Ascending();

            Create.Table("Users")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("username").AsString(50)
                .WithColumn("password").AsString(50)
                .WithColumn("name").AsString(int.MaxValue)
                .WithColumn("active").AsInt16().WithDefaultValue(1)
                .WithColumn("created").AsDateTime().WithDefaultValue(SystemMethods.CurrentUTCDateTime);

            Create.Index("IX_Users_UserId_Active")
                .OnTable("Folder")
                .OnColumn("username").Ascending()
                .OnColumn("active").Ascending();

            Create.Table("Note")
                .WithColumn("id").AsString(50).PrimaryKey()
                .WithColumn("text").AsString(int.MaxValue).Nullable()
                .WithColumn("title").AsString(int.MaxValue).Nullable()
                .WithColumn("folder").AsString(50)
                .WithColumn("timestamp").AsInt64().WithDefaultValue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                .WithColumn("created").AsDateTime().WithDefaultValue(SystemMethods.CurrentUTCDateTime)
                .WithColumn("active").AsInt16().WithDefaultValue(1)
                .WithColumn("user_id").AsInt32();

            Create.Index("IX_Note_UserId_Active")
                .OnTable("Folder")
                .OnColumn("user_id").Ascending()
                .OnColumn("active").Ascending()
                .OnColumn("timestamp").Ascending();
        }

        public override void Down()
        {
        }
    }
}