using System.Linq;
using Godot;
using System;
using System.IO;
using System.Collections.Generic;

public class Game : Node2D
{
    public List<GameStates> GamePhases = new List<GameStates>();
    public int index = -1;
    bool StartGame;   //True if Ready button has been pressed
    bool GameReady;         //True if all is ready to play the game
    public bool PassTurnPressed;       //True if Pass Turn button has been pressed
    public bool RoundEnded;
    bool EndGame;      //True if the game has ended      
    bool NextRoundPressed;       //True if Next Round button has been pressed
    Dictionary<string, Player> RoundWinner = new Dictionary<string, Player>();  //Dictionary that contains the winner of each round
    int Round = 1;      //Round number
    public List<Position2D> UserPlayerField = new List<Position2D>();       //List of positions where the user can place his cards
    public List<Position2D> EnemyPlayerField = new List<Position2D>();      //List of positions where the enemy can place his cards
    public static Player UserPlayer;      //User player
    public static Player EnemyPlayer;       //Enemy player
    public string SelectedCardName;     //Name of the selected card
    public bool CardSelected;       //True if a card has been selected
    public static string EffectObjetive;       //Name of the card that will be affected by the effect
    public string ActionMessage;        //Message that will be displayed in the action message label, it will offers information about the game
    public Position2D ClickPosition = new Position2D();    //Position where the mouse was clicked
    public InputEventMouseButton MouseClick;        //Mouse click event
    public CardSupport SelectedCard;        //Selected card
    public bool UserSide = true;        //True if the user is playing
    public bool HumanVsHuman;       //True if the game is human vs human
    public bool HumanVsMachine;     //True if the game is human vs machine
    public bool MachineVsMachine;       //True if the game is machine vs machine
    public bool PrintPhaseMessage;
    bool OnGame;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ActionMessage = "Press Deck to Start";
        Menu.Instance = this;

        SetFields();
        SetInitialStateOfBoard();
    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        GetNode<RichTextLabel>("Board/ActionMessage").Text = ActionMessage;

