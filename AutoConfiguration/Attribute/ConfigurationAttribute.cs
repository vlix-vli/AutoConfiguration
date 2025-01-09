namespace AutoConfiguration.Attribute;

using System;

/// <summary>
/// Mark you Configuration File and It will be Auto Config
/// </summary>
/// <param name="key"></param>
[AttributeUsage(AttributeTargets.Class)]
public class ConfigurationAttribute(string key) : Attribute
{
    /// <summary>
    /// The key in .json file
    /// </summary>
    public string Key { get; } = key;
}