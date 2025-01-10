using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using AutoConfiguration.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoConfiguration.Core;

/// <summary>
/// Auto Read json file and Create Instance with properties
/// </summary>
public abstract class ConfigurationLoader
{
    private static bool _loaded = false;

    private static readonly List<Type> ConfigurationTypes = [];

    private static readonly List<Type> TypeConverterTypes = [];

    private static ServiceCollection? _serviceCollection;

    private static ServiceProvider? _serviceProvider;

    /// <summary>
    /// Load json file
    /// </summary>
    /// <param name="settingsPath"></param>
    /// <param name="types">type to scan</param>
    public static void Load(string settingsPath, Type[] types)
    {
        if (_loaded)
        {
            throw new InvalidOperationException("ConfigurationLoader is already loaded");
        }

        Register(types);
        foreach (var typeConverterType in TypeConverterTypes)
        {
            TypeDescriptor.AddAttributes(typeof(IPAddress), new TypeConverterAttribute(typeConverterType));
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(settingsPath, optional: false, reloadOnChange: true)
            .Build();
        _serviceCollection = [];
        foreach (var type in ConfigurationTypes)
        {
            var configurationAttribute = type.GetCustomAttribute<ConfigurationAttribute>();
            var configurationSection = configuration.GetSection(configurationAttribute!.Key);
            var configureMethod = typeof(OptionsConfigurationServiceCollectionExtensions)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(m =>
                    m.Name == nameof(OptionsConfigurationServiceCollectionExtensions.Configure)
                    && m.GetParameters().Length == 2)?.MakeGenericMethod(type);
            configureMethod?.Invoke(null, [_serviceCollection, configurationSection]);
        }

        _serviceProvider = _serviceCollection.BuildServiceProvider();
        CheckRequiredProperties();
        _loaded = true;
    }

    private static void Register(Type[] types)
    {
        foreach (var type in types)
        {
            var configurationAttribute = type.GetCustomAttribute<ConfigurationAttribute>();
            if (configurationAttribute != null) ConfigurationTypes.Add(type);
            var typeConverterAttribute = type.GetCustomAttribute<TypeConverterAttribute>();
            if (typeConverterAttribute != null) TypeConverterTypes.Add(type);
        }
    }

    /// <summary>
    /// Get Configuration Instance
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public static T? GetConfiguration<T>() where T : class =>
        (_serviceProvider ?? throw new NullReferenceException(
            "Configuration has not been loaded. Please call the Load method with a valid settings path before attempting to retrieve services."))
        .GetService<IOptions<T>>()?.Value;

    private static void CheckRequiredProperties()
    {
        foreach (var configurationType in ConfigurationTypes)
        {
            var optionsWrapperType = typeof(IOptions<>).MakeGenericType(configurationType);
            // 根据类型获取服务实例
            var optionsInstance = _serviceProvider?.GetService(optionsWrapperType);
            var options = optionsInstance as IOptions<object>;
            foreach (var property in configurationType.GetProperties())
            {
                var requiredAttribute = property.GetCustomAttribute<RequiredAttribute>();
                if (requiredAttribute == null) continue;
                var value = property.GetValue(options?.Value);
                if (value == null)
                {
                    throw new ArgumentNullException(
                        $"Configuration '{configurationType.FullName}' is missing a required valid '{property.Name}' property.");
                }
            }
        }
    }
}