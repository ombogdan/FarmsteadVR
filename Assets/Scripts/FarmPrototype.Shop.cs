using System.Collections.Generic;
using UnityEngine;

// Stage 4 (part of FarmPrototype, see FarmPrototype.cs): Marquis's shop by the road.
// - Put eggs in the basket on the counter: 3 coins each.
// - Buy more hens (up to 6), planks and paint (for repairing the house later), a shovel and a rake.
// - At night the shop is closed.
public partial class FarmPrototype
{
    const int EggPrice = 3;
    const int CarrotPrice = 5;
    const int SeedsInPack = 4;

    static readonly Dictionary<string, int> Prices = new Dictionary<string, int>
    {
        { "hen", 5 }, { "planks", 15 }, { "paint", 10 }, { "shovel", 8 }, { "rake", 8 }, { "seeds", 4 },
    };

    static readonly string[] MarquisIntro =
    {
        "Мур-р... Новенький фермер? Дозволь представитись: Маркіз. Купець, мандрівник і просто красень.",
        "Яйця купую по 3 монети. Найкраща ціна в окрузі! Ну... єдина ціна в окрузі.",
        "А ще продаю все, що треба фермеру: курочок, дошки, фарбу, лопати. Майже нові!",
        "Клади яйця в кошик на прилавку — і монети твої. Мур.",
    };
    static readonly string[] MarquisLater =
    {
        "Мур. Яйця — в кошик, монети — тобі.",
        "Свіжі новини: Борис знову з'їв чиюсь шапку.",
        "Чув, Жора хоче стати мером? Мур-ха-ха.",
        "Морква? Беру по 5 монет. Мур-р, обожнюю моркв... тобто клієнтів.",
        "Ситі курочки несуться частіше. Це я тобі як кіт кажу. Мур.",
    };

    GameObject marquis;
    GameObject basketGlow;
    int marquisTalk;

    void FindShopParts()
    {
        Transform shop = world.Find("Shop");
        marquis = shop.Find("Marquis").gameObject;
        basketGlow = shop.Find("Egg Basket/Glow Ring").gameObject;
        foreach (FarmInteractable thing in shop.GetComponentsInChildren<FarmInteractable>(true))
            if (thing.kind == FarmInteractable.Kind.ShopItem && thing.itemId != "hen")
                itemLooks[thing.itemId] = thing.gameObject; // so the inventory can show a small copy
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
        if (stage == Stage.MeetMarquis) MeetMarquis();
        else if (dayNight.IsNight) MarquisSays("Мур-р... Лавка зачинена. Приходь зранку.");
        else MarquisSays(MarquisLater[marquisTalk++ % MarquisLater.Length]);
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

    void SellEggs()
    {
        if (!ShopReady()) return;
        int eggs = ItemCount("egg");
        int carrots = ItemCount("carrot");
        if (eggs + carrots == 0)
        {
            MarquisSays("Мур? Кошик порожній. Принось яйця з курника!");
            return;
        }
        RemoveItem("egg", eggs);
        RemoveItem("carrot", carrots);
        int earned = eggs * EggPrice + carrots * CarrotPrice;
        coins += earned;
        FarmAudio.Play2D("coin", 0.8f);
        string what = eggs > 0 ? Plural(eggs, "яйце", "яйця", "яєць") : "";
        if (carrots > 0) what += (what.Length > 0 ? " і " : "") + Plural(carrots, "морквина", "морквини", "морквин");
        MarquisSays(what + "? Мур-р, свіженькі! Тримай " + CoinsText(earned) + ".");
        SaveGame();
    }

    void Buy(FarmInteractable item)
    {
        if (!ShopReady()) return;
        string id = item.itemId;
        int price = Prices[id];
        if (id == "hen" && hensBought >= hens.Count)
        {
            MarquisSays("Мур... Твій курник уже повний. Більше курочок не влізе.");
            return;
        }
        bool onlyOne = id == "shovel" || id == "rake" || id == "planks" || id == "paint";
        if (onlyOne && Has(id))
        {
            Say("У тебе вже є " + ItemName(id).ToLower() + ".", 4f);
            return;
        }
        if ((id == "planks" || id == "paint") && stage >= Stage.EpisodeDone)
        {
            MarquisSays("Будинок і так як новий! Дошки й фарба тобі поки ні до чого. Мур.");
            return;
        }
        if (coins < price)
        {
            FarmAudio.Play2D("nope", 0.6f);
            Say("Це коштує " + CoinsText(price) + ", а в тебе " + CoinsText(coins) + ".", 5f);
            return;
        }

        coins -= price;
        FarmAudio.Play2D("coin", 0.6f);
        if (id == "hen") BuyShopHen();
        else if (id == "seeds")
        {
            AddItem(id, SeedsInPack);
            FarmAudio.Play("pickup", item.transform.position + Vector3.up * 0.3f, 0.8f);
            MarquisSays("Насіння моркви — " + SeedsInPack + " штуки. Посадиш — побачиш! Мур.");
        }
        else
        {
            AddItem(id);
            FarmAudio.Play("pickup", item.transform.position + Vector3.up * 0.3f, 0.8f);
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
