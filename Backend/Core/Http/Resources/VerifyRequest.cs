namespace Core.Http.Resources;

public class VerifyRequest
{
    public string ApplicationId { get; set; }
    public string Code { get; set; }
}