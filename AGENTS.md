# Casino Simulation repository guidance

## Product constraints

- This is an entertainment-only casino simulation. Chips cannot be purchased, withdrawn, transferred between players, or exchanged for anything of real-world value.
- Multiplayer is friends-only for the current scope. The player who creates a room is the authoritative Host; there is no dedicated game server, public matchmaking, or host migration.
- The casino uses compact themed rooms. Players do not walk freely. A seating cinematic places them at a table, after which the camera remains at the seat but may rotate locally.
- The first playable game is blackjack. Roulette and sic bo are later modules used to validate extensibility.
- Visual style is deliberately low-detail and stylized. Gameplay objects such as cards, chips, dice, and betting areas receive the strongest readability.

## Engineering rules

- Use Unity 6000.3 LTS and URP. Do not upgrade the editor or render pipeline without an explicit decision.
- Keep rules and economy logic in pure C# assemblies with no dependency on `UnityEngine` or networking packages.
- The Host owns random results, legal-action validation, AI decisions, state transitions, and settlement. Clients submit intentions and render confirmed state.
- Presentation must never determine game results. Animations consume confirmed events from the rules layer.
- Use integer chip units. Every balance change must have a unique transaction or round identifier and be safe to apply more than once.
- Do not implement public matchmaking, dedicated servers, host migration, real-money systems, player-to-player chip transfers, or free-roaming character movement.
- Keep gameplay variants configurable, but implement only the rules explicitly selected in `Docs/二十一点玩法详细设计_v0.1.md`.

## Repository practices

- Preserve Unity `.meta` files and move assets together with their `.meta` files.
- Do not commit `Library`, `Temp`, `Logs`, `Obj`, `UserSettings`, IDE caches, or local builds.
- Put runtime code under `Assets/Casino/Scripts`, edit-mode tests under `Assets/Casino/Tests/EditMode`, and play-mode tests under `Assets/Casino/Tests/PlayMode`.
- Use assembly definitions to keep Core, game modules, presentation, networking, persistence, and tests separated.
- Prefer small, reviewable changes. Do not combine rule changes with scene or art rework.
- Before completing a task, run the relevant Unity tests when the editor is available and report any checks that could not run.

## Source of truth

Read these files before implementation:

1. `Docs/赌场模拟游戏_整体设计_v0.1.md`
2. `Docs/二十一点玩法详细设计_v0.1.md`
3. `Docs/技术架构_v0.1.md`
4. `TASKS.md`

If documents conflict, the most specific gameplay document wins for its game, while this file wins for repository and engineering constraints. Ask before changing a settled product decision.
