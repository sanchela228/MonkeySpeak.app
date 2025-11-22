using Platforms.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Platforms;

public static class PlatformServiceFactory
{
    private static readonly Dictionary<string, IPlatformService> _services = new();
    
    public static void Register<T> (T service) where T : IPlatformService => 
        _services[$"{typeof(T).FullName}|{service.ServicePlatform()}"] = service;
    
    public static void Register<T> (List<T> services) where T : IPlatformService
    {
        foreach (var service in services)
            _services[$"{typeof(T).FullName}|{service.ServicePlatform()}"] = service;
    }
    
    public static T GetService<T>( Platforms platform ) where T : IPlatformService
    {
        var key = $"{typeof(T).FullName}|{platform}";

        if (!_services.TryGetValue(key, out var service) || service is null)
        {
            var anyImpl = _services.Values.OfType<T>().FirstOrDefault();
            var isCritical = anyImpl?.IsCritical() ?? true;

            if (isCritical)
                throw new KeyNotFoundException($"Critical service is not registered: {typeof(T).FullName} for platform {platform}");

            return default;
        }

        if (service is T typed)
            return typed;

        if (service.IsCritical())
            throw new InvalidCastException($"Registered service type mismatch for key '{key}'. Actual: {service.GetType().FullName}, Expected: {typeof(T).FullName}");

        return default;
    }
}