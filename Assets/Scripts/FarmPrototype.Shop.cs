using System.Collections.Generic;
using UnityEngine;

// Stage 4 (part of FarmPrototype, see FarmPrototype.cs): Marquis's shop by the road.
// Two separate windows (ShopWindow.cs): goods on the left, a basket on the right.
// - BUY: point at Marquis (or at the goods on his counter); drag goods into the basket, press КУПИТИ;
// - SELL: point at the basket on the counter; drag your eggs/carrots into it, press ПРОДАТИ.
// To add a new thing to the shop: give it a name (ItemNames), a price (Prices) and put it in Catalog.
// At night the shop is closed.
public partial class FarmPrototype
{
    const int EggPrice = 3;
    const int CarrotPrice = 5;
    const int SeedsInPack = 4;
    const int CoopUpgradePrice = 50;

    static readonly Dictionary<string, int> Prices = new Dictionary<string, int>
    {
        { "hen", 5 }, { "seeds", 4 }, { "shovel", 8 }, { "rake", 8 }, { "planks", 15 }, { "paint", 10 },
        { "coop_upgrade", CoopUpgradePrice }, { "cow", 40 }, { "goat", 20 },
        { "grain25", 12 }, { "grain50", 22 }, { "hay20", 8 }, { "cheese_house", 60 }, { "kitchen", 45 }, { "feeder_upgrade", 35 }, { "barn_upgrade", 70 },
    };
    // The BUY menu, in this order.
    static readonly string[] Catalog = { "hen", "seeds", "shovel", "rake", "planks", "paint", "coop_upgrade", "feeder_upgrade", "cow", "goat", "barn_upgrade", "cheese_house", "kitchen" };
    // The storage shed sells animal food (more kinds later: hay for cows and goats...).
    static readonly string[] StorageCatalog = { "grain25", "grain50", "hay20" };
    // What Marquis buys, and for how much.
    static readonly Dictionary<string, int> SellPrices = new Dictionary<string, int>
        { { "egg", EggPrice }, { "golden_egg", 20 }, { "carrot", CarrotPrice }, { "milk", 10 }, { "goat_milk", 6 }, { "cheese", 28 }, { "goat_cheese", 16 },
          { "omelet", 22 }, { "carrot_pie", 45 }, { "syrnyky", 40 }, { "goat_bake", 32 } };

    static readonly string[] MarquisIntro =
    {
        "Мур-р... Новенький фермер? Дозволь представитись: Маркіз. Купець, мандрівник і просто красень.",
        "Яйця купую по 3 монети. Найкраща ціна в окрузі! Ну... єдина ціна в окрузі.",
        "А ще продаю все, що треба фермеру: курочок, дошки, фарбу, лопати. Майже нові!",
        "Хочеш купити — клич мене. Хочеш продати — клади в кошик на прилавку. Мур.",
    };
    static readonly string[] MarquisLater =
    {
        "Мур. Подивись, що в мене є!",
        "Свіжі новини: Борис знову з'їв чиюсь шапку.",
        "Чув, Жора хоче стати мером? Мур-ха-ха.",
        "Ситі курочки несуться частіше. Це я тобі як кіт кажу. Мур.",
        "Тісно курочкам? Можу розширити курник. За помірну ціну, мур.",
    };

    GameObject marquis;
    FarmInteractable marquisAction;
    GameObject basketGlow;
    int marquisTalk;
    ShopWindow shopWindow;
    bool menuSelling;
    bool menuStorage; // the window is the storage shed's (animal food), not Marquis's
    readonly Dictionary<string, int> cart = new Dictionary<string, int>();
    readonly List<string> cartOrder = new List<string>();

    void FindShopParts()
    {
        Transform shop = world.Find("Shop");
        marquis = shop.Find("Marquis").gameObject;
        marquisAction = marquis.GetComponent<FarmInteractable>();
        basketGlow = shop.Find("Egg Basket/Glow Ring").gameObject;
        foreach (FarmInteractable thing in shop.GetComponentsInChildren<FarmInteractable>(true))
            if (thing.kind == FarmInteractable.Kind.ShopItem && thing.itemId != "hen")
                itemLooks[thing.itemId] = thing.gameObject; // so the inventory (and the animations) can show a copy
    }

