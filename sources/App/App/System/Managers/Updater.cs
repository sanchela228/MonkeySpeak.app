using System.Diagnostics;
using System.IO.Compression;
using System.Security.Principal;
using System.Xml.Serialization;
using App.Configurations.Roots;
using App.System.Services;

namespace App.System.Managers;

public class Updater(NetworkConfig networkConfig)
{
    public readonly NetworkConfig NetworkConfig = networkConfig;
    
    private Manifest _manifest;
    private const string ManifestFileName = "Manifest.xml";

    public async Task<bool> CheckUpdate()
    {
        Logger.Write($"[Updater] CheckUpdate: {_manifest}");
        _manifest = await GetUpdateInfoFromStreamAsync(NetworkConfig.DomainUrl() + "/" + ManifestFileName);
        
        Logger.Write($"[Updater] From server: {_manifest.Version}, current: {Context.AppConfig.Version}");
        
        if (_manifest is null)
            return false;

        if (_manifest.Version <= Context.AppConfig.Version)
        {
            Logger.Write("[Updater] Nothing to update");
            return false;
        }
        
        return true;
    }

    public async Task StartProcessUpdate()
    {
        Logger.Write($"[Updater] Update started");
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

        var updateFolderPath = Path.Combine(downloadsPath, "ActualUpdate");
        
        await DownloadFileFromServer(GetActualDownloadUrl(), actualUpdateZipPath, Context.Network.DownloadUpdateState);
        
        // TODO: ADD BACKUP AND UNPACK THEM IF UPDATE FAILED
        if (!HasError)
        {
            ExtractFile(actualUpdateZipPath, updateFolderPath);
            DeleteFile(actualUpdateZipPath);
        }
        
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
            Logger.Write($"[Updater] Start download from server {path}");
            using var headResponse = await new HttpClient().SendAsync(new HttpRequestMessage(HttpMethod.Head, path));
            headResponse.EnsureSuccessStatusCode();
            updaterState.TotalBytes = headResponse.Content.Headers.ContentLength ?? 0;

            using var response = await new HttpClient().GetAsync(path, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            if (updaterState.TotalBytes == 0)
                updaterState.TotalBytes = response.Content.Headers.ContentLength ?? 0;
            
            Logger.Write($"[Updater] From server bytes {updaterState.TotalBytes}");

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
            
            Logger.Write($"[Updater] End download");
        }
        catch (Exception ex)
        {
            HasError = true;
            Logger.Write(Logger.Type.Error, $"Error download file from server", ex);
        }
    }
    
    private void ExtractFile(string path, string output)
    {
        Logger.Write($"[Updater] Start extract: {path}");
        
        try
        {
            if (!File.Exists(path))
                throw new Exception("Not found zip file");

            if (!Directory.Exists(output)) Directory.CreateDirectory(output);

            ZipFile.ExtractToDirectory(path, output, overwriteFiles: true);
            Logger.Write("[Updater] End extract");
        }
        catch (Exception ex)
        {
            HasError = true;
            Logger.Write(Logger.Type.Error, $"Extraction failed: {path}", ex);
        }
    }

    private void DeleteFile(string path)
    {
        Logger.Write("[Updater] Delete zip");
        
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

    private static void ApplyUpdate(string updateFolderPath, string targetFolderPath, string executableName = "App.exe")
    {
        try
        {
            Logger.Write("[Updater] ApplyUpdate start");
            
            var batFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "update.bat");
            
            if (!File.Exists(batFilePath))
            {
                Logger.Write(Logger.Type.Error, "update.bat file not found");
                throw new FileNotFoundException("update.bat file not found");
            }
            
            Logger.Write($"[Updater] BAT file finded: {batFilePath}");
            
            var processId = Process.GetCurrentProcess().Id;
            var arguments = $"\"{targetFolderPath}\" \"{updateFolderPath}\" \"{executableName}\" {processId}";
            
            Logger.Write($"[Updater] BAT process: {processId}, arguments: {arguments}");

            
            bool needElevation = !Context.SystemUser.CanWriteTo(targetFolderPath);
            bool elevated = Context.SystemUser.IsAdministrator();

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{batFilePath}\" {arguments}\"",
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            
            Logger.Write("[Updater] Start process");
            
            if (needElevation && !elevated)
            {
                startInfo.Verb = "runas";
            }
            
            Process.Start(startInfo);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to start update: {ex.Message}");
        }
    }

    public static void RestoreFromBackup()
    {
        Context.Network.DownloadUpdateState.IsDownloading = false;
        Context.Network.DownloadUpdateState.StatusMessage = "Update failed";
    }
    
    public bool HasError;

    public string GetActualDownloadUrl()
    {
        string baseUrl = NetworkConfig.DomainUrl();
        string path = $"{_manifest.PathDownload.Trim('/')}/{_manifest.FileSource.Trim('/')}";
    
        return new Uri( new Uri(baseUrl), path).ToString();
    }
    
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
        public string VersionName;
        public string PathDownload;
        public string FileSource;
        public string FileSetup;
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