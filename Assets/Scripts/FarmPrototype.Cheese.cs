using System.Collections;
using UnityEngine;

// The cheese house (part of FarmPrototype, see FarmPrototype.cs): growing the farm by making things worth
// more. Marquis sells it after the barn opens (60 coins); it's built part by part in the yard's left corner.
//   2 bottles of cow's milk  -> 1 cheese       (Marquis pays 28, the milk alone was 20)
//   2 bottles of goat's milk -> 1 goat cheese  (16, the milk alone was 12)
// A batch takes about 1.5 minutes; steam puffs from the chimney while it cooks. The hut: FarmWorldBuilder.Chores.cs.
public partial class FarmPrototype
{
    const float CheeseSeconds = 90f;

    bool cheeseBuilt;
    int cheeseBatch;          // 0 = nothing cooking, 1 = cow's cheese, 2 = goat cheese
    float cheeseProgress;     // 0..1
    float nextSteam;
    GameObject cheeseHouse, cheeseGlow;
    FarmInteractable cheeseAction;

    void FindCheeseParts()
    {
        cheeseHouse = Find("Cheese House");
        cheeseGlow = cheeseHouse.transform.Find("Glow Ring").gameObject;
        cheeseAction = cheeseHouse.GetComponent<FarmInteractable>();
        itemLooks["cheese"] = Find("Cheese Look");
        itemLooks["goat_cheese"] = Find("Goat Cheese Look");
    }

    bool CheeseReady => cheeseBatch != 0 && cheeseProgress >= 1f;

    // Bought from Marquis: hammer knocks, and the hut pops up part by part with dust.
    IEnumerator BuildCheeseHouse()
    {
        cheeseBuilt = true; // ownership is saved with the purchase, before the animation yields
        repairing = true;
        Say("Стук-стук! Будується сироварня…", 4f);
        StartCoroutine(HammerKnocks(FarmWorldBuilder.CheesePosition + new Vector3(1.4f, 1f, 0f), 4, 0.2f));
        for (int k = 0; k < 4; k++)
        {
            FarmAudio.Play(k % 2 == 0 ? "knock_1" : "knock_2", FarmWorldBuilder.CheesePosition + Vector3.up, 1f, Random.Range(0.95f, 1.05f));
            yield return new WaitForSeconds(0.2f);
        }
        yield return PopIn(cheeseHouse, 0.12f);
        FarmAudio.Play("success", FarmWorldBuilder.CheesePosition + Vector3.up, 1f);
        repairing = false;
        ShowBanner("СИРОВАРНЯ ГОТОВА!", "Дві пляшки молока — одна головка сиру. Сир Маркіз купує дорожче, ніж молоко!");
        SaveGame();
    }

    void UseCheesePress()
    {
        if (CheeseReady)
        {
            string item = cheeseBatch == 1 ? "cheese" : "goat_cheese";
            cheeseBatch = 0;
            cheeseProgress = 0f;
            AddItem(item);
            TaskEvent("make_cheese");
            Transform look = ItemModel(item) != null ? Instantiate(ItemModel(item)).transform : null;
            if (look != null)
            {
                look.position = cheeseHouse.transform.TransformPoint(new Vector3(0.45f, 1.15f, -0.95f));
                StartCoroutine(FlyToHand(look, 0.5f));
            }
            FarmAudio.Play2D("pickup", 0.8f);
            Say((item == "cheese" ? "Головка сиру" : "Козячий сир") + " готовий! Маркіз купує по " + SellPrice(item) + ".", 5f);
            SaveGame();
            return;
        }
        if (cheeseBatch != 0)
        {
            Say("Сир вариться… " + Mathf.RoundToInt(cheeseProgress * 100f) + "%", 3f);
            return;
        }
        int batch = ItemCount("milk") >= 2 ? 1 : ItemCount("goat_milk") >= 2 ? 2 : 0;
        if (batch == 0)
        {
            FarmAudio.Play2D("nope", 0.6f);
            Say("Для сиру потрібні 2 пляшки молока (коров'ячого або козячого).", 5f);
            return;
        }
        RemoveItem(batch == 1 ? "milk" : "goat_milk", 2);
        cheeseBatch = batch;
        cheeseProgress = 0f;
        FarmAudio.Play("water_pour", cheeseHouse.transform.position + Vector3.up, 0.9f, 0.7f);
        Say("Молоко в казані! Сир буде готовий приблизно за півтори хвилини.", 5f);
        SaveGame();
    }

    void UpdateCheese()
    {
        if (!cheeseBuilt) return;
        bool ready = CheeseReady;
        if (cheeseGlow.activeSelf != ready) cheeseGlow.SetActive(ready);
        cheeseAction.SetLabel(ready ? "забрати сир" : cheeseBatch != 0 ? "сир вариться" : "зварити сир (2 пляшки молока)");
        if (cheeseBatch == 0 || ready) return;
        cheeseProgress = Mathf.Min(1f, cheeseProgress + Time.deltaTime / CheeseSeconds);
        if (Time.time >= nextSteam) // steam from the chimney
        {
            nextSteam = Time.time + 0.7f;
            StartCoroutine(Puff(cheeseHouse.transform.TransformPoint(new Vector3(0.55f, 2.8f, 0.6f)), 0.25f));
        }
        if (cheeseProgress >= 1f)
        {
            FarmAudio.Play("pop", cheeseHouse.transform.position + Vector3.up * 1.5f, 1f, 1.2f, 6f);
            if (!MessageVisible) Say("Сир на сироварні готовий — забери його!", 5f);
        }
    }

    void ApplyCheeseState(GameSave save)
    {
        cheeseBuilt = save.cheeseBuilt;
        cheeseBatch = save.cheeseBatch;
        cheeseProgress = save.cheeseProgress;
        cheeseHouse.SetActive(cheeseBuilt);
    }
}
