# Casino Simulation

A stylized Unity casino simulation where players can play table games with AI or invited friends using entertainment-only virtual chips.

## Current direction

- Unity 6000.3 LTS with URP
- Windows PC first
- Friends-only Host/Client multiplayer
- Compact casino rooms with seating cinematics
- Fixed seat position with a locally rotatable view
- Modular games, beginning with blackjack
- No chip purchases, cash-out, real-world value, or player-to-player transfers

## Current milestone

The project is establishing its pure C# blackjack rules core. See [TASKS.md](TASKS.md) and the design documents under [`Docs/`](Docs/).

## Repository contents

- `Assets/` — Unity assets and runtime code
- `Packages/` — Unity package manifest and lockfile
- `ProjectSettings/` — Unity 6000.3 project settings
- `Docs/` — product, gameplay, and architecture specifications
- `AGENTS.md` — durable implementation constraints for coding agents
- `TASKS.md` — ordered development backlog

Generated Unity and IDE directories are excluded through `.gitignore`.
