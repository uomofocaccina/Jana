using FluentMigrator.Runner;

namespace JanaApi;

public static class DbMigrator
{
    public static void MigrateUp(string connectionString)
    {
        //migration part
        //documentation here https://fluentmigrator.github.io/articles/fluent-interface.html

        var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSQLite()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(Program).Assembly).For.Migrations())
                    .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);

        try
        {
            var migrationRunner = serviceProvider.GetRequiredService<IMigrationRunner>();
            migrationRunner.MigrateUp();
        }
        catch (Exception ex)
        {
            var logger = (ILogger<Program>)serviceProvider.GetService(typeof(ILogger<Program>));
            logger.LogError("Errore durante la migrazione del database: " + ex.Message);
        }
    }
}