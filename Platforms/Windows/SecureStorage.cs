using System.Security.Cryptography;
using System.Text;
using App;

namespace Platforms.Windows;

public class SecureStorage
{
    private static readonly byte[] s_entropy = Encoding.UTF8.GetBytes(
        Context.Instance.ContextData.ApplicationId + ":" + Context.Instance.ContextData.MachineId
    );
    
    private static readonly string _directoryPath = Context.Instance.DataDirectory + "/"; 

    public static void Save(string key, string value)
    {
        byte[] data = Encoding.UTF8.GetBytes(value);
        byte[] encryptedData = ProtectedData.Protect(data, s_entropy, DataProtectionScope.CurrentUser);
        
        File.WriteAllBytes( _directoryPath + $"{key}.secure", encryptedData);
    }

    public static string Load(string key)
    {
        string filePath = _directoryPath + $"{key}.secure";
        
        if (!File.Exists(filePath)) 
            return null;

        byte[] encryptedData = File.ReadAllBytes(filePath);
        byte[] decryptedData = ProtectedData.Unprotect(encryptedData, s_entropy, DataProtectionScope.CurrentUser);
        
        return Encoding.UTF8.GetString(decryptedData);
    }

    public static void Delete(string key)
    {
        string filePath = _directoryPath + $"{key}.secure";
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}