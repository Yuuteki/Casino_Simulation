# Casino Simulation — Codex handoff (2026-07-12)

Read this entire handoff before taking action. Treat the decisions below as the current product source of truth. If older repository documents conflict with this handoff, update those documents first and follow this handoff. Do not invent or expand features that are marked pending.

## 1. Current user intent

The user wants to continue **discussing and refining the game design** with Codex. Do not automatically start Phase 2 or later implementation work. Phase 1 has reportedly been completed in the user's local project, but must be inspected and tested before any later code work.

When the user eventually asks to resume implementation:

1. Inspect `git status`, the current branch, existing source, assembly definitions, and tests.
2. Read `AGENTS.md`, `TASKS.md`, and every file under `Docs/`.
3. Run the available Unity EditMode tests.
4. Report what Phase 1 actually contains, any conflicts with the design, and what could not be verified.
5. Do not start the next phase until the user approves the reviewed state.

## 2. Project and repository

- Repository: `https://github.com/Yuuteki/Casino_Simulation`
- Engine: Unity `6000.3.10f1` (Unity 6.3 LTS)
- Render pipeline: URP
- Initial target: Windows PC
- Art direction: deliberately rough, stylized, low-detail 3D rather than realism
- Keep cards, chips, dice, roulette surfaces, and betting information more readable and polished than background furniture.
- GitHub initially contained a clean URP project with a correct Unity `.gitignore`; no `Library`, `Temp`, `Logs`, `Obj`, or `UserSettings` were committed.

## 3. Product identity

- Primary identity: a friends-oriented **casino party game**.
- Secondary identity: a social casino sandbox containing multiple modular games.
- It is not primarily a casino-management game.
- It does not use real money.
- In-game chips cannot be purchased, withdrawn, sold, traded for real-world value, or manually transferred between players.
- Do not include betting based on external results such as real sports, horse races, lotteries, financial prices, or third-party outcomes.

## 4. Core casino layout and navigation

- The casino is a compact 3D interior divided into small functional rooms.
- Proposed functional rooms:
  - Card room: blackjack first; baccarat and poker later
  - Roulette room
  - Dice room: sic bo first; craps later
  - Machine room: slots and other single-player machines later
- Each room contains only playable equipment, necessary seats/dealer positions, lighting, and a small amount of decoration.
- Do not build a large resort, long corridors, hotel rooms, restaurants, bars, or empty explorable areas.
- Players do **not** walk freely and there is no free-roaming character controller.
- Selecting a room/game plays a short entry and seating cinematic, then places the player directly at the table.
- The exact main-menu method for selecting rooms is **pending**: it may become a fixed casino overview/floor plan or a conventional menu.

## 5. Camera and embodiment

- After seating, the camera position is fixed at the player's seat.
- The player may rotate the local view horizontally and within a comfortable vertical range.
- The player cannot translate, stand up, walk, or use a flying/free-roam camera.
- Provide a recenter action that smoothly looks back toward the table/dealer.
- The local player's body and hands are visible when looking down.
- Important actions, timers, hand values, and betting UI remain readable even when the player looks away.
- Camera orientation is local presentation state and does not affect game rules.

## 6. Player avatar design

- Characters use low-detail humanoid bodies.
- Head choice:
  - a playing-card head, or
  - a casino-chip head
- During first-time onboarding, the player receives an anonymous casino invitation, chooses a blank card or chip, draws a face on it, and it becomes the character's head before the casino doors open.
- Each player has exactly **one current hand-drawn face**.
- Drawing happens inside the game; do not add arbitrary image importing.
- Redrawing replaces the previous face.
- The face cannot be changed during an active game session.
- Do not add separate neutral/win/lose faces, facial rigs, expression variants, or multiple saved face presets.
- Emotion is shown through head tilt/shake/spin, body and hand animation, and simple overhead symbols.
- A local safety option may replace other users' drawings with a default face.

## 7. Party interaction

- This is not a free-form physics sandbox.
- Players cannot freely grab, hide, move, or throw gameplay cards, chips, dice, or roulette parts.
- Legal game actions produce controlled hand/object animations after authoritative confirmation.
- Synchronized cosmetic actions may include:
  - knock on table
  - clap
  - wave
  - shrug
  - facepalm
  - thumbs-up
  - card/chip-head shake, turn, or wobble
- Add a short cooldown and limit repeated sounds to prevent spam.
- No built-in voice chat in the current scope; friends may use external voice tools such as Discord.
- A small number of preset phrases/emote symbols is acceptable.

## 8. Multiplayer model

- Current multiplayer is friends-only.
- The player who creates the room is the authoritative Host and also plays normally.
- No dedicated gameplay server for the current scope.
- No public matchmaking.
- No automatic host migration; if the Host leaves or loses connection, the party closes safely.
- The precise internet transport/relay solution is not yet a settled product decision.
- Host responsibilities include randomness, legal-action validation, AI, timers, round state, and settlement.
- Clients send intentions and render confirmed state; presentation never decides results.

## 9. Persistent party session

- A friends room is a persistent party, not a room tied to one specific table.
- Maximum party size is currently **six human players**.
- AI does not consume party-member slots; AI only fills unused positions on the active table.
- After joining once, the same connected group can switch between blackjack, roulette, sic bo, poker, and other supported games without leaving, rebuilding the room, or receiving new invitations.
- Party membership, Host, connections, appearance data, and confirmed official chip balances survive a game switch.
- Round state, shoe/deck, bets, timers, and game-specific temporary data never carry into another game.
- Core games should allow all six party members to participate simultaneously when their rules permit.
- If a future game has fewer positions, extra party members remain spectators instead of being removed.

