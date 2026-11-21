using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The big barn and the big animals (part of FarmPrototype, see FarmPrototype.cs):
// - after the house is repaired, Boris gives you the key of the old barn;
// - the key turns in the lock, the chain falls, the big doors creak open (dust!) -> stalls, pens, hay;
// - then Marquis sells animals, unlocked step by step:
//     she-goats: once the barn is open;  cows: once you have 2 she-goats;
// - they graze on the lawn in the yard by day and sleep in the barn at night.
// The things themselves are built in FarmWorldBuilder.Chores.cs (BarnDoors, Animals).
public partial class FarmPrototype
{
    static readonly string[] BorisBarn =
    {
        "Ме-е! Ти відчинив стодолу! Я сюди не заходив, відколи дід поїхав.",
        "Колись тут жили корови Зірка й Ласка, і кізочки — мої подружки. Ліворуч стійла, праворуч загончики, нагорі сіно.",
        "Маркіз може привезти кізочок. А як матимеш дві — то й корову. Корови люблять компанію.",
        "Пасовище вже є — дивись, яка трава! Ферма «Сидорівка» знову замукає. Ме-е!",
    };
    static readonly string[] CowNoises = { "Му-у-у!", "Му?", "*жує траву*", "Му-у." };
    static readonly string[] SheGoatNoises = { "Ме-е!", "Ме-е-е?", "*скубе травичку*" };

    GameObject barnDoors, barnLock, barnGlow;
    Transform leftDoor, rightDoor;
    BoxCollider barnCollider;
    FarmInteractable barnAction;
    readonly List<GameObject> cows = new List<GameObject>();
    readonly List<GameObject> goats = new List<GameObject>();
    int cowsBought, goatsBought;
    bool barnUpgraded; // room for one more cow and two more goats
    int CowCapacity => barnUpgraded ? cows.Count : Mathf.Min(cows.Count, FarmWorldBuilder.CowsBeforeUpgrade);
    int GoatCapacity => barnUpgraded ? goats.Count : Mathf.Min(goats.Count, FarmWorldBuilder.GoatsBeforeUpgrade);
    int animalTalk;
    int[] cowMilkedDay, goatMilkedDay; // the day each one was last milked (once a day)
    int[] cowPettedDay, goatPettedDay; // the day each one was last petted (happy: twice the milk)

    void FindBarnParts()
    {
        barnDoors = Find("Barn Doors");
        barnLock = barnDoors.transform.Find("Lock").gameObject;
        barnGlow = barnDoors.transform.Find("Glow Ring").gameObject;
        leftDoor = barnDoors.transform.Find("Left Door");
        rightDoor = barnDoors.transform.Find("Right Door");
        barnCollider = barnDoors.GetComponent<BoxCollider>();
        barnAction = barnDoors.GetComponent<FarmInteractable>();
        for (int i = 0; i < FarmWorldBuilder.CowNames.Length; i++) cows.Add(Find("Cow " + i));
        for (int i = 0; i < FarmWorldBuilder.GoatNames.Length; i++) goats.Add(Find("She-goat " + i));
        itemLooks["barn_key"] = Find("Barn Key Look");
        itemLooks["milk"] = Find("Milk Bottle Look");
        itemLooks["goat_milk"] = Find("Goat Milk Look");
        cowMilkedDay = new int[cows.Count];
        goatMilkedDay = new int[goats.Count];
        cowPettedDay = new int[cows.Count];
        goatPettedDay = new int[goats.Count];
    }

    // At the end of the house repair (and for games saved after it): Boris hands over the key.
    void GiveBarnKey()
    {
        if (stage != Stage.EpisodeDone) return;
        stage = Stage.OpenBarn;
        AddItem("barn_key");
        barnGlow.SetActive(true);
        FarmAudio.Play2D("pickup", 0.8f);
        Say("Борис дав тобі ключ від старої великої стодоли! Відчини її — подивимось, що там.", 8f);
        SaveGame();
    }

    void UseBarn()
    {
        if (repairing) return;
        if (stage >= Stage.BarnOpen)
        {
            Say("Стодола відчинена. Заходь! Ліворуч стійла для корів, праворуч — загончики для кіз.", 5f);
            return;
        }
        if (stage < Stage.OpenBarn || !Has("barn_key"))
        {
            Say("Стара стодола діда Панаса. Ворота замкнені на великий іржавий замок, а ключа в тебе немає.", 6f);
            return;
        }
        StartCoroutine(OpenBarnDoors());
    }

