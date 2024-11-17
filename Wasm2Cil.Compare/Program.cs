using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        // Define database path
        var dbPath = "./test.3.sqlite";
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }

        // Initialize and open the SQLite database
        var connectionString = $"Data Source={dbPath}";
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            // Create table
            using (var createCommand = connection.CreateCommand())
            {
                createCommand.CommandText = 
                    "CREATE TABLE IF NOT EXISTS Users (ID INT PRIMARY KEY NOT NULL, Name TEXT NOT NULL);";
                createCommand.ExecuteNonQuery();
            }

            // Prepare the insert statement
            using (var transaction = connection.BeginTransaction())
            using (var insertCommand = connection.CreateCommand())
            {
                insertCommand.CommandText = "INSERT INTO Users (ID, Name) VALUES ($id, $name);";
                insertCommand.Parameters.Add(new SqliteParameter("$id", System.Data.DbType.Int32));
                insertCommand.Parameters.Add(new SqliteParameter("$name", System.Data.DbType.String));

                // Begin transaction
                
                {
                    var sw = Stopwatch.StartNew();

                    // Insert 3 million rows
                    for (int i = 0; i < 10000000; i++)
                    {
                        insertCommand.Parameters["$id"].Value = i;
                        insertCommand.Parameters["$name"].Value = "TEstTest";
                        insertCommand.ExecuteNonQuery();
                    }

                    // Commit transaction
                    transaction.Commit();
                    sw.Stop();
                    Console.WriteLine($"Time: {sw.Elapsed.TotalSeconds} seconds");
                }
            }

            // Query to select rows and count them
            using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.CommandText = "SELECT ID, Name FROM Users;";
                int rowCount = 0;
                using (var reader = selectCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rowCount++;
                    }
                }
                Console.WriteLine($"Step: {rowCount}");
            }
        }
    }
}