    // After the first egg: Marquis opens his shop.
    void MarquisArrives()
    {
        stage = Stage.MeetMarquis;
        marquis.SetActive(true);
        basketGlow.SetActive(true);
        Say("Перше яйце! Яйця купує кіт Маркіз — його лавка біля дороги, зліва від стежки.", 10f);
        SaveGame();
    }

    void UpdateShop()
    {
        if (stage == Stage.MeetMarquis && PlayerDistance(marquis.transform) < 4f) MeetMarquis();
        if (shopWindow == null || !shopWindow.IsOpen) return;
        shopWindow.Tick(FarmInput.InteractPressed(), FarmInput.InteractHeld(), FarmInput.InteractReleased());
        // Walked away, or night fell: the window closes by itself.
        if (shopWindow.IsOpen && FlatDistance(playerCamera.transform.position, shopWindow.transform.position) > 2.5f) CloseShop();
        else if (shopWindow.IsOpen && dayNight.IsNight && !menuStorage && !menuKitchen)
        {
            CloseShop();
            MarquisSays("Мур-р... Темніє. Зачиняюсь до ранку!");
        }
    }

    void MeetMarquis()
    {
        if (InDialogue) return;
        StartDialogue(marquis.transform, "Маркіз", MarquisIntro, () =>
        {
            stage = Stage.Trading;
            basketGlow.SetActive(false);
            Say("Тепер можна продавати яйця Маркізу і купувати в нього все для ферми. А ще Борис хоче тобі щось розповісти!", 8f);
            SaveGame();
        });
    }

    void TalkToMarquis()
    {
        if (stage == Stage.MeetMarquis)
        {
            MeetMarquis();
            return;
        }
        if (!ShopReady()) return;
        MarquisSays(MarquisLater[marquisTalk++ % MarquisLater.Length]);
        OpenShop(false);
    }

    void MarquisSays(string text)
    {
        SpeakerSays(marquis.transform, "Маркіз: " + text, 5f);
    }

    // Is the shop open for business? If not, says why.
    bool ShopReady()
    {
        if (stage < Stage.MeetMarquis)
        {
            Say("Лавка ще зачинена. Схоже, господаря немає на місці.", 4f);
            return false;
        }
        if (stage == Stage.MeetMarquis)
        {
            MeetMarquis();
            return false;
        }
        if (dayNight.IsNight)
        {
            MarquisSays("Мур-р... Лавка зачинена. Приходь зранку.");
            return false;
        }
        return true;
    }

    // ---------- the shop window: goods on the left, the basket on the right ----------

    void OpenShop(bool selling)
    {
        if (!ShopReady()) return;
        if (shopWindow == null)
        {
            shopWindow = ShopWindow.Create();
            shopWindow.OnAction = ShopAction;
        }
        menuSelling = selling;
        menuStorage = false;
        menuKitchen = false;
        cart.Clear();
        cartOrder.Clear();
        shopWindow.Open(playerCamera.transform);
        RefreshShopWindow();
    }

    // The storage shed: buy grain (it's delivered straight into the shed). Open day and night.
    void OpenStorage()
    {
        if (stage < Stage.WaitEggs)
        {
            Say("Склад діда Панаса: мішки із зерном для курей. Згодяться, коли з'являться кури.", 5f);
            return;
        }
        if (shopWindow == null)
        {
            shopWindow = ShopWindow.Create();
            shopWindow.OnAction = ShopAction;
        }
        menuSelling = false;
        menuStorage = true;
        menuKitchen = false;
        cart.Clear();
        cartOrder.Clear();
        shopWindow.Open(playerCamera.transform);
        RefreshShopWindow();
    }

    void CloseShop()
    {
        if (shopWindow != null && shopWindow.IsOpen) shopWindow.Close();
        cart.Clear();
        cartOrder.Clear();
    }

    int InCart(string id) => cart.TryGetValue(id, out int n) ? n : 0;

