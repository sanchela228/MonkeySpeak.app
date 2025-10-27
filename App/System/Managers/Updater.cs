using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Serialization;
using App.Configurations.Interfaces;
using App.Configurations.Realisation;
using App.System.Services;

namespace App.System.Managers;

public class Updater(INetworkConfig networkConfig)
{
    public readonly INetworkConfig NetworkConfig = networkConfig;
    
    private Manifest _manifest;
    private const string ManifestFileName = "Manifest.xml";

    public async Task<bool> CheckUpdate()
    {
        _manifest = await GetUpdateInfoFromStreamAsync(NetworkConfig.DomainUrl() + "/" + ManifestFileName);
        
        Logger.Write($"[Updater] CheckUpdate: {_manifest}");
        
        if (_manifest is null)
            return false;

        if (_manifest.Version <= Context.AppConfig.Version)
            return false;
        
        return true;
    }

    public async Task StartProcessUpdate()
    {
        Context.Network.DownloadUpdateState = new DownloadUpdateState()
        {
            IsDownloading = true,
            StatusMessage = "Downloading update"
        };

        var dataDirectory = Context.DataDirectory;
        var downloadsPath =  Path.Combine(dataDirectory, "Downloads");
        var actualUpdateZipPath = Path.Combine(downloadsPath, "ActualUpdate.zip");

        if (!Directory.Exists(downloadsPath))
        {
            try
            {
                Directory.CreateDirectory(downloadsPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Write($"Cannot create directory: {ex.Message}", Logger.Type.Error);
            }
        }

        var downloadPath = NetworkConfig.DomainUrl() + "/" + GetActualDownloadUrl();
        var updateFolderPath = Path.Combine(downloadsPath, "ActualUpdate");
        
        await DownloadFileFromServer(downloadPath, actualUpdateZipPath, Context.Network.DownloadUpdateState);
        
        // TODO: ADD BACKUP AND UNPACK THEM IF UPDATE FAILED
        ExtractFile(actualUpdateZipPath, updateFolderPath);
        DeleteFile(actualUpdateZipPath);
        
        if (HasError) RestoreFromBackup();
        else
        {
            ApplyUpdate(updateFolderPath, AppDomain.CurrentDomain.BaseDirectory);
            
            Context.Network.DownloadUpdateState.IsDownloading = false;
            Context.Network.DownloadUpdateState.StatusMessage = "Update applied";
        }
    }

    private async Task DownloadFileFromServer(string path, string output, DownloadUpdateState updaterState)
    {
        try
        {
            using var headResponse = await new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, path));
            headResponse.EnsureSuccessStatusCode();
            updaterState.TotalBytes = headResponse.Content.Headers.ContentLength ?? 0;

            using var response = await new HttpClient().GetAsync(path, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            if (updaterState.TotalBytes == 0)
                updaterState.TotalBytes = response.Content.Headers.ContentLength ?? 0;

            await using (var contentStream = await response.Content.ReadAsStreamAsync())
            {
                await using (var fileStream = new FileStream(output , FileMode.Create, FileAccess.Write))
                {
                    var buffer = new byte[32768]; //32kb
                    int bytesRead;
            
                    while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        updaterState.DownloadedBytes += bytesRead;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, $"Error download file from server", ex);
        }
    }
    
    private void ExtractFile(string path, string output)
    {
        try
        {
            if (!File.Exists(path))
                throw new Exception("Not found zip file");

            if (!Directory.Exists(output)) Directory.CreateDirectory(output);

            ZipFile.ExtractToDirectory(path, output, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            HasError = true;
            Logger.Write(Logger.Type.Error, $"Extraction failed: {path}", ex);
        }
    }

    private void DeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Can't delete file", ex);
            throw;
        }
    }

    private static void ApplyUpdate(string updateFolderPath, string targetFolderPath, string executableName = "MonkeySpeak.exe")
    {
        try
        {
            var batFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.bat");
            
            if (!File.Exists(batFilePath))
            {
                throw new FileNotFoundException("update.bat file not found");
            }

            var processId = Process.GetCurrentProcess().Id;
            var arguments = $"\"{targetFolderPath}\" \"{updateFolderPath}\" \"{executableName}\" {processId}";

            var startInfo = new ProcessStartInfo
            {
                FileName = batFilePath,
                Arguments = arguments,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start update: {ex.Message}");
        }
    }

    public void RestoreFromBackup()
    {
        
    }
    
    public bool HasError;

    private string GetActualDownloadUrl() => $"download/versions/{_manifest.Version}/win64/source/source_win64_{_manifest.Version}.zip";
    
    public async Task<Manifest> GetUpdateInfoFromStreamAsync(string xmlUrl)
    {
        try
        {
            using (HttpResponseMessage response = await new HttpClient().GetAsync(xmlUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
            
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Manifest));
                    return (Manifest) serializer.Deserialize(stream);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Write(Logger.Type.Error, "Can't read manifest.xml, from server", ex);
            return null;
        }
    }
    

    [XmlRoot("Manifest")]
    public class Manifest()
    {
        public int Version;
    }
    
    public class DownloadUpdateState
    {
        public bool IsDownloading;
        public long TotalBytes;
        public long DownloadedBytes;
        public string StatusMessage;
        public float Progress => TotalBytes > 0 ? (float) DownloadedBytes / TotalBytes : 0;
    }
}