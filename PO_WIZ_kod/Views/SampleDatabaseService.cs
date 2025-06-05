using Microsoft.Data.Sqlite;
using PO_WIZ_kod.Models;
using System;
using System.Collections.Generic;
using System.IO;

public static class SampleDatabaseService
{
    private static readonly string dbPath = "sample.db";

    public static void InitializeDatabase()
    {
        if (!File.Exists(dbPath))
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Samples (
                    Id TEXT PRIMARY KEY,
                    Name TEXT,
                    Type TEXT,
                    CollectionDate TEXT,
                    Notes TEXT
                );";
            command.ExecuteNonQuery();
        }
    }

    public static void ZapiszSample(Sample sample)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
        INSERT INTO Samples (Id, Name, Type, CollectionDate, Notes)
        VALUES ($id, $name, $type, $date, $notes);";

        cmd.Parameters.AddWithValue("$id", sample.Id);
        cmd.Parameters.AddWithValue("$name", sample.Name);
        cmd.Parameters.AddWithValue("$type", sample.Type);
        cmd.Parameters.AddWithValue("$date", sample.CollectionDate.HasValue
            ? sample.CollectionDate.Value.ToString("yyyy-MM-dd")
            : DBNull.Value);
        cmd.Parameters.AddWithValue("$notes", sample.Notes);

        cmd.ExecuteNonQuery();
    }


    public static List<Sample> PobierzWszystkie()
    {
        var result = new List<Sample>();

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Samples;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Sample
            {
                Id = reader["Id"].ToString() ?? "",
                Name = reader["Name"].ToString() ?? "",
                Type = reader["Type"].ToString() ?? "",
                CollectionDate = DateTime.TryParse(reader["CollectionDate"]?.ToString(), out var date) ? date : null,
                Notes = reader["Notes"].ToString() ?? ""
            });
        }

        return result;
    }

    public static void AktualizujSample(Sample sample)
    {
        using var connection = new SqliteConnection("Data Source=sample.db");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
        UPDATE Samples
        SET Name = $name,
            Type = $type,
            CollectionDate = $date,
            Notes = $notes
        WHERE Id = $id;
    ";

        command.Parameters.AddWithValue("$id", sample.Id);
        command.Parameters.AddWithValue("$name", sample.Name);
        command.Parameters.AddWithValue("$type", sample.Type);
        command.Parameters.AddWithValue("$date", sample.CollectionDate?.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$notes", sample.Notes);

        command.ExecuteNonQuery();
    }


}