    // The key flies to the lock and turns, the chain falls, the doors swing open with a creak and dust.
    IEnumerator OpenBarnDoors()
    {
        repairing = true;
        barnGlow.SetActive(false);
        Vector3 lockAt = barnLock.transform.position;
        Transform key = ToolCopy("barn_key");
        if (key != null)
        {
            Vector3 from = HandPosition();
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                key.position = Vector3.Lerp(from, lockAt + Vector3.back * 0.06f, t / 0.5f) + Vector3.up * Mathf.Sin(t / 0.5f * Mathf.PI) * 0.2f;
                key.rotation = Quaternion.LookRotation(Vector3.right);
                yield return null;
            }
            FarmAudio.Play("tick", lockAt, 1f, 0.6f);
            for (float t = 0f; t < 0.35f; t += Time.deltaTime) // turn!
            {
                key.rotation = Quaternion.LookRotation(Vector3.right) * Quaternion.Euler(0f, 0f, 90f * t / 0.35f);
                yield return null;
            }
            FarmAudio.Play("knock_2", lockAt, 1f, 1.3f);
            Destroy(key.gameObject);
        }
        // the lock and the chain drop to the ground
        Vector3 start = barnLock.transform.position;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            barnLock.transform.position = start + Vector3.down * (0.5f * 9.8f * t * t);
            barnLock.transform.rotation = Quaternion.Euler(0f, 0f, t * 200f);
            yield return null;
        }
        FarmAudio.Play("knock_1", barnLock.transform.position, 1f, 0.8f);
        barnLock.SetActive(false);

        FarmAudio.Play("creak", barnDoors.transform.position + new Vector3(0f, 2f, -4.6f), 1f, 1f, 8f);
        for (float t = 0f; t < 1.8f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 1.8f);
            leftDoor.localRotation = Quaternion.Euler(0f, 100f * k, 0f);   // both swing outwards
            rightDoor.localRotation = Quaternion.Euler(0f, -100f * k, 0f);
            yield return null;
        }
        for (int i = 0; i < 4; i++)
            StartCoroutine(Puff(barnDoors.transform.position + new Vector3(-1.5f + i, 0.1f, -4.6f), 0.6f));
        barnCollider.enabled = false; // the way in is free
        FarmAudio.Play("success", barnDoors.transform.position + new Vector3(0f, 2f, -4.6f), 1f);
        RemoveItem("barn_key", 1);
        stage = Stage.BarnOpen;
        repairing = false;
        SaveGame();

        yield return new WaitForSeconds(1f);
        yield return BorisComesOver();
        if (!InDialogue)
            StartDialogue(goat, "Борис", BorisBarn, () =>
                Say("Нове: у Маркіза можна купити кізочок (а згодом і корову). Вони пастимуться на газоні.", 9f));
    }

    // A bought animal walks out of the barn onto the pasture.
    void BuyAnimal(List<GameObject> herd, int index)
    {
        if (index >= herd.Count) return;
        GameObject animal = herd[index];
        Transform t = animal.transform;
        t.position = FarmWorldBuilder.BarnDoorInside;
        t.rotation = Quaternion.Euler(0f, 180f, 0f); // facing out (animals look along -Z)
        animal.SetActive(true);
        StartCoroutine(Puff(t.position + Vector3.up * 0.2f, 0.7f));
        FarmAudio.Play("pop", t.position + Vector3.up, 1f, 0.8f);
        Grazer grazer = animal.GetComponent<Grazer>();
        grazer.GoVia(FarmWorldBuilder.BarnDoorOutside, PasturePoint());
        string name = animal.GetComponent<FarmInteractable>().itemId;
        bool isCow = herd == cows;
        SpeakerSays(t, name + ": " + (isCow ? "Му-у-у!" : "Ме-е!"), 4f);
        TaskEvent("buy_animal");
        if (!isCow && goatsBought == 2)
            Say("Дві кізочки вже пасуться! Маркіз тепер може привезти й корову.", 7f);
    }

    Vector3 PasturePoint()
    {
        Vector2 min = FarmWorldBuilder.PastureMin, max = FarmWorldBuilder.PastureMax;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 p = new Vector3(Random.Range(min.x + 1f, max.x - 1f), 0f, Random.Range(min.y + 1f, max.y - 1f));
            bool blocked = false;
            foreach (Rect r in FarmWorldBuilder.PastureObstacles) blocked |= r.Contains(new Vector2(p.x, p.z));
            if (!blocked) return p;
        }
        return FarmWorldBuilder.BarnDoorOutside + Vector3.back * 3f;
    }

    // ---------- milk: every cow and she-goat can be milked once a day ----------

    bool MilkReady(GameObject animal)
    {
        if (!dayNight.running || dayNight.IsNight || bigHungry) return false; // hungry animals give no milk
        int cow = cows.IndexOf(animal), goat = goats.IndexOf(animal);
        if (cow >= 0) return cow < cowsBought && cowMilkedDay[cow] != dayNight.day;
        return goat >= 0 && goat < goatsBought && goatMilkedDay[goat] != dayNight.day;
    }

    // An orange circle under an animal that can be milked; the hint says what you can do.
    void UpdateBarn()
    {
        foreach (Grazer animal in BigAnimals())
        {
            bool ready = MilkReady(animal.gameObject);
            Transform ring = animal.transform.Find("Glow Ring");
            if (ring != null && ring.gameObject.activeSelf != ready) ring.gameObject.SetActive(ready);
            animal.GetComponent<FarmInteractable>().SetLabel(ready ? "подоїти" : "погладити");
        }
    }

    // A bottle appears under the udder, milk drips into it, and it flies into your hand.
    IEnumerator Milk(FarmInteractable animal)
    {
        repairing = true;
        bool isCow = cows.Contains(animal.gameObject);
        int index = isCow ? cows.IndexOf(animal.gameObject) : goats.IndexOf(animal.gameObject);
        string item = isCow ? "milk" : "goat_milk";
        Grazer grazer = animal.GetComponent<Grazer>();
        grazer.Face(playerCamera.transform); // stands still while you milk
        Vector3 udder = animal.transform.TransformPoint(isCow ? new Vector3(0f, 0.55f, 0.42f) : new Vector3(0f, 0.32f, 0.22f));
        Transform bottle = ToolCopy(item);
        if (bottle != null) bottle.position = new Vector3(udder.x, 0.02f, udder.z);
        FarmAudio.Play("water_pour", udder, 0.7f, 1.6f);
        for (float t = 0f; t < 1.2f; t += 0.12f)
        {
            Burst(udder, "FarmMaterials/Paper", 2, 0.015f, Vector3.down * 1.2f, 0.08f);
            yield return new WaitForSeconds(0.12f);
        }
        if (bottle != null) StartCoroutine(FlyToHand(bottle, 0.5f));
        grazer.StopFacing();
        bool happy = (isCow ? cowPettedDay : goatPettedDay)[index] == dayNight.day; // petted today: twice the milk
        if (isCow) cowMilkedDay[index] = dayNight.day;
        else goatMilkedDay[index] = dayNight.day;
        AddItem(item, happy ? 2 : 1);
        TaskEvent(isCow ? "milk" : "goat_milk");
        if (happy) Hearts(udder + Vector3.up * 0.5f);
        SpeakerSays(animal.transform, animal.itemId + ": " + (isCow ? "Му-у! Смачного!" : "Ме-е! На здоров'я!"), 3f);
        if (!usedHints.Contains("#milk"))
        {
            usedHints.Add("#milk");
            Say("Пляшка молока в рюкзаку! Маркіз купує коров'яче по " + SellPrices["milk"] + ", козяче по " + SellPrices["goat_milk"] + ". Доїти можна раз на день.", 8f);
        }
        repairing = false;
        SaveGame();
    }

    void TalkToAnimal(FarmInteractable animal)
    {
        if (MilkReady(animal.gameObject))
        {
            StartCoroutine(Milk(animal));
            return;
        }
        TaskEvent("pet_animal");
        Grazer grazer = animal.GetComponent<Grazer>();
        bool isCow = animal.name.StartsWith("Cow");
        if (grazer != null && !grazer.Sleeping)
        {
            Hearts(animal.transform.position + Vector3.up * (isCow ? 1.5f : 0.9f));
            int index = isCow ? cows.IndexOf(animal.gameObject) : goats.IndexOf(animal.gameObject);
            int[] petted = isCow ? cowPettedDay : goatPettedDay;
            if (index >= 0 && petted[index] != dayNight.day)
            {
                petted[index] = dayNight.day;
                SayOnce("#happy_milk", "Погладжені тварини щасливі: сьогодні при доїнні дадуть ДВІ пляшки молока!", 7f);
            }
        }
        string[] noises = isCow ? CowNoises : SheGoatNoises;
        string sound = grazer != null && grazer.Sleeping ? "Хр-р-р..." : noises[animalTalk++ % noises.Length];
        SpeakerSays(animal.transform, animal.itemId + ": " + sound, 3f);
    }

    IEnumerable<Grazer> BigAnimals()
    {
        for (int i = 0; i < cowsBought && i < cows.Count; i++) yield return cows[i].GetComponent<Grazer>();
        for (int i = 0; i < goatsBought && i < goats.Count; i++) yield return goats[i].GetComponent<Grazer>();
    }

    Vector3 BarnSpot(Grazer animal)
    {
        int cow = cows.IndexOf(animal.gameObject);
        return cow >= 0 ? FarmWorldBuilder.CowStall(cow) : FarmWorldBuilder.GoatPen(goats.IndexOf(animal.gameObject));
    }

    // Evening: everybody walks into the barn to their stall. Morning: out to the pasture.
    void BarnOnEvening()
    {
        foreach (Grazer animal in BigAnimals())
        {
            animal.GoVia(FarmWorldBuilder.BarnDoorOutside, FarmWorldBuilder.BarnDoorInside, BarnSpot(animal));
            animal.SetSleeping(true);
        }
    }

    void BarnOnMorning()
    {
        HayMorning();
        foreach (Grazer animal in BigAnimals())
        {
            animal.GoVia(FarmWorldBuilder.BarnDoorInside, FarmWorldBuilder.BarnDoorOutside, PasturePoint());
            animal.SetSleeping(false);
        }
    }

    // ---------- hay: cows and goats eat hay from the troughs in the barn ----------
    // The troughs hold 20 kg; every morning a cow eats 4 kg and a she-goat 1.5 kg. Not enough -> no milk
    // that day. Hay comes from the bales by the storage shed (grandpa left 30 kg; more is sold there).

    const float HayTroughCapacity = 20f, HayPerCow = 4f, HayPerGoat = 1.5f;
    const int HayBag = 10;
    float barnHayKg = 6f, storageHayKg = 30f;
    bool bigHungry;
    GameObject hayStack;
    Transform troughHay;

    void FindHayParts()
    {
        hayStack = Find("Hay Stack");
        troughHay = world.Find("Hay Trough/Trough Hay");
    }

    void TakeHay()
    {
        if (cowsBought + goatsBought == 0 && stage < Stage.BarnOpen)
        {
            Say("Тюки сіна. Згодяться для корів і кіз, коли відчиниш стодолу.", 5f);
            return;
        }
        int carried = ItemCount("hay");
        if (carried >= HayBag)
        {
            Say("У тебе вже оберемок сіна (" + HayBag + " кг). Неси в ясла в стодолі!", 5f);
            return;
        }
        if (storageHayKg < 1f)
        {
            FarmAudio.Play2D("nope", 0.6f);
            Say("Сіно скінчилось! Купи ще на складі (вивіска «СКЛАД»).", 6f);
            return;
        }
        int take = Mathf.Min(HayBag - carried, Mathf.FloorToInt(storageHayKg));
        storageHayKg -= take;
        AddItem("hay", take);
        FarmAudio.Play("rustle", hayStack.transform.position + Vector3.up * 0.5f, 1f, 0.9f);
        Burst(hayStack.transform.position + Vector3.up * 0.8f, "FarmMaterials/Straw", 8, 0.04f, Vector3.up, 0.7f);
        ShowHay();
        Say("+" + take + " кг сіна (на складі лишилось " + Mathf.FloorToInt(storageHayKg) + " кг). Неси в ясла в стодолі.", 6f);
        SaveGame();
    }

    void FillTrough()
    {
        if (stage < Stage.BarnOpen)
        {
            Say("Ясла для корів і кіз.", 3f);
            return;
        }
        if (barnHayKg >= HayTroughCapacity - 0.5f)
        {
            Say("Ясла повні (" + HayTroughCapacity + " кг сіна).", 4f);
            return;
        }
        if (!Has("hay"))
        {
            FarmAudio.Play2D("nope", 0.6f);
            Say("Спершу набери сіна — тюки біля складу. Ясла: " + Mathf.FloorToInt(barnHayKg) + " / " + HayTroughCapacity + " кг.", 6f);
            return;
        }
        int put = Mathf.Min(Mathf.CeilToInt(HayTroughCapacity - barnHayKg), ItemCount("hay"));
        RemoveItem("hay", put);
        barnHayKg = Mathf.Min(HayTroughCapacity, barnHayKg + put);
        bool wasHungry = bigHungry;
        bigHungry = false;
        TaskEvent("hay");
        FarmAudio.Play("rustle", troughHay.position, 1f, 1.1f);
        Burst(troughHay.position + Vector3.up * 0.4f, "FarmMaterials/Straw", 10, 0.04f, Vector3.down * 0.5f, 0.5f);
        ShowHay();
        Say(wasHungry ? "Корови й кізочки нарешті поїли! Тепер знову даватимуть молоко." : "Ясла: " + Mathf.FloorToInt(barnHayKg) + " / " + HayTroughCapacity + " кг сіна.", 5f);
        SaveGame();
    }

    void ShowHay()
    {
        if (troughHay != null)
        {
            troughHay.gameObject.SetActive(barnHayKg > 0.1f);
            Vector3 size = troughHay.localScale;
            troughHay.localScale = new Vector3(size.x, Mathf.Lerp(0.02f, 0.08f, barnHayKg / HayTroughCapacity), size.z);
        }
        Transform bales = hayStack != null ? hayStack.transform.Find("Model") : null;
        if (bales == null) return;
        int show = Mathf.CeilToInt(storageHayKg / 10f);
        for (int i = 0; i < bales.childCount; i++) bales.GetChild(i).gameObject.SetActive(i < show);
    }

    // Every morning the cows and goats eat; not enough hay -> no milk today.
    void HayMorning()
    {
        float need = cowsBought * HayPerCow + goatsBought * HayPerGoat;
        if (need <= 0f) return;
        if (barnHayKg >= need)
        {
            barnHayKg -= need;
            bigHungry = false;
            if (barnHayKg < need) morningNote += " Сіна в яслах лишилось мало — принеси ще.";
        }
        else
        {
            barnHayKg = 0f;
            bigHungry = true;
            morningNote += " Ясла в стодолі порожні: корови й кізочки голодні, молока сьогодні не буде! Принеси сіна.";
        }
        ShowHay();
    }

    string HayText()
    {
        if (cowsBought + goatsBought == 0) return "";
        string text = "\nЯсла: " + Mathf.FloorToInt(barnHayKg) + " / " + HayTroughCapacity + " кг • Склад: " + Mathf.FloorToInt(storageHayKg) + " кг сіна";
        if (bigHungry) text += "\nКорови й кізочки голодні — принеси сіна в ясла!";
        return text;
    }

    string BarnQuestText()
    {
        if (stage == Stage.OpenBarn) return "Відімкни велику стодолу ключем від Бориса: наведи на ворота.";
        if (goatsBought == 0) return "Купи в Маркіза кізочку (" + CoinsText(Prices["goat"]) + "): вона пастиметься на газоні, а спатиме в стодолі.";
        if (goatsBought < 2) return "Купи ще одну кізочку: коли їх буде дві, Маркіз привезе й корову.";
        if (cowsBought == 0) return "Тепер можна купити корову (" + CoinsText(Prices["cow"]) + ")! У стодолі є стійла для двох.";
        return "Біля лавки на автобусній зупинці є синя поштова скринька. Водій рейсу 21 залишив там листа для діда Панаса.";
    }

    // ---------- loading a saved game ----------

    void ApplyBarnState(GameSave save)
    {
        cowsBought = Mathf.Min(save.cowsBought, cows.Count);
        goatsBought = Mathf.Min(save.goatsBought, goats.Count);
        for (int i = 0; i < cowMilkedDay.Length && save.cowMilked != null && i < save.cowMilked.Length; i++) cowMilkedDay[i] = save.cowMilked[i];
        for (int i = 0; i < goatMilkedDay.Length && save.goatMilked != null && i < save.goatMilked.Length; i++) goatMilkedDay[i] = save.goatMilked[i];
        if (save.cowPetted != null) System.Array.Copy(save.cowPetted, cowPettedDay, Mathf.Min(save.cowPetted.Length, cowPettedDay.Length));
        if (save.goatPetted != null) System.Array.Copy(save.goatPetted, goatPettedDay, Mathf.Min(save.goatPetted.Length, goatPettedDay.Length));
        bool open = stage >= Stage.BarnOpen;
        barnLock.SetActive(!open);
        barnCollider.enabled = !open;
        leftDoor.localRotation = Quaternion.Euler(0f, open ? 100f : 0f, 0f);
        rightDoor.localRotation = Quaternion.Euler(0f, open ? -100f : 0f, 0f);
        barnGlow.SetActive(stage == Stage.OpenBarn);
        ShowHay();
        bool night = dayNight.running && dayNight.IsNight;
        foreach (Grazer animal in BigAnimals())
        {
            animal.gameObject.SetActive(true);
            animal.PlaceAt(night ? BarnSpot(animal) : PasturePoint());
            animal.SetSleeping(night);
        }
    }
}
