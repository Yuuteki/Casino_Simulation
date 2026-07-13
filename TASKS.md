# Development tasks

## Current milestone: Blackjack rules foundation

Goal: build a testable, Unity-independent blackjack rules core before UI, 3D scenes, animation, persistence, AI personalities, or multiplayer transport.

## Frozen first playable MVP

The first playable MVP is a local blackjack vertical slice. It must prove the end-to-end local loop before friends networking, casino shell, final art, or additional games.

Must include:

- Local blackjack round state machine.
- Local official-chip balance, ledger, profile persistence, and idempotent settlement.
- Basic dealer and AI player behavior for single-player blackjack.
- Greybox table or screen-space UI for betting, actions, status, and settlement.
- EditMode/unit tests plus local simulation checks.

Out of scope for the first playable MVP:

- Friends Host/Client networking, invite flow, reconnect, or host-exit handling.
- Casino overview/floor-plan, compact 3D room, seated camera, or seating cinematic.
- Avatar drawing implementation and multiplayer appearance sync.
- Roulette, sic bo, poker, slots, and other games.
- Final art, polished animation/audio, progression, tasks, cosmetic shop, and full credit-system UI.

### Phase 0 — Foundation

- [x] Create Unity 6000.3 LTS URP project.
- [x] Publish clean Unity project with a working `.gitignore`.
- [x] Record product and blackjack design documents.
- [x] Define technical architecture and repository rules.

### Phase 1 — Pure C# card and blackjack core

- [x] Create assembly definitions for `Casino.Core`, `Casino.Blackjack`, and edit-mode tests.
- [x] Implement immutable card suit, rank, and card value types.
- [x] Implement injectable random source and six-deck shoe with Fisher–Yates shuffle.
- [x] Implement blackjack hand evaluation, including multiple aces, soft totals, bust, and natural blackjack.
- [x] Implement dealer S17 decision rules.
- [x] Implement legal-action checks for hit, stand, double, split, and insurance.
- [x] Implement win, loss, push, blackjack, and insurance settlement using integer chips.
- [x] Add edit-mode tests for all selected rules and edge cases.

### Phase 2 — Local round state machine

- [x] Implement betting, initial deal, dealer peek, insurance, player turns, dealer turn, settlement, and intermission states.
- [x] Add unique round and action identifiers with duplicate-request protection.
- [x] Add deterministic scripted random sources for reproducible tests.
- [x] Simulate at least 100,000 local rounds without state deadlock before presentation work.

### Phase 3 — First playable local blackjack MVP

- [x] Add local official-chip profile, ledger persistence, and settlement checkpoints.
- [ ] Add basic dealer and AI player behavior.
- [ ] Add greybox blackjack table or screen-space UI.
- [ ] Verify at least 100 local playable rounds from UI without state deadlock.

### Later milestones

- Friends-only Host/Client synchronization.
- Seated camera, compact card room, and seating cinematic.
- Casino overview/floor-plan navigation.
- Credit-system UI, progression, tasks, and cosmetic shop.
- Roulette and sic bo framework validation.

## Not now

- Final art, character models, or detailed room decoration.
- Roulette, sic bo, poker, or slot machines.
- Dedicated servers, public matchmaking, host migration, rankings, or tournaments.
