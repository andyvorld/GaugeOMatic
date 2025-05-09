using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using GaugeOMatic.Config;
using GaugeOMatic.Trackers;
using GaugeOMatic.Utility;
using GaugeOMatic.Widgets;
using ImGuiNET;
using System;
using Dalamud.Interface.Components;
using static Dalamud.Interface.FontAwesomeIcon;
using static Dalamud.Interface.UiBuilder;
using static Dalamud.Interface.Utility.ImGuiHelpers;
using static GaugeOMatic.GameData.JobData;
using static GaugeOMatic.Widgets.Common.WidgetUI;
using static GaugeOMatic.Widgets.Common.WidgetUI.UpdateFlags;
using static GaugeOMatic.Widgets.Common.WidgetUI.WidgetUiTab;
using static ImGuiNET.ImGuiCond;
using static ImGuiNET.ImGuiTableColumnFlags;
using static ImGuiNET.ImGuiTableFlags;

namespace GaugeOMatic.Windows;

public class TrackerWindow : Window, IDisposable
{
    public Configuration Configuration;
    public Tracker Tracker;
    public Widget? Widget;
    public string Hash => Tracker.GetHashCode() + "-" + Widget?.GetHashCode();

    public TrackerWindow(Tracker tracker, Widget widget, Configuration configuration, string name) : base(name)
    {
        Tracker = tracker;
        Widget = widget;
        Configuration = configuration;
        Collapsed = false;
        Flags = ImGuiWindowFlags.NoCollapse;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(300f, 300f),
            MaximumSize = new(1100f)
        };
        SizeCondition = Appearing;
    }

    public override void Draw()
    {
        UpdateFlag = 0;
        if (!Tracker.Available || Widget == null) IsOpen = false;

        HeaderTable();

        WidgetOptionTable();

        if (UpdateFlag.HasFlag(UpdateFlags.Save)) Configuration.Save();
        if (UpdateFlag.HasFlag(Reset)) Tracker.JobModule.ResetWidgets();
    }

    private void HeaderTable()
    {
        using (var table = ImRaii.Table("TrackerHeaderTable" + Hash, 2, SizingFixedFit | PadOuterX))
        {
            if (table.Success) {
                ImGui.TableSetupColumn("Labels", WidthFixed, 60f * GlobalScale);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiHelpy.TextRightAligned("Widget");
                ImGui.TableNextColumn();
                Tracker.WidgetMenuWindow.Draw("[Select Widget]", 182f);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiHelpy.TextRightAligned("Pinned to:", true);
                ImGui.TableNextColumn();
                if (Tracker.AddonDropdown.Draw($"AddonSelect{GetHashCode()}", 182f))
                {
                    Tracker.AddonName = Tracker.AddonDropdown.CurrentSelection;
                    UpdateFlag |= Reset | UpdateFlags.Save;
                }

                PreviewControls();

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
            }
        }

        ImGui.TextDisabled("Widget Settings");
        ImGui.SameLine();

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - (80 * GlobalScale));
        if (ImGuiComponents.IconButtonWithText(UndoAlt,"Default",null, null, null, new(80f,0)))
        {
            Widget?.ResetConfigs();
            Widget?.ApplyConfigs();
            Tracker.UpdateTracker();
            UpdateFlag |= UpdateFlags.Save;
        }

        if (ImGui.IsItemHovered())
        {
            using var tt = ImRaii.Tooltip();
            if (tt.Success)
            {
                ImGui.TextUnformatted($"This will reset to the defaults for {Widget?.GetAttributes.DisplayName}.\nTo restore a particular preset for this tracker instead, use the Presets window.");
            }
        }

    }

    public void PreviewControls()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGuiHelpy.TextRightAligned("Test");
        var preview = Tracker.TrackerConfig.Preview;
        var previewValue = Tracker.TrackerConfig.PreviewValue;
        ImGui.TableNextColumn();
        if (ImGui.Checkbox($"##Preview{Hash}", ref preview))
        {
            Tracker.TrackerConfig.Preview = preview;
            Tracker.Widget?.ApplyDisplayRules();
        }
        ImGui.SameLine();

        if (preview)
        {
            ImGui.SetNextItemWidth(153f * GlobalScale);
            if (ImGui.SliderFloat($"##PreviewSlider{Tracker.GetHashCode()}", ref previewValue, 0, 1f, ""))
                Tracker.TrackerConfig.PreviewValue = previewValue;
        }
    }

    private void WidgetOptionTable()
    {
        ImGui.Spacing();

        using (var tb = ImRaii.TabBar("UiTab" + Hash))
        {
            if (tb.Success)
            {
                var tabOptions = Tracker.Widget?.GetAttributes.UiTabOptions ?? WidgetUiTab.None;
                DrawTab(tabOptions, "Layout", Layout, ArrowsUpDownLeftRight);
                DrawTab(tabOptions, "Colors", Colors, PaintBrush);
                DrawTab(tabOptions, "Text", Text, Font);
                DrawTab(tabOptions, "Behavior", Behavior, SlidersH);
                DrawTab(tabOptions, "Icon", Icon, Tag);
                DrawTab(tabOptions, "Sound", Sound, VolumeUp);
            }
        }

        using var table = ImRaii.Table($"TrackerWidgetOptionTable{Tracker.Widget?.UiTab}{Hash}", 2, SizingStretchProp | PadOuterX | ImGuiTableFlags.NoClip);
        if (table.Success) {
            ImGui.TableSetupColumn("Labels", WidthStretch, 0.75f);
            ImGui.TableSetupColumn("Controls", WidthStretch, 1);

            ImGui.Spacing();
            ImGui.Spacing();

            Widget?.DrawUI();

            if (Tracker.Widget?.UiTab == Behavior) DisplayRuleTable();

            if (UpdateFlag.HasFlag(UpdateFlags.Save)) Tracker.WriteWidgetConfig();
        }

        return;

        void DrawTab(WidgetUiTab tabs, string label, WidgetUiTab uiTab, FontAwesomeIcon icon)
        {
            if (tabs.HasFlag(uiTab))
            {
                using var f = ImRaii.PushFont(IconFont);
                using var ti = ImRaii.TabItem($"{icon.ToIconString()}###{label}Tab{Hash}");
                f.Pop();

                if (ImGui.IsItemHovered())
                {
                    using var tt = ImRaii.Tooltip();
                    if (tt.Success)
                    {
                        ImGui.TextUnformatted(label);
                    }
                }

                if (ti) Tracker.Widget!.UiTab = uiTab;
            }
        }
    }

    private void DisplayRuleTable()
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextColored(new(1, 1, 1, 0.3f), "Display Rules");
        ImGui.TableNextColumn();

        var cond1 = RadioControls("Visibility", ref Tracker.TrackerConfig.HideOutsideCombatDuty, [false, true], ["Anytime", "Combat / Duty Only"]);
        var cond2 = ToggleControls("Set Level Range", ref Tracker.TrackerConfig.LimitLevelRange);
        var cond3 = false;
        var cond4 = false;
        if (Tracker.TrackerConfig.LimitLevelRange)
        {
            var min = Tracker.TrackerConfig.LevelMin;
            var max = Tracker.TrackerConfig.LevelMax;
            cond3 = IntControls("Minimum Level", ref min, 1, max, 1);
            if (cond3) { Tracker.TrackerConfig.LevelMin = min; }
            cond4 = IntControls("Maximum Level", ref max, min, LevelCap, 1);
            if (cond4) { Tracker.TrackerConfig.LevelMax = max; }
        }

        if (cond1 || cond2 || cond3 || cond4) Tracker.Widget?.ApplyDisplayRules();
    }

    public void Dispose() { }
}
