
using System;
using System.Collections.Generic;
using Godot;

public class ColdWarProgram : ASTNode
{
    public List<CompilingError> Errors {get; set;}
    public Dictionary<string, PoliticalCurrent> political_currents {get; set;}
    public Dictionary<string, Card> Cards {get; set;}
    public List<Expression> PrintingList {get; set;}
    public List<EffectExpression> Effects{get;set;}
    public Dictionary<string,List<(Token,Expression)>> AuxiliarCardParser {get;set;}

    public ColdWarProgram(CodeLocation location) : base (location)
    {
        Errors = new List<CompilingError>();
        political_currents = new Dictionary<string, PoliticalCurrent>();
        Cards = new Dictionary<string, Card>();
        PrintingList = new List<Expression>();
        Effects = new List<EffectExpression>();
        AuxiliarCardParser = new Dictionary<string, List<(Token, Expression)>>();
    }
    
    /* To check a program semantic we sould first collect all the existing PoliticalCurrents and store them in the context.
    Then, we check semantics of PoliticalCurrents and cards */
    public override bool CheckSemantic(Context context, Scope scope, List<CompilingError> errors)
    {
        bool checkPoliticalCurrents = true;
        foreach (PoliticalCurrent political_current in political_currents.Values)
        {
            checkPoliticalCurrents = checkPoliticalCurrents && political_current.CollectPoliticalCurrents(context, scope.CreateChild(), errors);
        }
        foreach (PoliticalCurrent political_current in political_currents.Values)
        {
            checkPoliticalCurrents = checkPoliticalCurrents && political_current.CheckSemantic(context, scope.CreateChild(), errors);
        }

        bool checkCards = true;

        foreach(var kvp in AuxiliarCardParser)
        {
            // The work will be done on Cards[kvp.key]
            foreach(var x in AuxiliarCardParser[kvp.Key])
            {
                checkCards = checkExp(x.Item1, x.Item2);

                switch(x.Item1.Value)
                {
                    case TokenValues.CardType:
                        Cards[kvp.Key].cardtype = x.Item2;
                        break;
                    case TokenValues.Rareness:
                        Cards[kvp.Key].Rareness = x.Item2;
                        break;
                    case TokenValues.Lore:
                        Cards[kvp.Key].Lore = x.Item2;
                        break;
                    case TokenValues.Health:
                        Cards[kvp.Key].Health = x.Item2;
                        break;
                    case TokenValues.Attack:
                        Cards[kvp.Key].Attack = x.Item2;
                        break;
                    case TokenValues.political_current:
                        Cards[kvp.Key].political_current = x.Item2;
                        break;
                    case TokenValues.PathToPhoto:
                        if(!checkCards) checkCards = true;
                        Cards[kvp.Key].PathToPhoto = x.Item2;
                        break;
                    case TokenValues.EffectText:
                        if(!checkCards) checkCards = true;
                        Cards[kvp.Key].EffectText = x.Item2;
                        break;
                    default:
                        errors.Add(new CompilingError(x.Item2.Location, ErrorCode.Invalid, "Invalid identifier"));
                        break;
                }
            }
        }

        if(checkCards)
        {
            foreach (Card card in Cards.Values)
            {
                checkCards = checkCards && card.CheckSemantic(context, scope, errors);
            }
        }

        return checkPoliticalCurrents && checkCards;
    }

    protected bool checkExp(Token tok, Expression exp2)
    {
        if(exp2 == null)
        {
            Errors.Add(new CompilingError(tok.Location, ErrorCode.Invalid, "Bad expression"));
            return false;
        }
        return true;
    }

    public void Evaluate()
    {
        foreach (Card card in Cards.Values)
        {
            card.Evaluate();
        }

        foreach (Expression exp in PrintingList)
        {
            GD.Print(exp);
        }
    }

    public override string ToString()
    {
        string s = "";
        foreach (PoliticalCurrent PoliticalCurrent in political_currents.Values)
        {
            s = s + "\n" + PoliticalCurrent.ToString();
        }
        foreach (Card card in Cards.Values)
        {
            s += "\n" + card.ToString();
        }
        return s;
    }
}