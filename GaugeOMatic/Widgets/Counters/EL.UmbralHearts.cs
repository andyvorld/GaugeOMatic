using CustomNodes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GaugeOMatic.CustomNodes.Animation;
using GaugeOMatic.Trackers;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;
using static CustomNodes.CustomNodeManager;
using static GaugeOMatic.CustomNodes.Animation.KeyFrame;
using static GaugeOMatic.CustomNodes.Animation.Tween.EaseType;
using static GaugeOMatic.Utility.Color;
using static GaugeOMatic.Widgets.Common.CommonParts;
using static GaugeOMatic.Widgets.UmbralHearts;
using static GaugeOMatic.Widgets.WidgetTags;
using static GaugeOMatic.Widgets.WidgetUI;
using static GaugeOMatic.Widgets.WidgetUI.UpdateFlags;
using static GaugeOMatic.Widgets.WidgetUI.WidgetUiTab;

#pragma warning disable CS8618

namespace GaugeOMatic.Widgets;

public sealed unsafe class UmbralHearts : FreeGemCounter
{
    public UmbralHearts(Tracker tracker) : base(tracker) { }

    public override WidgetInfo WidgetInfo => GetWidgetInfo;

    public static WidgetInfo GetWidgetInfo { get; } = new()
    {
        DisplayName = "Umbral Hearts",
        Author = "ItsBexy",
        Description = "A counter based on BLM's Umbral Heart counter",
        WidgetTags = Counter | Replica | MultiComponent,
        MultiCompData = new("EL", "Elemental Gauge Replica", 5),
        UiTabOptions = Layout | Colors | Behavior
    };

    public override CustomPartsList[] PartsLists { get; } = { BLM0 };

    #region Nodes

    public List<CustomNode> Hearts;
    public List<CustomNode> GlowWrappers;
    public List<CustomNode> Glows;

    public override CustomNode BuildContainer()
    {
        Max = GetMax();
        BuildStacks(Max);
        return new CustomNode(CreateResNode(), Stacks.ToArray()).SetOrigin(12, 34);
    }

    private void BuildStacks(int count)
    {
        Stacks = new();
        Hearts = new();
        GlowWrappers = new();
        Glows = new();

        for (var i = 0; i < count; i++)
        {
            Hearts.Add(ImageNodeFromPart(0, 5).SetPos(0,-20).SetOrigin(12, 34).SetAlpha(0));

            Glows.Add(ImageNodeFromPart(0, 20).SetScale(1.3f, 1.2f).SetOrigin(12, 34).SetImageWrap(1));

            Animator += new Tween(Glows[i],
                                  new(0) { ScaleX = 1, ScaleY = 1, Alpha = 4 },
                                  new(300) { ScaleX = 1.2f, ScaleY = 1.1f, Alpha = 152 },
                                  new(630) { ScaleX = 1.3f, ScaleY = 1.2f, Alpha = 0 },
                                  new(960) { ScaleX = 1.3f, ScaleY = 1.2f, Alpha = 0 })
                                  { Repeat = true, Ease = SinInOut };

            Glows[i].UnsetNodeFlags(NodeFlags.UseDepthBasedPriority);
            GlowWrappers.Add(new CustomNode(CreateResNode(), Glows[i]).SetAlpha(0));

            Stacks.Add(new CustomNode(CreateResNode(), Hearts[i], GlowWrappers[i]).SetOrigin(12, 34));
        }
    }

    #endregion

    #region Animations

    public override void ShowStack(int i) =>
        Animator += new Tween[]
        {
            new(Hearts[i],
                new(0) { Y = -20, Alpha = 0 },
                new(200) { Y = 0, Alpha = 200 },
                new(300) { Y = 0, Alpha = 255 }),
            new(GlowWrappers[i],
                Hidden[0],
                Visible[300])
        };

    public override void HideStack(int i) =>
        Animator += new Tween[]
        {
            new(Hearts[i],
                new(0) { Y = 0, Alpha = 255 },
                new(200) { Y = -20, Alpha = 255 },
                new(300) { Y = -20, Alpha = 0 }
            ),
            new(GlowWrappers[i],
                Visible[0],
                Hidden[300])
        };

    #endregion

    #region UpdateFuncs

    public override void OnFirstRun(int count, int max)
    {
        for (var i = 0; i < max; i++)
        {
            Hearts[i].SetAlpha(i < count).SetY(0);
            GlowWrappers[i].SetAlpha(i < count);
        }
    }

    #endregion

    #region Configs

    public class UmbralHeartConfig : FreeGemCounterConfig
    {
        public AddRGB StackColor = new(0);
        public AddRGB GlowColor = new(0);

        public UmbralHeartConfig(WidgetConfig widgetConfig) : base(widgetConfig.UmbralHeartCfg)
        {
            var config = widgetConfig.UmbralHeartCfg;

            if (config == null) return;

            StackColor = config.StackColor;
            GlowColor = config.GlowColor;
        }

        public UmbralHeartConfig()
        {
            Spacing = -16.5f;
            Angle = -126f;
            Curve = -20f;
        }
    }

    public override FreeGemCounterConfig GetConfig => Config;

    public UmbralHeartConfig Config;

    public override void InitConfigs() => Config = new(Tracker.WidgetConfig);

    public override void ResetConfigs() => Config = new();

    public override void ApplyConfigs()
    {

        WidgetContainer.SetPos(Config.Position + new Vector2(19, 22))
                       .SetScale(Config.Scale);

        PlaceFreeGems();

        for (var i = 0; i < Stacks.Count; i++)
        {
            Hearts[i].SetAddRGB(Config.StackColor);
            GlowWrappers[i].SetAddRGB(Config.GlowColor);
        }
    }

    public override string StackTerm => "Heart";
    public override void DrawUI(ref WidgetConfig widgetConfig)
    {
        base.DrawUI(ref widgetConfig);
        switch (UiTab)
        {
            case Layout:
                break;
            case Colors:
                ColorPickerRGB("Color Modifier", ref Config.StackColor);
                ColorPickerRGB("Glow Color", ref Config.GlowColor);
                break;
            case Behavior:
                break;
            default:
                break;
        }

        if (UpdateFlag.HasFlag(Save)) ApplyConfigs();
        widgetConfig.UmbralHeartCfg = Config;
    }

    #endregion
}

public partial class WidgetConfig
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public UmbralHeartConfig? UmbralHeartCfg { get; set; }
}