    void RefreshShopWindow()
    {
        if (menuKitchen) { RefreshKitchenWindow(); return; } // FarmPrototype.Kitchen.cs
        var goods = new List<ShopWindow.Entry>();
        var lines = new List<ShopWindow.Entry>();
        int total = 0;
        if (menuSelling)
        {
            foreach (var price in SellPrices)
            {
                int have = ItemCount(price.Key);
                if (have == 0) continue;
                int left = have - InCart(price.Key);
                goods.Add(new ShopWindow.Entry { id = price.Key, text = ItemName(price.Key) + " ×" + left + "   по " + SellPrice(price.Key), dim = left == 0, look = ItemModel(price.Key) });
            }
            foreach (string id in cartOrder)
            {
                total += cart[id] * SellPrice(id);
                lines.Add(new ShopWindow.Entry { id = id, text = ItemName(id) + " ×" + cart[id] + "  =  " + cart[id] * SellPrice(id), look = ItemModel(id) });
            }
            shopWindow.Show(IsFairDay ? "ЛАВКА МАРКІЗА — ПРОДАТИ\nЯРМАРОК: усе на третину дорожче!" : "ЛАВКА МАРКІЗА — ПРОДАТИ", "Твої речі", goods, true, "На продаж", lines,
                "Отримаєш: " + CoinsText(total), "ПРОДАТИ", cart.Count > 0);
            return;
        }
        foreach (string id in menuStorage ? StorageCatalog : Catalog)
        {
            if (!InCatalog(id)) continue;
            string why = CantBuy(id);
            goods.Add(new ShopWindow.Entry
            {
                id = id,
                text = GoodsName(id) + " — " + Prices[id] + (why != null ? "  (" + why + ")" : ""),
                dim = why != null || Prices[id] > coins,
                look = GoodsLook(id),
            });
        }
        foreach (string id in cartOrder)
        {
            total += cart[id] * Prices[id];
            lines.Add(new ShopWindow.Entry { id = id, text = GoodsName(id) + (cart[id] > 1 ? " ×" + cart[id] : "") + "  =  " + cart[id] * Prices[id], look = GoodsLook(id) });
        }
        bool afford = total <= coins;
        shopWindow.Show(menuStorage ? "СКЛАД — КОРМ ДЛЯ ТВАРИН\nзерна " + Mathf.FloorToInt(storageKg) + " кг • сіна " + Mathf.FloorToInt(storageHayKg) + " кг" : "ЛАВКА МАРКІЗА — КУПИТИ",
            "Товари (у тебе " + CoinsText(coins) + ")", goods, false, "Кошик", lines,
            (afford ? "Разом: " : "Бракує монет! Разом: ") + CoinsText(total), "КУПИТИ", cart.Count > 0 && afford);
    }

    bool InCatalog(string id)
    {
        if (id == "coop_upgrade") return stage >= Stage.Trading;
        if (id == "cow" || id == "goat") return stage >= Stage.OpenBarn;
        if (id == "cheese_house" || id == "barn_upgrade") return stage >= Stage.BarnOpen;
        if (id == "feeder_upgrade") return stage >= Stage.Trading;
        if (id == "kitchen") return stage >= Stage.EpisodeDone;
        return true;
    }

    string GoodsName(string id)
    {
        switch (id)
        {
            case "hen": return "Курочка";
            case "seeds": return "Насіння моркви ×" + SeedsInPack;
            case "coop_upgrade": return "Розширити курник";
            case "cow": return "Корова";
            case "goat": return "Кізочка";
            case "cheese_house": return "Сироварня (молоко → сир)";
            case "kitchen": return "Кухня в хаті (рецепти страв)";
            case "feeder_upgrade": return "Велика годівниця (20 кг)";
            case "barn_upgrade": return "Розширити стодолу (+1 корова, +2 кізочки)";
            case "grain25": return "Зерно, 25 кг";
            case "grain50": return "Зерно, 50 кг";
            case "hay20": return "Сіно, 20 кг";
            default: return ItemName(id);
        }
    }

    // A small model for the line in the window (what it looks like).
    GameObject GoodsLook(string id)
    {
        if (id == "hen") return ItemModel("egg");
        if (id == "coop_upgrade") return ItemModel("planks");
        if (id == "cow" || id == "goat") return null;
        if (id == "cheese_house") return ItemModel("cheese");
        if (id == "kitchen") return ItemModel("carrot_pie");
        if (id == "feeder_upgrade") return ItemModel("grain");
        if (id == "barn_upgrade") return ItemModel("planks");
        if (id == "grain25" || id == "grain50") return ItemModel("grain");
        if (id == "hay20") return null;
        return ItemModel(id);
    }

