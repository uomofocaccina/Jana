
using FluentMigrator;

namespace JanaApi.Migrations
{

    [Migration(1)]
    public class Template : Migration
    {
        public override void Up()
        {
            // Creazione tabella Clienti
            //Create.Table("Clienti")
            //    .WithColumn("Id").AsInt32().PrimaryKey().Identity()
            //    .WithColumn("id_Cliente_Realm").AsString(150).NotNullable()
            //    .WithColumn("id_Realm").AsInt32().NotNullable()
            //    .WithColumn("PartitaIva").AsString(50).Nullable()
            //    .WithColumn("CodiceFiscale").AsString(50).Nullable()
            //    .WithColumn("Denominazione").AsString(int.MaxValue).Nullable()
            //    .WithColumn("Cellulare").AsString(50).Nullable()
            //    .WithColumn("Email").AsString(50).Nullable();

            //insert di dati
            //Insert.IntoTable("Users").Row(new { FirstName = "John", LastName = "Smith" });

            //modifica di una tabella
            //Alter.Table("Users").AddColumn("Age").AsInt32().Nullable();

            //update di dati
            //Update.Table("Users").Set(new { Age = 30 }).Where(new { FirstName = "John" });

            //esecuzione di script sql
            Execute.Sql("select 1");

            // esecuzione di script sql da file
            //Execute.Script("myscript.sql");

            //modifica di una vista
            //Execute.Sql("DROP VIEW IF EXISTS MyView");
            //Execute.Sql(@"
            //CREATE VIEW MyView AS
            //SELECT 
            //    c.Id,
            //    c.Denominazione,
            //    c.PartitaIva,
            //    r.Nome AS RealmName
            //FROM 
            //    Clienti c
            //INNER JOIN 
            //    Realms r ON c.id_Realm = r.Id
            //");

        }

        public override void Down()
        {
            //rollaback.
            //usare solo se ci sono da ripristinare dei dati nelle tabelle.
        }
    }
}