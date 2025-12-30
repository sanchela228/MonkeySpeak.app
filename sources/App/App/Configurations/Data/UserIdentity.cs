using System.Xml.Serialization;
using App.Configurations;

namespace App.Configurations.Data;

[Serializable]
public class UserIdentityData
{
    public string Username { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string KeyFingerprint { get; set; } = string.Empty;
}

public class UserIdentity : XmlConfigBase<UserIdentity>
{
    protected override string RootDirectory => Path.Combine(Context.DataDirectory, "Configurations");
    
    [XmlIgnore]
    private string _username = string.Empty;
    [XmlIgnore]
    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            Save();
        }
    }
    
    [XmlElement("Username")]
    public string UsernameString => Username;
    
    private string _userId = string.Empty;
    [XmlIgnore]
    public string UserId
    {
        get => _userId;
        set
        {
            _userId = value;
            Save();
        }
    }
    
    private string _keyFingerprint = string.Empty;
    [XmlIgnore]
    public string KeyFingerprint
    {
        get => _keyFingerprint;
        set
        {
            _keyFingerprint = value;
            Save();
        }
    }
    
    [XmlElement("UserId")]
    public string UserIdString => UserId;
    
    [XmlElement("KeyFingerprint")]
    public string KeyFingerprintString => KeyFingerprint;
    
    public override string FileName => "UserIdentity.xml";
    
    [XmlIgnore]
    public bool IsRegistered => !string.IsNullOrEmpty(UserId);

    public override void LoadOrDefault()
    {
        try
        {
            if (!Exists())
            {
                ApplyDefaults();
                return;
            }

            var serializer = new XmlSerializer(typeof(UserIdentityData));
            using var reader = new StreamReader(FilePath);
            var loaded = (UserIdentityData)serializer.Deserialize(reader)!;

            _userId = loaded.UserId;
            _keyFingerprint = loaded.KeyFingerprint;
            _username = string.IsNullOrWhiteSpace(loaded.Username) ? GenerateRandomUsername() : loaded.Username;
        }
        catch (Exception ex)
        {
            System.Services.Logger.Write(System.Services.Logger.Type.Error, "Failed to load UserIdentity", ex);
            ApplyDefaults();
        }
    }

    public override void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var serializer = new XmlSerializer(typeof(UserIdentityData));
            var data = new UserIdentityData
            {
                UserId = UserId,
                KeyFingerprint = KeyFingerprint,
                Username = Username
            };

            using var writer = new StreamWriter(FilePath);
            serializer.Serialize(writer, data);
        }
        catch (Exception ex)
        {
            System.Services.Logger.Write(System.Services.Logger.Type.Error, "Failed to save UserIdentity", ex);
        }
    }

    protected override void ApplyDefaults()
    {
        _userId = string.Empty;
        _username = GenerateRandomUsername();
        _keyFingerprint = string.Empty;
    }

    protected override void CopyFrom(UserIdentity other)
    {
        _userId = other._userId;
        _username = other._username;
        _keyFingerprint = other._keyFingerprint;
    }
    
    private static string GenerateRandomUsername()
    {
        var random = new Random();
        return $"user{random.Next(1000, 9999)}";
    }
}