    // Why you can't buy it right now (null = you can, if you have the coins).
    string CantBuy(string id)
    {
        if (id == "hen" && AliveHens >= HenLimit) return coopUpgraded ? "курник повний" : "тісно: розшир курник";
        if ((id == "shovel" || id == "rake" || id == "planks" || id == "paint") && Has(id)) return "уже є";
        if ((id == "planks" || id == "paint") && stage >= Stage.EpisodeDone) return "будинок уже як новий";
        if (id == "coop_upgrade" && coopUpgraded) return "уже розширено";
        if (id == "cheese_house" && cheeseBuilt) return "уже збудовано";
        if (id == "kitchen" && kitchenBuilt) return "уже є";
        if ((id == "cow" || id == "goat") && stage < Stage.BarnOpen) return "відкриється: відчини стодолу";
        if (id == "cow" && goatsBought < 2) return "відкриється, коли матимеш 2 кізочки";
        if (id == "cow" && cowsBought >= CowCapacity) return barnUpgraded ? "у стодолі нема місця" : "тісно: розшир стодолу";
        if (id == "goat" && goatsBought >= GoatCapacity) return barnUpgraded ? "у стодолі нема місця" : "тісно: розшир стодолу";
        if (id == "feeder_upgrade" && feederUpgraded) return "уже є";
        if (id == "barn_upgrade" && barnUpgraded) return "уже розширено";
        return null;
    }

    // Can one more go into the basket?
    string CantAddToCart(string id)
    {
        string why = CantBuy(id);
        if (why != null) return why;
        bool onlyOne = id == "shovel" || id == "rake" || id == "planks" || id == "paint" || id == "coop_upgrade" || id == "cheese_house" || id == "feeder_upgrade" || id == "barn_upgrade" || id == "kitchen";
        if (onlyOne && InCart(id) >= 1) return "одного досить";
        if (id == "hen" && AliveHens + InCart(id) >= HenLimit) return "більше курочок не влізе";
        if (id == "cow" && cowsBought + InCart(id) >= CowCapacity) return "більше корів не влізе";
        if (id == "goat" && goatsBought + InCart(id) >= GoatCapacity) return "більше кіз не влізе";
        return null;
    }

    // Everything the window asks for: "add:id", "all:id", "remove:id", "page:1", "confirm", "close".
    void ShopAction(string action)
    {
        if (shopWindow == null || !shopWindow.IsOpen) return;
        int colon = action.IndexOf(':');
        string what = colon >= 0 ? action.Substring(colon + 1) : "";
        string verb = colon >= 0 ? action.Substring(0, colon) : action;
        if (menuKitchen) { KitchenAction(verb, what); return; }
        switch (verb)
        {
            case "close":
                CloseShop();
                return;
            case "page":
                shopWindow.Page += int.Parse(what);
                FarmAudio.Play2D("page_turn", 0.5f);
                break;
            case "add":
            case "all":
                string why = menuSelling ? (InCart(what) >= ItemCount(what) ? "більше немає" : null) : CantAddToCart(what);
                if (why != null)
                {
                    FarmAudio.Play2D("nope", 0.6f);
                    MarquisSays("Мур... " + char.ToUpper(why[0]) + why.Substring(1) + ".");
                    break;
                }
                if (!cart.ContainsKey(what)) cartOrder.Add(what);
                cart[what] = verb == "all" ? ItemCount(what) : InCart(what) + 1;
                FarmAudio.Play2D("pop", 0.5f, 1.2f);
                break;
            case "remove":
                if (InCart(what) <= 1)
                {
                    cart.Remove(what);
                    cartOrder.Remove(what);
                }
                else cart[what]--;
                FarmAudio.Play2D("pop", 0.4f, 0.9f);
                break;
            case "confirm":
                if (menuSelling) SellCart();
                else BuyCart();
                break;
        }
        if (shopWindow.IsOpen) RefreshShopWindow();
    }

    void SellCart()
    {
        int total = 0, pieces = 0;
        foreach (string id in cartOrder)
        {
            int n = Mathf.Min(cart[id], ItemCount(id));
            RemoveItem(id, n);
            total += n * SellPrice(id);
            pieces += n;
            if (id == "egg" || id == "golden_egg") TaskEvent("sell_egg", n);
            else if (id == "carrot") TaskEvent("sell_carrot", n);
            else if (id == "milk" || id == "goat_milk") TaskEvent("sell_milk", n);
        }
        cart.Clear();
        cartOrder.Clear();
        coins += total;
        FarmAudio.Play2D("coin", 0.9f);
        MarquisSays(Plural(pieces, "штука", "штуки", "штук") + "? Мур-р, свіженьке! Тримай " + CoinsText(total) + ".");
        SaveGame();
    }

