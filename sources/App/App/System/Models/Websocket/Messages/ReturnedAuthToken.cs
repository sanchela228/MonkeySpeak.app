namespace App.System.Models.Websocket.Messages;

public class ReturnedAuthToken : IMessage
{
    public string Value { get; set; } = "1234";
}