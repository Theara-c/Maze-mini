# Maze Escape (Unity)

A first-person maze game. The maze is random every run. You collect coins (each coin is one life),
dodge ghosts and traps, open doors, and escape by walking off the ledge into the exit pit.

This README explains what each script does and how the scripts connect. 

---

## Controls

| Key | Action |
|---|---|
| W A S D / arrow keys | Move |
| Mouse | Look around |
| Shift | Sprint |
| Space | Jump |
| E | Open / close a door (stand within 2 units) |
| Left click | Shrink the wall you are looking at, so you can jump over it (5 per game, not the outer wall) |
| Esc / P | Pause |
| R | Restart |

## Rules

- **1 coin = 1 life.** A ghost that touches you takes one coin and then disappears. With 0 coins, the touch is game over.
- After a ghost takes a coin you are protected for 2 seconds (`GameManager.hitCooldown`).
- **Traps** end the game immediately.
- Falling off the map (`fallY`) ends the game.
- **Win:** walk off the ledge at the far corner into the exit pit.
  If `ExitPit.requireKey` is on, you need the key first.

---



Order of events when you press Play:

1. `Obstaclerandom.Start()` builds the grid of `MazeCell` objects and carves the maze (recursive backtracking).
2. `MazeFrame.Build()` adds the floor, entrance, exit pit, then creates the `GameManager` (if the scene has none), the player and the camera look script.
3. `Obstaclerandom.SpawnGameElements()` places doors, the key chest, coins, traps and ghosts in random cells.
4. `GameManager` adds a `UIManager`, which builds the HUD and menus.
5. `GhostWarning` starts itself on every scene load. You never place it in the scene.


## Script reference

| Script | What it does | Created by | Talks to |
|---|---|---|---|
| `ObstacleRandom.cs` (class `Obstaclerandom`) | Builds the maze, then spawns doors, key chest, coins, traps and ghosts. Counts and distances are set in the Inspector. | Placed on a scene object | `MazeCell`, `MazeFrame`, `GameManager`, `Coin`, `GhostChase`, `Door` prefab |
| `MazeGenerator.cs` | Older maze builder. `Obstaclerandom` switches it off at start, so it does nothing. | Scene (optional) | none |
| `MazeCell.cs` | One grid cell with four walls. `ClearXWall()` removes a wall; `HasXWall` tells you if one is still there. | `Obstaclerandom` | none |
| `MazeFrame.cs` | Static helper `Build(...)`: floor, entrance, exit pit, player, camera, `GameManager`. | `Obstaclerandom` | `GameManager`, `ExitPit`, `PlayerController`, `WallShrinker`, `PlayerLook` |
| `GameManager.cs` | Game state: menu, pause, coins, key, win / lose, hint text. Exposes C# events that the UI listens to. Key methods: `AddCoin()`, `HitByGhost()`, `CollectKey()`, `Win()`, `Lose()`, `ShowHint()`. | `MazeFrame` (or the scene) | `UIManager`, `GameInput` |
| `UIManager.cs` | Builds the whole UI from code (hint, coins, key, menus, game-over panel). Updates itself from `GameManager` events and sends button clicks back. | `GameManager` | `GameManager` |
| `GameInput.cs` | Static wrapper around keyboard and mouse. Supports the new Input System and the old Input Manager. | not an object | used by `PlayerController`, `PlayerLook`, `WallShrinker`, `Door`, `GameManager` |
| `PlayerController.cs` | Rigidbody movement, sprint, jump (with coyote time and jump buffer). Stops moving while the game is paused or over. | `MazeFrame` | `GameInput`, `GameManager` |
| `PlayerLook.cs` | Mouse look. Turns the player left and right via `AddYaw()`, tilts the camera up and down. | `MazeFrame` | `GameInput`, `PlayerController` |
| `WallShrinker.cs` | Left-click shrinks the wall you look at (max 5, never the outer wall). Ghosts still treat a shrunk wall as a wall. | `MazeFrame` | `GameInput`, `MazeCell`, `GameManager` |
| `door.cs` (class `Door`) | Closed door that opens and closes with E. Has a static `IsClosedBetween(from, to)` for other scripts. | `Obstaclerandom` | `GameInput`, `GameManager` |
| `Coin.cs` | Coin pickup. Stands on its edge, spins, and gives +1 coin. | `Obstaclerandom` | `GameManager` |
| `TrapScript.cs` | Trap trigger. The player touching it is game over. | prefab / `Obstaclerandom` | `GameManager` |
| `ExitPit.cs` | Trigger at the bottom of the exit pit. Win (or lose if `winOnFall` is off or the key is required and missing). | `MazeFrame` | `GameManager` |
| `GhostChase.cs` | Ghost AI. Breadth-first search through the maze cells, so it never walks through walls. Shows the photo in `Resources/GhostFace.png`, turns off the asset's own keyboard script, and calls `HitByGhost()` on touch. | `Obstaclerandom` | `MazeCell`, `GameManager`, `Sample.GhostScript` (asset) |
| `GhostWarning.cs` | Red pulsing alert and looping sound when a ghost is close. Creates itself automatically. | itself (on scene load) | `GhostChase`, `GameManager` |

