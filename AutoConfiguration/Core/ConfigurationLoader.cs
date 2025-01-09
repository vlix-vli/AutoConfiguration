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
        RegisterTypeConverters(types);
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(settingsPath, optional: false, reloadOnChange: true)
            .Build();
        _serviceCollection = [];
        foreach (var type in types)
        {
            var attributes = type.GetCustomAttributes(typeof(ConfigurationAttribute), false);
            if (attributes.Length == 0) continue;
            var configurationAttribute = (ConfigurationAttribute)attributes.First();
            var configurationSection = configuration.GetSection(configurationAttribute.Key);
            // CheckRequiredParameters(type, configurationAttribute.Key, configurationSection);
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

    private static void RegisterTypeConverters(Type[] types)
    {
        foreach (var type in types)
        {
            var attributes = type.GetCustomAttributes(typeof(TypeConverterAttribute), false);
            if (attributes.Length == 0) continue;
            TypeDescriptor.AddAttributes(typeof(IPAddress), new TypeConverterAttribute(type));
        }
    }

    private static void CheckRequiredProperties()
    {
        var serviceDescriptors = _serviceCollection?.Where(sd => sd.ServiceType != typeof(IServiceProvider));
        if (serviceDescriptors == null) return;
        foreach (var descriptor in serviceDescriptors)
        {
            var serviceInstance = _serviceProvider?.GetService(descriptor.ServiceType);
            var type = serviceInstance?.GetType();
            if (type == null) continue;
            foreach (var property in type.GetProperties())
            {
                var attributes = property.GetCustomAttributes(typeof(RequiredAttribute), false);
                if (attributes.Length == 0) continue;
                var value = property.GetValue(serviceInstance);
                if (value == null)
                {
                    throw new ArgumentNullException(
                        $"Configuration '{type.FullName}' is missing a required valid '{property.Name}' property.");
                }
            }
        }
    }
}