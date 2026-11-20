using System.Collections.Generic;
using UnityEngine;

// Stage 4 (part of FarmPrototype, see FarmPrototype.cs): Marquis's shop by the road.
// Two separate menus (ShopMenu.cs), each a list of buttons in the air, in pages:
// - BUY: point at Marquis (or at the goods on his counter) -> everything he sells;
// - SELL: point at the basket on the counter -> what you have to sell and for how much.
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
        { "coop_upgrade", CoopUpgradePrice },
    };
    // The BUY menu, in this order.
    static readonly string[] Catalog = { "hen", "seeds", "shovel", "rake", "planks", "paint", "coop_upgrade" };
    // What Marquis buys, and for how much.
    static readonly Dictionary<string, int> SellPrices = new Dictionary<string, int> { { "egg", EggPrice }, { "carrot", CarrotPrice } };

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
    ShopMenu shopMenu;
    bool menuSelling;

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
        if (shopMenu == null || !shopMenu.IsOpen) return;
        shopMenu.Hover(aimedAt);
        // Walked away, or night fell: the menu closes by itself.
        if (FlatDistance(playerCamera.transform.position, shopMenu.Position) > 2.5f) CloseShop();
        else if (dayNight.IsNight)
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

    // ---------- the menus ----------

    void OpenShop(bool selling)
    {
        if (!ShopReady()) return;
        if (shopMenu == null) shopMenu = new ShopMenu();
        menuSelling = selling;
        shopMenu.Open(playerCamera.transform);
        FarmAudio.Play2D("page_turn", 0.6f);
        RefreshShopMenu();
    }

    void CloseShop()
    {
        if (shopMenu != null && shopMenu.IsOpen) shopMenu.Close();
    }

    void RefreshShopMenu()
    {
        var lines = new List<ShopMenu.Line>();
        string title;
        if (menuSelling)
        {
            title = "ПРОДАТИ Маркізу\nу тебе " + CoinsText(coins);
            int total = 0, kinds = 0;
            foreach (var price in SellPrices)
            {
                int have = ItemCount(price.Key);
                if (have == 0) continue;
                total += have * price.Value;
                kinds++;
                lines.Add(new ShopMenu.Line
                {
                    text = ItemName(price.Key) + " ×" + have + "  (по " + price.Value + ")  →  " + CoinsText(have * price.Value),
                    action = "sell:" + price.Key,
                });
            }
            if (kinds > 1) lines.Insert(0, new ShopMenu.Line { text = "Продати все  →  " + CoinsText(total), action = "sell:all" });
            if (kinds == 0) lines.Add(new ShopMenu.Line { text = "Поки нічого продати.\nНеси яйця й моркву!", dim = true });
        }
        else
        {
            title = "КУПИТИ в Маркіза\nу тебе " + CoinsText(coins);
            foreach (string id in Catalog)
            {
                if (id == "coop_upgrade" && stage < Stage.Trading) continue;
                string why = CantBuy(id);
                lines.Add(new ShopMenu.Line
                {
                    text = GoodsName(id) + "  —  " + CoinsText(Prices[id]) + (why != null ? "\n(" + why + ")" : ""),
                    action = "buy:" + id,
                    dim = why != null || coins < Prices[id],
                });
            }
        }
        shopMenu.Show(title, lines);
    }

    string GoodsName(string id)
    {
        switch (id)
        {
            case "hen": return "Курочка";
            case "seeds": return "Насіння моркви ×" + SeedsInPack;
            case "coop_upgrade": return "Розширити курник (+4 гнізда)";
            default: return ItemName(id);
        }
    }

    // Why you can't buy it right now (null = you can, if you have the coins).
    string CantBuy(string id)
    {
        if (id == "hen" && hensBought >= HenLimit) return coopUpgraded || hensBought >= hens.Count ? "курник повний" : "тісно: розшир курник";
        if ((id == "shovel" || id == "rake" || id == "planks" || id == "paint") && Has(id)) return "уже є";
        if ((id == "planks" || id == "paint") && stage >= Stage.EpisodeDone) return "будинок уже як новий";
        if (id == "coop_upgrade" && coopUpgraded) return "уже розширено";
        return null;
    }

    // A button in the menu was pressed.
    void PressMenu(string action)
    {
        if (shopMenu == null || !shopMenu.IsOpen) return;
        if (action == "close")
        {
            FarmAudio.Play2D("tick", 0.5f);
            CloseShop();
            return;
        }
        if (action.StartsWith("page:"))
        {
            shopMenu.TurnPage(int.Parse(action.Substring(5)));
            FarmAudio.Play2D("page_turn", 0.5f);
        }
        else if (action.StartsWith("buy:")) Buy(action.Substring(4));
        else if (action == "sell:all") foreach (string id in SellPrices.Keys) Sell(id);
        else if (action.StartsWith("sell:")) Sell(action.Substring(5));
        if (shopMenu.IsOpen) RefreshShopMenu();
    }

    void Sell(string id)
    {
        int have = ItemCount(id);
        if (have == 0 || !SellPrices.TryGetValue(id, out int price)) return;
        RemoveItem(id, have);
        coins += have * price;
        FarmAudio.Play2D("coin", 0.8f);
        MarquisSays(Plural(have, id == "egg" ? "яйце" : "морквина", id == "egg" ? "яйця" : "морквини", id == "egg" ? "яєць" : "морквин")
            + "? Мур-р, свіженькі! Тримай " + CoinsText(have * price) + ".");
        SaveGame();
    }

    void Buy(string id)
    {
        if (!ShopReady()) return;
        string why = CantBuy(id);
        if (why != null)
        {
            FarmAudio.Play2D("nope", 0.6f);
            MarquisSays(id == "hen" && !coopUpgraded && hensBought < hens.Count
                ? "Мур... Твоїм курочкам уже тісно. Спершу розшир курник!"
                : "Мур... " + char.ToUpper(why[0]) + why.Substring(1) + ".");
            return;
        }
        int price = Prices[id];
        if (coins < price)
        {
            FarmAudio.Play2D("nope", 0.6f);
            Say("Це коштує " + CoinsText(price) + ", а в тебе " + CoinsText(coins) + ".", 5f);
            return;
        }

        coins -= price;
        FarmAudio.Play2D("coin", 0.6f);
        if (id == "hen") BuyShopHen();
        else if (id == "coop_upgrade")
        {
            CloseShop();
            MarquisSays("Мур! Мої майстри вже біжать до твого курника!");
            StartCoroutine(UpgradeCoop()); // FarmPrototype.Eggs.cs
        }
        else if (id == "seeds")
        {
            AddItem(id, SeedsInPack);
            FarmAudio.Play2D("pickup", 0.7f);
            MarquisSays("Насіння моркви — " + SeedsInPack + " штуки. Посадиш — побачиш! Мур.");
        }
        else
        {
            AddItem(id);
            FarmAudio.Play2D("pickup", 0.7f);
            string later = id == "shovel" ? " Нею можна копати." : id == "rake" ? " Ними полють грядки." : "";
            MarquisSays(ItemName(id) + " — твої! Мур." + later);
        }

        if (Has("planks") && Has("paint") && !usedHints.Contains("#house"))
        {
            usedHints.Add("#house");
            Say(stage >= Stage.RepairHouse
                ? "Дошки й фарба є! Тепер — до будинку діда Панаса."
                : "Дошки й фарба є! Вони знадобляться для ремонту будинку діда Панаса.", 8f);
        }
        SaveGame();
    }

    // The new hen appears in front of the coop and joins the others.
    void BuyShopHen()
    {
        int index = hensBought++;
        GameObject hen = hens[index];
        Vector3 spot = FarmWorldBuilder.CoopDoorOutside + new Vector3(Random.Range(-1.5f, 1.5f), 0f, -0.6f);
        StartCoroutine(HenAppears(hen, spot));
        henNextLay[index] = Time.time + Random.Range(30f, 60f);
        string name = hen.GetComponent<FarmInteractable>().itemId;
        MarquisSays("Курочка " + name + " вже біжить до твого курника! Мур.");
    }

    // ---------- loading a saved game ----------

    void ApplyShopState()
    {
        if (stage >= Stage.MeetMarquis) marquis.SetActive(true);
        if (stage == Stage.MeetMarquis) basketGlow.SetActive(true);
    }
}