---

## Needed in the project

- **Render pipeline:** URP (the materials use the URP Lit shader).
- **Scene:** an object with `Obstaclerandom` and its prefabs assigned (maze cell, chest, door, trap, ghost, floor material).
- **Resources folder** (`Assets/Resources`):
  - `GhostFace.png`: the ghost picture (optional; without it the normal ghost model shows).
  - `GhostWarning` (`.mp3`, `.wav` or `.ogg`): the warning sound (optional).
- **Ghost asset:** `GhostChase` refers to `Sample.GhostScript` from the free ghost asset, so keep that asset in the project.
- **Prefabs:** For coins, `Obstaclerandom` removes it and adds `Coin`. The trap prefab needs `TrapScript` and a trigger collider.

## Settings you will probably change

| Where | Setting | Default |
|---|---|---|
| `Obstaclerandom` | `_mazeWidth`, `_mazeDepth`, `_coinChestCount`, `_trapCount`, `_ghostCount`, `_doorCount`, `_ghostMinSpawnDistance` | coins 5, traps 15, ghosts 2, ghost distance 8 (maze size and door count are set on the scene object) |
| `GameManager` | `startingCoins`, `hitCooldown`, `fallY`, `showKeyStatus` | 0, 2 s, -30, off |
| `PlayerController` | `moveSpeed`, `sprintMultiplier`, `jumpHeight` | 1.5, 1.6, 0.35 |
| `GhostChase` | `moveSpeed`, `startDelay`, `catchDistance`, `faceHeight` | 1, 5 s, 0.35, 0.6 |
| `GhostWarning` | `warningDistance`, `warningVolume` | 3.5, 1 |
| `WallShrinker` | `maxShrinks`, `shrunkHeight`, `reach` | 5, 0.2, 1.5 |
| `ExitPit` | `winOnFall`, `requireKey` | on, off |

---

## Known gaps and cleanup ideas

1. **Closed doors do not stop ghosts yet.** `Door.IsClosedBetween()` exists, but nothing calls it, and
   `GhostChase` has no colliders, so ghosts pass through doors while you cannot. To fix it, add the
   door check in `GhostChase.CanPass()` and re-run the path search a few times per second so the ghost
   notices doors opening and closing. 
2. **`PlayerController.cs`** contains an old commented-out copy of the class at the top, and the fields
   `jumpForce` and `groundCheckDistance` appear to be leftovers. They can be deleted.
3. **`MazeGenerator.cs`** is not used. It is only kept around and switched off. You can remove it, and
   remove the `GetComponent<MazeGenerator>()` lines at the top of `Obstaclerandom.Awake()` too.
4. The key and the "key hint" text: `GameManager.showKeyStatus` is off by default, so the "Key: Yes / No"
   label is hidden even though the key works. Turn it on if you want the label.
5. `door.cs` has a comment mentioning `GhostScript`. The ghost movement script is now `GhostChase`.
