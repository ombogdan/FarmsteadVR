# Farmstead VR — first prototype

A small Unity prototype for the opening of a Quest 3 farm adventure. The scene uses simple 3D shapes so the first quest can be tested before art assets are added.

## Open it

1. Install Unity Hub and a Unity 6.1 or newer **Apple Silicon** editor. Include **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK** modules.
2. In Unity Hub, use **Add project from disk** and select this folder.
3. Open `Assets/Scenes/FarmPrototype.unity`. The scene initializer generates the farm and materials on the first editor import. Wait for script compilation to finish, then open the scene again if needed.
4. Press **Play**. Click the Game view to capture the mouse.

## Episode 1 (stages 1–5)

Intro (your room with a storybook, then a bus ride) → bus stop → broken fence → find hammer, nails and saw (orange circles) → repair the fence (+50 coins) → Boris the goat starts talking and points at the old chicken coop → repair the coop (−40) → Zhora the rooster lands on the roof → buy two hens (5 each) → day and night start; feed the hens every day (fed hens lay twice as often) and collect eggs in the walk-in coop → sell eggs to Marquis the cat at his shop by the road → while the hens lay: find grandpa Panas's 5 lucky horseshoes for Boris → dig up grandpa's chest with a shovel (coins, carrot seeds, a letter) → weed, plant, water and harvest carrots in the garden, treat Boris to one → repair the farmhouse with planks and paint → end of episode 1 (the fair comes next). You sleep in grandpa's old house (walk in, use the bed). Marquis has two menus — BUY (point at him or his goods) and SELL (point at the basket) — with pages, and sells a coop upgrade (+4 nests, room for 4 more hens). Eggs and carrots pay for the shovel, rake, seeds, planks and paint. The game saves itself; the next start continues where you left off. The full idea and the stage plan are in [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md).

**Testing in the editor:** progress is remembered between Play sessions. To start from the intro again use **Farm Prototype → Reset Save**, or hold Tab for 4 seconds in the game.

## Controls

| Action | Quest 3 | Mac |
| --- | --- | --- |
| Walk | left stick | WASD / arrows |
| Turn | right stick sideways (45° steps) | mouse |
| Teleport | right stick forward, release | — |
| Use (take, talk, repair) | right-hand laser + trigger / grip / A | aim + E |
| Tasks | X (short press) | Tab (short press) |
| New game | hold X for 4 s | hold Tab for 4 s |
| Inventory | Y | I |
| Turn the intro book's page | trigger | E |

VR mode turns on automatically when a headset is active. All input goes through `Assets/Scripts/FarmInput.cs` (Unity Input System package; **Active Input Handling** = **Input System Package (New)**).

## Where things are

- `Assets/Scripts/FarmWorldBuilder.cs` — builds the farm (stage 5 things: `FarmWorldBuilder.Chores.cs`); `CountryFarmArt.cs` dresses it with the CC0 textures and models. When you change the builder, bump `FarmPrototype.WorldVersion`: the editor then rebuilds the scene by itself.
- `Assets/Scripts/FarmPrototype.cs` — the common game rules (aiming, inventory, messages, conversations, saving); its other parts: `FarmPrototype.Story.cs` (stages 1–2), `FarmPrototype.Eggs.cs` (stage 3), `FarmPrototype.Shop.cs` (stage 4), `FarmPrototype.Chores.cs` (stage 5: feeding, horseshoes, chest, garden, house), `FarmPrototype.Animations.cs` (tools that dig/rake/water/hammer, flying bits, things flying into your hand). `ShopMenu.cs` — the shop's button menus. `GoatGait.cs` / `ChickenGait.cs` — legs that walk.
- `Assets/Scripts/DayNight.cs` — the sun, the moon, the sky color and the stars. `GameSave.cs` — saving progress.
- `Assets/Scripts/IntroSequence.cs` — the storybook intro.
- `Assets/Scripts/Vr*.cs` — VR player, hands and laser, teleport, texts in the headset.
- `Tools/SoundGen/generate_sounds.py` — makes all sounds from math (no copyright issues). `Tools/ModelGen/generate_props.py` — makes our models the same way (horseshoe, carrot, watering can, grain sack, Boris the goat, the lawn inside the fence); `clear_road.py` removes far-forest trees that stood on the road. See `docs/ASSETS.md`.
- `docs/QUEST_BUILD.md` — building to Quest 3 from a Mac.
