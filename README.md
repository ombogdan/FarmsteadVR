# Farmstead VR — first prototype

A small Unity prototype for the opening of a Quest 3 farm adventure. The scene uses simple 3D shapes so the first quest can be tested before art assets are added.

## Open it

1. Install Unity Hub and a Unity 6.1 or newer **Apple Silicon** editor. Include **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK** modules.
2. In Unity Hub, use **Add project from disk** and select this folder.
3. Open `Assets/Scenes/FarmPrototype.unity`. The scene initializer generates the farm and materials on the first editor import. Wait for script compilation to finish, then open the scene again if needed.
4. Press **Play**, then choose **Нова гра** or **Продовжити**. On Mac click the menu; in VR use the right-hand laser and trigger.

## Episode 1 (stages 1–5)

Intro (your room with a storybook, then a bus ride) → bus stop → broken fence → find hammer, nails and saw (orange circles) → repair the fence (+50 coins) → Boris the goat starts talking and points at the old chicken coop → repair the coop (−40) → Zhora the rooster lands on the roof → buy two hens (5 each) → day and night start; feed the hens every day (fed hens lay twice as often) and collect eggs in the walk-in coop → sell eggs to Marquis the cat at his shop by the road → while the hens lay: find grandpa Panas's 5 lucky horseshoes for Boris → dig up grandpa's chest with a shovel (coins, carrot seeds, a letter) → weed, plant, water and harvest carrots in the garden, treat Boris to one → repair the farmhouse with planks and paint → end of episode 1 (the fair comes next). You sleep in grandpa's old house (walk in, use the bed). Marquis's shop is a window with goods on the left and a basket on the right (drag or click goods into the basket, then BUY / SELL). After the house, Boris gives you the barn key: the barn opens, and she-goats (then cows, once you have two goats) can be bought; they graze on the lawn and sleep in the barn. Hens lay about one egg per 2 minutes (per minute when fed), at most 2–3 a day each. Cows and she-goats can be milked once a day (milk sells for 10 / 6). Besides the story there are always up to 3 side jobs that pay coins (`FarmPrototype.Tasks.cs`). The inventory (Y / I) is a backpack panel with a cell and a turning model for each thing. Hens eat 1 kg of grain a day from the feeder (8 kg); grain is carried from the storage shed (100 kg to start, more can be bought there); hungry hens don't lay, and after 3 hungry days one dies. Fun: "Курячий політ" — a slingshot by the road launches hens at hay targets (`FarmPrototype.Slingshot.cs`); "Перегони" — Boris, Zhora and Marquis race along the road while you spur Boris on in rhythm (`FarmPrototype.Race.cs`); "Тир Маркіза" — 30 s of knocking down cans and sliding ducks with pebbles (`FarmPrototype.Gallery.cs`). The watering can holds water for 4 beds (refill at the well); cows and goats eat hay from the barn trough (carried from the bales by the storage shed), no hay — no milk. Every 3rd day is a fair day (double prizes, Marquis pays more). Random events: a runaway hen to catch, rare golden eggs (`FarmPrototype.Events.cs`). The cheese house (bought from Marquis) turns 2 bottles of milk into a cheese worth more (`FarmPrototype.Cheese.cs`). Eggs and carrots pay for the shovel, rake, seeds, planks and paint. The game saves itself; choose Continue at startup to resume. The full idea and the stage plan are in [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md).

**Testing in the editor:** progress is remembered between Play sessions. Press Tab (X in VR) once to save and return to the main menu. New Game asks before replacing existing progress.

## Controls

| Action | Quest 3 | Mac |
| --- | --- | --- |
| Walk | left stick | WASD / arrows |
| Turn | right stick sideways (45° steps) | mouse |
| Teleport | right stick forward, release | — |
| Use (take, talk, repair) | right-hand laser + trigger / grip / A | aim + E |
| Tasks | B (right controller) | J |
| Save and return to main menu | X (left controller) | Tab |
| Inventory | Y | I |
| Turn the intro book's page | trigger | E |

Look at the face of either VR controller to see its button guide. B (J on Mac) also leaves an active minigame.

VR mode turns on automatically when a headset is active. All input goes through `Assets/Scripts/FarmInput.cs` (Unity Input System package; **Active Input Handling** = **Input System Package (New)**).

## Where things are

- `Assets/Scripts/FarmWorldBuilder.cs` — builds the farm (stage 5 things: `FarmWorldBuilder.Chores.cs`); `CountryFarmArt.cs` dresses it with the CC0 textures and models. When you change the builder, bump `FarmPrototype.WorldVersion`: the editor then rebuilds the scene by itself.
- `Assets/Scripts/FarmPrototype.cs` — the common game rules (aiming, inventory, messages, conversations, saving); its other parts: `FarmPrototype.Story.cs` (stages 1–2), `FarmPrototype.Eggs.cs` (stage 3), `FarmPrototype.Shop.cs` (stage 4), `FarmPrototype.Chores.cs` (stage 5: feeding, horseshoes, chest, garden, house), `FarmPrototype.Animations.cs` (tools that dig/rake/water/hammer, flying bits, things flying into your hand). `FarmPrototype.Barn.cs` (the barn, cows and goats). `ShopWindow.cs` — the shop window (drag & drop basket). `GoatGait.cs` / `ChickenGait.cs` — legs that walk.
- `Assets/Scripts/DayNight.cs` — the sun, the moon, the sky color and the stars. `GameSave.cs` — saving progress.
- `Assets/Scripts/IntroSequence.cs` — the storybook intro.
- `Assets/Scripts/Vr*.cs` — VR player, hands and laser, teleport, texts in the headset.
- `Tools/SoundGen/generate_sounds.py` — makes all sounds from math (no copyright issues). `Tools/ModelGen/generate_props.py` — makes our models the same way (horseshoe, carrot, watering can, grain sack, Boris the goat, the lawn inside the fence); `clear_road.py` removes far-forest trees that stood on the road. See `docs/ASSETS.md`.
- `docs/QUEST_BUILD.md` — building to Quest 3 from a Mac.
