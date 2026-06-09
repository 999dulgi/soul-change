using System;
using Godot;
using HarmonyLib;

public partial class ModInit : Node
{
    private Harmony _harmony = null!;

    public override void _Ready()
    {
        _harmony = new Harmony("SoulChange");
        Patch<SoulChange.PatchCharacterSwapOnFloor>();
        Patch<SoulChange.PatchCharacterSelectUI>();
        Patch<SoulChange.PatchCustomRunUI>();
        Patch<SoulChange.PatchLobbyClientSettings>();
        Patch<SoulChange.PatchCustomRunClientSettings>();
    }

    private void Patch<T>()
    {
        try
        {
            _harmony.CreateClassProcessor(typeof(T)).Patch();
            GD.Print($"[SoulChange] 패치 성공: {typeof(T).Name}");
        }
        catch (Exception e)
        {
            GD.PrintErr($"[SoulChange] 패치 실패: {typeof(T).Name}: {e.Message}");
        }
    }
}
