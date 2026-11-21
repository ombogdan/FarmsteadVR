using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The kitchen in grandpa's house (part of FarmPrototype, see FarmPrototype.cs). Marquis sells the
// kitchenware after the house is repaired (45 coins). Point at the old stove: a window with recipes
// (the same kind of window as the shop: pick a recipe into the "pot", press ГОТУВАТИ).
// A dish cooks for ~45 s (steam over the pot) and is worth more than what went into it:
//   omelet        2 eggs + 1 milk                -> 22   (raw 16)
//   carrot pie    2 carrots + 2 eggs + 1 milk    -> 45   (raw 26)
//   syrnyky       1 cheese + 1 egg               -> 40   (raw 31)
//   goat bake     1 goat cheese + 2 carrots      -> 32   (raw 26)
// The stove and the dishes' looks are built in FarmWorldBuilder.Chores.cs (Kitchen).
public partial class FarmPrototype
{
    const float CookSeconds = 45f;
    static readonly string[] Recipes = { "omelet", "carrot_pie", "syrnyky", "goat_bake" };

    bool kitchenBuilt;
    bool menuKitchen;        // the shop window is showing the kitchen's recipes
    string cooking = "";     // the dish in the pot ("" = nothing)
    float cookProgress;
    float nextKitchenSteam;
    Transform kitchenStove;
    GameObject kitchenExtras, kitchenGlow;
    FarmInteractable kitchenAction;

    void FindKitchenParts()
    {
        kitchenStove = world.Find("Kitchen Stove");
        kitchenExtras = kitchenStove.Find("Kitchen Extras").gameObject;
        kitchenGlow = kitchenStove.Find("Glow Ring").gameObject;
        kitchenAction = kitchenStove.GetComponent<FarmInteractable>();
        itemLooks["omelet"] = Find("Omelet Look");
        itemLooks["carrot_pie"] = Find("Pie Look");
        itemLooks["syrnyky"] = Find("Syrnyky Look");
        itemLooks["goat_bake"] = Find("Casserole Look");
    }

    static KeyValuePair<string, int>[] Ingredients(string recipe)
    {
        switch (recipe)
        {
            case "omelet": return new[] { Pair("egg", 2), Pair("milk", 1) };
            case "carrot_pie": return new[] { Pair("carrot", 2), Pair("egg", 2), Pair("milk", 1) };
            case "syrnyky": return new[] { Pair("cheese", 1), Pair("egg", 1) };
            default: return new[] { Pair("goat_cheese", 1), Pair("carrot", 2) }; // goat_bake
        }
    }

    static KeyValuePair<string, int> Pair(string id, int n) => new KeyValuePair<string, int>(id, n);

    bool HasIngredients(string recipe)
    {
        foreach (var need in Ingredients(recipe)) if (ItemCount(need.Key) < need.Value) return false;
        return true;
    }

    string IngredientText(string recipe)
    {
        var parts = new List<string>();
        foreach (var need in Ingredients(recipe)) parts.Add((need.Value > 1 ? need.Value + " " : "") + ItemName(need.Key).ToLower());
        return string.Join(" + ", parts);
    }

    bool DishReady => cooking.Length > 0 && cookProgress >= 1f;

    // Bought from Marquis: pots and pans appear on the old stove.
    IEnumerator BuildKitchen()
    {
        kitchenBuilt = true;
        kitchenExtras.SetActive(true);
        StartCoroutine(Puff(kitchenStove.position + Vector3.up * 1.2f, 0.5f));
        FarmAudio.Play("success", kitchenStove.position + Vector3.up, 0.9f);
        ShowBanner("КУХНЯ ГОТОВА!", "Пічка в хаті діда Панаса: омлет, морквяний пиріг, сирники, запіканка. Страви коштують дорожче за продукти!");
        SaveGame();
        yield break;
    }

    void UseKitchen()
    {
        if (!kitchenBuilt)
        {
            Say(stage >= Stage.EpisodeDone
                ? "Стара пічка діда Панаса. Кухонне начиння продає Маркіз («Кухня») — і можна готувати!"
                : "Стара пічка діда Панаса. Колись тут пахло пирогами...", 6f);
            return;
        }
        if (DishReady)
        {
            string dish = cooking;
            cooking = "";
            cookProgress = 0f;
            AddItem(dish);
            TaskEvent("cook");
            GameObject look = ItemModel(dish);
            if (look != null)
            {
                Transform copy = Instantiate(look).transform;
                copy.position = kitchenStove.position + Vector3.up * 1.2f;
                StartCoroutine(FlyToHand(copy, 0.5f));
            }
            FarmAudio.Play2D("pickup", 0.8f);
            Hearts(kitchenStove.position + Vector3.up * 1.4f);
            Say(ItemName(dish) + " готов" + (dish == "syrnyky" ? "і" : dish == "carrot_pie" ? "ий" : dish == "omelet" ? "ий" : "а") + "! Маркіз купує по " + SellPrice(dish) + ".", 5f);
            SaveGame();
            return;
        }
        if (cooking.Length > 0)
        {
            Say(ItemName(cooking) + " готується… " + Mathf.RoundToInt(cookProgress * 100f) + "%", 3f);
            return;
        }
        if (shopWindow == null)
        {
            shopWindow = ShopWindow.Create();
            shopWindow.OnAction = ShopAction;
        }
        menuSelling = false;
        menuStorage = false;
        menuKitchen = true;
        cart.Clear();
        cartOrder.Clear();
        shopWindow.Open(playerCamera.transform);
        RefreshShopWindow();
    }

