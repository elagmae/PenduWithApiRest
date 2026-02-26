using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

int _maxGuesses = 10;
int _currentGuesses = 0;

#region Database & Swagger Init

using var connection = new SqliteConnection("Data Source=C:..\\Pendu.db");
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

app.MapGet("/GetGames", () =>
{
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText =
    """
    SELECT COUNT(*) FROM GAMES WHERE STATE = 0;
    """;

    int count = 0;
    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        count = (int)reader.GetInt64(0);
    }
    connection.Close();
    return "You have " + count + " active games.";
});

app.MapPost("/CreateGame", (string wordToGuess) =>
{
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText =
    """
    INSERT INTO GAMES (WORD, STATE) VALUES ($wordToGuess, 0);
    """;

    command.Parameters.AddWithValue("$wordToGuess", wordToGuess);

    using var reader = command.ExecuteReader();
    connection.Close();
});

app.MapPost("/GuessLetter", (int id, char letter) =>
{
    connection.Open();
    using var command = connection.CreateCommand();

    string wordToGuess = "";
    string answer = ""; 

    command.CommandText =
    """
    INSERT INTO GUESSES(GAME_ID, GUESS) VALUES($id, $guess);
    SELECT WORD FROM GAMES WHERE ID = $id;
    """;

    command.Parameters.AddWithValue("$id", id);
    command.Parameters.AddWithValue("$guess", letter);

    using var reader = command.ExecuteReader();

    while (reader.Read())
    {
        wordToGuess = reader.GetString(0);
    }

    if(wordToGuess.Contains(letter))
    {
        string indexes = "";
        for (int i = 0; i < wordToGuess.Length; i++)
        {
            if (wordToGuess[i] == letter)
            {
                indexes += " " + i;
            } 

            answer = "Letter " + letter + " is in the word at index(es) :" + indexes + ".";
        }
    }

    else
    {
        _currentGuesses++;

        if(_currentGuesses >= _maxGuesses)
        {
            command.CommandText =
            """
            UPDATE GAMES SET STATE = 1 WHERE ID = $id;
            """;
            command.Parameters.AddWithValue("$id", id);
            using var reader2 = command.ExecuteReader();
            answer = "Game over! The word was " + wordToGuess + ".";
        }

        else answer = "Letter " + letter + " is not in the word.";
    }

    connection.Close();

    return answer;
});

app.MapPost("/GuessWord", (int id, string word) =>
{
    connection.Open();
    using var command = connection.CreateCommand();
    string wordToGuess = "";
    string answer = "";
    command.CommandText =
    """
    SELECT WORD FROM GAMES WHERE ID = $id;
    """;
    command.Parameters.AddWithValue("$id", id);
    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        wordToGuess = reader.GetString(0);
    }
    if(word == wordToGuess)
    {
        command.CommandText =
        """
        UPDATE GAMES SET STATE = 2 WHERE ID = $id;
        """;
        command.Parameters.AddWithValue("$id", id);
        using var reader2 = command.ExecuteReader();
        answer = "Congratulations! You've guessed the word!";
    }
    else
    {
        _currentGuesses++;
        if (_currentGuesses >= _maxGuesses)
        {
            command.CommandText =
            """
            UPDATE GAMES SET STATE = 1 WHERE ID = $id;
            """;
            command.Parameters.AddWithValue("$id", id);
            using var reader2 = command.ExecuteReader();
            answer = "Game over! The word was " + wordToGuess + ".";
        }
        else answer = "Wrong guess! Try again.";
    }
    connection.Close();
    return answer;
});

app.MapPost("/DeleteGame", (int id) =>
{
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText =
    """
    DELETE FROM GAMES WHERE ID = $id;
    """;
    command.Parameters.AddWithValue("$id", id);
    using var reader = command.ExecuteReader();
    connection.Close();
});

app.MapGet("/GetFinishedGames", () =>
{
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText =
    """
    SELECT COUNT(*) FROM GAMES WHERE STATE != 0;
    """;
    int count = 0;
    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        count = (int)reader.GetInt64(0);
    }
    connection.Close();
    return "You have " + count + " finished games.";
});

app.Run();

