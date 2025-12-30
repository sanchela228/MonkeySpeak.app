using System.Text;
using App.Configurations.Data;
using App.System.Crypto;
using App.System.Models.Websocket.Messages.AuthCall;
using App.System.Modules;
using App.System.Services;

namespace App.System.Auth;

public class RegistrationManager
{
    private readonly WebSocketClient _webSocketClient;
    private readonly UserIdentity _userIdentity;
    private readonly UserSettings _userSettings;

    public event Action<string, string>? OnRegistrationSuccess;
    public event Action<string>? OnRegistrationError;
    public event Action<string, string>? OnAuthenticationSuccess;

    public RegistrationManager(WebSocketClient webSocketClient, UserIdentity userIdentity, UserSettings userSettings)
    {
        _webSocketClient = webSocketClient;
        _userIdentity = userIdentity;
        _userSettings = userSettings;
        
        _webSocketClient.MessageDispatcher.On<KeyRegistered>(HandleKeyRegistered);
        _webSocketClient.MessageDispatcher.On<ErrorRegistration>(HandleErrorRegistration);
        
        _webSocketClient.MessageDispatcher.On<AuthChallenge>(HandleAuthChallenge);
        _webSocketClient.MessageDispatcher.On<Authenticated>(HandleAuthenticated);
    }
    
    public bool IsRegistered => _userIdentity.IsRegistered;
    
    public async Task RegisterAsync()
    {
        try
        {
            var (publicKeyEd25519, publicKeyX25519) = UserCrypto.EnsureKeys();

            var username = _userIdentity.Username;
            if (string.IsNullOrWhiteSpace(username))
            {
                Logger.Write(Logger.Type.Error, "Username is empty, cannot register");
                OnRegistrationError?.Invoke("EMPTY_USERNAME");
                return;
            }

            var nonce = Guid.NewGuid().ToString();
            var nonceBytes = Encoding.UTF8.GetBytes(nonce);

            var signature = UserCrypto.SignData(nonceBytes);

            var registerMessage = new RegisterKey
            {
                Username = username,
                PublicKeyEd25519Base64 = Convert.ToBase64String(publicKeyEd25519),
                PublicKeyX25519Base64 = Convert.ToBase64String(publicKeyX25519),
                ProofSignature = Convert.ToBase64String(signature),
                Nonce = nonce
            };

            await _webSocketClient.SendAsync(registerMessage);

            Logger.Write(Logger.Type.Info, $"Sent RegisterKey for user: {username}");
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to register user", ex);
            OnRegistrationError?.Invoke("REGISTRATION_FAILED");
        }
    }
    
    private void HandleKeyRegistered(KeyRegistered message)
    {
        try
        {
            Logger.Write(Logger.Type.Info, $"Registration successful! UserId: {message.UserId}, Fingerprint: {message.Fingerprint}");

            _userIdentity.UserId = message.UserId;
            _userIdentity.KeyFingerprint = message.Fingerprint;

            OnRegistrationSuccess?.Invoke(message.UserId, message.Fingerprint);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to handle KeyRegistered", ex);
        }
    }
    
    private void HandleErrorRegistration(ErrorRegistration message)
    {
        Logger.Write(Logger.Type.Error, $"Registration error: {message.ErrorCode} - {message.Value}");
        OnRegistrationError?.Invoke(message.ErrorCode);
    }
    
    public string GetUserId() => _userIdentity.UserId;
    
    public string GetFingerprint() => _userIdentity.KeyFingerprint;
    
    public async Task AuthenticateAsync()
    {
        try
        {
            if (!_userIdentity.IsRegistered)
            {
                Logger.Write(Logger.Type.Error, "Cannot authenticate: user not registered");
                OnRegistrationError?.Invoke("NOT_REGISTERED");
                return;
            }

            var requestAuth = new RequestAuth
            {
                UserId = _userIdentity.UserId,
                Value = "Request authentication"
            };

            await _webSocketClient.SendAsync(requestAuth);
            Logger.Write(Logger.Type.Info, $"Requested authentication for user: {_userIdentity.UserId}");
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to request authentication", ex);
            OnRegistrationError?.Invoke("AUTH_REQUEST_FAILED");
        }
    }
    
    private async void HandleAuthChallenge(AuthChallenge message)
    {
        try
        {
            Logger.Write(Logger.Type.Info, $"Received auth challenge, nonce: {message.Nonce}");

            var nonceBytes = Encoding.UTF8.GetBytes(message.Nonce);
            var signature = UserCrypto.SignData(nonceBytes);

            var authenticate = new Authenticate
            {
                UserId = _userIdentity.UserId,
                Signature = Convert.ToBase64String(signature),
                Value = "Authentication response"
            };

            await _webSocketClient.SendAsync(authenticate);
            Logger.Write(Logger.Type.Info, "Sent authentication response");
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to handle auth challenge", ex);
            OnRegistrationError?.Invoke("AUTH_CHALLENGE_FAILED");
        }
    }
    
    private void HandleAuthenticated(Authenticated message)
    {
        try
        {
            Logger.Write(Logger.Type.Info, $"Authentication successful! UserId: {message.UserId}, Username: {message.Username}");
            OnAuthenticationSuccess?.Invoke(message.UserId, message.Username);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to handle Authenticated", ex);
        }
    }
}
