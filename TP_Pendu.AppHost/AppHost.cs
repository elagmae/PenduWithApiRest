using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

int _maxGuesses = 10;

#region Database & Swagger Init

using var connection = new SqliteConnection("Data Source=..\\Pendu.db");
connection.Open();

using var command = connection.CreateCommand();
command.CommandText =
""""
CREATE TABLE IF NOT EXISTS GAMES
(
    ID INTEGER NOT NULL UNIQUE,
    WORD STRING,
    STATE INTEGER,
    PRIMARY KEY (ID AUTOINCREMENT)
);

CREATE TABLE IF NOT EXISTS GUESSES
(
    GAME_ID INTEGER NOT NULL,
    GUESS STRING, 
    FOREIGN KEY (GAME_ID) REFERENCES GAMES(ID)
);
"""";

using var reader = command.ExecuteReader();

connection.Close();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "TodoAPI";
    config.Title = "TodoAPI v1";
    config.Version = "v1";
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi(config =>
    {
        config.DocumentTitle = "TodoAPI";
        config.Path = "/swagger";
        config.DocumentPath = "/swagger/{documentName}/swagger.json";
        config.DocExpansion = "list";
    });
}

#endregion

#region MapGet

app.MapGet("/GetGames", () =>
{
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM GAMES WHERE STATE = 0;";

    int count = 0;
    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        count = (int)reader.GetInt64(0);
    }
    connection.Close();
    return "You have " + count + " active games.";
});

app.MapGet("/GetFinishedGames", () =>
{
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM GAMES WHERE STATE != 0;";
    int count = 0;
    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        count = (int)reader.GetInt64(0);
    }
    connection.Close();
    return "You have " + count + " finished games.";
});

#endregion

#region MapPost

app.MapPost("/CreateGame", (string wordToGuess) =>
{
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText =
    """
    INSERT INTO GAMES (WORD, STATE) VALUES ($wordToGuess, 0);
    SELECT COUNT(*) FROM GAMES;
    """;

    command.Parameters.AddWithValue("$wordToGuess", wordToGuess);

    using var reader = command.ExecuteReader();

    if (!reader.Read())
    {
        connection.Close();
        return "An error occurred while creating the game.";
    }

    int count = reader.GetInt32(0);
    connection.Close();

    return $"Game created with ID {count}. Your choosed word is {wordToGuess}.";

});

app.MapPost("/GuessLetter", (int id, char letter) =>
{
    if (!IsCurrentGame(id)) return "You already finished this game ! Create a new one to play again !";
    connection.Open();

    using var command = connection.CreateCommand();

    string wordToGuess = "";
    string answer = "";

    command.CommandText = "SELECT WORD FROM GAMES WHERE ID = $id;";
    command.Parameters.AddWithValue("$id", id);

    using (var reader = command.ExecuteReader())
    {
        if (!reader.Read())
        {
            connection.Close();
            return $"Game with ID {id} does not exist.";
        }

        wordToGuess = reader.GetString(0);
    }

    command.Parameters.Clear();

    command.CommandText = "INSERT INTO GUESSES(GAME_ID, GUESS) VALUES($id, $guess);";
    command.Parameters.AddWithValue("$id", id);
    command.Parameters.AddWithValue("$guess", letter);

    command.ExecuteNonQuery();

    command.Parameters.Clear();

    if (wordToGuess.Contains(letter))
    {
        string indexes = "";

        for (int i = 0; i < wordToGuess.Length; i++)
        {
            if (wordToGuess[i] == letter) indexes += " " + i;
        }

        answer = $"Letter {letter} is in the word at index(es):{indexes}. Remember, your word is {wordToGuess.Length} letters long. You have {GetRemainingGuesses(id)} guesses left.";
    }

    else
    {
        if (!CanGuessAgain(id))
        {
            command.CommandText = "UPDATE GAMES SET STATE = 1 WHERE ID = $id;";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();

            answer = $"Game over! The word was {wordToGuess}.";
            command.Parameters.Clear();
            connection.Close();
        }

        else
        {
            answer = $"Letter {letter} is not in the word. You have {GetRemainingGuesses(id)} guesses left.";
            command.Parameters.Clear();
            connection.Close();
        }
    }
    
    return answer;
});

