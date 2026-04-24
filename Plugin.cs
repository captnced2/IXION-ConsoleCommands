using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using IMHelper;
using UnityEngine;

namespace ConsoleCommands;

[BepInPlugin(Guid, Name, Version)]
[BepInProcess("IXION.exe")]
[BepInDependency("captnced.IMHelper", ">=3.3.0")]
public class Plugin : BasePlugin
{
    private const string Guid = "captnced.ConsoleCommands";
    private const string Name = "ConsoleCommands";
    private const string Version = "1.0.2";
    internal new static ManualLogSource Log;
    internal static bool enabled = true;
    private static SettingsHelper.SettingsSection sec;
    public override void Load()
    {
        Log = base.Log;
        if (IL2CPPChainloader.Instance.Plugins.ContainsKey("captnced.IMHelper")) setEnabled();
        GameStateHelper.addSceneChangedToInGameListener(Console.inGameListener);
        GameStateHelper.addSceneChangedListener(Console.mainMenuListener, GameStateHelper.GameScene.MainMenu);
        KeyListenerHelper.addInGameKeyListener(KeyCode.Escape, Console.escPressed);
        if (!enabled)
            Log.LogInfo("Disabled by IMHelper!");
        else
            init();
    }

    internal static string getStringFromEmbedTxtFile(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream("ConsoleCommands.resources." + fileName);
        if (stream == null)
        {
            Log.LogError("Error reading embedded resource: " + fileName);
            return null;
        }

        var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void setEnabled()
    {
        enabled = ModsMenu.isSelfEnabled();
    }

    private static void init()
    {
        sec = new SettingsHelper.SettingsSection("Console Commands");
        SettingsHelper.addTopSection(sec);
        sec.addItem(new SettingsHelper.KeySetting("Open Console",
            "Opens the mods console. Feedback is only provided in the BepInEx Console", KeyCode.F8,
            Console.consoleHotkeyPressed, true));
        Log.LogInfo("Loaded \"" + Name + "\" version " + Version + "!");
    }

    private static void disable()
    {
        sec.destroy();
        Log.LogInfo("Unloaded \"" + Name + "\" version " + Version + "!");
    }

    public static void enable(bool value)
    {
        enabled = value;
        if (enabled) init();
        else disable();
    }
}