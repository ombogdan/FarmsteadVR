using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class FarmPrototype
{
    public bool AtMainMenu { get; private set; } = true;
    bool confirmNew, changingScene, restoring;
    GameSave menuSave;
    ShopWindow titleBoard;
    int introPage;
    bool hasReturnSpot;
    Vector3 returnSpot;
    float returnYaw;

    string SaveDescription()
    {
        if (menuSave == null) return "Збереженої гри поки немає";
        string place = menuSave.stage == 0 ? (menuSave.introPage >= 3 ? "Автобус до села" : "Лист від діда") :
            "День " + menuSave.day + "   •   " + CoinsText(menuSave.coins);
        if (menuSave.savedAt > 0)
            place += "\n" + DateTimeOffset.FromUnixTimeSeconds(menuSave.savedAt).ToLocalTime().ToString("dd.MM.yyyy   HH:mm");
        return place;
    }

    void UpdateMainMenu()
    {
        if (!FarmInput.HeadsetActive) return;
        if (titleBoard == null)
        {
            titleBoard = ShopWindow.Create();
            titleBoard.OnAction = MenuChoice;
            titleBoard.Open(playerCamera.transform);
            RenderTitleBoard();
        }
        Transform source = aimSource != null ? aimSource : playerCamera.transform;
        aimHasHit = titleBoard.Aim(source.position, source.forward, out aimPoint);
        if (aimHasHit) aimHitDistance = Vector3.Distance(source.position, aimPoint);
        titleBoard.Tick(FarmInput.InteractPressed(), FarmInput.InteractHeld(), FarmInput.InteractReleased());
    }

    void RenderTitleBoard()
    {
        if (titleBoard != null) titleBoard.ShowTitleMenu(confirmNew, menuSave != null, SaveDescription());
    }

    void MenuChoice(string choice)
    {
        if (!AtMainMenu || changingScene) return;
        if (choice == "back" || (choice == "new" && menuSave != null))
        {
            confirmNew = choice == "new";
            RenderTitleBoard();
            return;
        }
        bool resume = choice == "continue";
        if ((resume && (menuSave == null || confirmNew)) ||
            (!resume && choice != "new" && !(choice == "replace" && confirmNew))) return;
        if (!resume)
        {
            menuSave = new GameSave();
            menuSave.Store(newGame: true);
        }
        AtMainMenu = false;
        if (titleBoard != null) titleBoard.Close();
        aimHasHit = false;
        if (!FarmInput.HeadsetActive) DesktopPlayer.LockCursor(true);
        if (menuSave.stage == (int)Stage.Intro)
        {
            introPage = Mathf.Clamp(menuSave.introPage, 0, 3);
            StartCoroutine(PlayIntro());
        }
        else StartCoroutine(ContinueSavedGame(menuSave));
        menuSave = null;
    }

    void DrawMainMenu()
    {
        float width = Mathf.Min(420f, Screen.width - 24f);
        Rect panel = new Rect((Screen.width - width) / 2f, Mathf.Max(4f, (Screen.height - 266f) / 2f), width, 266f);
        Color previous = GUI.color;
        GUI.color = new Color(0.065f, 0.14f, 0.13f, 0.96f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(0.92f, 0.74f, 0.39f);
        GUI.DrawTexture(new Rect(panel.x, panel.y, width, 3f), Texture2D.whiteTexture);
        GUI.color = previous;
        GUILayout.BeginArea(new Rect(panel.x + 24f, panel.y + 14f, width - 48f, panel.height - 24f));
        var title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 24, fontStyle = FontStyle.Bold };
        var caption = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = 16 };
        GUILayout.Space(12);
        GUILayout.Label(confirmNew ? "Почати спочатку?" : "Ферма «Сидорівка»", title);
        GUILayout.Label(confirmNew ? "Нова гра замінить твій збережений прогрес." : SaveDescription(), caption, GUILayout.Height(56));
        GUILayout.Space(12);
        if (confirmNew)
        {
            if (GUILayout.Button("Назад", GUILayout.Height(40))) MenuChoice("back");
            if (GUILayout.Button("Замінити й почати", GUILayout.Height(40))) MenuChoice("replace");
        }
        else
        {
            GUI.enabled = menuSave != null;
            if (GUILayout.Button("Продовжити", GUILayout.Height(40))) MenuChoice("continue");
            GUI.enabled = true;
            if (GUILayout.Button("Нова гра", GUILayout.Height(40))) MenuChoice("new");
        }
        GUILayout.EndArea();
    }

    void RecordIntroPage(int page)
    {
        introPage = page;
        SaveGame();
    }

    void ReturnToMenu()
    {
        SaveGame();
        changingScene = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
