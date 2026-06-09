using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Runs;

namespace SoulChange;

[HarmonyPatch(typeof(CombatStateSynchronizer), "StartSync")]
public class PatchCharacterSwapOnFloor
{
    private static int _lastSwapFloor = -1;

    static void Prefix(CombatStateSynchronizer __instance)
    {
        var runState = Traverse.Create(__instance).Field("_runState").GetValue<RunState>();
        if (runState == null || runState.Players.Count < 2) return;

        int currentFloor = runState.ActFloor;
        if (currentFloor == _lastSwapFloor) return;
        _lastSwapFloor = currentFloor;

        var players = Traverse.Create(runState).Field("_players").GetValue<List<Player>>();
        if (players == null || players.Count < 2) return;

        Log($"[SoulChange] Floor {currentFloor}: SWAP START. Before: " +
            string.Join(", ", players.Select((p, i) => $"[{i}]NetId={p.NetId} Char={p.Character?.Id}")));

        var playerStateNodes = GetPlayerStateNodes();
        foreach (var node in playerStateNodes)
            node._ExitTree();

        RotateField(players, "<Character>k__BackingField");
        RotateField(players, "<Creature>k__BackingField");
        RebindCreaturePlayers(players);
        RotateField(players, "<Deck>k__BackingField");
        RebindCardOwners(players);
        ResetRunPiles(players);
        RotateField(players, "_relics");
        RebindRelicOwners(players);
        RotateField(players, "_gold");
        RotateField(players, "_potionSlots");
        RotateField(players, "<ExtraFields>k__BackingField");
        RotateField(players, "<PlayerRng>k__BackingField");
        RotateField(players, "<PlayerOdds>k__BackingField");
        RotateField(players, "<RelicGrabBag>k__BackingField");
        RotatePublic(players, p => p.MaxEnergy, (p, v) => p.MaxEnergy = v);
        RotatePublic(players, p => p.BaseOrbSlotCount, (p, v) => p.BaseOrbSlotCount = v);
        RotatePublic(players, p => p.DiscoveredCards, (p, v) => p.DiscoveredCards = v);
        RotatePublic(players, p => p.DiscoveredRelics, (p, v) => p.DiscoveredRelics = v);
        RotatePublic(players, p => p.DiscoveredPotions, (p, v) => p.DiscoveredPotions = v);
        RotatePublic(players, p => p.DiscoveredEnemies, (p, v) => p.DiscoveredEnemies = v);
        RotatePublic(players, p => p.DiscoveredEpochs, (p, v) => p.DiscoveredEpochs = v);
        RotateField(players, "<MaxAscensionWhenRunStarted>k__BackingField");
        RotateField(players, "_canRemovePotions");

        foreach (var node in playerStateNodes)
        {
            var healthBar = Traverse.Create(node).Field("_healthBar").GetValue();
            if (healthBar != null)
                Traverse.Create(healthBar).Field("_creature").SetValue(null);
            node._Ready();
        }
        RefreshRelicInventory(runState);

        Log($"[SoulChange] SWAP DONE. After: " +
            string.Join(", ", players.Select((p, i) => $"[{i}]NetId={p.NetId} Char={p.Character?.Id}")));
    }

    private static List<NMultiplayerPlayerState> GetPlayerStateNodes()
    {
        var container = NRun.Instance?.GlobalUi?.MultiplayerPlayerContainer;
        if (container == null) return new List<NMultiplayerPlayerState>();

        var result = new List<NMultiplayerPlayerState>();
        for (int i = 0; i < container.GetChildCount(); i++)
        {
            if (container.GetChild(i) is NMultiplayerPlayerState node)
                result.Add(node);
        }
        return result;
    }

    private static void RefreshRelicInventory(RunState runState)
    {
        var inventory = NRun.Instance?.GlobalUi?.RelicInventory;
        if (inventory == null) return;

        var relicNodes = Traverse.Create(inventory).Field("_relicNodes").GetValue() as IList;
        if (relicNodes != null)
        {
            var toRemove = relicNodes.Cast<Godot.Node>().ToList();
            relicNodes.Clear();
            foreach (var node in toRemove)
            {
                inventory.RemoveChild(node);
                node.QueueFree();
            }
        }

        inventory.Initialize(runState);
    }

    private static void Log(string msg) => Godot.GD.Print(msg);

    private static void RotateField(List<Player> players, string field)
    {
        var traversals = players.Select(p => Traverse.Create(p).Field(field)).ToList();
        var saved = traversals[0].GetValue();
        for (int i = 0; i < players.Count - 1; i++)
            traversals[i].SetValue(traversals[i + 1].GetValue());
        traversals[^1].SetValue(saved);
    }

    private static void RotatePublic<T>(List<Player> players, Func<Player, T> getter, Action<Player, T> setter)
    {
        var values = players.Select(getter).ToList();
        var saved = values[0];
        for (int i = 0; i < players.Count - 1; i++)
            setter(players[i], values[i + 1]);
        setter(players[^1], saved);
    }

    private static void ResetRunPiles(List<Player> players)
    {
        foreach (var player in players)
            Traverse.Create(player).Field("_runPiles").SetValue(null);
    }

    private static void RebindCreaturePlayers(List<Player> players)
    {
        foreach (var player in players)
        {
            var creature = Traverse.Create(player).Field("<Creature>k__BackingField").GetValue();
            if (creature == null) continue;
            Traverse.Create(creature).Field("<Player>k__BackingField").SetValue(player);
        }
    }

    private static void RebindCardOwners(List<Player> players)
    {
        foreach (var player in players)
        {
            var deck = Traverse.Create(player).Field("<Deck>k__BackingField").GetValue();
            if (deck == null) continue;
            var cards = Traverse.Create(deck).Field("_cards").GetValue() as IList;
            if (cards == null) continue;
            foreach (var card in cards)
                Traverse.Create(card).Field("_owner").SetValue(player);
        }
    }

    private static void RebindRelicOwners(List<Player> players)
    {
        foreach (var player in players)
        {
            var relics = Traverse.Create(player).Field("_relics").GetValue() as IList;
            if (relics == null) continue;
            foreach (var relic in relics)
                Traverse.Create(relic).Field("_owner").SetValue(player);
        }
    }
}
