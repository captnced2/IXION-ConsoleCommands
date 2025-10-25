using IMHelper;
using Stanford.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ConsoleCommands;

public static class Console
{
    private static GameInputLockHandle lockHandle;
    private static TMP_InputField input;
    private static Transform consoleObject;
    private static Canvas escMenu;

    internal static void inGameListener()
    {
        if (input != null) return;
        var container = GameObject.Find("Canvas/WindowManagerCenterOption/UI Window Save Game/NewSavePopup/Container");
        consoleObject =
            Object.Instantiate(container.transform.FindChild("Content"), GameObject.Find("Canvas").transform);
        consoleObject.name = "ConsoleCommands";
        Object.Instantiate(
            GameObject.Find(
                "Canvas/WindowManagerCenterOption/UI Window Save Game/Container/ContentTop/New Save/Border"),
            consoleObject.transform.FindChild("InputField"));
        consoleObject.transform.FindChild("InputField/Text Area/Placeholder").gameObject.SetActive(false);
        var rect = consoleObject.gameObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(0, 0);
        input = consoleObject.FindChild("InputField").GetComponent<TMP_InputField>();
        var inRect = input.GetComponent<RectTransform>();
        inRect.sizeDelta = new Vector2(-250, 30);
        inRect.anchoredPosition = new Vector2(165, 20);
        input.textComponent.alignment = TextAlignmentOptions.Left;
        input.textComponent.fontSize = 14;
        input.textComponent.fontStyle = FontStyles.Normal;
        input.textComponent.transform.parent.gameObject.AddComponent<RectMask2D>();
        input.onDeselect.AddListener((UnityAction<string>)onDeselect);
        input.onSubmit.AddListener((UnityAction<string>)onSubmit);
        consoleObject.gameObject.SetActive(false);
        escMenu = GameObject.Find("Canvas/WindowManagerCenterOption/UI Window Option").GetComponent<Canvas>();
    }

    internal static void consoleHotkeyPressed()
    {
        if (!Plugin.enabled) return;
        if (escMenu.enabled) return;
        if (consoleObject == null) inGameListener();
        consoleObject.gameObject.SetActive(true);
        input.ActivateInputField();
        lockHandle = GameInputLockAll.CreateLock();
        KeyListenerHelper.lockInputs();
        input.Select();
    }

    internal static void escPressed()
    {
        onDeselect("");
    }

    internal static void mainMenuListener()
    {
        Object.Destroy(consoleObject?.gameObject);
        consoleObject = null;
        Commands.loadAllCommands();
    }

    private static void onDeselect(string s)
    {
        consoleObject.gameObject.SetActive(false);
        lockHandle?.Stop();
        KeyListenerHelper.unlockInputs();
        input.SetText("");
        input.DeactivateInputField();
    }

    private static void onSubmit(string s)
    {
        Commands.runCommand(s);
        onDeselect("");
    }
}