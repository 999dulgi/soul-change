# Soul Change

A multiplayer mod for **Slay the Spire 2** that swaps every player's character, deck, relics, and full run state at the start of each floor.

## What It Does

In a multiplayer run, every time the party moves to a new floor, all players' "souls" rotate — each player takes over the other's character completely. This includes:

- Character and creature (HP, powers)
- Deck (all cards)
- Relics and potions
- Gold, energy, orb slots
- RNG streams (PlayerRng, PlayerOdds)
- Discovered cards/relics/potions/enemies/epochs
- Ascension level and extra player fields

Implemented as a [Harmony](https://github.com/pardeike/Harmony) `Prefix` patch on `CombatStateSynchronizer.StartSync`, which fires before every room entry (combat, event, shop, rest). All UI — health bar, portrait, relic inventory, potion slots, deck button, gold display — is refreshed automatically after each swap.

## Requirements

- Slay the Spire 2 (Steam)
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Godot 4 with .NET (for the project structure)

## Building

```bash
dotnet build SoulChange.csproj
```

The build automatically copies `SoulChange.dll` and `SoulChange_manifest.json` to:

```
C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\soul-change\
```

## Project Structure

```
PatchCharacterSwapOnFloor.cs   # Core Harmony patch — field swap + UI refresh
ModInit.cs                     # Harmony bootstrap (PatchAll)
SoulChange_manifest.json       # Mod metadata
SoulChange.csproj              # Project file with game assembly references
```

## Notes

- The swap preserves each player's `NetId` and `UnlockState` (account-level progress).
- `LocalContext.NetId` is never modified — multiplayer sync remains intact.
- Tested with 2-player local multiplayer sessions.
