namespace Core.Websockets.Messages.AuthCall;

public class RegisterKey : IMessage
{
    public string Username { get; set; }
    public string PublicKeyEd25519Base64 { get; set; }
    public string PublicKeyX25519Base64 { get; set; }
    public string ProofSignature { get; set; }
    public string Nonce { get; set; }
    public string Value { get; set; }
}