        if (index != -1)
        {
            if (GamePhases[index] is MainPhase1)
            {
                // Main Phase 1
                if (PrintPhaseMessage)
                {
                    ActionMessage = "Main Phase 1";
                    AddSideMessage();
                    PrintPhaseMessage = false;
                }
                GetNode<Button>("Board/EffectButton").Disabled = false;
                GetNode<Button>("Board/EndPhase").Disabled = false;
                GetNode<Button>("Board/PassTurn").Disabled = false;
                GetNode<Button>("Board/NextRound").Disabled = true;
            }

            if (GamePhases[index] is BattlePhase)
            {
                // Battle Phase
                if (PrintPhaseMessage)
                {
                    ActionMessage = "Battle Phase";
                    PrintPhaseMessage = false;
                    AddSideMessage();
                }
                GetNode<Button>("Board/EffectButton").Disabled = true;
                GetNode<Button>("Board/EndPhase").Disabled = false;
                GetNode<Button>("Board/NextRound").Disabled = true;
                GetNode<Button>("Board/PassTurn").Disabled = false;

            }

            if (GamePhases[index] is MainPhase2)
            {
                // Main Phase2
                if (PrintPhaseMessage)
                {
                    ActionMessage = "Main Phase 2";
                    AddSideMessage();
                    PrintPhaseMessage = false;
                }
                GetNode<Button>("Board/EffectButton").Disabled = false;
                GetNode<Button>("Board/EndPhase").Disabled = false;
                GetNode<Button>("Board/PassTurn").Disabled = false;
                GetNode<Button>("Board/NextRound").Disabled = true;
            }

            if (GamePhases[index] is EffectPhase)
            {
                EffectPhase effectPhase = (EffectPhase)GamePhases[index];

                for (int i = 0; i < effectPhase.effects.Count; i++)
                {
                    Effect effect = effectPhase.effects[i];
                    if (!effect.Used)
                    {
                        if (IsPossibleToUseTheEffect(effect, effectPhase.UserSide))
                        {
                            effect.Used = true;
                            if (effectPhase.effects[i].AutomaticEffect)
                            {
                                EffectObjetive = ChooseForAutomaticEffect(effect);

                                GamePhases.Add(new ResolvePhase(effect, EffectObjetive, effectPhase.UserSide));
                                GamePhases.Add(effectPhase);
                                index++;
                                // _Process(delta);
                            }
                            else
                            {
                                PrintPhaseMessage = true;
                                WaitingForChoosingACardToDoEffect wait = new WaitingForChoosingACardToDoEffect(UserSide, effectPhase.effects[i]);
                                EffectObjetive = null;
                                GamePhases.Add(wait);
                                GamePhases.Add(new ResolvePhase(effect, EffectObjetive, effectPhase.UserSide));
                                GamePhases.Add(effectPhase);
                                index++;
                                // _Process(delta);
                            }
                        }
                    }
                }
                
                if (index + 1 >= GamePhases.Count)
                {
                    effectPhase.CardDoingTheEffect.HasActivatedEffect = true;
                    if (effectPhase.CardDoingTheEffect.LogicCard is Politic)
                    {
                        DestroyCard(effectPhase.CardDoingTheEffect);
                    }
                    if (effectPhase.IsMainPhase1)
                        GamePhases.Add(new MainPhase1(UserSide));
                    else
                        GamePhases.Add(new MainPhase2(UserSide));
                    index++;
                }
            }

            if (GamePhases[index] is WaitingForChoosingACardToDoEffect)
            {
                WaitingForChoosingACardToDoEffect wait = (WaitingForChoosingACardToDoEffect)GamePhases[index];
                if (PrintPhaseMessage)
                {
                    ActionMessage = "Waiting for a selection";
                    PrintPhaseMessage = false;
                }

                if (EffectObjetive != null)
                {
                    if (GamePhases[index + 1] is ResolvePhase)
                    {
                        ResolvePhase resolvePhase = (ResolvePhase)GamePhases[index + 1];
                        resolvePhase.EffectObjetive = EffectObjetive;
                        GamePhases[index + 1] = resolvePhase;
                        index++;
                    }
                }
            }

            if (GamePhases[index] is ResolvePhase)
            {
                // Phase where the effects will take place on the board
                ResolvePhase resolvePhase = (ResolvePhase)GamePhases[index];
                Effect effect = resolvePhase.effect;

                switch (effect.EffectString)
                {
                    case TokenValues.DestroyCard:
                        DestroyCard(GetNode<CardSupport>("Board/" + EffectObjetive));
                        break;
                    case TokenValues.DecreaseHealth:
                        DecreaseHealth(GetNode<CardSupport>("Board/" + EffectObjetive), effect.TempAmount);
                        break;
                    case TokenValues.DecreaseAttack:
                        DecreaseAttack(GetNode<CardSupport>("Board/" + EffectObjetive), effect.TempAmount);
                        break;
                    case TokenValues.IncreaseHealth:
                        IncreaseHealth(GetNode<CardSupport>("Board/" + EffectObjetive), effect.TempAmount);
                        break;
                    case TokenValues.IncreaseAttack:
                        IncreaseAttack(GetNode<CardSupport>("Board/" + EffectObjetive), effect.TempAmount);
                        break;
                    case TokenValues.DrawCards:
                        if (resolvePhase.UserSide) UserPlayer.DrawCards(effect.TempAmount);
                        else EnemyPlayer.DrawCards(effect.TempAmount);
                        break;
                    case TokenValues.AddCardToBoard:
                        if (resolvePhase.UserSide) AddCardToTheBoard(UserPlayer, UserPlayerField, InstanceANewCardSupport(effect.CardToHandle));
                        else AddCardToTheBoard(EnemyPlayer, EnemyPlayerField, InstanceANewCardSupport(effect.CardToHandle));

                        ActionMessage = $"{effect.CardToHandle.CardName} is added to the board";
                        break;
                    case TokenValues.AddCardToDeck:
                        ActionMessage = $"{effect.CardToHandle.CardName} is added to the deck of ";
                        if (resolvePhase.UserSide)
                        {
                            UserPlayer.PlayerBoard.Deck.Add(InstanceANewCardSupport(effect.CardToHandle));
                            ShuffleCards(UserPlayer.PlayerBoard.Deck);
                            ActionMessage += $"{UserPlayer.name}";
                        }
                        else
                        {
                            EnemyPlayer.PlayerBoard.Deck.Add(InstanceANewCardSupport(effect.CardToHandle));
                            ShuffleCards(EnemyPlayer.PlayerBoard.Deck);
                            ActionMessage += $"{EnemyPlayer.name}";
                        }
                        break;
                    default:
                        break;
                }

                effect.Used = true;

                index++;
            }

            if (GamePhases[index] is EndRoundPhase)
            {
                GetNode<Button>("Board/EffectButton").Disabled = true;
                GetNode<Button>("Board/EndPhase").Disabled = true;
                GetNode<Button>("Board/PassTurn").Disabled = true;
                GetNode<Button>("Board/NextRound").Disabled = false;
            }
        }

