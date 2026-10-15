using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ChessGameApi.Data;

public sealed class SqliteGameRepository
{
    private readonly string connectionString;

    public SqliteGameRepository()
    {
        string databaseDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Data"
        );

        Directory.CreateDirectory(databaseDirectory);

        string databasePath = Path.Combine(
            databaseDirectory,
            "chessgames.db"
        );

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        };

        connectionString = builder.ToString();

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Games (
                Id TEXT PRIMARY KEY,
                StateJson TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );
            """;

        command.ExecuteNonQuery();
    }

    public void Save(Guid gameId, string stateJson)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO Games (Id, StateJson, UpdatedAtUtc)
            VALUES ($id, $stateJson, $updatedAtUtc)
            ON CONFLICT(Id) DO UPDATE SET
                StateJson = excluded.StateJson,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;

        command.Parameters.AddWithValue(
            "$id",
            gameId.ToString()
        );

        command.Parameters.AddWithValue(
            "$stateJson",
            stateJson
        );

        command.Parameters.AddWithValue(
            "$updatedAtUtc",
            DateTime.UtcNow.ToString("O")
        );

        command.ExecuteNonQuery();
    }

    public string? Load(Guid gameId)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT StateJson
            FROM Games
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue(
            "$id",
            gameId.ToString()
        );

        object? result = command.ExecuteScalar();

        return result as string;
    }

    public bool Delete(Guid gameId)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = """
            DELETE FROM Games
            WHERE Id = $id;
            """;

        command.Parameters.AddWithValue(
            "$id",
            gameId.ToString()
        );

        return command.ExecuteNonQuery() > 0;
    }
}