using UnityEngine;

// Marks something the player can point at and use.
public class FarmInteractable : MonoBehaviour
{
    public enum Kind
    {
        Goat, Fence, Item, Coop, Rooster, Hen, Egg, ShopItem, SellBasket, Cat, Sleep,
        GrainSack, Feeder, Horseshoe, Mound, Chest, Bed, House, // stage 5 (FarmPrototype.Chores.cs)
        Barn, Animal, Storage, Well, HayStack, Trough,           // the barn, animals, food, water
        CheesePress,                                             // the cheese house (FarmPrototype.Cheese.cs)
        Kitchen,                                                 // the kitchen in the house (FarmPrototype.Kitchen.cs)
        CoopDoor,                                                // shut the coop at night: a fox! (FarmPrototype.Events.cs)
        Slingshot,                                               // fun: "Курячий політ" (FarmPrototype.Slingshot.cs)
        Race,                                                    // fun: "Перегони" (FarmPrototype.Race.cs)
        Gallery,                                                 // fun: "Тир Маркіза" (FarmPrototype.Gallery.cs)
        EggRush,                                                 // fun: "Яєчний переполох" (FarmPrototype.EggRush.cs)
        RoutePost,
    }

    public Kind kind;
    public string label;     // shown in the hint: "Курок — <label>"
    public string itemId;    // Item/ShopItem: what it is ("hammer", "planks"...). Hen: her name. Horseshoe/Bed: its number.
    public bool alwaysHint;  // e.g. prices: show the hint every time, not only until the first use

    // A new action gets its hint shown again (FarmPrototype remembers which hints were already used).
    public void SetLabel(string newLabel)
    {
        label = newLabel;
    }
}
