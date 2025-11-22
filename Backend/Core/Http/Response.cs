namespace MonkeySpeak.Backend.Core.Http;

public class Response(bool success, string message, object data = null)
{
    public bool Success { get; set; } = success;
    public string Message { get; set; } = message;
    public object Data { get; set; } = data;
}