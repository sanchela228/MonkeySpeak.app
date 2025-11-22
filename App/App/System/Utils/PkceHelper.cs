using System;
using System.Security.Cryptography;
using System.Text;

namespace App.System.Utils;

public static class PkceHelper
{
    public static string GenerateCodeVerifier(int length = 64)
    {
        if (length < 43 || length > 128)
            throw new ArgumentException("Code verifier length must be between 43 and 128 characters");
        
        const string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                                    "abcdefghijklmnopqrstuvwxyz" +
                                    "0123456789" +
                                    "-._~";
        
        using (var rng = RandomNumberGenerator.Create())
        {
            var byteBuffer = new byte[length];
            rng.GetBytes(byteBuffer);
            
            var result = new StringBuilder(length);
            foreach (byte b in byteBuffer)
                result.Append(allowedChars[b % allowedChars.Length]);
            
            return result.ToString();
        }
    }
    
    public static string GenerateCodeChallenge(string codeVerifier)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] challengeBytes = sha256.ComputeHash(
                Encoding.UTF8.GetBytes(codeVerifier));
            
            return Base64UrlEncode(challengeBytes);
        }
    }
    
    private static string Base64UrlEncode(byte[] data)
    {
        string base64 = Convert.ToBase64String(data);
        
        string base64Url = base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        
        return base64Url;
    }
    
    public static bool VerifyCodeChallenge(string codeVerifier, string codeChallenge)
    {
        string computedChallenge = GenerateCodeChallenge(codeVerifier);
        return computedChallenge.Equals(codeChallenge, StringComparison.Ordinal);
    }
}