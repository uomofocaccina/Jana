using Microsoft.Data.SqlClient;
using JanaApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1
{
    public class DbInit
    {
        private static string database = "scaffold";

        public static void CreateDatabase(string connectionstrings)
        {
            string sql = "create database " + database;
            using (var connection = new SqlConnection(connectionstrings))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void LanciaMigrazione(string connectionstrings)
        {
            DbMigrator.MigrateUp(connectionstrings);
        }

        public static void InizializzaDatabase(string connectionstrings)
        {
            CreateDatabase(connectionstrings);
            string nuovaConnectionstring = ModificaConnectionstring(connectionstrings);
            LanciaMigrazione(nuovaConnectionstring);
        }

        public static string InizializzaDatabaseRestituisciCN(string connectionstrings)
        {
            InizializzaDatabase(connectionstrings);
            return ModificaConnectionstring(connectionstrings);
        }
        private static string ModificaConnectionstring(string connectionstrings)
        {
            return connectionstrings.Replace("master", database);
        }
    }

}
