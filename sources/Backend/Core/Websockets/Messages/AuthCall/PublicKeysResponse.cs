namespace Core.Websockets.Messages.AuthCall;

public class PublicKeysResponse : IMessage
{
    public string UserId { get; set; }
    public string PublicKeyEd25519Base64 { get; set; }
    public string PublicKeyX25519Base64 { get; set; }
    public string Fingerprint { get; set; }
    public string Value { get; set; }
}
