using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx.Unity.IL2CPP;

namespace ConsoleCommands;

public static class Commands
{
    private static MethodInfo[] moddedCommandsMethods;

    internal static void loadAllCommands()
    {
        if (moddedCommandsMethods != null) return;
        List<MethodInfo> methods = [];
        foreach (var p in IL2CPPChainloader.Instance.Plugins)
            methods.AddRange(p.Value.Instance.GetType().Assembly.GetTypes().SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(ModdedCommand), false).Length > 0));
        moddedCommandsMethods = methods.ToArray();
        var cmdString = "Loaded " +
                        moddedCommandsMethods.Where(m =>
                                !((ModdedCommand)m.GetCustomAttributes(typeof(ModdedCommand), true)[0]).disabled)
                            .ToArray()
                            .Length + " commands";
        if (moddedCommandsMethods.Length > 0) cmdString += ": ";
        cmdString = moddedCommandsMethods
            .Where(m => !((ModdedCommand)m.GetCustomAttributes(typeof(ModdedCommand), true)[0]).disabled).Aggregate(
                cmdString,
                (current, method) =>
                    current + ((ModdedCommand)method.GetCustomAttributes(typeof(ModdedCommand), true)[0]).name + ", ");
        if (moddedCommandsMethods.Length > 0) cmdString = cmdString[..^2];
        Plugin.Log.LogInfo(cmdString);
    }

    public static void runCommand(string cmd)
    {
        var split = cmd.Split(' ');
        var cmdName = split[0].ToLower();
        if (cmdName == "help")
        {
            displayUsages();
            return;
        }

        MethodInfo cmdMethod = null;
        foreach (var m in moddedCommandsMethods!)
        {
            var att = (ModdedCommand)m.GetCustomAttributes(typeof(ModdedCommand), true)[0];
            if (cmdName == att.name) cmdMethod = m;
        }

        if (cmdMethod == null)
        {
            Plugin.Log.LogError("Command not found: \"" + cmdName + "\"");
            return;
        }
        
        if (((ModdedCommand) cmdMethod.GetCustomAttributes(typeof(ModdedCommand), true)[0]).disabled)
        {
            Plugin.Log.LogError("Command \"" + cmdName + "\" is disabled!");
            return;
        }

        var param = cmdMethod.GetParameters().Select(p => p.ParameterType).ToArray();
        var sendSuccess = true;
        if (param.Length > 0)
        {
            if (param[0] == typeof(string) && param.Length == 1)
            {
                try
                {
                    var result = cmdMethod.Invoke(null, [string.Join(" ", split[1..])]);
                    if (cmdMethod.ReturnType == typeof(bool)) sendSuccess = (bool)result!;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("Command \"" + cmdName + "\" threw an exception: " + e);
                    return;
                }
            }
            else
            {
                if (param.Length != split.Length - 1)
                {
                    Plugin.Log.LogError("Command \"" + cmdName + "\" has " + param.Length +
                                        " parameter(s) but was invoked with " + (split.Length - 1));
                    return;
                }

                var cmdAtt = split[1..];
                for (var i = 0; i < param.Length; i++)
                {
                    if (isOfType(cmdAtt[i], param[i])) continue;
                    Plugin.Log.LogError("Parameter \"" + cmdAtt[i] + "\" can't be converted to type \"" + param[i] +
                                        "\"");
                    return;
                }

                try
                {
                    var test = cmdAtt.Select(a =>
                    {
                        var converter = TypeDescriptor.GetConverter(param[Array.IndexOf(cmdAtt, a)]);
                        return converter.ConvertFrom(a);
                    }).ToArray();
                    var result = cmdMethod.Invoke(null, test);
                    if (cmdMethod.ReturnType == typeof(bool)) sendSuccess = (bool)result!;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("Command \"" + cmdName + "\" threw an exception: " + e);
                    return;
                }
            }
        }
        else
        {
            try
            {
                var result = cmdMethod.Invoke(null, null);
                if (cmdMethod.ReturnType == typeof(bool)) sendSuccess = (bool)result!;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Command \"" + cmdName + "\" threw an exception: " + e);
                return;
            }
        }

        if (sendSuccess)
            Plugin.Log.LogInfo("Successfully executed command \"" + cmdName + "\"");
    }

    private static bool isOfType(string value, Type type)
    {
        var converter = TypeDescriptor.GetConverter(type);
        return converter.IsValid(value);
    }

    [ModdedCommand("help")]
    public static void displayUsages()
    {
        if (moddedCommandsMethods.Length == 0)
        {
            Plugin.Log.LogInfo("No commands active");
            return;
        }

        var usages = moddedCommandsMethods
            .Select(mi => (ModdedCommand)mi.GetCustomAttributes(typeof(ModdedCommand), true)[0]).ToList()
            .OrderBy(u => u.name)
            .Aggregate("Command usages:",
                (current, u) => current + "\n\t" + u.name + (u.usage == "" ? "" : " ") + u.usage +
                                (u.disabled ? " (disabled)" : ""));
        Plugin.Log.LogInfo(usages);
    }

    private static bool sectorValid(int sector)
    {
        if (sector is >= 1 and <= 6) return true;
        Plugin.Log.LogError("Invalid sector number, must be 1-6");
        return false;
    }

    [ModdedCommand("addhullintegrity", "{amount}")]
    public static void AddHullIntegrity(int amount)
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.AddHullIntegrity(amount);
    }

    [ModdedCommand("removehullintegrity", "{amount}")]
    public static void RemoveHullIntegrity(int amount)
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.RemoveHullIntegrity(amount);
    }

    [ModdedCommand("addnonworkers", "{amount} {sector}")]
    public static bool AddNonWorkers(int amount, int sector)
    {
        if (!sectorValid(sector)) return false;
        BulwarkStudios.Stanford.Common.ConsoleCommands.AddNonWorkers(amount, sector);
        return true;
    }

    [ModdedCommand("addworkers", "{amount} {sector}")]
    public static bool AddWorkers(int amount, int sector)
    {
        if (!sectorValid(sector)) return false;
        BulwarkStudios.Stanford.Common.ConsoleCommands.AddWorkers(amount, sector);
        return true;
    }

    [ModdedCommand("addtrust", "{amount} [in %]")]
    public static void AddTrust(float amount)
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.AddTrust(amount);
    }

    [ModdedCommand("removetrust", "{amount} [in %]")]
    public static void RemoveTrust(float amount)
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.RemoveTrust(amount);
    }

    [ModdedCommand("changechapter", "{chapter}")]
    public static bool ChangeChapter(int chapter)
    {
        if (chapter is < 0 or > 5)
        {
            Plugin.Log.LogError("Invalid chapter, must be 0-5");
            return false;
        }

        BulwarkStudios.Stanford.Common.ConsoleCommands.ChangeChapter(chapter);
        return true;
    }

    [ModdedCommand("finishresearch")]
    public static void FinishCurrentTechResearch()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.FinishCurrentTechResearch();
    }

    [ModdedCommand("gameoverhull")]
    public static void GameOverHull()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.GameOverHull();
    }

    [ModdedCommand("gameoverremus")]
    public static void GameOverRemus()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.GameOverRemus();
    }

    [ModdedCommand("gameoverromulus")]
    public static void GameOverRomulus()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.GameOverRomulus();
    }

    [ModdedCommand("gameovertrust")]
    public static void GameOverTrust()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.GameOverTrust();
    }

    [ModdedCommand("killcitizenbyinjury", "{amount}", true)]
    public static void KillCitizenByInjury(int amount)
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.KillCitizenByInjury(amount);
    }

    [ModdedCommand("killcitizenbystarving", "{amount}", true)]
    public static void KillCitizenByStarving(int amount)
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.KillCitizenByStarving(amount);
    }

    [ModdedCommand("revealasteroid", "{id}")]
    public static void RevealAsteroid(int id)
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.RevealAsteroid(id);
    }

    [ModdedCommand("revealsolarsystem")]
    public static void RevealSolarSystemAll()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.RevealSolarSystemAll();
    }

    [ModdedCommand("sethullintegrity", "{amount} [in %]")]
    public static void SetHullIntegrity(int amount)
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.SetHullIntegrity(amount);
    }

    [ModdedCommand("setscience", "{amount}")]
    public static void SetScience(int amount)
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.SetScience(amount);
    }

    [ModdedCommand("settrust", "{amount} [in %]")]
    public static void SetTrust(float amount)
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.SetTrust(amount);
    }

    [ModdedCommand("togglebuildingcheat")]
    public static void ToggleBuildBuildingCheat()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.ToggleBuildBuildingCheat();
    }

    [ModdedCommand("togglestockpilecheat")]
    public static void ToggleStockpileCheat()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.ToggleStockpileCheat();
    }

    [ModdedCommand("toggleweathers")]
    public static void ToggleWeathers()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.ToggleWeathers();
    }

    [ModdedCommand("unlockalltechnology")]
    public static void UnlockAllTechnology()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.UnlockAllTechnology();
    }

    [ModdedCommand("unlocktechnology", "{name}")]
    public static void UnlockTechnology(string tech)
    {
        //foreach (var t in GameObject.Find("GameData/TechList").GetComponent<TechnologyList>().GetDataByReflection().ToArray()) Plugin.Log.LogInfo(t.GetName());
        BulwarkStudios.Stanford.Common.ConsoleCommands.UnlockTechnology(tech);
    }

    [ModdedCommand("unlocktechnologylist", "[shows all available technologies]")]
    public static bool UnlockTechnologyList()
    {
        var txt = Plugin.getStringFromEmbedTxtFile("TechnologyNames.txt");
        if (txt != null) Plugin.Log.LogInfo(txt.Replace("\n", "\n\t"));
        return false;
    }

    [ModdedCommand("setbuildinglightinghigh")]
    public static void SetHighBuildingLightOption()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.SetHighBuildingLightOption();
    }

    [ModdedCommand("setbuildinglightinglow")]
    public static void SetLowBuildingLightOption()
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.SetLowBuildingLightOption();
    }

    [ModdedCommand("togglebuildingsshow", "{index} [0-45, -1 to hide all, -2 to show all]")]
    public static void ToggleBuildingsShow(int index)
    {
        BulwarkStudios.Stanford.Common.ConsoleCommands.ToggleBuildingsShow(index);
    }

    [ModdedCommand("togglecitizenshow", "{sector}")]
    public static bool ToggleCitizenShow(int sector)
    {
        if (!sectorValid(sector)) return true;
        BulwarkStudios.Stanford.Common.ConsoleCommands.ToggleCitizenShow(sector);
        return false;
    }

    [ModdedCommand("togglesectorbuildingsshow", "{sector}")]
    public static bool ToggleSectorBuildingsShow(int sector)
    {
        if (!sectorValid(sector)) return true;
        BulwarkStudios.Stanford.Common.ConsoleCommands.ToggleSectorBuildingsShow(sector);
        return false;
    }
}