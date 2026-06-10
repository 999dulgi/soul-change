using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;

namespace SoulChange;

public static class SoulChangeSettingsUi
{
    private static bool IsKorean =>
        LocManager.Instance?.Language == "kor";

    private static string T(string korean, string english) =>
        IsKorean ? korean : english;

    public static Control Build()
    {
        // 부모를 꽉 채우되 마우스 이벤트는 통과
        var container = new Control();
        container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        container.MouseFilter = Control.MouseFilterEnum.Ignore;

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        panel.OffsetLeft = -280f;
        panel.OffsetRight = -12f;
        panel.OffsetTop = 12f;
        panel.OffsetBottom = 432f;
        container.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        margin.AddChild(vbox);

        AddTitle(vbox, T("Soul Change 설정", "Soul Change Settings"));
        AddSeparator(vbox);
        AddLabel(vbox, T("스왑 발동 방:", "Swap on:"));

        AddRoomCheckBox(vbox, RoomType.Monster, T("일반 전투", "Normal Combat"));
        AddRoomCheckBox(vbox, RoomType.Elite, T("엘리트", "Elite"));
        AddRoomCheckBox(vbox, RoomType.Boss, T("보스", "Boss"));
        AddRoomCheckBox(vbox, RoomType.Event, T("미지방 (이벤트)", "Unknown (Event)"));
        AddRoomCheckBox(vbox, RoomType.Shop, T("상점", "Shop"));
        AddRoomCheckBox(vbox, RoomType.Treasure, T("보물방", "Treasure"));
        AddRoomCheckBox(vbox, RoomType.RestSite, T("휴식", "Rest Site"));

        AddSeparator(vbox);

        var nFloorRow = new HBoxContainer();
        var nFloorLabel = new Label();
        nFloorLabel.Text = T("N회마다 스왑:", "Swap every N floors:");
        nFloorLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        nFloorRow.AddChild(nFloorLabel);
        var spinBox = new SpinBox();
        spinBox.MinValue = 1;
        spinBox.MaxValue = 20;
        spinBox.Step = 1;
        spinBox.Value = SoulChangeConfig.SwapEveryNFloors;
        spinBox.CustomMinimumSize = new Vector2(80, 0);
        spinBox.ValueChanged += (val) => { SoulChangeConfig.SwapEveryNFloors = (int)val; SettingsSync.OnSettingsRequested(default); };
        nFloorRow.AddChild(spinBox);
        vbox.AddChild(nFloorRow);

        AddSeparator(vbox);

        var restoreCb = new CheckBox();
        restoreCb.Text = T("보스 진입시 원래 캐릭터로 복구", "Restore on Boss");
        restoreCb.ButtonPressed = SoulChangeConfig.RestoreOnBoss;
        restoreCb.Toggled += (pressed) => { SoulChangeConfig.RestoreOnBoss = pressed; SettingsSync.OnSettingsRequested(default); };
        vbox.AddChild(restoreCb);

        return container;
    }

    private static void AddTitle(VBoxContainer parent, string text)
    {
        var label = new Label();
        label.Text = text;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        parent.AddChild(label);
    }

    private static void AddLabel(VBoxContainer parent, string text)
    {
        var label = new Label();
        label.Text = text;
        parent.AddChild(label);
    }

    private static void AddSeparator(VBoxContainer parent)
    {
        parent.AddChild(new HSeparator());
    }

    private static void AddRoomCheckBox(VBoxContainer parent, RoomType roomType, string text)
    {
        var cb = new CheckBox();
        cb.Text = text;
        cb.ButtonPressed = SoulChangeConfig.TriggerRooms.Contains(roomType);
        cb.Toggled += (pressed) =>
        {
            if (pressed)
                SoulChangeConfig.TriggerRooms.Add(roomType);
            else
                SoulChangeConfig.TriggerRooms.Remove(roomType);
            SettingsSync.OnSettingsRequested(default);
        };
        parent.AddChild(cb);
    }
}
