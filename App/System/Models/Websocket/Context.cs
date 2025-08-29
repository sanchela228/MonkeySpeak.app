using System.Text.Json;

namespace App.System.Models.Websocket;

public class Context
{
    public string Type { get; set; }
    public JsonElement Message { get; set; }
    
    public static Context Create(IMessage message) => new()
    {
        Type = message.GetType().AssemblyQualifiedName,
        Message = JsonSerializer.SerializeToElement(message)
    };
    
    public IMessage ToMessage()
    {
        var type = Type.GetType() ?? throw new InvalidOperationException($"Type {Type} not found");
        return (IMessage) JsonSerializer.Deserialize(Message.GetRawText(), type)!;
    }
}