        if (GameReady)
        {
            PrintCardsinRange(UserPlayer);
            PrintCardsinRange(EnemyPlayer);

            if (OnGame && (UserPlayer.PlayerBoard.HandCards.Count == 0 && UserPlayer.PlayerBoard.CardsOnBoard.Count == 0) ||
            OnGame && (EnemyPlayer.PlayerBoard.HandCards.Count == 0 && EnemyPlayer.PlayerBoard.CardsOnBoard.Count == 0))
            {
                EndRound();
            }
            if (UserSide)
            {
                UserPlayer.Play();
            }
            else
            {
                EnemyPlayer.Play();
            }

        }
    }
    private bool IsPossibleToUseTheEffect(Effect effect, bool userSide)
    {
        switch (effect.EffectString)
        {
            case TokenValues.DestroyCard:
            case TokenValues.DecreaseHealth:
            case TokenValues.DecreaseAttack:
            case TokenValues.IncreaseHealth:
            case TokenValues.IncreaseAttack:
                CardSupport card = SearchRandomCardOnBoard();
                if (card != null)
                {
                    effect.EffectObjetive = card.LogicCard.CardName;
                }
                else return false;
                break;
            case TokenValues.AddCardToBoard:
                return ThereAreSetPossiblePositions(userSide);
            default:
                break;
        }

        return true;
    }
    private bool ThereAreSetPossiblePositions(bool userSide)
    {
        for (int i = 0; i < 8; i++)
        {
            if (userSide)
            {
                if (!UserPlayer.PlayerBoard.CardsOnBoard.ContainsValue(UserPlayerField.ElementAt(i)))
                {
                    return true;
                }
            }
            else
            {
                if (!EnemyPlayer.PlayerBoard.CardsOnBoard.ContainsValue(EnemyPlayerField.ElementAt(i)))
                {
                    return true;
                }
            }
        }
        return false;
    }
    public void AddSideMessage()
    {
        if (UserSide)
            ActionMessage += $"\n{UserPlayer.name} Turn";
        else
            ActionMessage += $"\n{EnemyPlayer.name} Turn";
    }
    private void CreatePlayers()   //Initialize the players
    {
        if (HumanVsHuman)
        {
            UserPlayer = new HumanPlayer(Menu.FinalDeck[0].LogicCard.political_current, new Board(Menu.FinalDeck), GetNode<Position2D>("Board/Position2D17"), GetNode<Position2D>("Board/Position2D18"), this);
            ImageTexture CardTexture = new ImageTexture();
            CardTexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/Card.jpg");
            List<CardSupport> Deck = new List<CardSupport>();
            Deck = MakeDeck(CardTexture, Menu.PathToEnemyDeck, new List<CardSupport>());
            EnemyPlayer = new HumanPlayer(Deck[0].LogicCard.political_current, new Board(Deck), GetNode<Position2D>("Board/Position2D19"), GetNode<Position2D>("Board/Position2D20"), this);
            ShuffleCards(UserPlayer.PlayerBoard.Deck);
            ShuffleCards(EnemyPlayer.PlayerBoard.Deck);
        }
        else if (HumanVsMachine)
        {
            UserPlayer = new HumanPlayer(Menu.FinalDeck[0].LogicCard.political_current, new Board(Menu.FinalDeck), GetNode<Position2D>("Board/Position2D17"), GetNode<Position2D>("Board/Position2D18"), this);
            ImageTexture CardTexture = new ImageTexture();
            CardTexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/Card.jpg");
            List<CardSupport> Deck = new List<CardSupport>();
            Deck = MakeDeck(CardTexture, Menu.PathToEnemyDeck, new List<CardSupport>());
            EnemyPlayer = new VirtualPlayer(Deck[0].LogicCard.political_current, new Board(Deck), GetNode<Position2D>("Board/Position2D19"), GetNode<Position2D>("Board/Position2D20"), this);
            ShuffleCards(UserPlayer.PlayerBoard.Deck);
            ShuffleCards(EnemyPlayer.PlayerBoard.Deck);
        }
        else if (MachineVsMachine)
        {
            UserPlayer = new VirtualPlayer(Menu.FinalDeck[0].LogicCard.political_current, new Board(Menu.FinalDeck), GetNode<Position2D>("Board/Position2D17"), GetNode<Position2D>("Board/Position2D18"), this);
            ImageTexture CardTexture = new ImageTexture();
            CardTexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/Card.jpg");
            List<CardSupport> Deck = new List<CardSupport>();
            Deck = MakeDeck(CardTexture, Menu.PathToEnemyDeck, new List<CardSupport>());
            EnemyPlayer = new VirtualPlayer(Deck[0].LogicCard.political_current, new Board(Deck), GetNode<Position2D>("Board/Position2D19"), GetNode<Position2D>("Board/Position2D20"), this);
            ShuffleCards(UserPlayer.PlayerBoard.Deck);
            ShuffleCards(EnemyPlayer.PlayerBoard.Deck);
        }
    }
    private void Start()        //Each player draw the cards and the game is ready to start
    {
        UserPlayer.DrawCards(8);
        EnemyPlayer.DrawCards(8);
        GamePhases = new List<GameStates>();
        GamePhases.Add(new MainPhase1(UserSide));
        index = 0;
        GameReady = true;
        PrintPhaseMessage = true;
        OnGame = true;
    }
    private bool TheCardIsUnit(string CardName)
    {
        if (GetNode<CardSupport>("Board/" + CardName).LogicCard.cardtype == TokenValues.Politic)
            return false;
        return true;
    }
    public void PrintCardsinRange(Player player)   //Prints the cards in the hand, add the cards to the node tree
    {
        double length = player.PositionRight.Position.x - player.PositionLeft.Position.x;
        double CardWidth = length / player.PlayerBoard.HandCards.Count;
        for (int i = 0; i < player.PlayerBoard.HandCards.Count; i++)
        {
            player.PlayerBoard.HandCards[i].GetNode<MarginContainer>("CardMargin").RectPosition = new Vector2((float)(player.PositionLeft.Position.x + i * CardWidth), player.PositionLeft.Position.y);
            if (!player.PlayerBoard.HandCards[i].HasParent)
            {
                GetNode<Sprite>("Board").AddChild(player.PlayerBoard.HandCards[i], true);
                GetNode<Node2D>("Board/CardSupport").Name = player.PlayerBoard.HandCards[i].LogicCard.CardName;
                player.PlayerBoard.HandCards[i].HasParent = true;
            }
        }
    }
    public void _on_Ready_pressed()
    {
        CreatePlayers();
        StartGame = true;
    }
    public void _on_Deck_pressed()
    {
        if (EndGame)
        {
            GetTree().Quit();
        }
        else
        {
            if (StartGame)
            {
                Start();
                StartGame = false;
            }
            if (NextRoundPressed)
            {
                NextRoundPressed = false;
                Start();
            }
            ActionMessage = "";
        }
    }
    public void MakeSummon(CardSupport CardToSummon, Position2D square) //Changes the card position to the square position where it is summoned
    {
        CardToSummon.GetNode<MarginContainer>("CardMargin").RectPosition = square.Position;
    }
    public void _on_Button_pressed()    //When the button is pressed, it checks if the selected card is in the hand, if it is, it summons it
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (EnemyPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        EnemyPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), EnemyPlayerField[0]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button2_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (EnemyPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        EnemyPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), EnemyPlayerField[1]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button3_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (EnemyPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        EnemyPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), EnemyPlayerField[2]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button4_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (EnemyPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        EnemyPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), EnemyPlayerField[3]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button5_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (EnemyPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        EnemyPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), EnemyPlayerField[4]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button6_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (EnemyPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        EnemyPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), EnemyPlayerField[5]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button7_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (EnemyPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        EnemyPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), EnemyPlayerField[6]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button8_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (EnemyPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        EnemyPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), EnemyPlayerField[7]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button9_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (UserPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        UserPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), UserPlayerField[0]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button10_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (UserPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        UserPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), UserPlayerField[1]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button11_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (UserPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        UserPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), UserPlayerField[2]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button12_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (UserPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        UserPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), UserPlayerField[3]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button13_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (UserPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        UserPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), UserPlayerField[4]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button14_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (UserPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        UserPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), UserPlayerField[5]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button15_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (UserPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        UserPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), UserPlayerField[6]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public void _on_Button16_pressed()
    {
        if ((ButtonList)MouseClick.ButtonIndex == ButtonList.Left)
        {
            CardSupport SelectedCard = GetCardSelected();
            if (SelectedCard != null)
            {
                if (GamePhases[index] is MainPhase1 || GamePhases[index] is MainPhase2)
                {
                    if (UserPlayer.PlayerBoard.HandCards.Contains(SelectedCard))
                    {
                        UserPlayer.Summon(GetNode<CardSupport>("Board/" + SelectedCardName), UserPlayerField[7]);
                    }
                }
            }
            else SelectedCardName = "";
        }
    }
    public List<CardSupport> MakeDeck(ImageTexture CardTexture, string PathToDeck, List<CardSupport> Deck)  //It creates the deck of the player
    {

        DirectoryInfo di = new DirectoryInfo(PathToDeck);
        FileInfo[] CardstoCreate = di.GetFiles();
        if (CardstoCreate.Length == 0)
        {
            throw new ArgumentException("No cards found in the deck");
        }
        CardTemplate[] LogicCards = new CardTemplate[CardstoCreate.Length];
        for (int i = 0; i < CardstoCreate.Length; i++)
        {
            LogicCards[i] = new CardTemplate(CardstoCreate[i].FullName);
            if (LogicCards[i].cardtype == TokenValues.Unit)
            {
                LogicCards[i] = new Unit(LogicCards[i].CardName, LogicCards[i].cardtype, LogicCards[i].Rareness, LogicCards[i].Lore, LogicCards[i].Health, LogicCards[i].Attack, LogicCards[i].political_current, LogicCards[i].PathToPhoto, LogicCards[i].EffectText, LogicCards[i].Effect);
            }
            else
            {
                LogicCards[i] = new Politic(LogicCards[i].CardName, LogicCards[i].cardtype, LogicCards[i].Rareness, LogicCards[i].Lore, LogicCards[i].Health, LogicCards[i].Attack, LogicCards[i].political_current, LogicCards[i].PathToPhoto, LogicCards[i].EffectText, LogicCards[i].Effect);
            }

            PackedScene NewNode = (PackedScene)GD.Load("res://CardSupport.tscn");
            Deck.Add((CardSupport)NewNode.Instance());

            Deck[i].LogicCard = LogicCards[i];
            Deck[i].Instance = this;

            Deck[i].GetNode<Sprite>("CardMargin/BackgroundCard").Texture = CardTexture;
            Deck[i].GetNode<MarginContainer>("CardMargin").RectSize = GetNode<MarginContainer>("Board/CardOnBoardMargin").RectSize;
            Deck[i].MakeCard(CardTexture);
        }
        return Deck;
    }
    public void ChangeSide()    //It changes the current player's turn
    {
        if (GetNode<TextureButton>("Board/EnemyField").Visible)
        {
            GetNode<TextureButton>("Board/EnemyField").Hide();
            GetNode<TextureButton>("Board/UserField").Show();
        }
        else
        {
            GetNode<TextureButton>("Board/EnemyField").Show();
            GetNode<TextureButton>("Board/UserField").Hide();
        }
        if (UserSide)
        {
            UserSide = false;
            ActionMessage = $"{EnemyPlayer.name} Side";
        }
        else
        {
            UserSide = true;
            ActionMessage = $"{UserPlayer.name} Side";
        }
        GamePhases = new List<GameStates>();
        GamePhases.Add(new MainPhase1(UserSide));
        index = 0;
    }
    public void DestroyCard(CardSupport Card)       //It destroys the card and sends it to the graveyard
    {
        Card.GetNode<MarginContainer>("CardMargin").RectPosition = GetNode<Position2D>("Board/Position2D18").Position;
        if (UserPlayer.name == Card.LogicCard.political_current)
        {
            UserPlayer.PlayerBoard.Graveyard.Add(Card);
            UserPlayer.PlayerBoard.CardsOnBoard.Remove(Card);
            Card.Summoned = false;
        }
        else
        {
            EnemyPlayer.PlayerBoard.Graveyard.Add(Card);
            EnemyPlayer.PlayerBoard.CardsOnBoard.Remove(Card);
            Card.Summoned = false;
        }
        ActionMessage = $"{Card.LogicCard.CardName} Destroyed";
        // AddSideMessage();
    }
    public void _on_EndPhase_pressed()
    {
        if (GamePhases[index] is MainPhase1)
        {
            GamePhases.Add(new BattlePhase(UserSide));
            index++;
        }
        else if (GamePhases[index] is BattlePhase)
        {
            GamePhases.Add(new MainPhase2(UserSide));
            index++;
        }

        else if (GamePhases[index] is MainPhase2)
        {
            UpdateCardStatus();
            ChangeSide();
        }

        PrintPhaseMessage = true;
    }
    public void UpdateCardStatus()  //It reset the HasAttacked and HasActivatedEffect conditions of the cards 
    {
        Player Player;
        if (UserSide)
        {
            Player = UserPlayer;
        }
        else
        {
            Player = EnemyPlayer;
        }
        foreach (CardSupport Card in Player.PlayerBoard.CardsOnBoard.Keys)
        {
            if (Card.HasAttacked)
                Card.HasAttacked = false;
            if (Card.HasActivatedEffect)
                Card.HasActivatedEffect = false;
        }
    }
    public void _on_PassTurn_pressed()  //If it is pressed twice (one by each player or two by the same player), it ends the round
    {
        if (PassTurnPressed)
        {
            EndRound();
        }
        else
        {
            PassTurnPressed = true;
            UpdateCardStatus();
            ChangeSide();
        }
    }
    public void EndRound()  //It ends the round and declares the round winner, if it is necessary, it declares the game winner
    {
        OnGame = false;
        int UserPlayerPoints = 0;
        int EnemyPlayerPoints = 0;
        foreach (CardSupport Card in UserPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            UserPlayerPoints += Card.LogicCard.Health;
        }
        foreach (CardSupport Card in EnemyPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            EnemyPlayerPoints += Card.LogicCard.Health;
        }
        if (UserPlayerPoints > EnemyPlayerPoints)
        {
            DeclareRoundWinner(UserPlayer);
        }
        else if (UserPlayerPoints < EnemyPlayerPoints)
        {
            DeclareRoundWinner(EnemyPlayer);
        }
        else if (PassTurnPressed)
        {
            ActionMessage = "Tie";
            PassTurnPressed = false;
        }
        else
        {
            if (UserPlayer.PlayerBoard.HandCards.Count > 0)
            {
                DeclareRoundWinner(UserPlayer);
            }
            else if (EnemyPlayer.PlayerBoard.HandCards.Count > 0)
            {
                DeclareRoundWinner(EnemyPlayer);
            }
            else
            {
                ActionMessage = "Tie";
                AddSideMessage();
            }
        }
        if (!EndGame)
        {
            GetNode<Button>("Board/NextRound").Disabled = false;
        }

        GamePhases = new List<GameStates>();
        GamePhases.Add(new EndRoundPhase(UserSide));
        index = 0;

        // RoundEnded = true;
    }
    public void _on_NextRound_pressed()   //It cleans the board (destroy summoned cards) and starts the next round
    {
        for (int i = UserPlayer.PlayerBoard.CardsOnBoard.Keys.Count - 1; i >= 0; i--)
        {
            DestroyCard(UserPlayer.PlayerBoard.CardsOnBoard.Keys.ElementAt(i));
        }
        for (int i = EnemyPlayer.PlayerBoard.CardsOnBoard.Keys.Count - 1; i >= 0; i--)
        {
            DestroyCard(EnemyPlayer.PlayerBoard.CardsOnBoard.Keys.ElementAt(i));
        }
        ActionMessage = "Press Deck";
        if (Round == 2) GetNode<RichTextLabel>("Board/Round").Text = "Second Round";
        else if (Round == 3) GetNode<RichTextLabel>("Board/Round").Text = "Third Round";

        NextRoundPressed = true;
    }
    public void DeclareWinner(Player player)  //It declares the game winner
    {
        _on_NextRound_pressed();
        EndGame = true;
        GetNode<Button>("Board/NextRound").Disabled = true;
        GetNode<RichTextLabel>("Board/GameWinner").Text = player.name + " Wins the Game";
        GetNode<RichTextLabel>("Board/GameWinner").Show();
        ActionMessage = "Press Deck to Exit";
    }
    public void DeclareRoundWinner(Player player)       //It declares the round winner
    {
        if (Round != 3)
        {
            ActionMessage = player.name + " Wins this Round";
            if (Round == 1) RoundWinner.Add("First", player);
            else
            {
                RoundWinner.Add("Second", player);
                if (RoundWinner["First"].name == player.name)
                {
                    DeclareWinner(player);
                }
            }
            Round++;
        }
        else
        {
            DeclareWinner(player);
        }
    }
    public void ShuffleCards(List<CardSupport> Cards)     //It shuffles the cards
    {
        Random rnd = new Random();
        for (int i = Cards.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            CardSupport temp = Cards[j];
            Cards[j] = Cards[i];
            Cards[i] = temp;
        }
    }
    public void _on_EffectButton_pressed()
    {
        PassTurnPressed = false;

        if (SelectedCard.CardPressed)
        {
            bool IsMainPhase1;
            if (GamePhases[index] is MainPhase1) IsMainPhase1 = true;
            else IsMainPhase1 = false;

            if ((UserSide && GetNode<CardSupport>("Board/" + SelectedCardName).LogicCard.political_current == UserPlayer.name) || (!UserSide && GetNode<CardSupport>("Board/" + SelectedCardName).LogicCard.political_current == EnemyPlayer.name))
            {
                if (!GetNode<CardSupport>("Board/" + SelectedCardName).HasActivatedEffect)
                {
                    EffectPhase effectPhase = new EffectPhase(GetNode<CardSupport>("Board/" + SelectedCardName), UserSide, IsMainPhase1);
                    CardSelected = false;

                    effectPhase.CreateEffects();
                    if (effectPhase.effects.Count > 0)
                    {
                        GamePhases.Add(effectPhase);
                        index++;
                    }
                    else
                    {
                        if (GetNode<CardSupport>("Board/" + SelectedCardName).LogicCard is Politic)
                        {
                            DestroyCard(GetNode<CardSupport>("Board/" + SelectedCardName));
                        }
                        ActionMessage = SelectedCardName + " doesn't have an effect";
                        AddSideMessage();
                    }
                }
                else
                {
                    ActionMessage = SelectedCardName + " has already activated effect";
                    AddSideMessage();
                }
            }
        }
    }
    private void CleanBoardPolitics()
    {
        List<CardSupport> ToDestroy = new List<CardSupport>();
        foreach (CardSupport cardSupport in UserPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard.cardtype == TokenValues.Politic && cardSupport.HasActivatedEffect)
                ToDestroy.Add(cardSupport);
        }
        foreach (CardSupport cardSupport in EnemyPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard.cardtype == TokenValues.Politic && cardSupport.HasActivatedEffect)
                ToDestroy.Add(cardSupport);
        }

        foreach (CardSupport cardSupport in ToDestroy)
            DestroyCard(cardSupport);
    }
    private CardSupport InstanceANewCardSupport(CardTemplate cardTemplate)
    {
        PackedScene NewNode = (PackedScene)GD.Load("res://CardSupport.tscn");
        CardSupport newcard = (CardSupport)NewNode.Instance();

        ImageTexture CardTexture = new ImageTexture();
        CardTexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/Card.jpg");

        newcard.GetNode<Sprite>("CardMargin/BackgroundCard").Texture = CardTexture;
        newcard.GetNode<MarginContainer>("CardMargin").RectSize = GetNode<MarginContainer>("Board/CardOnBoardMargin").RectSize;

        newcard.LogicCard = cardTemplate;
        newcard.Instance = this;

        newcard.Show();

        newcard.MakeCard(CardTexture);

        return newcard;
    }
    private void AddCardToTheBoard(Player player, List<Position2D> PlayerField, CardSupport newcard)
    {
        for (int i = 0; i < 8; i++)
        {
            if (!player.PlayerBoard.CardsOnBoard.ContainsValue(PlayerField.ElementAt(i)))
            {
                GetNode<Sprite>("Board").AddChild(newcard, true);
                GetNode<Node2D>("Board/CardSupport").Name = newcard.LogicCard.CardName;
                newcard.HasParent = true;
                player.PlayerBoard.HandCards.Add(newcard);
                player.Summon(newcard, PlayerField[i]);
                break;
            }
        }
        newcard.Show();
        // The card is on the board
    }
    public bool ThereIsSpaceInTheBoard(Player player, List<Position2D> PlayerField)
    {
        for (int i = 0; i < 8; i++)
        {
            if (!player.PlayerBoard.CardsOnBoard.ContainsValue(PlayerField.ElementAt(i)))
            {
                return true;
            }
        }
        return false;
    }
    public void DecreaseHealth(CardSupport Card, int Amount)
    {
        Card.LogicCard.Health -= Amount;
        ActionMessage = Card.LogicCard.CardName + " has lost " + Amount + " of points of health";
    }
    public void DecreaseAttack(CardSupport Card, int Amount)
    {
        Card.LogicCard.Attack -= Amount;
        ActionMessage = Card.LogicCard.CardName + " has lost " + Amount + " of points of attack";
    }
    public void IncreaseHealth(CardSupport Card, int Amount)
    {

        Card.LogicCard.Health += Amount;
        ActionMessage = Card.LogicCard.CardName + " has gain " + Amount + " of points of health";
    }
    public void IncreaseAttack(CardSupport Card, int Amount)
    {
        Card.LogicCard.Attack += Amount;
        ActionMessage = Card.LogicCard.CardName + " has gain " + Amount + " of points of attack";
    }
    private string ChooseForAutomaticEffect(Effect effect)
    {
        CardSupport cardSupport = SearchRandomCardOnBoard();
        string TempObjective = cardSupport.LogicCard.CardName;
        switch (effect.EffectConditional)
        {
            case TokenValues.minHealth:
                TempObjective = minHealth(TempObjective);
                break;
            case TokenValues.minAttack:
                TempObjective = minAttack(TempObjective);
                break;
            case TokenValues.maxHealth:
                TempObjective = maxHealth(TempObjective);
                break;
            case TokenValues.maxAttack:
                TempObjective = maxAttack(TempObjective);
                break;
            default:
                break;
        }

        return TempObjective;
    }
    public string minHealth(string EffectObjetive)
    {
        string effobj = "";
        foreach (CardSupport cardSupport in EnemyPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard is Unit)
            {
                if (cardSupport.LogicCard.Health <= GetNode<CardSupport>("Board/" + EffectObjetive).LogicCard.Health)
                {
                    effobj = cardSupport.LogicCard.CardName;
                }
            }
        }

        foreach (CardSupport cardSupport in UserPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard is Unit)
                if (cardSupport.LogicCard.Health <= GetNode<CardSupport>("Board/" + EffectObjetive).LogicCard.Health)
                {
                    effobj = cardSupport.LogicCard.CardName;
                }

        }

        return effobj;
    }
    public string minAttack(string EffectObjetive)
    {
        string effobj = "";
        foreach (CardSupport cardSupport in EnemyPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard is Unit)
                if (cardSupport.LogicCard.Attack <= GetNode<CardSupport>("Board/" + EffectObjetive).LogicCard.Attack && TheCardIsUnit(cardSupport.LogicCard.CardName))
                {
                    effobj = cardSupport.LogicCard.CardName;
                }

        }

        foreach (CardSupport cardSupport in UserPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard is Unit)
                if (cardSupport.LogicCard.Attack <= GetNode<CardSupport>("Board/" + EffectObjetive).LogicCard.Attack && TheCardIsUnit(cardSupport.LogicCard.CardName))
                {
                    effobj = cardSupport.LogicCard.CardName;
                }
        }

        return effobj;
    }
    public string maxHealth(string EffectObjetive)
    {
        string effobj = "";
        foreach (CardSupport cardSupport in EnemyPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard is Unit)
                if (cardSupport.LogicCard.Health >= GetNode<CardSupport>("Board/" + EffectObjetive).LogicCard.Health && TheCardIsUnit(cardSupport.LogicCard.CardName))
                {
                    effobj = cardSupport.LogicCard.CardName;
                }
        }

        foreach (CardSupport cardSupport in UserPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard is Unit)
                if (cardSupport.LogicCard.Health >= GetNode<CardSupport>("Board/" + EffectObjetive).LogicCard.Health && TheCardIsUnit(cardSupport.LogicCard.CardName))
                {
                    effobj = cardSupport.LogicCard.CardName;
                }
        }

        return effobj;
    }
    public string maxAttack(string EffectObjetive)
    {
        string effobj = "";
        foreach (CardSupport cardSupport in EnemyPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard is Unit)
                if (cardSupport.LogicCard.Attack >= GetNode<CardSupport>("Board/" + EffectObjetive).LogicCard.Attack && TheCardIsUnit(cardSupport.LogicCard.CardName))
                {
                    effobj = cardSupport.LogicCard.CardName;
                }
        }

        foreach (CardSupport cardSupport in UserPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard is Unit)
                if (cardSupport.LogicCard.Attack >= GetNode<CardSupport>("Board/" + EffectObjetive).LogicCard.Attack && TheCardIsUnit(cardSupport.LogicCard.CardName))
                {
                    effobj = cardSupport.LogicCard.CardName;
                }
        }

        return effobj;
    }
    public CardSupport SearchRandomCardOnBoard()
    {
        foreach (CardSupport cardSupport in EnemyPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard is Unit)
            {
                return cardSupport;
            }
        }
        foreach (CardSupport cardSupport in UserPlayer.PlayerBoard.CardsOnBoard.Keys)
        {
            if (cardSupport.LogicCard is Unit)
            {
                return cardSupport;
            }
        }

        return null;
    }
    public override void _Input(InputEvent inputEvent)  //This method receives all the input events
    {
        if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        //If the event is a mouse button event and the button was pressed
        {
            switch ((ButtonList)mouseEvent.ButtonIndex)
            {
                case ButtonList.Left:
                    MouseClick = mouseEvent;
                    ClickPosition.Position = mouseEvent.Position;
                    break;
                case ButtonList.Right:
                    MouseClick = mouseEvent;
                    ClickPosition.Position = mouseEvent.Position;
                    break;
            }
        }
    }
    public CardSupport GetCardSelected()    //This method returns the card that was selected
    {
        CardSupport Card;
        Card = Checkifselectedcard(UserPlayer.PlayerBoard.CardsOnBoard);
        if (Card != null)
        {
            SelectedCard = Card;
            return Card;
        }
        Card = Checkifselectedcard(EnemyPlayer.PlayerBoard.CardsOnBoard);
        if (Card != null)
        {
            SelectedCard = Card;
            return Card;
        }
        Card = Checkifselectedcard(UserPlayer.PlayerBoard.HandCards);
        if (Card != null)
        {
            SelectedCard = Card;
            return Card;
        }
        Card = Checkifselectedcard(EnemyPlayer.PlayerBoard.HandCards);
        if (Card != null)
        {
            SelectedCard = Card;
            return Card;
        }
        SelectedCard = null;
        return null;
    }
    private CardSupport Checkifselectedcard(List<CardSupport> Cards)        //This method checks if a card was selected on a List
    {
        foreach (CardSupport card in Cards)
        {
            if (card.CardPressed)
            {
                return card;
            }
        }
        return null;
    }
    private CardSupport Checkifselectedcard(Dictionary<CardSupport, Position2D> Cards)     //This method checks if a card was selected on a Dictionary
    {
        foreach (CardSupport card in Cards.Keys)
        {
            if (card.CardPressed)
            {
                return card;
            }
        }
        return null;
    }
    public void NonCardPressed()
    {
        CardSupport Card = GetCardSelected();
        if (Card != null)
            Card.CardPressed = false;
    }
    private void SetFields()        //This method sets the fields of the players
    {
        UserPlayerField.Add(GetNode<Position2D>("Board/Position2D9"));
        UserPlayerField.Add(GetNode<Position2D>("Board/Position2D10"));
        UserPlayerField.Add(GetNode<Position2D>("Board/Position2D11"));
        UserPlayerField.Add(GetNode<Position2D>("Board/Position2D12"));
        UserPlayerField.Add(GetNode<Position2D>("Board/Position2D13"));
        UserPlayerField.Add(GetNode<Position2D>("Board/Position2D14"));
        UserPlayerField.Add(GetNode<Position2D>("Board/Position2D15"));
        UserPlayerField.Add(GetNode<Position2D>("Board/Position2D16"));

        EnemyPlayerField.Add(GetNode<Position2D>("Board/Position2D"));
        EnemyPlayerField.Add(GetNode<Position2D>("Board/Position2D2"));
        EnemyPlayerField.Add(GetNode<Position2D>("Board/Position2D3"));
        EnemyPlayerField.Add(GetNode<Position2D>("Board/Position2D4"));
        EnemyPlayerField.Add(GetNode<Position2D>("Board/Position2D5"));
        EnemyPlayerField.Add(GetNode<Position2D>("Board/Position2D6"));
        EnemyPlayerField.Add(GetNode<Position2D>("Board/Position2D7"));
        EnemyPlayerField.Add(GetNode<Position2D>("Board/Position2D8"));
    }
    private void SetInitialStateOfBoard()       //This method sets the initial state of the board, the default statuses of the buttons and the show card visual
    {
        ImageTexture CardTexture = new ImageTexture();
        CardTexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/Card.jpg");
        GetNode<RichTextLabel>("Board/GameWinner").Hide();

        GetNode<Button>("Board/EffectButton").Disabled = true;
        GetNode<Button>("Board/EndPhase").Disabled = true;
        GetNode<Button>("Board/PassTurn").Disabled = true;
        GetNode<Button>("Board/NextRound").Disabled = true;

        GetNode<TextureButton>("Board/EnemyField").Show();
        GetNode<TextureButton>("Board/UserField").Hide();

        GetNode<Sprite>("Board/ShowMargin/BackgroundCard").Position = new Vector2(GetNode<MarginContainer>("Board/ShowMargin").RectSize.x / 2, GetNode<MarginContainer>("Board/ShowMargin").RectSize.y / 2);
        GetNode<Sprite>("Board/ShowMargin/BackgroundCard").Texture = CardTexture;
        GetNode<Sprite>("Board/ShowMargin/BackgroundCard").Scale = GetNode<MarginContainer>("Board/ShowMargin").RectSize / CardTexture.GetSize();

        var PhotoCardTexture = new ImageTexture();
        PhotoCardTexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/foto-perfil-generica.jpg");
        GetNode<Sprite>("Board/ShowMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Texture = PhotoCardTexture;
        GetNode<Sprite>("Board/ShowMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Scale = GetNode<MarginContainer>("Board/ShowMargin/BackgroundCard/PhotoCardMargin").RectSize / PhotoCardTexture.GetSize();
    }
}