app.MapPost("/GuessWord", (int id, string word) =>
{
    if (!IsCurrentGame(id)) return "You already finished this game ! Create a new one to play again !";
    connection.Open();
    using var command = connection.CreateCommand();

    string wordToGuess = "";
    string answer = "";
    int state = 0;

    command.CommandText = "SELECT WORD FROM GAMES WHERE ID = $id;";
    command.Parameters.AddWithValue("$id", id);

    using (var reader = command.ExecuteReader())
    {
        if (!reader.Read())
        {
            connection.Close();
            return $"Game with ID {id} does not exist.";
        }

        wordToGuess = reader.GetString(0);
    }

    command.Parameters.Clear();

    command.CommandText = "INSERT INTO GUESSES(GAME_ID, GUESS) VALUES($id, $guess);";
    command.Parameters.AddWithValue("$id", id);
    command.Parameters.AddWithValue("$guess", word);

    command.ExecuteNonQuery();

    command.Parameters.Clear();

    if (wordToGuess == word)
    {
        answer = $"Victory, your word was {word} indeed !";
        state = 1;
    }

    else
    {
        if (CanGuessAgain(id))
        {
            state = 0;
            answer = $"Wrong guess ! Try again ! Remember, your word is {wordToGuess.Length} letters long. You have {GetRemainingGuesses(id)} guesses left.";
            connection.Close();
            return answer;
        }

        else
        {
            answer = $"Nope ! You failed ! The word was {wordToGuess}.";
            state = 2;
        }
    }

    command.Parameters.Clear();
    command.CommandText = "UPDATE GAMES SET STATE = $state WHERE ID = $id;";
    command.Parameters.AddWithValue("$id", id);
    command.Parameters.AddWithValue("$state", state);

    command.ExecuteNonQuery();
    command.Parameters.Clear();
    connection.Close();

    return answer;
});

app.MapDelete("/DeleteGame", (int id) =>
{
    connection.Open();
    using var command = connection.CreateCommand();

    command.CommandText = "SELECT WORD FROM GAMES WHERE ID = $id;";
    command.Parameters.AddWithValue("$id", id);

    using (var reader = command.ExecuteReader())
    {
        if (!reader.Read())
        {
            connection.Close();
            return $"Game with ID {id} does not exist.";
        }
    }
    command.Parameters.Clear();

    command.CommandText =
    """
    
    DELETE FROM GUESSES WHERE GAME_ID = $id;
    DELETE FROM GAMES WHERE ID = $id;
    DELETE FROM sqlite_sequence WHERE name='GUESSES';
    DELETE FROM sqlite_sequence WHERE name='GAMES';
    """;
    
    command.Parameters.AddWithValue("$id", id);
    command.ExecuteNonQuery();

    command.Parameters.Clear();
    connection.Close();

    return "Game with ID " + id + " has been deleted.";
});

#endregion

#region Utility

bool CanGuessAgain(int id)
{
    int guesses = 0;
    command.Parameters.Clear();
    command.CommandText = "SELECT COUNT(*) FROM GUESSES WHERE GAME_ID = $id;";
    command.Parameters.AddWithValue("$id", id);

    using (var reader = command.ExecuteReader())
    {
        if (!reader.Read())
        {
            connection.Close();
            return false; // Game does not exist
        }

        guesses = (int)reader.GetInt64(0);
    }

    command.Parameters.Clear();
    return guesses < _maxGuesses;
}

int GetRemainingGuesses(int id)
{
    int guesses = 0;
    command.Parameters.Clear();
    command.CommandText = "SELECT COUNT(*) FROM GUESSES WHERE GAME_ID = $id;";
    command.Parameters.AddWithValue("$id", id);

    using (var reader = command.ExecuteReader())
    {
        if (!reader.Read())
        {
            connection.Close();
            return -1; // Game does not exist
        }

        guesses = (int)reader.GetInt64(0);
    }

    command.Parameters.Clear();
    return _maxGuesses - guesses;
}

bool IsCurrentGame(int id)
{
    connection.Open();
    command.Parameters.Clear();
    command.CommandText = "SELECT STATE FROM GAMES WHERE ID = $id;";
    command.Parameters.AddWithValue("$id", id);

    using var reader = command.ExecuteReader();

    int state = -1;
    if (reader.Read()) state = (int)reader.GetInt64(0);

    command.Parameters.Clear();
    connection.Close();
    return state == 0;
}

#endregion

app.Run();

