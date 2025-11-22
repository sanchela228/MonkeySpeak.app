using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TypeSystem = System.Type;

namespace App.System.Models.Websocket;

public class Context
{
    public static string RootNamespace = "App.System.Models.Websocket";
    private const string Anchor = ".Models.Websocket.";
    
    public string Type { get; set; }
    
    public Guid ApplicationId { get; set; }
    public JsonElement Message { get; set; }
    
    public static Context Create(IMessage message) => new()
    {
        Type = GetRelativeTypeName(message.GetType()),
        Message = JsonSerializer.SerializeToElement(message, message.GetType())
    };
    
    public IMessage ToMessage()
    {
        var fullName = $"{RootNamespace}.{Type}";
        var resolvedType = ResolveType(fullName)
                          ?? throw new InvalidOperationException($"Type {fullName} not found");
        return (IMessage) JsonSerializer.Deserialize(Message.GetRawText(), resolvedType)!;
    }

    private static string GetRelativeTypeName(TypeSystem t)
    {
        var fullName = t.FullName ?? t.Name;
        var idx = fullName.IndexOf(Anchor, StringComparison.Ordinal);
        if (idx >= 0)
        {
            return fullName.Substring(idx + Anchor.Length);
        }
        
        return t.Name;
    }

    private static TypeSystem? ResolveType(string fullName)
    {
        var t = TypeSystem.GetType(fullName);
        if (t != null) return t;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                t = asm.GetType(fullName, throwOnError: false, ignoreCase: false);
                if (t != null) return t;
            }
            catch
            {
            }
        }
        
        return null;
    }
}