using Godot;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;

public class Player
{
    public string name { get; private set; }     // Player's name, Deck's political current
    public Board PlayerBoard { get; set; }          //Each player has a Board
    public Position2D PositionLeft { get; set; }      // Range where the cards
    public Position2D PositionRight { get; set; }     //  will be displayed
    public Game Instance { get; set; }              //A reference to the game

    public Player(string name, Board PlayerBoard, Position2D left, Position2D right, Game instance)
    {
        this.name = name;
        this.PlayerBoard = PlayerBoard;
        this.PositionLeft = left;
        this.PositionRight = right;
        this.Instance = instance;
    }
    public void Summon(CardSupport CardToSummon, Position2D position)
    {
        CardToSummon.Summoned = true;
        PlayerBoard.HandCards.Remove(CardToSummon);
        PlayerBoard.CardsOnBoard.Add(CardToSummon, position);
        Instance.ActionMessage = $"{name} summoned {CardToSummon.LogicCard.CardName}";
        Instance.MakeSummon(CardToSummon, position);

        Instance.PassTurnPressed = false;
    }
    public void Attack(CardSupport AttackingCard, CardSupport AttackedCard)
    {
        if (AttackingCard.LogicCard is Politic || AttackedCard.LogicCard is Politic)
        {
            Instance.ActionMessage = "Politcs cards cant'be involved in the Battle Phase";
        }
        else
        {
            if (!AttackingCard.HasAttacked)
            {
                if (!PlayerBoard.CardsOnBoard.ContainsKey(AttackedCard))
                {
                    int Attack = AttackingCard.LogicCard.Attack;
                    int Health = AttackedCard.LogicCard.Health;
                    Health -= Attack;
                    AttackedCard.LogicCard.Health = Health;

                    Instance.ActionMessage = $"{AttackedCard.LogicCard.CardName} has received {Attack} damage";
                    if (Health <= 0)
                    {
                        Instance.DestroyCard(AttackedCard);
                    }
                    else
                    {
                        Instance.DestroyCard(AttackingCard);
                    }

                    AttackingCard.UpdateCardVisual();
                    AttackedCard.UpdateCardVisual();

                    AttackingCard.HasAttacked = true;
                    Instance.PassTurnPressed = false;
                }
            }
            else
            {
                Instance.ActionMessage = $"{AttackingCard.LogicCard.CardName} has already attacked";
            }
        }
    }
    public void DrawCards(int AmountToDraw)
    {
        if (PlayerBoard.Deck.Count > 0)
        {
            if (PlayerBoard.Deck.Count < AmountToDraw)
            {
                AmountToDraw = PlayerBoard.Deck.Count;
            }
            for (var i = AmountToDraw - 1; i >= 0; i--)
            {
                PlayerBoard.HandCards.Add(PlayerBoard.Deck[i]);
                Instance.ActionMessage = $"{name} has drawn {PlayerBoard.Deck[i].LogicCard.CardName}";
                PlayerBoard.Deck.RemoveAt(i);
            }
        }
        else
        {
            Instance.ActionMessage = "No more cards to draw";
        }
    }
    public virtual void Play()
    {

    }
}
public interface IAggressiveVirtualPlayer
{
    void PlayVirtualPlayer(); //Virtual Player strategy, it will be an aggressive strategy
    void SortDescendingByLife(List<CardSupport> Cards); //Makes easier to execute the strategy
    void SortAscendingByLife(List<CardSupport> Cards);    //Makes easier to execute the strategy
    void SortDescendingByAttack(List<CardSupport> Cards);   //Makes easier to execute the strategy   
    void SortAscendingByAttack(List<CardSupport> Cards);    //Makes easier to execute the strategy
    void SetPossiblePositions(Player player, List<Position2D> PlayerField, List<Position2D> PossiblePositions); //Check for empty positions on the player's field
    void SummonAllVirtualPlayer(List<Position2D> thisField); //Summon all the cards in the hand

}
public class VirtualPlayer : Player, IAggressiveVirtualPlayer
{
    public VirtualPlayer(string name, Board board, Position2D left, Position2D right, Game Instance) : base(name, board, left, right, Instance)
    {

    }
    public void PlayVirtualPlayer()
    {
        Player Opponent;
        List<Position2D> thisField;
        if (this.name == Game.UserPlayer.name)
        {
            Opponent = Game.EnemyPlayer;
            thisField = Instance.UserPlayerField;
        }
        else
        {
            Opponent = Game.UserPlayer;
            thisField = Instance.EnemyPlayerField;
        }
        //Set the own and opposite player
        if (Instance.GamePhases[Instance.index] is MainPhase1)
        {
            if (this.PlayerBoard.HandCards.Count > 0 && this.PlayerBoard.CardsOnBoard.Count < 8)
            {
                if (Opponent.PlayerBoard.CardsOnBoard.Count > 0)
                {
                    SummonAllVirtualPlayer(thisField);
                }
                else
                {
                    int amounttosummon = 4;
                    if (amounttosummon > this.PlayerBoard.HandCards.Count)
                    {
                        amounttosummon = this.PlayerBoard.HandCards.Count;
                    }
                    SortAscendingByLife(this.PlayerBoard.HandCards);
                    List<Position2D> PossiblePositions = new List<Position2D>();
                    SetPossiblePositions(this, thisField, PossiblePositions);
                    for (int i = amounttosummon - 1; i >= 0 && i < PossiblePositions.Count; i--)
                    {
                        Summon(this.PlayerBoard.HandCards[i], PossiblePositions[i]);
                    }
                }
            }
            for (int i = 0; i < this.PlayerBoard.CardsOnBoard.Count; i++)
            {
                this.PlayerBoard.CardsOnBoard.Keys.ElementAt(i)._on_SelectCard_pressed();
                Instance._on_EffectButton_pressed();
            }
            Instance._on_EndPhase_pressed();
        }
        if (Instance.GamePhases[Instance.index] is ResolvePhase)
        {
            GD.Print("Effect");
        }
        if (Instance.GamePhases[Instance.index] is WaitingForChoosingACardToDoEffect)
        {
            Random rnd = new Random();
            int x = rnd.Next(1, 3);
            if (x == 1)
            {
                if (this.PlayerBoard.CardsOnBoard.Count > 0)
                {
                    Random rnd2 = new Random();
                    int y = rnd2.Next(0, this.PlayerBoard.CardsOnBoard.Count);
                    Game.EffectObjetive = this.PlayerBoard.CardsOnBoard.Keys.ElementAt(y).LogicCard.CardName;
                }
            }
            if (x == 2)
            {
                if (Opponent.PlayerBoard.CardsOnBoard.Count > 0)
                {
                    Random rnd2 = new Random();
                    int y = rnd2.Next(0, Opponent.PlayerBoard.CardsOnBoard.Count);
                    Game.EffectObjetive = Opponent.PlayerBoard.CardsOnBoard.Keys.ElementAt(y).LogicCard.CardName;
                }
            }
        }
        if (Instance.GamePhases[Instance.index] is BattlePhase)
        {
            if (Opponent.PlayerBoard.CardsOnBoard.Count > 0 && this.PlayerBoard.CardsOnBoard.Count > 0)
            {
                List<CardSupport> OpponentCardsOnBoard = new List<CardSupport>();
                foreach (CardSupport Card in Opponent.PlayerBoard.CardsOnBoard.Keys)
                {
                    if (!(Card.LogicCard is Politic))
                        OpponentCardsOnBoard.Add(Card);
                }
                SortAscendingByLife(OpponentCardsOnBoard);
                List<CardSupport> thisCardsOnBoard = new List<CardSupport>();
                foreach (CardSupport Card in this.PlayerBoard.CardsOnBoard.Keys)
                {
                    if (!(Card.LogicCard is Politic))
                    {
                        thisCardsOnBoard.Add(Card);
                    }
                }
                SortAscendingByAttack(thisCardsOnBoard);
                if (OpponentCardsOnBoard.Count > 0 && thisCardsOnBoard.Count > 0)
                {

                    int i = 0;
                    do
                    {
                        CardSupport AttackingCard;
                        if (thisCardsOnBoard[i].LogicCard.Attack > OpponentCardsOnBoard[0].LogicCard.Health)
                        {
                            AttackingCard = thisCardsOnBoard[i];
                        }
                        else
                        {
                            AttackingCard = thisCardsOnBoard[thisCardsOnBoard.Count - 1];
                            i--;
                        }
                        CardSupport AttackedCard = OpponentCardsOnBoard[0];
                        Attack(AttackingCard, AttackedCard);
                        if (Opponent.PlayerBoard.Graveyard.Contains(AttackedCard))
                        {
                            OpponentCardsOnBoard.Remove(AttackedCard);
                        }
                        if (this.PlayerBoard.Graveyard.Contains(AttackingCard))
                        {
                            thisCardsOnBoard.Remove(AttackingCard);
                        }
                        i++;
                    }
                    while (Opponent.PlayerBoard.CardsOnBoard.Count > 0 && i < thisCardsOnBoard.Count && OpponentCardsOnBoard.Count > 0);
                }
            }
            Instance._on_EndPhase_pressed();
        }
        if (Instance.GamePhases[Instance.index] is MainPhase2)
        {
            if (this.PlayerBoard.HandCards.Count > 0 && this.PlayerBoard.CardsOnBoard.Count < 8 && Opponent.PlayerBoard.CardsOnBoard.Count > 0)
            {
                SummonAllVirtualPlayer(thisField);
            }
            Instance._on_EndPhase_pressed();
        }
        if (Instance.GamePhases[Instance.index] is EndRoundPhase)
        {
            Instance._on_NextRound_pressed();
        }
    }
    public void SortDescendingByLife(List<CardSupport> Cards)
    {
        for (int i = 0; i < Cards.Count; i++)
        {
            for (int j = 0; j < Cards.Count; j++)
            {
                if (Cards[i].LogicCard.Health < Cards[j].LogicCard.Health)
                {
                    CardSupport temp = Cards[i];
                    Cards[i] = Cards[j];
                    Cards[j] = temp;
                }
            }
        }
    }
    public void SortAscendingByLife(List<CardSupport> Cards)
    {
        for (int i = 0; i < Cards.Count; i++)
        {
            for (int j = 0; j < Cards.Count; j++)
            {
                if (Cards[i].LogicCard.Health > Cards[j].LogicCard.Health)
                {
                    CardSupport temp = Cards[i];
                    Cards[i] = Cards[j];
                    Cards[j] = temp;
                }
            }
        }
    }
    public void SortDescendingByAttack(List<CardSupport> Cards)
    {
        for (int i = 0; i < Cards.Count; i++)
        {
            for (int j = 0; j < Cards.Count; j++)
            {
                if (Cards[i].LogicCard.Attack < Cards[j].LogicCard.Attack)
                {
                    CardSupport temp = Cards[i];
                    Cards[i] = Cards[j];
                    Cards[j] = temp;
                }
            }
        }
    }
    public void SortAscendingByAttack(List<CardSupport> Cards)
    {
        for (int i = 0; i < Cards.Count; i++)
        {
            for (int j = 0; j < Cards.Count; j++)
            {
                if (Cards[i].LogicCard.Attack > Cards[j].LogicCard.Attack)
                {
                    CardSupport temp = Cards[i];
                    Cards[i] = Cards[j];
                    Cards[j] = temp;
                }
            }
        }
    }
    public void SetPossiblePositions(Player player, List<Position2D> PlayerField, List<Position2D> PossiblePositions)
    {
        for (int i = 0; i < 8; i++)
        {
            if (!player.PlayerBoard.CardsOnBoard.ContainsValue(PlayerField[i]))
            {
                PossiblePositions.Add(PlayerField[i]);
            }
        }
    }
    public void SummonAllVirtualPlayer(List<Position2D> thisField)
    {
        int amounttosummon = Math.Min(8 - this.PlayerBoard.CardsOnBoard.Count, this.PlayerBoard.HandCards.Count);
        SortDescendingByAttack(this.PlayerBoard.HandCards);
        List<Position2D> PossiblePositions = new List<Position2D>();
        SetPossiblePositions(this, thisField, PossiblePositions);
        for (int i = amounttosummon - 1; i >= 0; i--)
        {
            Summon(this.PlayerBoard.HandCards[i], PossiblePositions[i]);
        }
    }
    public override void Play()
    {
        PlayVirtualPlayer();
    }
    void WaitFor3Seconds()
    {

        System.Threading.Thread.Sleep(3000);
    }
}

public class HumanPlayer : Player
{
    public HumanPlayer(string name, Board board, Position2D left, Position2D right, Game Instance) : base(name, board, left, right, Instance)
    {

    }
}