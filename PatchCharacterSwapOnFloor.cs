using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace SoulChange;

[HarmonyPatch(typeof(AbstractRoom), nameof(AbstractRoom.Enter))]
public class PatchCharacterSwapOnFloor
{
    private static int _lastSwapFloor = -1;
    private static int _rotationOffset = 0;
    private static int _triggerCount = 0; // 트리거 방 통과 횟수

    static void Prefix(AbstractRoom __instance, IRunState runState, bool isRestoringRoomStackBase)
    {
        if (runState is not RunState state || isRestoringRoomStackBase) return;
        if (state.Players.Count < 2) return;

        bool isBoss = SoulChangeConfig.RestoreOnBoss && __instance.RoomType == RoomType.Boss;
        bool isTrigger = SoulChangeConfig.TriggerRooms.Contains(__instance.RoomType);
        if (!isBoss && !isTrigger) return;

        int currentFloor = state.ActFloor;
        if (currentFloor == _lastSwapFloor) return;

        if (currentFloor < _lastSwapFloor)
        {
            _rotationOffset = 0;
            _triggerCount = 0;
        }

        _lastSwapFloor = currentFloor;

        var players = state.Players.ToList();
        int playerCount = players.Count;

        int rotateBy;
        if (isBoss)
        {
            rotateBy = (playerCount - _rotationOffset) % playerCount;
            _rotationOffset = 0;
            _triggerCount = 0;
        }
        else
        {
            _triggerCount++;
            if (_triggerCount < SoulChangeConfig.SwapEveryNFloors)
            {
                Log($"[SoulChange] Floor {currentFloor} ({__instance.RoomType}): 트리거 {_triggerCount}/{SoulChangeConfig.SwapEveryNFloors}, 아직 스왑 안 함.");
                return;
            }
            _triggerCount = 0;
            rotateBy = 1;
            _rotationOffset = (_rotationOffset + 1) % playerCount;
        }

        if (rotateBy == 0)
        {
            Log($"[SoulChange] Floor {currentFloor} (BOSS): 이미 원래 정렬 상태, 스왑 없음.");
            return;
        }

        Log($"[SoulChange] Floor {currentFloor} ({(isBoss ? "BOSS→원복" : $"SWAP/{__instance.RoomType}")}): rotateBy={rotateBy}. Before: " +
            string.Join(", ", players.Select((p, i) => $"[{i}]NetId={p.NetId} Char={p.Character?.Id}")));

        var playerStateNodes = GetPlayerStateNodes();
        foreach (var node in playerStateNodes)
            node._ExitTree();

        var topBar = NRun.Instance?.GlobalUi?.TopBar;
        if (topBar != null)
        {
            topBar.Hp._ExitTree();
            topBar.Gold._ExitTree();
            topBar.Deck._ExitTree();
        }

        for (int r = 0; r < rotateBy; r++)
            DoRotateAllFields(players);

        var capturedRunState = state;
        var capturedTopBar = topBar;
        var capturedNodes = playerStateNodes;
        Godot.Callable.From(() =>
        {
            foreach (var node in capturedNodes)
            {
                var healthBar = Traverse.Create(node).Field("_healthBar").GetValue();
                if (healthBar != null)
                    Traverse.Create(healthBar).Field("_creature").SetValue(null);
                node._Ready();
            }
            RefreshRelicInventory(capturedRunState);
            if (capturedTopBar != null)
                RefreshTopBar(capturedTopBar, capturedRunState);
        }).CallDeferred();

        Log($"[SoulChange] DONE. After: " +
            string.Join(", ", players.Select((p, i) => $"[{i}]NetId={p.NetId} Char={p.Character?.Id}")));
    }

    private static void DoRotateAllFields(IReadOnlyList<Player> players)
    {
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
        RebindPotionOwners(players);
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
    }

    private static void RefreshTopBar(NTopBar topBar, RunState runState)
    {
        var player = LocalContext.GetMe(runState);
        if (player == null) return;

        for (int i = topBar.Portrait.GetChildCount() - 1; i >= 0; i--)
            topBar.Portrait.RemoveChild(topBar.Portrait.GetChild(i));
        topBar.Portrait.Initialize(player);

        topBar.Hp.Initialize(player);
        topBar.Gold.Initialize(player);
        topBar.Deck.Initialize(player);

        ClearPotionHolders(topBar.PotionContainer);
        topBar.PotionContainer.Initialize(runState);
    }

    private static void ClearPotionHolders(Godot.Node container)
    {
        var holders = Traverse.Create(container).Field("_holders").GetValue() as IList;
        if (holders == null) return;

        var toRemove = holders.Cast<Godot.Node>().ToList();
        holders.Clear();
        foreach (var holder in toRemove)
        {
            holder.GetParent()?.RemoveChild(holder);
            holder.QueueFree();
        }
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

    private static void RotateField(IReadOnlyList<Player> players, string field)
    {
        var traversals = players.Select(p => Traverse.Create(p).Field(field)).ToList();
        var saved = traversals[0].GetValue();
        for (int i = 0; i < players.Count - 1; i++)
            traversals[i].SetValue(traversals[i + 1].GetValue());
        traversals[^1].SetValue(saved);
    }

    private static void RotatePublic<T>(IReadOnlyList<Player> players, Func<Player, T> getter, Action<Player, T> setter)
    {
        var values = players.Select(getter).ToList();
        var saved = values[0];
        for (int i = 0; i < players.Count - 1; i++)
            setter(players[i], values[i + 1]);
        setter(players[^1], saved);
    }

    private static void ResetRunPiles(IReadOnlyList<Player> players)
    {
        foreach (var player in players)
            Traverse.Create(player).Field("_runPiles").SetValue(null);
    }

    private static void RebindCreaturePlayers(IReadOnlyList<Player> players)
    {
        foreach (var player in players)
        {
            var creature = Traverse.Create(player).Field("<Creature>k__BackingField").GetValue();
            if (creature == null) continue;
            Traverse.Create(creature).Field("<Player>k__BackingField").SetValue(player);
        }
    }

    private static void RebindCardOwners(IReadOnlyList<Player> players)
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

    private static void RebindRelicOwners(IReadOnlyList<Player> players)
    {
        foreach (var player in players)
        {
            var relics = Traverse.Create(player).Field("_relics").GetValue() as IList;
            if (relics == null) continue;
            foreach (var relic in relics)
                Traverse.Create(relic).Field("_owner").SetValue(player);
        }
    }

    private static void RebindPotionOwners(IReadOnlyList<Player> players)
    {
        foreach (var player in players)
        {
            var potions = Traverse.Create(player).Field("_potionSlots").GetValue() as IList;
            if (potions == null) continue;
            foreach (var potion in potions)
            {
                if (potion == null) continue;
                Traverse.Create(potion).Field("_owner").SetValue(player);
            }
        }
    }
}
