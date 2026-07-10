# Development tasks

## Current milestone: Blackjack rules foundation

Goal: build a testable, Unity-independent blackjack rules core before UI, 3D scenes, animation, persistence, AI personalities, or multiplayer transport.

### Phase 0 — Foundation

- [x] Create Unity 6000.3 LTS URP project.
- [x] Publish clean Unity project with a working `.gitignore`.
- [x] Record product and blackjack design documents.
- [x] Define technical architecture and repository rules.

### Phase 1 — Pure C# card and blackjack core

- [ ] Create assembly definitions for `Casino.Core`, `Casino.Blackjack`, and edit-mode tests.
- [ ] Implement immutable card suit, rank, and card value types.
- [ ] Implement injectable random source and six-deck shoe with Fisher–Yates shuffle.
- [ ] Implement blackjack hand evaluation, including multiple aces, soft totals, bust, and natural blackjack.
- [ ] Implement dealer S17 decision rules.
- [ ] Implement legal-action checks for hit, stand, double, split, and insurance.
- [ ] Implement win, loss, push, blackjack, and insurance settlement using integer chips.
- [ ] Add edit-mode tests for all selected rules and edge cases.

### Phase 2 — Local round state machine

- [ ] Implement betting, initial deal, dealer peek, insurance, player turns, dealer turn, settlement, and intermission states.
- [ ] Add unique round and action identifiers with duplicate-request protection.
- [ ] Add deterministic scripted random sources for reproducible tests.
- [ ] Simulate at least 100,000 local rounds without state deadlock before presentation work.

### Later milestones

- Greybox blackjack table and screen-space UI.
- AI players and deterministic dealer presentation.
- Local chip ledger and atomic save files.
- Friends-only Host/Client synchronization.
- Seated camera, compact card room, and seating cinematic.

## Not now

- Final art, character models, or detailed room decoration.
- Roulette, sic bo, poker, or slot machines.
- Dedicated servers, public matchmaking, host migration, rankings, or tournaments.
