using Godot;
using System;
using System.Collections.Generic;

public abstract class GameStates
{
    public bool UserSide{get; protected set;}
    public GameStates(bool UserSide)
    {
        this.UserSide = UserSide;
    }
}

public class MainPhase1 : GameStates
{
    public MainPhase1(bool UserSide) : base(UserSide)
    {

    }
}

public class MainPhase2 : GameStates
{
    public MainPhase2(bool UserSide) : base(UserSide)
    {
        
    }
}

public class Effect
{
    public int TempAmount;
    public string EffectString;
    public bool AutomaticEffect;
    public string EffectConditional;
    public CardTemplate CardToHandle;
    public bool Used;
    public string EffectObjetive;
    public Effect()
    {
        EffectObjetive = null;
    }
}

public class EffectPhase : GameStates
{
    public List<Effect> effects;
    public CardSupport CardDoingTheEffect;
    public bool IsMainPhase1;
    public EffectPhase(CardSupport card,bool UserSide, bool IsMainPhase1) : base(UserSide)
    {
        this.CardDoingTheEffect = card;
        this.effects = new List<Effect>();
        this.IsMainPhase1 = IsMainPhase1;
    }

    public void CreateEffects()
    {
        List<EffectExpression> eff = CardDoingTheEffect.DoEffect();
        if(eff != null)
        {
            this.effects = new List<Effect>(eff.Count);
            
            for (int i=0; i<eff.Count; i++)
            {
                effects.Add(new Effect());
                effects[i].EffectString = eff[i].GetValue().ToString();
                switch (eff[i].GetValue().ToString())
                {
                    case TokenValues.DrawCards:
                        effects[i].TempAmount = Convert.ToInt32(eff[i].Amount.GetValue());
                        effects[i].AutomaticEffect = true;
                        break;
                    case TokenValues.DestroyCard:
                        break;
                    case TokenValues.DecreaseAttack:
                    case TokenValues.DecreaseHealth:
                    case TokenValues.IncreaseAttack:
                    case TokenValues.IncreaseHealth:
                        effects[i].EffectString = eff[i].GetValue().ToString();
                        effects[i].TempAmount = Convert.ToInt32(eff[i].Amount.GetValue());
                        break;
                    case TokenValues.AddCardToBoard:
                        effects[i].EffectString = eff[i].GetValue().ToString();
                        effects[i].CardToHandle = eff[i].CardToHandle.ConvertToCardTemplate();
                        effects[i].AutomaticEffect = true;
                        break;
                    case TokenValues.AddCardToDeck:
                        effects[i].EffectString = eff[i].GetValue().ToString();
                        effects[i].CardToHandle = eff[i].CardToHandle.ConvertToCardTemplate();
                        effects[i].AutomaticEffect = true;
                        break;
                    default:
                        break;
                }
                if(eff[i].EffectConditional != null)
                {
                    effects[i].AutomaticEffect = true;
                    effects[i].EffectConditional = eff[i].EffectConditional;
                }
            }
        }
    }
}

public class WaitingForChoosingACardToDoEffect : GameStates
{
    public string EffectObjetive{get;set;}
    public Effect effect{get;set;}
    public WaitingForChoosingACardToDoEffect(bool UserSide, Effect effect) : base(UserSide)
    {
        this.EffectObjetive = null;
        this.effect = effect;
    }
}

public class ResolvePhase : GameStates
{
    public Effect effect;
    public string EffectObjetive;

    public ResolvePhase(Effect effect, string EffectObjetive, bool UserSide) : base(UserSide)
    {
        this.effect = effect;
        this.EffectObjetive = EffectObjetive;
        this.UserSide = UserSide;

    }
}

public class EndRoundPhase: GameStates
{
    public EndRoundPhase(bool UserSide) : base(UserSide)
    {
        
    }
}

public class BattlePhase : GameStates
{
    public BattlePhase(bool UserSide) : base(UserSide)
    {

    }
}