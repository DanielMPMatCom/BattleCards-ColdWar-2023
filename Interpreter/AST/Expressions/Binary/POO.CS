using System;
using System.Collections.Generic;
using Godot;

public class POObject : BinaryExpression
{
     public override ExpressionType Type{get;set;}

    private object value;

    public override object GetValue()
    {
        return value;
    }

    public override void SetValue(object value)
    {
        this.value = value;
    }

    public POObject(CodeLocation location) : base(location){}

    public override bool CheckSemantic(Context context, Scope scope, List<CompilingError> errors)
    {
        bool checkId = false;
        if(context.cards.Contains(Left.GetValue().ToString())) checkId = true;
        else
        {
            errors.Add(new CompilingError(Location, ErrorCode.None, "Doesn't exist in the current context"));
            return false;
        }

        bool checkKeyword = false;
        if(CardAttributes.Contains(Right.GetValue().ToString())) checkKeyword = true;
        else
        {
            errors.Add(new CompilingError(Location, ErrorCode.Invalid, "Doesn't exist that card Attribute"));
            return false;
        }

        return checkId && checkKeyword;
    }
    public override void Evaluate()
    {
        Evaluate(true);
    }

    public void Evaluate(bool OnGame)
    {
        if(OnGame)
        {
            foreach(CardSupport cardSupport in Game.EnemyPlayer.PlayerBoard.CardsOnBoard.Keys)
            {
                if(SeparatedName(Left.GetValue().ToString()) == cardSupport.LogicCard.CardName)
                {
                    switchRight(cardSupport);
                }
            }

            foreach(CardSupport cardSupport in Game.UserPlayer.PlayerBoard.CardsOnBoard.Keys)
            {
                if(SeparatedName(Left.GetValue().ToString()) == cardSupport.LogicCard.CardName)
                {
                    switchRight(cardSupport);
                }
            }
        }
        else
        {
            if(Left != null && Left.GetValue() != null)
            {
                CardTemplate cardSupport = Card.SearchCardTemplate(Left.GetValue().ToString());
                if(cardSupport != null)
                {
                    switch(Right.GetValue().ToString())
                    {
                        case TokenValues.Health:
                            this.SetValue(cardSupport.Health);
                            this.Type = ExpressionType.Number;
                            break;
                        case TokenValues.Attack:
                            this.SetValue(cardSupport.Attack);
                            this.Type = ExpressionType.Number;
                            break;
                        case TokenValues.Rareness:
                            this.SetValue(cardSupport.Rareness.ToString());
                            this.Type = ExpressionType.Text;
                            break;
                        case TokenValues.political_current:
                            this.SetValue(cardSupport.political_current.ToString());
                            this.Type = ExpressionType.Text;
                            break;
                        case TokenValues.CardType:
                            this.SetValue(cardSupport.cardtype.ToString());
                            this.Type = ExpressionType.Text;
                            break;
                        default:
                            this.SetValue(null);
                            this.Type = ExpressionType.Number;
                            break;
                    }
                }
                else
                {
                    this.SetValue(null);
                }
            }
        }
    }

    protected void switchRight(CardSupport cardSupport)
    {
        switch(Right.GetValue().ToString())
        {
            case TokenValues.Health:
                this.SetValue(cardSupport.LogicCard.Health);
                this.Type = ExpressionType.Number;
                break;
            case TokenValues.Attack:
                this.SetValue(cardSupport.LogicCard.Attack);
                this.Type = ExpressionType.Number;
                break;
            case TokenValues.Rareness:
                this.SetValue(cardSupport.LogicCard.Rareness.ToString());
                this.Type = ExpressionType.Text;
                break;
            case TokenValues.political_current:
                this.SetValue(cardSupport.LogicCard.political_current.ToString());
                this.Type = ExpressionType.Text;
                break;
            case TokenValues.CardType:
                this.SetValue(cardSupport.LogicCard.cardtype.ToString());
                this.Type = ExpressionType.Text;
                break;
            default:
                this.SetValue(null);
                this.Type = ExpressionType.Number;
                break;
        }
    }

    public override string ToString()
    {
        return String.Format(GetValue().ToString());
    }

    protected string SeparatedName(string name)
    {
        string nname = "";
        for(int i=0; i<name.Length; i++)
        {
            if(Char.IsUpper(name[i]) && i!=0) nname+=" ";
            nname+=name[i];
        }

        return nname;
    }

    private List<string> CardAttributes = new List<string>(){TokenValues.CardType, TokenValues.Rareness, TokenValues.Attack, TokenValues.Health, TokenValues.political_current};
}