    void BuyCart()
    {
        int total = 0;
        foreach (string id in cartOrder) total += cart[id] * Prices[id];
        if (total > coins)
        {
            FarmAudio.Play2D("nope", 0.6f);
            Say("Разом " + CoinsText(total) + ", а в тебе " + CoinsText(coins) + ".", 5f);
            return;
        }
        coins -= total;
        FarmAudio.Play2D("coin", 0.6f);
        FarmAudio.Play2D("pickup", 0.7f);
        bool showSomething = false; // things that happen out on the farm: close the window so you can watch
        var bought = new List<string>(cartOrder);
        var counts = new Dictionary<string, int>(cart);
        cart.Clear();
        cartOrder.Clear();
        foreach (string id in bought)
            for (int k = 0; k < counts[id]; k++)
                showSomething |= Purchase(id);
        if (menuStorage) Say("Корм привезли на склад! Зерна " + Mathf.FloorToInt(storageKg) + " кг, сіна " + Mathf.FloorToInt(storageHayKg) + " кг.", 5f);
        else MarquisSays("Мур! Дякую за покупку. Приходь ще!");
        if (Has("planks") && Has("paint") && !usedHints.Contains("#house"))
        {
            usedHints.Add("#house");
            Say(stage >= Stage.RepairHouse ? "Дошки й фарба є! Тепер — до будинку діда Панаса." : "Дошки й фарба є! Вони знадобляться для ремонту будинку діда Панаса.", 8f);
        }
        if (showSomething) CloseShop();
        SaveGame();
    }

    // One bought thing. Returns true if something happens out on the farm (worth watching).
    bool Purchase(string id)
    {
        switch (id)
        {
            case "hen": BuyShopHen(); return true;
            case "seeds": AddItem(id, SeedsInPack); return false;
            case "coop_upgrade": StartCoroutine(UpgradeCoop()); return true;   // FarmPrototype.Eggs.cs
            case "cheese_house": StartCoroutine(BuildCheeseHouse()); return true; // FarmPrototype.Cheese.cs
            case "feeder_upgrade": StartCoroutine(UpgradeFeeder()); return true;
            case "kitchen": StartCoroutine(BuildKitchen()); return false;
            case "barn_upgrade": barnUpgraded = true; ShowBanner("СТОДОЛУ РОЗШИРЕНО!", "Тепер є місце ще для корови Ромашки і кізочок Кнопки та Ляльки."); return false;
            case "cow": BuyAnimal(cows, cowsBought++); return true;              // FarmPrototype.Barn.cs
            case "grain25": storageKg += 25f; ShowStorageSacks(); return false; // delivered to the storage shed
            case "grain50": storageKg += 50f; ShowStorageSacks(); return false;
            case "hay20": storageHayKg += 20f; ShowHay(); return false;
            case "goat": BuyAnimal(goats, goatsBought++); return true;
            default: AddItem(id); return false;
        }
    }

    // The new hen appears in front of the coop and joins the others.
    void BuyShopHen()
    {
        int index = System.Array.IndexOf(henGone, true); // first, the place of a hen that was lost
        if (index >= 0 && index < hensBought) henGone[index] = false;
        else index = hensBought++;
        GameObject hen = hens[index];
        Vector3 spot = FarmWorldBuilder.CoopDoorOutside + new Vector3(Random.Range(-1.5f, 1.5f), 0f, -0.6f);
        StartCoroutine(HenAppears(hen, spot));
        henNextLay[index] = Time.time + Random.Range(30f, 60f);
        string name = hen.GetComponent<FarmInteractable>().itemId;
        MarquisSays("Курочка " + name + " вже біжить до твого курника! Мур.");
        TaskEvent("buy_animal");
    }

    // ---------- loading a saved game ----------

    void ApplyShopState()
    {
        if (stage >= Stage.MeetMarquis) marquis.SetActive(true);
        if (stage == Stage.MeetMarquis) basketGlow.SetActive(true);
    }
}
