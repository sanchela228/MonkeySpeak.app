namespace Platforms.Windows;

using System.Security.Cryptography;
using System.Text;

public static class DeviceCrypto
{
    public static (string publicKey, string privateKey) GenerateKeyPair()
    {
        using (var rsa = RSA.Create(2048))
        {
            string publicKey = rsa.ToXmlString(false);
            string privateKey = rsa.ToXmlString(true);
            
            return (publicKey, privateKey);
        }
    }

    public static string SignData(string data, string privateKeyXml)
    {
        using (var rsa = RSA.Create())
        {
            rsa.FromXmlString(privateKeyXml);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }
    }

    public static bool VerifyData(string data, string signature, string publicKeyXml)
    {
        using (var rsa = RSA.Create())
        {
            rsa.FromXmlString(publicKeyXml);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] signatureBytes = Convert.FromBase64String(signature);
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }
}