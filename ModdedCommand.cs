using System;

namespace ConsoleCommands;

[AttributeUsage(AttributeTargets.Method)]
public class ModdedCommand(string name, string usage = "", bool disabled = false) : Attribute
{
    public readonly bool disabled = disabled;
    public readonly string name = name.ToLower();
    public readonly string usage = usage.ToLower();
}