## 10. Joining, leaving, and game switching

- A friend may join a non-full party at any time.
- Joining never resets, rewinds, or pauses an active round.
- A mid-round joiner loads as a spectator and becomes eligible at the next safe participation point defined by that game.
- A locked bet cannot be refunded merely because the player leaves or disconnects.
- Absence handling is game-specific: AI takeover, automatic stand/check, or fold while preserving authoritative settlement.
- Reconnecting restores party membership and the latest confirmed state; it cannot undo actions confirmed during absence.
- Normal tables are open-ended. There is no required number of rounds, mandatory match score, or automatic return to a lobby.
- The party continues until members leave or the Host requests a different table.
- A Host table-change request made during a round is queued. The current round completes and settles, new betting pauses, and only then does the party load the next table.

## 11. Chip economy

- Use the term **official in-game chips** rather than “real chips” to avoid confusion with real money.
- Normal single-player games against AI use the player's persistent official chip balance.
- Normal friends-only games use the same persistent official chip balance.
- Practice/tutorial mode uses separate, non-persistent practice chips and must be selected explicitly.
- Practice results do not affect official balance, official records, economic statistics, achievements, or progression rewards.
- If official chips reach zero, the player may still practice and spectate.
- Provide a daily chip supply and a limited relief mechanism; exact amounts and timing are **pending**.
- Direct gifting, lending, trading, and manual player-to-player transfers are forbidden.
- Player-versus-player tables such as poker may legitimately move official chips between players through bets, pots, and authoritative table settlement.
- Every chip change must reference a concrete completed game/round and a unique identifier; there is no generic transfer API.

## 12. First and future casino games

- First complete game: blackjack.
- Blackjack was changed from four seats to **six player seats** so the entire party can participate.
- Initial blackjack direction already documented:
  - six decks
  - dealer stands on soft 17
  - natural blackjack pays 3:2
  - hit, stand, double, one split, split-aces rule, and insurance
- Next modules: roulette and sic bo, used to verify the common framework.
- Poker is a later, more complex player-versus-player module.
- Baccarat, craps, slots, and other suitable self-contained casino games may follow.

## 13. AI

- Normal AI games use official chips, not free/practice chips.
- The system dealer follows deterministic game rules.
- AI players may fill empty table positions and must never read hidden information they are not entitled to know.
- Whether AI characters are a fixed cast of named casino regulars or randomly generated visitors is **pending**.
- Persistent AI bankroll and personality details are also not finalized.

## 14. Narrative and tone

- The game is a party game first; there is no mandatory main story, campaign, story level sequence, or ending structure.
- Light narrative is allowed only to add atmosphere.
- Tone: playful and absurd on the surface, with a lightly mysterious background that is never fully explained.
- Do not fully explain why the casino exists or why people have playing-card/chip heads.
- First-time onboarding uses the anonymous invitation and hand-drawn-face transformation described above.
- Environmental storytelling may appear through posters, props, wall details, broadcasts, short character lines, rumors, and small unlockable illustrations.
- Story must never gate core casino games.
- Multiplayer must never force everyone to wait through long dialogue or cutscenes; narrative presentation should be brief, local, and skippable.

## 15. Progression

The following is proposed but **not yet confirmed by the user**:

- a lightweight casino reputation level earned for completing official games
- wins/losses affect chips but not whether reputation is earned
- cosmetic unlocks only: clothes, gloves, card backs, chip-edge patterns, head accessories, emotes, seating cinematic variants, and room themes
- no odds, win-rate, hidden-information, or AI advantages
- no core games locked behind progression

Discuss this with the user before treating it as settled.

## 16. Explicitly pending design decisions

- Fixed casino overview/floor plan versus conventional button menu for selecting rooms
- Fixed named AI cast versus random visitor AI
- Exact starting chip balance, daily supply, relief amount, cooldown, and economic tuning
- Detailed progression/reputation design
- Final game title and names of the casino/rooms
- Exact internet transport/relay service
- Final art palette and individual character/room designs

## 17. Engineering constraints

- Keep rules and economy logic in pure C# assemblies independent of `UnityEngine`, UI, scenes, transport, and persistence.
- Separate Core, blackjack, application/session, persistence, networking, presentation, and test assemblies.
- Single-player and multiplayer use the same rules core.
- Host authority wraps the rules core; clients cannot declare cards, random outcomes, wins, or balances.
- All round/action/ledger operations must be idempotent and use stable identifiers.
- Animations consume confirmed domain events and never advance or determine game logic.
- Use integer chip units and guarantee exact payout representation.
- Preserve Unity `.meta` files and do not commit generated Unity/IDE folders.

## 18. Immediate next behavior for Codex

- Continue as a design collaborator unless the user explicitly asks for implementation.
- When proposing a design choice, clearly distinguish recommendation from already settled decisions.
- Record newly confirmed decisions in `AGENTS.md` and the appropriate `Docs/` file.
- Keep undecided items explicitly marked pending rather than guessing.
- Avoid repeatedly asking about implementation phases while the user wants to discuss the game itself.
