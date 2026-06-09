using Godot;
using HarmonyLib;

public partial class ModInit : Node
{
    private Harmony _harmony = null!;

    public override void _Ready()
    {
        _harmony = new Harmony("SoulChange");
        _harmony.PatchAll();
    }
}
