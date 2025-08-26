namespace App.System.Models.Websocket;

public class Context
{
    public string type;
    public IMessage Message;
    
    public static Context Create(IMessage message) => new()
    {
        type = message.GetType().ToString(), 
        Message = message
    };
}