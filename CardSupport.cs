using Godot;
using System;
using System.Collections.Generic;

public class CardSupport : Node2D
{
    public CardTemplate LogicCard { get; set; }
    public bool Summoned { get; set; }      //True if the card is summoned
    public bool HasParent { get; set; }     //True if the card has been added to the node tree
    public bool HasAttacked { get; set; }   //True if the card has attacked
    public bool HasActivatedEffect { get; set; }   //True if the card has activated its effects
    public bool CardPressed { get; set; }       //True if the card's button has been pressed
    public Game Instance { get; set; }      //Game object to call the Game class mathods
    ImageTexture CardTexture { get; set; }
   
    public override void _Ready()
    {
        GenerateCardVisualBase();
    }
    public void _on_SelectCard_pressed()
    {
        if ((ButtonList)Instance.MouseClick.ButtonIndex == ButtonList.Right)
        {
            if (this.Summoned && Instance.SelectedCard != null && Instance.SelectedCard.Summoned && Instance.GamePhases[Instance.index] is BattlePhase)
            //The card is under potential attack
            {
                if ((Instance.UserSide && this.LogicCard.political_current != Game.UserPlayer.name) || (!Instance.UserSide && this.LogicCard.political_current != Game.EnemyPlayer.name))
                {
                    if (Instance.SelectedCard.LogicCard.political_current != this.LogicCard.political_current)
                    //The card is under attack
                    {
                        if (Instance.UserSide)
                        {
                            Game.UserPlayer.Attack(Instance.SelectedCard, this);
                        }
                        else
                        {
                            Game.EnemyPlayer.Attack(Instance.SelectedCard, this);
                        }
                    }
                }
            }
        }
        else
        {
            Instance.SelectedCardName = this.LogicCard.CardName;
            Instance.NonCardPressed();
            Instance.SelectedCard = this;
            CardPressed = true;
            Instance.ActionMessage = $"{this.LogicCard.CardName} Selected";
            Instance.AddSideMessage();

            if(Instance.GamePhases[Instance.index] is WaitingForChoosingACardToDoEffect)
            {
                Game.EffectObjetive = this.LogicCard.CardName;
            }
        }
        UpdateCardVisual();
    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    // public override void _Process(float delta)
    // {
    // }
    public void MakeCard(ImageTexture CardTexture)   //Construct the complete visual of the card
    {
        this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Name").Text = this.LogicCard.CardName;

        this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Lore").Text = this.LogicCard.Lore;

        this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/ClassCard").Text = this.LogicCard.political_current;

        this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Effect").Text = this.LogicCard.EffectText;

        ImageTexture typetexture = new ImageTexture();
        if (LogicCard is Unit)
        {
            typetexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/Unit.png");
            this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Attack").Text = $"{this.LogicCard.Attack}";
            this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Life").Text = $"{this.LogicCard.Health}";
        }
        else
        {
            typetexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/Politic.png");
            this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Attack").Text = "Aguacate";
            this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Life").Text = "Aguacate";
        }


        this.GetNode<Sprite>("CardMargin/BackgroundCard/TypeMargin/TypePhoto").Texture = typetexture;
        this.GetNode<Sprite>("CardMargin/BackgroundCard/TypeMargin/TypePhoto").Scale = this.GetNode<MarginContainer>("CardMargin/BackgroundCard/TypeMargin").RectSize / typetexture.GetSize();
        var rarenesstexture = new ImageTexture();
        switch (this.LogicCard.Rareness)
        {
            case "Legendary":
                rarenesstexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/GoldenShield.png");
                break;
            case "Common":
                rarenesstexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/BronzeShield.png");
                break;
        }
        this.GetNode<Sprite>("CardMargin/BackgroundCard/RarenessMargin/RarenessPhoto").Texture = rarenesstexture;
        this.GetNode<Sprite>("CardMargin/BackgroundCard/RarenessMargin/RarenessPhoto").Scale = this.GetNode<MarginContainer>("CardMargin/BackgroundCard/RarenessMargin").RectSize / rarenesstexture.GetSize();

        var phototexture = new ImageTexture();
        if (this.LogicCard.PathToPhoto != null && this.LogicCard.PathToPhoto != "")
        {
            phototexture.Load(this.LogicCard.PathToPhoto);
            this.GetNode<Sprite>("CardMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Texture = phototexture;
            this.GetNode<Sprite>("CardMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Scale = this.GetNode<MarginContainer>("CardMargin/BackgroundCard/PhotoCardMargin").RectSize / phototexture.GetSize();
        }
        else
        {
            phototexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/foto-perfil-generica.jpg");
            this.GetNode<Sprite>("CardMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Texture = phototexture;
            this.GetNode<Sprite>("CardMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Scale = this.GetNode<MarginContainer>("CardMargin/BackgroundCard/PhotoCardMargin").RectSize / phototexture.GetSize();
        }
    }
    private void GenerateCardVisualBase()  //Generates background of the card
    {
        var CardTexture = new ImageTexture();
        CardTexture.Load(System.IO.Directory.GetCurrentDirectory() + "/Textures/Card.jpg");
        GetNode<Sprite>("CardMargin/BackgroundCard").Position = new Vector2(GetNode<MarginContainer>("CardMargin").RectSize.x / 2, GetNode<MarginContainer>("CardMargin").RectSize.y / 2);
        GetNode<Sprite>("CardMargin/BackgroundCard").Texture = CardTexture;
        GetNode<Sprite>("CardMargin/BackgroundCard").Scale = GetNode<MarginContainer>("CardMargin").RectSize / CardTexture.GetSize();
    }
    public void UpdateCardVisual()   //Updates the card visual in the game 
    {
        this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Name").Text = this.LogicCard.CardName;
        this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Effect").Text = this.LogicCard.EffectText;
        this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Lore").Text = this.LogicCard.Lore;
        if (LogicCard is Politic)
        {
            this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Attack").Text = "-";
            this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Life").Text = "-";
        }
        else
        {
            this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Attack").Text = $"{this.LogicCard.Attack}";
            this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/Life").Text = $"{this.LogicCard.Health}";
        }

        this.GetNode<RichTextLabel>("CardMargin/BackgroundCard/ClassCard").Text = this.LogicCard.political_current;

        GetNode<RichTextLabel>("/root/Main/Game/Board/ShowMargin/BackgroundCard/Name").Text = this.LogicCard.CardName;
        GetNode<RichTextLabel>("/root/Main/Game/Board/ShowMargin/BackgroundCard/Effect").Text = this.LogicCard.EffectText;
        GetNode<RichTextLabel>("/root/Main/Game/Board/ShowMargin/BackgroundCard/Lore").Text = this.LogicCard.Lore;
        if (LogicCard is Unit)
        {
            GetNode<RichTextLabel>("/root/Main/Game/Board/ShowMargin/BackgroundCard/Attack").Text = $"{this.LogicCard.Attack}";
            GetNode<RichTextLabel>("/root/Main/Game/Board/ShowMargin/BackgroundCard/Life").Text = $"{this.LogicCard.Health}";
        }
        else
        {
            GetNode<RichTextLabel>("/root/Main/Game/Board/ShowMargin/BackgroundCard/Attack").Text = "-";
            GetNode<RichTextLabel>("/root/Main/Game/Board/ShowMargin/BackgroundCard/Life").Text = "-";
        }
        GetNode<RichTextLabel>("/root/Main/Game/Board/ShowMargin/BackgroundCard/ClassCard").Text = this.LogicCard.political_current;
        GetNode<Sprite>("/root/Main/Game/Board/ShowMargin/BackgroundCard/TypeMargin/TypePhoto").Texture = GetNode<Sprite>("CardMargin/BackgroundCard/TypeMargin/TypePhoto").Texture;
        GetNode<Sprite>("/root/Main/Game/Board/ShowMargin/BackgroundCard/TypeMargin/TypePhoto").Scale = GetNode<MarginContainer>("/root/Main/Game/Board/ShowMargin/BackgroundCard/TypeMargin").RectSize / GetNode<Sprite>("CardMargin/BackgroundCard/TypeMargin/TypePhoto").Texture.GetSize();
        GetNode<Sprite>("/root/Main/Game/Board/ShowMargin/BackgroundCard/RarenessMargin/RarenessPhoto").Texture = GetNode<Sprite>("CardMargin/BackgroundCard/RarenessMargin/RarenessPhoto").Texture;
        GetNode<Sprite>("/root/Main/Game/Board/ShowMargin/BackgroundCard/RarenessMargin/RarenessPhoto").Scale = GetNode<MarginContainer>("/root/Main/Game/Board/ShowMargin/BackgroundCard/RarenessMargin").RectSize / GetNode<Sprite>("CardMargin/BackgroundCard/RarenessMargin/RarenessPhoto").Texture.GetSize();
        GetNode<Sprite>("/root/Main/Game/Board/ShowMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Texture = GetNode<Sprite>("CardMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Texture;
        GetNode<Sprite>("/root/Main/Game/Board/ShowMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Scale = GetNode<MarginContainer>("/root/Main/Game/Board/ShowMargin/BackgroundCard/PhotoCardMargin").RectSize / GetNode<Sprite>("CardMargin/BackgroundCard/PhotoCardMargin/PhotoCard").Texture.GetSize();
    }
    public List<EffectExpression> DoEffect()
    {
        if (this.LogicCard.Effect != null)
        {
            TokenStream effect_stream = new TokenStream(this.LogicCard.Effect);
            EffectParser effect_parser = new EffectParser(effect_stream);
            List<CompilingError> effect_errors = new List<CompilingError>();
            ColdWarProgram effect_program = effect_parser.ParseProgram(effect_errors);

            if (effect_errors.Count > 0)
            {
                foreach (CompilingError error in effect_errors)
                {
                    GD.Print(error.Location.Line + " " + error.Code + " " + error.Argument);
                }
            }
            else
            {
                Context context = new Context();
                Scope scope = new Scope();

                effect_program.CheckSemantic(context, scope, effect_errors);

                if (effect_errors.Count > 0)
                {
                    foreach (CompilingError error in effect_errors)
                    {
                        GD.Print(error.Location.Line + " " + error.Code + " " + error.Argument);
                    }
                }
                else
                {
                    effect_program.Evaluate();

                    return effect_program.Effects;
                }
            }
        }
        return null;
    }
}