    void RefreshKitchenWindow()
    {
        var goods = new List<ShopWindow.Entry>();
        var lines = new List<ShopWindow.Entry>();
        foreach (string recipe in Recipes)
            goods.Add(new ShopWindow.Entry
            {
                id = recipe,
                text = ItemName(recipe) + ": " + IngredientText(recipe) + "  → " + SellPrice(recipe),
                dim = !HasIngredients(recipe),
                look = ItemModel(recipe),
            });
        foreach (string recipe in cartOrder)
            lines.Add(new ShopWindow.Entry { id = recipe, text = ItemName(recipe), look = ItemModel(recipe) });
        bool ready = cartOrder.Count == 1 && HasIngredients(cartOrder[0]);
        shopWindow.Show("КУХНЯ — РЕЦЕПТИ", "Що приготувати?", goods, false, "У казані", lines,
            ready ? "Готується ~" + Mathf.RoundToInt(CookSeconds) + " с" : "Обери рецепт", "ГОТУВАТИ", ready);
    }

    void KitchenAction(string verb, string what)
    {
        switch (verb)
        {
            case "close":
                CloseShop();
                return;
            case "page":
                shopWindow.Page += int.Parse(what);
                break;
            case "add":
            case "all":
                if (!HasIngredients(what))
                {
                    FarmAudio.Play2D("nope", 0.6f);
                    Say("Бракує продуктів: потрібно " + IngredientText(what) + ".", 5f);
                    break;
                }
                cart.Clear();
                cartOrder.Clear();
                cart[what] = 1;
                cartOrder.Add(what);
                FarmAudio.Play2D("pop", 0.5f, 1.2f);
                break;
            case "remove":
                cart.Clear();
                cartOrder.Clear();
                break;
            case "confirm":
                if (cartOrder.Count == 1 && HasIngredients(cartOrder[0]))
                {
                    string recipe = cartOrder[0];
                    foreach (var need in Ingredients(recipe)) RemoveItem(need.Key, need.Value);
                    cooking = recipe;
                    cookProgress = 0f;
                    FarmAudio.Play("water_pour", kitchenStove.position + Vector3.up, 0.8f, 0.6f);
                    Say(ItemName(recipe) + " готується! Зазирни приблизно за " + Mathf.RoundToInt(CookSeconds) + " секунд.", 5f);
                    CloseShop();
                    SaveGame();
                    return;
                }
                break;
        }
        if (shopWindow.IsOpen) RefreshShopWindow();
    }

    void UpdateKitchen()
    {
        if (!kitchenBuilt) return;
        bool ready = DishReady;
        if (kitchenGlow.activeSelf != ready) kitchenGlow.SetActive(ready);
        kitchenAction.SetLabel(ready ? "забрати страву" : cooking.Length > 0 ? "страва готується" : "кухня: готувати");
        if (cooking.Length == 0 || ready) return;
        cookProgress = Mathf.Min(1f, cookProgress + Time.deltaTime / CookSeconds);
        if (Time.time >= nextKitchenSteam)
        {
            nextKitchenSteam = Time.time + 0.8f;
            StartCoroutine(Puff(kitchenStove.position + new Vector3(0.15f, 1.35f, 0f), 0.15f));
        }
        if (cookProgress >= 1f)
        {
            FarmAudio.Play("pop", kitchenStove.position + Vector3.up * 1.2f, 1f, 1.3f, 6f);
            if (!MessageVisible) Say(ItemName(cooking) + " на кухні готов" + (cooking == "goat_bake" ? "а" : cooking == "syrnyky" ? "і" : "ий") + " — забери!", 5f);
        }
    }

    void ApplyKitchenState(GameSave save)
    {
        kitchenBuilt = save.kitchenBuilt;
        cooking = save.cooking ?? "";
        cookProgress = save.cookProgress;
        kitchenExtras.SetActive(kitchenBuilt);
    }
}
