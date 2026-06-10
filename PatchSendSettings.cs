using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;

namespace SoulChange;

internal static class SettingsSync
{
    private static MessageHandlerDelegate<SoulChangeSettingsMessage> _settingsHandler;
    private static MessageHandlerDelegate<SoulChangeSettingsRequestMessage> _requestHandler;
    private static INetGameService _netService;

    internal static void RegisterHandler(INetGameService netService)
    {
        if (!netService.Type.IsMultiplayer()) return;
        if (_netService == netService) return;

        if (_netService != null)
        {
            if (_settingsHandler != null) _netService.UnregisterMessageHandler(_settingsHandler);
            if (_requestHandler != null) _netService.UnregisterMessageHandler(_requestHandler);
            _requestHandler = null;
        }

        _netService = netService;
        _settingsHandler = OnSettingsReceived;
        _netService.RegisterMessageHandler(_settingsHandler);

        if (netService.Type == NetGameType.Host)
        {
            _requestHandler = OnSettingsRequested;
            _netService.RegisterMessageHandler(_requestHandler);
            Godot.GD.Print("[SoulChange] 호스트: 핸들러 등록 완료");
        }
        else
        {
            // 핸들러 등록 후 즉시 호스트에게 설정 요청
            _netService.SendMessage(new SoulChangeSettingsRequestMessage());
            Godot.GD.Print("[SoulChange] 클라이언트: 설정 요청 전송");
        }
    }

    private static void OnSettingsRequested(SoulChangeSettingsRequestMessage _, ulong senderId)
    {
        if (_netService == null) return;
        _netService.SendMessage(SoulChangeConfig.ToMessage(), senderId);
        Godot.GD.Print($"[SoulChange] 호스트: peer {senderId}에게 설정 전송");
    }

    internal static void OnSettingsRequested(SoulChangeSettingsRequestMessage _)
    {
        if (_netService == null) return;
        _netService.SendMessage(SoulChangeConfig.ToMessage());
        Godot.GD.Print("[SoulChange] 호스트: 모든 클라이언트에게 설정 전송");
    }

    internal static void OnSettingsReceived(SoulChangeSettingsMessage msg, ulong _)
    {
        if (msg.ModVersion != SoulChangeConfig.ModVersion)
        {
            Godot.GD.Print($"[SoulChange] 버전 불일치: 호스트={msg.ModVersion}, 클라이언트={SoulChangeConfig.ModVersion}");
            bool isKorean = LocManager.Instance?.Language == "kor";
            var popup = NErrorPopup.Create(
                isKorean ? "Soul Change - 버전 불일치" : "Soul Change - Version Mismatch",
                isKorean
                    ? $"호스트의 Soul Change 버전({msg.ModVersion})과\n클라이언트의 버전({SoulChangeConfig.ModVersion})이 다릅니다.\n모드를 동일한 버전으로 업데이트해주세요."
                    : $"Host's Soul Change version ({msg.ModVersion}) does not match\nclient's version ({SoulChangeConfig.ModVersion}).\nPlease update the mod to the same version.",
                showReportBugButton: false
            );
            if (popup != null)
                NModalContainer.Instance.Add(popup);
            return;
        }
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
