using GaugeOMatic.Trackers;
using GaugeOMatic.Windows;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;
using static CustomNodes.CustomNodeManager;
using static FFXIVClientStructs.FFXIV.Component.GUI.AlignmentType;
using static FFXIVClientStructs.FFXIV.Component.GUI.FontType;
using static GaugeOMatic.Utility.Color;
using static GaugeOMatic.Widgets.BalanceBar;
using static GaugeOMatic.Widgets.NumTextProps;
using static GaugeOMatic.Widgets.WidgetTags;
using static GaugeOMatic.Widgets.WidgetUI;
using static GaugeOMatic.Windows.UpdateFlags;

namespace GaugeOMatic.Widgets;

public sealed unsafe class BalanceBar : GaugeBarWidget
{
    public BalanceBar(Tracker tracker) : base(tracker)
    {
        SharedEvents.Add("SpendShake", () =>
        {
            SpendShake(ref Tweens, BarContainer, Config.FlashColor, 30, 48);
            SpendShake(ref Tweens, BarOverlay, Config.FlashColor, Config.Side == 0 ? 25 : 59, 40);
        });
    }

    public override WidgetInfo WidgetInfo => GetWidgetInfo;

    public static WidgetInfo GetWidgetInfo => new()
    {
        DisplayName = "Balance Bar",
        Author = "ItsBexy",
        Description = "A recreation of a mana bar from Red Mage's Balance Gauge",
        WidgetTags = GaugeBar | MultiComponent | Replica,
        KeyText = "BL2"
    };

    public override CustomPartsList[] PartsLists { get; } = {
        new("ui/uld/JobHudRDM0.tex",new Dictionary<string, Vector4>
        {
            {"Plate"              , new(0,0,116,208)},
            {"WhiteBar"           , new(186,3,26,124)},
            {"BlackBar"           , new(212,3,26,124)},
            {"BarOverlay1"        , new(116,0,34,144)},
            {"Crystal"            , new(0,208,40,56)},
            {"CrystalBorder"      , new(40,208,40,56)},
            {"Star"               , new(116,144,40,60)},
            {"Backdrop"           , new(184,132,84,188)},
            {"Petals3"            , new(125,212,24,22)},
            {"Petals2"            , new(123,234,28,20)},
            {"Petals1"            , new(148,222,14,15)},
            {"BarTexture"         , new(242,3,26,124)},
            {"MotionLinesObsolete", new(116,332,72,32)},
            {"Slash"              , new(0,264,40,48)},
            {"Diamond"            , new(81,239,30,40)},
            {"DiamondGlow"        , new(81,279,30,40)},
            {"DiamondSockets"     , new(0,319,117,61)},
            {"Halo"               , new(114,258,60,60)},
            {"MotionLines"        , new(118,321,89,59)},
            {"Freckles"           , new(207,321,39,59)},
            {"BarOverlay2"        , new(150,0,34,144)}
        })
    };

    #region Nodes

    public CustomNode BarContainer;
    public override CustomNode Drain { get; set; }
    public override CustomNode Gain { get; set; }
    public override CustomNode Main { get; set; }

    public CustomNode BarOverlay;

    public CustomNode PetalContainer;

    public CustomNode Petal1 { get; set; }
    public CustomNode Petal2 { get; set; }
    public CustomNode Petal3 { get; set; }

    public override CustomNode NumTextNode { get; set; }

    public override CustomNode BuildRoot()
    {
        Drain = ImageNodeFromPart(0, 11).SetScale(1, -1)
                                           .SetSize(26, 0)
                                           .SetOrigin(0, 62)
                                           .SetAddRGB(Config.DrainColor)
                                           .SetImageWrap(1);

        Gain = ImageNodeFromPart(0, 11).SetScale(1, -1)
                                       .SetSize(26, 0)
                                       .SetOrigin(0, 62)
                                       .SetAddRGB(Config.GainColor)
                                       .SetImageWrap(1);

        Main = ImageNodeFromPart(0, 1).SetScale(1, -1)
                                      .SetSize(26, 0)
                                      .SetOrigin(0, 62)
                                      .SetAlpha(0xCC)
                                      .SetImageWrap(1);

        Petal1 = ImageNodeFromPart(0, 10).SetPos(53,70)
                                         .SetScale(1.277778f)
                                         .SetRotation(2.0556102f)
                                         .SetImageWrap(1)
                                         .SetDrawFlags(0xE)
                                         .SetAlpha(0);

        Petal2 = ImageNodeFromPart(0, 9).SetPos(38, 69)
                                        .SetScale(0.575f)
                                        .SetRotation(0.1308997f)
                                        .SetOrigin(28,0)
                                        .SetImageWrap(1)
                                        .SetDrawFlags(0xE)
                                        .SetAlpha(0);

        Petal3 = ImageNodeFromPart(0, 8).SetPos(27,82)
                                        .SetScale(1.1f)
                                        .SetRotation(0.20943953f)
                                        .SetOrigin(24, 0)
                                        .SetImageWrap(1)
                                        .SetDrawFlags(0xE)
                                        .SetAlpha(0);


        PetalContainer = new CustomNode(CreateResNode(), Petal1, Petal2, Petal3).SetPos(-12,66.62929f).SetSize(116,125).SetDrawFlags(0xA);

        NumTextNode = CreateNumTextNode().SetTextColor(0xffffffff, 0x440b00ff).SetPos(0, 0);
        NumTextNode.Node->GetAsAtkTextNode()->CharSpacing = 255;

        BarContainer = new CustomNode(CreateResNode(), Drain, Gain, Main).SetPos(30,48).SetSize(26,124).SetOrigin(28,62);

        BarOverlay = ImageNodeFromPart(0, 3).SetPos(25, 40)
                                            .SetSize(34, 144)
                                            .SetImageWrap(1)
                                            .SetOrigin(33,62)
                                            .SetImageFlag(32);

        return new CustomNode(CreateResNode(), BarContainer, BarOverlay, PetalContainer, NumTextNode).SetSize(116,208).SetOrigin(68,114);
    }

