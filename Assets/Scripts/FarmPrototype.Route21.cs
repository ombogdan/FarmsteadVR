using UnityEngine;

// A one-off delivery to the village school, exchanged at the stop where the player first arrived.
public partial class FarmPrototype
{
    const int RouteReward = 55;
    int routeSentDay;
    FarmInteractable routeBox;
    GameObject routeNote, routeParcel, routeRosette;

    void FindRouteParts()
    {
        Transform box = world.Find("Route 21 Post");
        routeBox = box.GetComponent<FarmInteractable>();
        routeNote = box.Find("Letter").gameObject;
        routeParcel = box.Find("Lunch Basket").gameObject;
        routeRosette = box.Find("Rosette").gameObject;
    }

    bool RouteInvitation => stage == Stage.BarnOpen && cowsBought > 0;
    bool RouteReplyReady => stage == Stage.RouteReply && dayNight.day > routeSentDay;

    void ShowRoutePost()
    {
        routeNote.SetActive(RouteInvitation || RouteReplyReady);
        routeParcel.SetActive(stage == Stage.RouteReply && !RouteReplyReady);
        routeRosette.SetActive(stage == Stage.RouteDone);
        routeBox.SetLabel(RouteInvitation || RouteReplyReady ? "прочитати лист" :
            stage == Stage.RoutePacking ? "передати гостинець" : "оглянути пошту рейсу 21");
    }

    string RouteQuestText()
    {
        switch (stage)
        {
            case Stage.RoutePacking:
                return "Гостинець для шкільної кухні. Принеси до пошти на зупинці два звичайні яйця (" +
                    Mathf.Min(2, ItemCount("egg")) + "/2) і будь-який сир (" +
                    (Has("cheese") || Has("goat_cheese") ? "1/1" : "0/1") + "). Сироварню продає Маркіз.";
            case Stage.RouteReply:
                return RouteReplyReady ? "Водій залишив відповідь зі школи. Зазирни до пошти на зупинці!" :
                    "Кошик уже на зупинці. Перевір пошту завтра — водій обіцяв привезти відповідь.";
            default:
                return "Стрічка зі шкільного свята прикрашає зупинку. Ферма знову годує село!";
        }
    }

    void UseRoutePost()
    {
        if (RouteInvitation)
        {
            StartDialogue(routeBox.transform, "Лист зі школи", new[] {
                "Панасе, пам’ятаєш, як ми везли твій сир у шкільній формі для скрипки? Досі пахне кропом. Це Галя, твоя колишня сусідка по парті.",
                "Тепер я куховарю в нашій школі. Хочемо спекти пиріг за твоїм рецептом. Бракує двох яєць і головки сиру. Залишиш біля лавки? Водій двадцять першого забере.",
                "А ще: Якщо це читає хтось із молодших — сподіваюся, Панас навчив тебе головного. Козла до кошика не підпускати!"
            }, () => { stage = Stage.RoutePacking; ShowRoutePost(); SaveGame(); });
            return;
        }
        if (stage == Stage.RoutePacking)
        {
            string cheese = Has("goat_cheese") ? "goat_cheese" : "cheese";
            if (ItemCount("egg") < 2 || !Has(cheese)) { Say(RouteQuestText(), 7f); return; }
            RemoveItem("egg", 2);
            RemoveItem(cheese, 1);
            routeSentDay = dayNight.day;
            stage = Stage.RouteReply;
            ShowRoutePost();
            SaveGame();
            FarmAudio.Play2D("pickup", 0.8f);
            Say("Ти накрив кошик рушником і підписав його: «Галі, на шкільну кухню». Відповідь буде завтра.", 7f);
            return;
        }
        if (RouteReplyReady)
        {
            StartDialogue(routeBox.transform, "Записка в порожньому кошику", new[] {
                "Пиріг трохи підгорів з одного боку. Усі просили саме той шматочок. Панас завжди казав, що то найкраща скоринка.",
                "За гостинець — гроші в конверті. А стрічку сплели діти. Повісь там, де її видно з автобуса. Будемо махати тобі в вікно! — Галя"
            }, () => {
                stage = Stage.RouteDone;
                coins += RouteReward;
                ShowRoutePost();
                SaveGame();
                FarmAudio.Play2D("success", 0.9f);
                Say("На поштовій скриньці тепер синьо-жовта стрічка. У конверті — " + CoinsText(RouteReward) + ".", 8f);
            });
            return;
        }
        Say(stage == Stage.RouteReply || stage == Stage.RouteDone ? RouteQuestText() :
            "Пошта рейсу 21. Під кришкою олівцем написано: «Передачі в село залишати тут. Панас».", 6f);
    }
}
