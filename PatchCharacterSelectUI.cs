using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

namespace SoulChange;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsHost))]
public class PatchCharacterSelectUI
{
    private const string PanelName = "SoulChangeSettingsPanel";

    static void Postfix(NCharacterSelectScreen __instance, INetGameService gameService)
    {
        Godot.GD.Print("[SoulChange] NCharacterSelectScreen.InitializeMultiplayerAsHost 패치 실행");
        SettingsSync.RegisterHandler(gameService);
        AddPanelDeferred(__instance);
    }

    internal static void AddPanelDeferred(Godot.Node __instance)
    {
        Godot.Callable.From(() =>
        {
            if (!Godot.GodotObject.IsInstanceValid(__instance)) return;
            if (__instance.FindChild(PanelName, owned: false) != null) return;

            Godot.GD.Print("[SoulChange] 설정 패널 추가");
            var panel = SoulChangeSettingsUi.Build();
            panel.Name = PanelName;
            __instance.AddChild(panel);
        }).CallDeferred();
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeMultiplayerAsHost))]
public class PatchCustomRunUI
{
    static void Postfix(NCustomRunScreen __instance, INetGameService gameService)
    {
        Godot.GD.Print("[SoulChange] NCustomRunScreen.InitializeMultiplayerAsHost 패치 실행");
        SettingsSync.RegisterHandler(gameService);
        PatchCharacterSelectUI.AddPanelDeferred(__instance);
    }
}
