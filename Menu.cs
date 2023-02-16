using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Godot;
using System;
public class Menu : Node2D
{
    // Declare member variables here. Examples:
    bool SelectedDeck;  //True if the user has selected a deck
    string PathToUserDeck;   //Path to the user deck
    public static string PathToEnemyDeck;   //Path to the enemy deck
    public static List<CardSupport> FinalDeck;   //Final deck of the user
    int CurrentCardIndex;   //Index of the current card to be selected or not
    List<CardSupport> Deck;   //All the cards in the deck selected by the user
    int NumberOfCardsSelected;      //Number of cards selected by the user
    bool PlayGamePressed;       //True if the user has pressed the Play Game button
    public string[] DeckNames;      //Names of the decks 
    int UserDeck;       //Index of the user deck selected
    int EnemyDeck;      //Index of the enemy deck selected
    public static Game Instance;        //Game object to call the Game class mathods
    int Amount;     //Amount of cards to be selected by the user
    public override void _Ready()
    {
        GetNode<Button>("SelectGameMode/Ready").Disabled = true;
        GetNode<Button>("SelectDeck/SelectCards").Disabled = true;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (PlayGamePressed)
        {
            PlayGamePressed = false;
            GenerateDecksList();
        }

        if (int.TryParse(GetNode<TextEdit>("SelectDeck/UserDeck").Text, out int n))
            UserDeck = n;
        if (int.TryParse(GetNode<TextEdit>("SelectDeck/EnemyDeck").Text, out int m))
            EnemyDeck = m;


        if (UserDeck > 0 &&
            UserDeck <= DeckNames.Length &&
            EnemyDeck > 0 &&
            EnemyDeck <= DeckNames.Length &&
            UserDeck != EnemyDeck)
        {
            GetNode<Button>("SelectDeck/SelectCards").Disabled = false;
        }
        else
        {
            GetNode<Button>("SelectDeck/SelectCards").Disabled = true;
        }

        if (SelectedDeck)
        {
            SelectedDeck = false;
            GenerateUserCards();
            GetNode<RichTextLabel>("SelectCards/MinimumCards").Text = $"Select a minimum of {Amount} cards";
        }

        GetNode<RichTextLabel>("SelectCards/NumberSelected").Text = $"{NumberOfCardsSelected}";

        if (NumberOfCardsSelected >= Amount)
        {
            GetNode<Button>("SelectCards/GameMode").Disabled = false;
        }
        else
        {
            GetNode<Button>("SelectCards/GameMode").Disabled = true;
        }
        // GetNode<Button>("SelectCards/Ready").Disabled = false;     


    }
    public void _on_PlayGame_pressed()
    {
        ReadCode();
        GetNode<Node2D>("MainMenu").Hide();
        GetNode<Node2D>("SelectDeck").Show();
        PlayGamePressed = true;

    }
    public void _on_CreateCards_pressed()
    {
        ProcessStartInfo psi = new ProcessStartInfo("code.txt");
        psi.UseShellExecute = true;
        Process.Start(psi);
        //Show the source code file
    }
    public void _on_Exit_pressed()
    {
        GetTree().Quit();
    }
    public void _on_ReturnToMainMenu_pressed()
    {
        UserDeck = 0;
        EnemyDeck = 0;
        GetNode<TextEdit>("SelectDeck/UserDeck").Text = "";
        GetNode<TextEdit>("SelectDeck/EnemyDeck").Text = "";
        GetNode<Button>("SelectDeck/SelectCards").Disabled = true;
        //Restore default values
        GetNode<Node2D>("SelectDeck").Hide();
        GetNode<Node2D>("MainMenu").Show();
    }
    public void _on_SelectCards_pressed()
    {
        PathToUserDeck = System.IO.Directory.GetCurrentDirectory() + "/Decks/" + DeckNames[UserDeck - 1] + "/";
        PathToEnemyDeck = System.IO.Directory.GetCurrentDirectory() + "/Decks/" + DeckNames[EnemyDeck - 1] + "/";
        SelectedDeck = true;
        GetNode<Node2D>("SelectDeck").Hide();
        GetNode<Node2D>("SelectCards").Show();
    }
    public void _on_ReturnToDeckSelector_pressed()
    {
        Deck.Clear();
        FinalDeck.Clear();
        CurrentCardIndex = 0;
        NumberOfCardsSelected = 0;
        GetNode<Button>("SelectCards/GameMode").Disabled = true;
        //Restore default values
        GetNode<Node2D>("SelectCards").Hide();
        GetNode<Node2D>("SelectDeck").Show();
    }
    public void _on_Previous_pressed()  //Show the previous card
    {
        if (CurrentCardIndex > 0)
        {
            CurrentCardIndex--;
            ShowCardsForSelection();
        }
    }
    public void _on_Next_pressed()  //Show the next card
    {
        if (CurrentCardIndex < Deck.Count - 1)
        {
            CurrentCardIndex++;
            ShowCardsForSelection();
        }
    }
    public void _on_SelectThisCard_pressed()  //Select the current card
    {
        if (!FinalDeck.Contains(Deck[CurrentCardIndex]))
        {
            FinalDeck.Add(Deck[CurrentCardIndex]);
            NumberOfCardsSelected++;
        }
    }
    public void _on_Random_pressed()
    {
        Random(Amount);
    }
    void Random(int Amount)     //Select random cards
    {
        Instance.ShuffleCards(Deck);
        for (int i = 0; i < Amount; i++)
        {
            CurrentCardIndex = i;
            _on_SelectThisCard_pressed();
        }
    }
    public void ShowCardsForSelection()  //Show the current card
    {
        var CardTexture = new ImageTexture();
        CardTexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/Card.jpg");
        GetNode<Sprite>("SelectCards/CardforSelection/BackgroundCard").Position = new Vector2(GetNode<MarginContainer>("SelectCards/CardforSelection").RectSize.x / 2, GetNode<MarginContainer>("SelectCards/CardforSelection").RectSize.y / 2);
        GetNode<Sprite>("SelectCards/CardforSelection/BackgroundCard").Texture = CardTexture;
        GetNode<Sprite>("SelectCards/CardforSelection/BackgroundCard").Scale = GetNode<MarginContainer>("SelectCards/CardforSelection").RectSize / CardTexture.GetSize();


        GetNode<RichTextLabel>("SelectCards/CardforSelection/BackgroundCard/Name").Text = Deck[CurrentCardIndex].LogicCard.CardName;
        GetNode<RichTextLabel>("SelectCards/CardforSelection/BackgroundCard/Effect").Text = Deck[CurrentCardIndex].LogicCard.EffectText;
        GetNode<RichTextLabel>("SelectCards/CardforSelection/BackgroundCard/Lore").Text = Deck[CurrentCardIndex].LogicCard.Lore;
        if (Deck[CurrentCardIndex].LogicCard is Unit)
        {
            GetNode<RichTextLabel>("SelectCards/CardforSelection/BackgroundCard/Attack").Text = $"{Deck[CurrentCardIndex].LogicCard.Attack}";
            GetNode<RichTextLabel>("SelectCards/CardforSelection/BackgroundCard/Life").Text = $"{Deck[CurrentCardIndex].LogicCard.Health}";
        }
        else
        {
            GetNode<RichTextLabel>("SelectCards/CardforSelection/BackgroundCard/Attack").Text = "-";
            GetNode<RichTextLabel>("SelectCards/CardforSelection/BackgroundCard/Life").Text = "-";
        }
        GetNode<RichTextLabel>("SelectCards/CardforSelection/BackgroundCard/ClassCard").Text = Deck[CurrentCardIndex].LogicCard.political_current;
        GetNode<Sprite>("SelectCards/CardforSelection/BackgroundCard/TypeMargin/TypePhoto").Texture = Deck[CurrentCardIndex].GetNode<Sprite>("CardMargin/BackgroundCard/TypeMargin/TypePhoto").Texture;
        GetNode<Sprite>("SelectCards/CardforSelection/BackgroundCard/TypeMargin/TypePhoto").Scale = GetNode<MarginContainer>("SelectCards/CardforSelection/BackgroundCard/TypeMargin").RectSize / Deck[CurrentCardIndex].GetNode<Sprite>("CardMargin/BackgroundCard/TypeMargin/TypePhoto").Texture.GetSize();
        GetNode<Sprite>("SelectCards/CardforSelection/BackgroundCard/RarenessMargin/RarenessPhoto").Texture = Deck[CurrentCardIndex].GetNode<Sprite>("CardMargin/BackgroundCard/RarenessMargin/RarenessPhoto").Texture;
        GetNode<Sprite>("SelectCards/CardforSelection/BackgroundCard/RarenessMargin/RarenessPhoto").Scale = GetNode<MarginContainer>("SelectCards/CardforSelection/BackgroundCard/RarenessMargin").RectSize / Deck[CurrentCardIndex].GetNode<Sprite>("CardMargin/BackgroundCard/RarenessMargin/RarenessPhoto").Texture.GetSize();
        GetNode<Sprite>("SelectCards/CardforSelection/BackgroundCard/PhotoCardMargin/PhotoCard").Texture = Deck[CurrentCardIndex].GetNode<Sprite>("CardMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Texture;
        GetNode<Sprite>("SelectCards/CardforSelection/BackgroundCard/PhotoCardMargin/PhotoCard").Scale = GetNode<MarginContainer>("SelectCards/CardforSelection/BackgroundCard/PhotoCardMargin").RectSize / Deck[CurrentCardIndex].GetNode<Sprite>("CardMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Texture.GetSize();
    }
    public void _on_GameMode_pressed()
    {
        GetNode<Node2D>("SelectCards").Hide();
        GetNode<Node2D>("SelectGameMode").Show();
    }
    public void _on_Ready_pressed()  //Start the game
    {
        GetNode<Node2D>("SelectCards").Hide();
        GetNode<Node2D>("/root/Main/Game").Show();
    }
    public void _on_ReturnToCardSelector_pressed()
    {
        GetNode<Button>("SelectGameMode/Ready").Disabled = true;
        Instance.HumanVsHuman = false;
        Instance.HumanVsMachine = false;
        Instance.MachineVsMachine = false;
        //Restore default values
        GetNode<Node2D>("SelectGameMode").Hide();
        GetNode<Node2D>("SelectCards").Show();
    }
    public void _on_HumanvsHuman_pressed()
    {
        GetNode<Button>("SelectGameMode/Ready").Disabled = false;
        Instance.HumanVsMachine = false;
        Instance.MachineVsMachine = false;
        Instance.HumanVsHuman = true;
    }
    public void _on_HumanvsMachine_pressed()
    {
        GetNode<Button>("SelectGameMode/Ready").Disabled = false;
        Instance.HumanVsHuman = false;
        Instance.MachineVsMachine = false;
        Instance.HumanVsMachine = true;
    }
    public void _on_MachinevsMachine_pressed()
    {
        GetNode<Button>("SelectGameMode/Ready").Disabled = false;
        Instance.HumanVsHuman = false;
        Instance.HumanVsMachine = false;
        Instance.MachineVsMachine = true;
    }
    private string[] GetFoldersNames(string[] Paths)  //Get the Deck's names
    {
        string[] names = new string[Paths.Length];
        for (int i = 0; i < Paths.Length; i++)
        {
            string[] temp = Paths[i].Split('/');
            names[i] = temp[temp.Length - 1];
        }
        return names;
    }
    public void ReadCode()   //Read and parse the source code 
    {
        LexicalAnalyzer lex = Compiling.Lexical;

        string text = System.IO.File.ReadAllText(@"code.txt");

        IEnumerable<Token> tokens = lex.GetTokens("code", text, new List<CompilingError>());

        TokenStream stream = new TokenStream(tokens);
        Parser parser = new Parser(stream);
        List<CompilingError> errors = new List<CompilingError>();

        ColdWarProgram program = parser.ParseProgram(errors);

        if (errors.Count > 0)
        {
            foreach (CompilingError error in errors)
            {
                GD.Print(error.Location.Line, " ", error.Code, " ", error.Argument);
            }
        }
        else
        {
            Context context = new Context();
            Scope scope = new Scope();

            program.CheckSemantic(context, scope, errors);

            if (errors.Count > 0)
            {
                foreach (CompilingError error in errors)
                {
                    GD.Print(error.Location.Line, " ", error.Code, " ", error.Argument);
                }
            }
            else
            {
                program.Evaluate();

                foreach (Card ncard in program.Cards.Values)
                {
                    ncard.AddToTheDeckAsCardTemplate();
                }
            }
        }
    }
    public void GenerateUserCards()     //Generate the cards for the user's deck
    {
        Deck = new List<CardSupport>();
        var CardTexture = new ImageTexture();
        CardTexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/Card.jpg");
        string a = PathToUserDeck;
        Deck = Instance.MakeDeck(CardTexture, PathToUserDeck, new List<CardSupport>());
        FinalDeck = new List<CardSupport>();
        ShowCardsForSelection();
        if (Deck.Count < 24) Amount = Deck.Count;
        else Amount = 24;
    }
    public void GenerateDecksList()   //Generate the list of decks
    {
        string[] paths = System.IO.Directory.GetDirectories(System.IO.Directory.GetCurrentDirectory() + "/Decks");
        DeckNames = GetFoldersNames(paths);
        string ListOfNames = "";
        for (int i = 0; i < DeckNames.Length; i++)
        {
            ListOfNames += $"{i + 1} - {DeckNames[i]} \n";
        }
        GetNode<RichTextLabel>("SelectDeck/Decks").Text = ListOfNames;
    }
}