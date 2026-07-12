# Casino Simulation repository guidance

## Product constraints

- This is an entertainment-only casino party game. Official in-game chips cannot be purchased, withdrawn, sold, manually transferred between players, or exchanged for anything of real-world value. Player-versus-player tables may move official chips only through authoritative game bets, pots, and settlement.
- Official in-game chip balances have no product-level hard cap. Betting limits are table or variant configuration, not a global maximum; normal tables may still define caps, and any no-limit or all-in table must be an explicit variant.
- If borrowing or relief exists, it is provided by the casino/system as personal player credit or relief. Player-to-player lending remains forbidden, and exact credit amounts, repayment behavior, and cooldowns are pending design decisions.
- Whether a player has borrowed from the casino/system must not by itself restrict which games, tables, or cosmetic/non-gameplay purchases they can access. Credit status may affect only credit-system terms such as borrowing limit, repayment, credit score, warnings, and ledger display.
- Multiplayer is friends-only for the current scope. A friends room is a persistent party with up to six human players; the player who creates the party is the authoritative Host. There is no dedicated gameplay server, public matchmaking, or host migration. The exact internet transport or relay service is pending.
- The casino uses compact themed rooms. Players do not walk freely. A seating cinematic places them at a table, after which the camera remains at the seat but may rotate locally. The local player's low-detail body and hands are visible from the seated view.
- The main room/game selection uses a fixed casino overview or floor-plan view as the primary interface, with a simple button menu as support. Do not build a freely walkable lobby or navigation hall.
- Switching games does not always require changing the physical table. If the target game or variant uses the same table layout, seats, betting surface, and room, switch rules/configuration in place; only queue a table or room transition when the physical table, equipment, betting layout, or room is different.
- Player avatars are low-detail humanoids with either a playing-card head or a casino-chip head. First-time onboarding lets the player draw one current face in-game with a deliberately simple childlike drawing tool: brush, eraser, undo, clear, and a small fixed color palette on a fixed-size canvas. Arbitrary image import, full drawing-app features, expression presets, facial rigs, multiple saved face presets, and changing the face during an active game session are out of scope.
- The first playable game is blackjack with six player seats so the whole party can participate. Roulette and sic bo are later modules used to validate extensibility; poker is a later, more complex player-versus-player module.
- Visual style is deliberately rough, low-detail, and stylized rather than realistic. Gameplay objects such as cards, chips, dice, roulette surfaces, and betting areas receive the strongest readability.
- This is not a free-form physics sandbox. Players cannot freely grab, hide, move, or throw gameplay cards, chips, dice, roulette parts, or other rule objects. Cosmetic party actions may be synchronized with cooldowns, but rules objects move only through confirmed game actions.
- There is no built-in voice chat in the current scope; friends may use external voice tools.

## Engineering rules

- Use Unity 6000.3 LTS and URP. Do not upgrade the editor or render pipeline without an explicit decision.
- Keep rules and economy logic in pure C# assemblies with no dependency on `UnityEngine` or networking packages.
- The Host owns random results, legal-action validation, AI decisions, state transitions, and settlement. Clients submit intentions and render confirmed state.
- Presentation must never determine game results. Animations consume confirmed events from the rules layer.
- Use integer chip units. Every balance change, including casino/system credit or relief, must have a unique transaction or round identifier and be safe to apply more than once.
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

Read these files before implementation or design changes:

1. `CODEX_HANDOFF_2026-07-12.md` when present
2. `Docs/赌场模拟游戏_整体设计_v0.1.md`
3. `Docs/二十一点玩法详细设计_v0.1.md`
4. `Docs/技术架构_v0.1.md`
5. `TASKS.md`

If documents conflict, the newest explicit handoff or user-confirmed decision wins first, then the most specific gameplay document wins for its game, while this file wins for repository and engineering constraints. Ask before changing a settled product decision, and keep pending decisions marked pending.
