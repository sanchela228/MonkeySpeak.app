using System.Xml.Serialization;
using App.Configurations.Data;
using App.Configurations.Roots;
using App.System.Calls.Application.Facade;
using App.System.Services;
using App.System.Utils;
using Engine.Helpers;
using Engine.Managers;
using Platforms;
using Platforms.Interfaces;
using Platforms.Windows;
using Raylib_cs;

namespace App;

public static class Context
{
    public static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonkeySpeak"
    );
    
    public static readonly string LogsDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonkeySpeak\\Logs"
    );
    
    public static readonly string DownloadDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MonkeySpeak\\Downloads"
    );
    
    public const string NameAuthorizationNetworkTokenFile = "AuthNetworkToken";

    public static Guid CurrentSessionToken { get; } = Guid.NewGuid();
    public static Platforms.Platforms CurrentPlatform { get; private set; } = Platforms.Platforms.Windows;

    public static AppConfig AppConfig { get; private set; }
    public static ContextData ContextData { get; private set; }
    public static UserSettings UserSettings { get; private set; }
    public static System.Modules.Network Network { get; private set; }

    public static CallFacade CallFacade { get; private set; }
    
    public static ISecureStorage SecureStorage { get; private set; }
    public static IUser SystemUser { get; private set; }

    public static void SetUp()
    {
        Logger.Write(Logger.Type.Info, "------- Starting SetUp context application ---------");
        
        if (!DataDirectoryInitialized())
            InitializeDataDirectory();
        
        ContextData = new ContextData();
        ContextData.LoadOrDefault();
        
        PlatformServiceFactory.Register( new List<ISecureStorage>
        {
            new Platforms.Windows.SecureStorage()
        });
        
        PlatformServiceFactory.Register( new List<IUser>
        {
            new Platforms.Windows.User()
        });
            
        SecureStorage = PlatformServiceFactory.GetService<ISecureStorage>(CurrentPlatform);
        SystemUser = PlatformServiceFactory.GetService<IUser>(CurrentPlatform);
        
        System.Services.Language.Load(ContextData.LanguageSelected);
        
        // DEMO COPY
        if (!File.Exists(Path.Combine(DataDirectory, "ffmpeg.exe")) && File.Exists(Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe")))
            File.Copy(Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"), Path.Combine(DataDirectory, "ffmpeg.exe"));
        
        if (!File.Exists(Path.Combine(DataDirectory, "ffprobe.exe")) && File.Exists(Path.Combine(AppContext.BaseDirectory, "ffprobe.exe")))
            File.Copy(Path.Combine(AppContext.BaseDirectory, "ffprobe.exe"), Path.Combine(DataDirectory, "ffprobe.exe"));
        
        Cache.Init(Path.Combine(DataDirectory, "Cache"));
        VideoReader.СhangeFFmpegDirPath(DataDirectory);
        
        if (!Cache.ExistsFastPermanent($"video:lz4:sticker1.webm"))
            VideoReader.PrepareCache("sticker1.webm");
        
        if (!Cache.ExistsFastPermanent($"video:lz4:sticker2.webm"))
            VideoReader.PrepareCache("sticker2.webm");
        
        if (!Cache.ExistsFastPermanent($"video:lz4:sticker3.webm"))
            VideoReader.PrepareCache("sticker3.webm");
        
        if (!Cache.ExistsFastPermanent($"video:lz4:sticker4.webm"))
            VideoReader.PrepareCache("sticker4.webm");
        
        try
        {
            _devicePrivateKey = SecureStorage.Load("device_private_key");
            
            if (string.IsNullOrEmpty(_devicePrivateKey))
            {
                (string publicKey, string privateKey) = DeviceCrypto.GenerateKeyPair();
                _devicePrivateKey = privateKey;
                
                SecureStorage.Save("device_private_key", _devicePrivateKey);
                SecureStorage.Save("device_public_key", publicKey);
            }
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to ensure device keys in SecureStorage", ex);
        }

        var netConfig = new NetworkConfig();
        netConfig.LoadOrDefault();
        Network = new System.Modules.Network(netConfig);

        AppConfig = new AppConfig();
        AppConfig.LoadOrDefault();

        UserSettings = new UserSettings();
        UserSettings.LoadOrDefault();

        // DEMO TEST
        Network.OnServerConnected += () =>
        {
            CallFacade = new CallFacade(Network.Config, Network.WebSocketClient);
        };
    }
    
    public static void RecreateCallFacade()
    {
        if (CallFacade != null)
            CallFacade.Clear();
        
        if (Network?.WebSocketClient != null)
            CallFacade = new CallFacade(Network.Config, Network.WebSocketClient);
    }

    public static bool DataDirectoryInitialized() => Directory.Exists(DataDirectory);

    private static string _devicePrivateKey;
    public static void InitializeDataDirectory()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(LogsDataDirectory);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Failed to initialize data directory", ex);
            throw new InvalidOperationException("Failed to initialize data directory", ex);
        }
    }
}