using Platforms.Interfaces;

namespace Platforms;

public static class PlatformServiceFactory
{
    private static readonly Dictionary<string, IPlatformService> _services = new();
    
    public static void Register<T> (T service) where T : IPlatformService => 
        _services[typeof(T).Assembly + service.ServicePlatform().ToString()] = service;
    
    public static void Register<T> (List<T> services) where T : IPlatformService
    {
        foreach (var service in services)
            _services[typeof(T).Assembly + service.ServicePlatform().ToString()] = service;
    }
    
    public static T GetService<T>( Platforms platform ) where T : IPlatformService
    {
        var s = (T)_services[typeof(T).Assembly + platform.ToString()];
        
        if (s is null && s.IsCritical())
            throw new Exception("Critical service is not registered");
        
        return s;
    }
}