using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

namespace SoulChange;

internal static class SettingsSync
{
    private static MessageHandlerDelegate<SoulChangeSettingsMessage> _settingsHandler;
    private static MessageHandlerDelegate<SoulChangeSettingsRequestMessage> _requestHandler;
    private static INetGameService _lastService;

    internal static void RegisterHandler(INetGameService netService)
    {
        if (!netService.Type.IsMultiplayer()) return;

        if (_lastService != null && _lastService != netService)
        {
            if (_settingsHandler != null) _lastService.UnregisterMessageHandler(_settingsHandler);
            if (_requestHandler != null) _lastService.UnregisterMessageHandler(_requestHandler);
            _requestHandler = null;
        }

        _lastService = netService;
        _settingsHandler = OnSettingsReceived;
        netService.RegisterMessageHandler(_settingsHandler);

        if (netService.Type == NetGameType.Host)
        {
            _requestHandler = OnSettingsRequested;
            netService.RegisterMessageHandler(_requestHandler);
            Godot.GD.Print("[SoulChange] 호스트: 핸들러 등록 완료");
        }
        else
        {
            // 핸들러 등록 후 즉시 호스트에게 설정 요청
            netService.SendMessage(new SoulChangeSettingsRequestMessage());
            Godot.GD.Print("[SoulChange] 클라이언트: 설정 요청 전송");
        }
    }

    private static void OnSettingsRequested(SoulChangeSettingsRequestMessage _, ulong senderId)
    {
        if (_lastService == null) return;
        _lastService.SendMessage(SoulChangeConfig.ToMessage(), senderId);
        Godot.GD.Print($"[SoulChange] 호스트: peer {senderId}에게 설정 전송");
    }

    internal static void OnSettingsReceived(SoulChangeSettingsMessage msg, ulong _)
    {
        SoulChangeConfig.ApplyMessage(msg);
        Godot.GD.Print($"[SoulChange] 클라이언트: 설정 수신 완료. RestoreOnBoss={msg.RestoreOnBoss}, TriggerRoomFlags={msg.TriggerRoomFlags}, SwapEveryNFloors={msg.SwapEveryNFloors}");
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsClient))]
public class PatchLobbyClientSettings
{
    static void Postfix(INetGameService gameService) => SettingsSync.RegisterHandler(gameService);
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeMultiplayerAsClient))]
public class PatchCustomRunClientSettings
{
    static void Postfix(INetGameService gameService) => SettingsSync.RegisterHandler(gameService);
}