    #endregion

    #region Animations

    #endregion

    #region UpdateFuncs

    public override string SharedEventGroup => "BalanceGauge";

    public override void OnDecrease(float prog, float prevProg)
    {
        if (Config.PetalDec) PetalAnim((CalcProg() + CalcProg(true)) / 2f);
    }

    public override void OnIncrease(float prog, float prevProg)
    {
        if (Config.PetalInc) PetalAnim(CalcProg());
    }

    private void PetalAnim(float pos)
    {
        PetalContainer.SetY((-93.8637401964753f * pos) + 86.0983813040982f);
        PetalScatter();
        InvokeSharedEvent("BalanceGauge", "SpendShake");
    }

    public void PetalScatter()
    {
        if (Config.Side == 0)
        {
            Tweens.Add(new(Petal1,
                           new(0) { X = 65, Y = 70, Scale = 0.5f, Rotation = 0.6981317F, Alpha = 255 },
                           new(300) { X = 50, Y = 70, Scale = 1.5f, Rotation = 2.443461F, Alpha = 255 },
                           new(500) { X = 30, Y = 60, Scale = 1.3f, Rotation = 3.490659F, Alpha = 0 }));

            Tweens.Add(new(Petal2,
                           new(0) { X = 40, Y = 70, Scale = 0.5f, Rotation = 0, Alpha = 175 },
                           new(275) { X = 30, Y = 62.6f, Scale = 1.2f, Rotation = 1.22173F, Alpha = 88 },
                           new(425) { X = 24, Y = 60, Scale = 2, Rotation = 2.094395F, Alpha = 0 }));


            Tweens.Add(new(Petal3,
                           new(0) { X = 45, Y = 70, Scale = 0.5f, Rotation = 0.3490658F, Alpha = 180 },
                           new(125) { X = 30, Y = 80, Scale = 1, Rotation = 0.3490658F, Alpha = 150 },
                           new(460) { X = 15, Y = 90, Scale = 1.5f, Rotation = -0.3490658F, Alpha = 0 }));
        }
        else
        {
            Tweens.Add(new(Petal1,
                           new(0) { X = 70, Y = 70, Scale = 0.4f, Rotation = -2.544358F, Alpha = 180 },
                           new(160) { X = 80, Y = 96, Scale = 1.4f, Rotation = -1.3918093F, Alpha = 150 },
                           new(560) { X = 90, Y = 110, Scale = 1.2f, Rotation = 0.5235988F, Alpha = 0 }));

            Tweens.Add(new(Petal2,
                           new(0) { X = 70, Y = 60, Scale = 0.4f, Rotation = -0.032460704F, Alpha = 180 },
                           new(190) { X = 80, Y = 50, Scale = 1.2f, Rotation = -0.6809802F, Alpha = 150 },
                           new(460) { X = 90, Y = 30, Scale = 1.2f, Rotation = -2.443461F, Alpha = 0 }));

            Tweens.Add(new(Petal3,
                           new(0) { X = 70, Y = 60, Scale = 0.2f, Rotation = -1.3916008F, Alpha = 255 },
                           new(210) { X = 80, Y = 80, Scale = 1.5f, Rotation = -0.8393953F, Alpha = 128 },
                           new(470) { X = 90, Y = 84, Scale = 1.2f, Rotation = 0.34906584F, Alpha = 0 }));
        }
    }

    public static void SpendShake(ref List<Tween> tweens, CustomNode node, AddRGB flash, float x, float y)
    {
        tweens.Add(new(node,
                       new(0) { X = x, Y = y, AddRGB = new(0) },
                       new(30) { X = x + 1.9f, Y = y + 0.95f, AddRGB = flash*0.1f },
                       new(100) { X = x - 0.8f, Y = y - 0.9f, AddRGB = flash*0.45f },
                       new(160) { X = x + 1.9f, Y = y + 0.9f, AddRGB = flash*1 },
                       new(180) { X = x + 1.75f, Y = y + 0.85f, AddRGB = flash*0.9f },
                       new(240) { X = x, Y = y, AddRGB = flash*0.5f },
                       new(500) { X = x, Y = y, AddRGB = new(0, 0, 0) }
                   ));
    }

