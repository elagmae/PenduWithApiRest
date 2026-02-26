var client = new HttpClient();
var response = await client.GetAsync("https://localhost:17259/");
var body = await response.Content.ReadAsStringAsync();

Console.WriteLine(body);

bool _wantsToPlay = true;

while (_wantsToPlay)
{
    Console.WriteLine("Do you want to keep playing ? Y/N");
    string input = Console.ReadLine();

    while(input != "Y" && input != "N")
    {
        Console.WriteLine("Invalid input, please enter Y or N");
        input = Console.ReadLine();
    }

    if (input == "Y")
    {
        Console.WriteLine("Great ! Let's keep playing !");
        await Game();
    }

    else
    {
        Console.WriteLine("Thank you for playing, see you next time ! Type enter to close the window.");
        Console.ReadLine();
        _wantsToPlay = false;
    }
}

async Task Game()
{
    await Task.Yield();

    string[] actions = new string[] { "GET ACTIVE GAMES", "GET FINISHED GAMES", "CREATE NEW GAME", "GUESS LETTER IN GAME", "GUESS WORD IN GAME", "DELETE GAME" };

    Console.WriteLine($"Here are your possibles actions :");
    for (int i = 0; i < actions.Length; i++)
    {
        Console.WriteLine($"{i + 1} - {actions[i]}");
    }
    string action = Console.ReadLine();
    while(!int.TryParse(action, out int actionNumber) || actionNumber < 1 || actionNumber > actions.Length)
    {
        Console.WriteLine("Invalid action, please enter a showed number.");
        action = Console.ReadLine();
    }

    int actionIndex = int.Parse(action) - 1;

    while (actionIndex >= actions.Length && actionIndex < 0)
    {
        Console.WriteLine("Invalid action, please enter a valid action");
        action = Console.ReadLine();
    }

    HttpResponseMessage response;
    string body;

    switch (actionIndex)
    {
        case 0:
            response = await client.GetAsync("https://localhost:17259/GetGames");
            body = await response.Content.ReadAsStringAsync();
            Console.WriteLine(body);
            break;
        
        case 1:
            response = await client.GetAsync("https://localhost:17259/GetFinishedGames");
            body = await response.Content.ReadAsStringAsync();
            Console.WriteLine(body);
            break;

        case 2:
            await CreateGame();
            break;

        case 3:
            await GuessLetter();
            break;

        case 4:
            await GuessWord();
            break;

        case 5:
            await DeleteGame();
            break;
    }

    #region Post Methods

    async Task CreateGame()
    {
        await Task.Yield();
        Console.WriteLine("Please enter the word you want to be guessed (only letters, no spaces) :");
        string wordToGuess = Console.ReadLine();

        while (string.IsNullOrEmpty(wordToGuess) || !wordToGuess.All(char.IsLetter))
        {
            Console.WriteLine("Invalid word, please enter a valid word (only letters, no spaces) :");
            wordToGuess = Console.ReadLine();
        }

        response = await client.PostAsync($"https://localhost:17259/CreateGame?wordToGuess={wordToGuess}", null);
        body = await response.Content.ReadAsStringAsync();
        Console.WriteLine(body);
    }

    async Task GuessLetter()
    {
        await Task.Yield();
        Console.WriteLine("Please enter the ID of the game you want to play :");
        string id = Console.ReadLine();

        while (!int.TryParse(id, out int gameId) || gameId < 1)
        {
            Console.WriteLine("Invalid ID, please enter a valid ID (positive integer) :");
            id = Console.ReadLine();
        }

        Console.WriteLine("Please enter the letter you want to guess :");
        string letter = Console.ReadLine();
        while (string.IsNullOrEmpty(letter) || !char.IsLetter(letter[0]) || letter.Length > 1)
        {
            Console.WriteLine("Invalid letter, please enter a valid letter (only one letter) :");
            letter = Console.ReadLine();
        }
        response = await client.PostAsync($"https://localhost:17259/GuessLetter?id={id}&letter={letter}", null);
        body = await response.Content.ReadAsStringAsync();
        Console.WriteLine(body);
    }

    async Task GuessWord()
    {
        await Task.Yield();
        Console.WriteLine("Please enter the ID of the game you want to play :");
        string id = Console.ReadLine();
        while (!int.TryParse(id, out int gameId) || gameId < 1)
        {
            Console.WriteLine("Invalid ID, please enter a valid ID (positive integer) :");
            id = Console.ReadLine();
        }
        Console.WriteLine("Please enter the word you want to guess :");
        string word = Console.ReadLine();
        while (string.IsNullOrEmpty(word) || !word.All(char.IsLetter))
        {
            Console.WriteLine("Invalid word, please enter a valid word (only letters, no spaces) :");
            word = Console.ReadLine();
        }
        response = await client.PostAsync($"https://localhost:17259/GuessWord?id={id}&word={word}", null);
        body = await response.Content.ReadAsStringAsync();
        Console.WriteLine(body);
    }

    async Task DeleteGame()
    {
        await Task.Yield();
        Console.WriteLine("Please enter the ID of the game you want to delete :");
        string id = Console.ReadLine();
        while (!int.TryParse(id, out int gameId) || gameId < 1)
        {
            Console.WriteLine("Invalid ID, please enter a valid ID (positive integer) :");
            id = Console.ReadLine();
        }
        response = await client.PostAsync($"https://localhost:17259/DeleteGame?id={id}", null);
        body = await response.Content.ReadAsStringAsync();
        Console.WriteLine(body);
    }

    #endregion

}



