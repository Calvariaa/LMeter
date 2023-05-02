﻿using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface;
using ImGuiNET;
using LMeter.Helpers;
using LMeter.Meter;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;
using System;


namespace LMeter.Config;

public class MeterListConfig : IConfigPage
{
    private const float MenuBarHeight = 40;
    [JsonIgnore]
    private string _input = string.Empty;
    public string Name =>
        "Profiles";
    public List<MeterWindow> Meters { get; set; }

    public MeterListConfig() =>
        this.Meters = new List<MeterWindow>();

    public IConfigPage GetDefault() =>
        new MeterListConfig();

    public void DrawConfig(Vector2 size, float padX, float padY)
    {
        this.DrawCreateMenu(size, padX);
        this.DrawMeterTable(size.AddY(-padY), padX);
    }

    public void ToggleMeter(int meterIndex, bool? toggle = null)
    {
        if (meterIndex >= 0 && meterIndex < this.Meters.Count)
        {
            this.Meters[meterIndex].VisibilityConfig.AlwaysHide = 
                toggle.HasValue
                    ? !toggle.Value
                    : !this.Meters[meterIndex].VisibilityConfig.AlwaysHide;
        }
    }

    public void ToggleClickThrough(int meterIndex)
    {
        if (meterIndex >= 0 && meterIndex < this.Meters.Count)
        {
            this.Meters[meterIndex].GeneralConfig.ClickThrough ^= true;
        }
    }

    private void DrawCreateMenu(Vector2 size, float padX)
    {
        var buttonSize = new Vector2(40, 0);
        var textInputWidth = size.X - buttonSize.X * 2 - padX * 4;

        if (!ImGui.BeginChild("##Buttons", new Vector2(size.X, MenuBarHeight), true))
        {
            ImGui.EndChild();
            return;
        }

        ImGui.PushItemWidth(textInputWidth);
        ImGui.InputTextWithHint("##Input", "Profile Name/Import String", ref _input, 10000);
        ImGui.PopItemWidth();

        ImGui.SameLine();
        DrawHelpers.DrawButton
        (
            string.Empty,
            FontAwesomeIcon.Plus,
            () => CreateMeter(_input),
            "Create new Meter",
            buttonSize
        );

        ImGui.SameLine();
        DrawHelpers.DrawButton
        (
            string.Empty,
            FontAwesomeIcon.Download,
            () => ImportMeter(_input),
            "Import new Meter",
            buttonSize
        );
        ImGui.PopItemWidth();

        ImGui.EndChild();
    }

    private void DrawMeterTable(Vector2 size, float padX)
    {
        ImGuiTableFlags flags =
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.BordersOuter |
            ImGuiTableFlags.BordersInner |
            ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.NoSavedSettings;

        if (!ImGui.BeginTable("##Meter_Table", 3, flags, new Vector2(size.X, size.Y - MenuBarHeight)))
        {
            ImGui.EndChild();
            return;
        }

        var buttonSize = new Vector2(30, 0);
        var actionsWidth = buttonSize.X * 3 + padX * 2;

        ImGui.TableSetupColumn("   #", ImGuiTableColumnFlags.WidthFixed, 18, 0);
        ImGui.TableSetupColumn("Profile Name", ImGuiTableColumnFlags.WidthStretch, 0, 1);
        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, actionsWidth, 2);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < this.Meters.Count; i++)
        {
            var meter = this.Meters[i];

            if 
            (
                !string.IsNullOrEmpty(_input) &&
                !meter.Name.Contains(_input, StringComparison.OrdinalIgnoreCase)
            )
            {
                continue;
            }

            ImGui.PushID(i.ToString());
            ImGui.TableNextRow(ImGuiTableRowFlags.None, 28);

            if (ImGui.TableSetColumnIndex(0))
            {
                var num = $"  {i + 1}.";
                var columnWidth = ImGui.GetColumnWidth();
                var cursorPos = ImGui.GetCursorPos();
                var textSize = ImGui.CalcTextSize(num);
                ImGui.SetCursorPos(new Vector2(cursorPos.X + columnWidth - textSize.X, cursorPos.Y + 3f));
                ImGui.Text(num);
            }

            if (ImGui.TableSetColumnIndex(1))
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3f);
                ImGui.Text(meter.Name);
            }

            if (ImGui.TableSetColumnIndex(2))
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1f);
                DrawHelpers.DrawButton
                (
                    string.Empty,
                    FontAwesomeIcon.Pen,
                    () => EditMeter(meter),
                    "Edit",
                    buttonSize
                );

                ImGui.SameLine();
                DrawHelpers.DrawButton
                (
                    string.Empty,
                    FontAwesomeIcon.Upload,
                    () => ExportMeter(meter),
                    "Export",
                    buttonSize
                );

                ImGui.SameLine();
                DrawHelpers.DrawButton
                (
                    string.Empty,
                    FontAwesomeIcon.Trash,
                    () => DeleteMeter(meter),
                    "Delete",
                    buttonSize
                );
            }
        }

        ImGui.EndTable();
    }

    private void CreateMeter(string name)
    {
        if (!string.IsNullOrEmpty(name)) this.Meters.Add(MeterWindow.GetDefaultMeter(name));

        _input = string.Empty;
    }

    private void EditMeter(MeterWindow meter) =>
        PluginManager.Instance.Edit(meter);

    private void DeleteMeter(MeterWindow meter) =>
        this.Meters.Remove(meter);

    private void ImportMeter(string input)
    {
        var importString = input;
        if (string.IsNullOrWhiteSpace(importString))
        {
            importString = ImGui.GetClipboardText();
        }

        var newMeter = ConfigHelpers.GetFromImportString<MeterWindow?>(importString);
        if (newMeter is not null)
        {
            this.Meters.Add(newMeter);
        }
        else
        {
            DrawHelpers.DrawNotification("Failed to Import Meter!", NotificationType.Error);
        }

        _input = string.Empty;
    }

    private void ExportMeter(MeterWindow meter) =>
        ConfigHelpers.ExportToClipboard(meter);
}