    public override void PostUpdate(float prog)
    {
        if (Gain.Height < Main.Height) Gain.SetHeight(Main.Height);
    }

    public override float CalcBarSize(float prog) => prog*124;

    public override void OnFirstRun(float prog) => Main.SetHeight(CalcBarSize(prog));

    public override DrainGainType DGType => DrainGainType.Height;

    #endregion

    #region Configs

    public sealed class BalanceBarConfig : GaugeBarWidgetConfig
    {
        public Vector2 Position = new(0);
        public float Scale = 1;
        public AddRGB MainColor = new(0);
        public AddRGB GainColor = new(100, -20, -20);
        public AddRGB DrainColor = new(255, -100, -100);
        public AddRGB FlashColor = new(40, 0, 0);
        public uint Side;
        public uint BaseColor;
        public bool PetalInc;
        public bool PetalDec = true;
        public AddRGB PetalColor = new(124, -125, -125);
        protected override NumTextProps NumTextDefault => new(true, new(0), 0xffffffff, 0x440b00ff, MiedingerMed, 18, Center, false, 0, true);

        public BalanceBarConfig(WidgetConfig widgetConfig)
        {
            NumTextProps = NumTextDefault;
            var config = widgetConfig.BalanceBarCfg;

            if (config == null) return;

            Position = config.Position;
            Scale = config.Scale;
            MainColor = config.MainColor;
            GainColor = config.GainColor;
            DrainColor = config.DrainColor;
            NumTextProps = config.NumTextProps;
            Invert = config.Invert;
            Side = config.Side;
            BaseColor = config.BaseColor;
            FlashColor = config.FlashColor;
            PetalDec = config.PetalDec;
            PetalInc = config.PetalInc;
            PetalColor = config.PetalColor;
        }

        public BalanceBarConfig() { }
    }

    public override GaugeBarWidgetConfig GetConfig => Config;

    public BalanceBarConfig Config = null!;

    public override void InitConfigs()
    {
        Config = new(Tracker.WidgetConfig);
        if (Tracker.WidgetConfig.BalanceBarCfg == null && Tracker.RefType == RefType.Action) { Config.Invert = true; }
    }

    public override void ResetConfigs() => Config = new();

    public override void ApplyConfigs()
    {
        var left = Config.Side == 0;
        var light = Config.BaseColor == 0;

        WidgetRoot.SetPos(Config.Position)
                  .SetScale(Config.Scale);
        
        BarContainer.SetScale(left?1:-1, 1);

        BarOverlay.SetPos(left?25:59, 40)
                  .SetPartId((ushort)(left?3:20));
        
        Main.SetPartId((ushort)(light?1:2))
            .SetImageFlag((byte)(light ? 0 : 1))
            .SetAddRGB(Config.MainColor);

        Gain.SetAddRGB(Config.GainColor);
        Drain.SetAddRGB(Config.DrainColor);
        PetalContainer.SetAddRGB(Config.PetalColor + new AddRGB(-124, 125, 125));

        var configNumText = Config.NumTextProps;
        configNumText.Position += new Vector2(left ? 16 : 68, 172);
        configNumText.ApplyTo(NumTextNode);
    }

    public override void DrawUI(ref WidgetConfig widgetConfig, ref UpdateFlags update)
    {
        Heading("Layout");

        PositionControls("Position", ref Config.Position, ref update);
        ScaleControls("Scale", ref Config.Scale, ref update);
        RadioControls("Side", ref Config.Side, new() { 0u, 1u }, new() { "Left", "Right" }, ref update);

        Heading("Colors");

        RadioControls("Base Color", ref Config.BaseColor, new() { 0u, 1u }, new() { "Light", "Dark" }, ref update);
        ColorPickerRGBA("Main Bar", ref Config.MainColor, ref update);
        ColorPickerRGBA("Gain", ref Config.GainColor, ref update);
        ColorPickerRGBA("Drain", ref Config.DrainColor, ref update);
        ColorPickerRGB("Flash", ref Config.FlashColor, ref update);
        ColorPickerRGB("Petal Color", ref Config.PetalColor, ref update);

        Heading("Behavior");

        ToggleControls("Invert Fill", ref Config.Invert, ref update);
        var petalEffect = new List<bool> { Config.PetalInc, Config.PetalDec };
        if (ToggleControls("Petal Effect", ref petalEffect, new List<string> { "On Increase", "On Decrease" }, ref update))
        {
            Config.PetalInc = petalEffect[0];
            Config.PetalDec = petalEffect[1];
        }
        // IntControls("Animation Time", ref Config.AnimationLength, 0, 2000, 50, ref update);

        NumTextControls($"{Tracker.TermGauge} Text", ref Config.NumTextProps, ref update);

        if (update.HasFlag(Save)) ApplyConfigs();
        widgetConfig.BalanceBarCfg = Config;
    }

    #endregion
}

public partial class WidgetConfig
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public BalanceBarConfig? BalanceBarCfg { get; set; }
}
