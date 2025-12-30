using NSec.Cryptography;
using System.Security.Cryptography;

namespace App.System.Crypto;

/// <summary>
/// Cryptographic operations for user authentication and P2P encryption
/// </summary>
public static class UserCrypto
{
    private static readonly string KeysDirectory = Path.Combine(Context.DataDirectory, "Keys");

    /// <summary>
    /// Generates Ed25519 and X25519 key pairs and saves them to disk
    /// </summary>
    public static (byte[] publicKeyEd25519, byte[] publicKeyX25519) GenerateAndSaveKeys()
    {
        Directory.CreateDirectory(KeysDirectory);

        // Generate Ed25519 key pair (for signatures)
        var ed25519Algorithm = SignatureAlgorithm.Ed25519;
        using var ed25519Key = Key.Create(ed25519Algorithm, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
        
        var publicKeyEd25519 = ed25519Key.PublicKey.Export(KeyBlobFormat.RawPublicKey);
        var privateKeyEd25519 = ed25519Key.Export(KeyBlobFormat.RawPrivateKey);

        // Generate X25519 key pair (for key exchange)
        var x25519Algorithm = KeyAgreementAlgorithm.X25519;
        using var x25519Key = Key.Create(x25519Algorithm, new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport });
        
        var publicKeyX25519 = x25519Key.PublicKey.Export(KeyBlobFormat.RawPublicKey);
        var privateKeyX25519 = x25519Key.Export(KeyBlobFormat.RawPrivateKey);

        File.WriteAllBytes(Path.Combine(KeysDirectory, "ed25519_public.key"), publicKeyEd25519);
        File.WriteAllBytes(Path.Combine(KeysDirectory, "ed25519_private.key"), privateKeyEd25519);
        File.WriteAllBytes(Path.Combine(KeysDirectory, "x25519_public.key"), publicKeyX25519);
        File.WriteAllBytes(Path.Combine(KeysDirectory, "x25519_private.key"), privateKeyX25519);

        Services.Logger.Write(Services.Logger.Type.Info, $"Generated and saved Ed25519 and X25519 keys to {KeysDirectory}");

        return (publicKeyEd25519, publicKeyX25519);
    }

    /// <summary>
    /// Loads existing keys from disk
    /// </summary>
    public static (byte[] publicKeyEd25519, byte[] publicKeyX25519)? LoadKeys()
    {
        try
        {
            var ed25519PublicPath = Path.Combine(KeysDirectory, "ed25519_public.key");
            var x25519PublicPath = Path.Combine(KeysDirectory, "x25519_public.key");

            if (!File.Exists(ed25519PublicPath) || !File.Exists(x25519PublicPath))
                return null;

            var publicKeyEd25519 = File.ReadAllBytes(ed25519PublicPath);
            var publicKeyX25519 = File.ReadAllBytes(x25519PublicPath);

            return (publicKeyEd25519, publicKeyX25519);
        }
        catch (Exception ex)
        {
            Services.Logger.Write(Services.Logger.Type.Error, "Failed to load keys", ex);
            return null;
        }
    }

    /// <summary>
    /// Checks if keys exist on disk
    /// </summary>
    public static bool KeysExist()
    {
        var ed25519PublicPath = Path.Combine(KeysDirectory, "ed25519_public.key");
        var ed25519PrivatePath = Path.Combine(KeysDirectory, "ed25519_private.key");
        var x25519PublicPath = Path.Combine(KeysDirectory, "x25519_public.key");
        var x25519PrivatePath = Path.Combine(KeysDirectory, "x25519_private.key");

        return File.Exists(ed25519PublicPath) && File.Exists(ed25519PrivatePath) &&
               File.Exists(x25519PublicPath) && File.Exists(x25519PrivatePath);
    }

    /// <summary>
    /// Signs data with Ed25519 private key
    /// </summary>
    public static byte[] SignData(byte[] data)
    {
        var privateKeyPath = Path.Combine(KeysDirectory, "ed25519_private.key");
        if (!File.Exists(privateKeyPath))
            throw new InvalidOperationException("Ed25519 private key not found");

        var privateKeyBytes = File.ReadAllBytes(privateKeyPath);
        var algorithm = SignatureAlgorithm.Ed25519;
        
        using var key = Key.Import(algorithm, privateKeyBytes, KeyBlobFormat.RawPrivateKey);
        return algorithm.Sign(key, data);
    }

    /// <summary>
    /// Computes SHA256 fingerprint of Ed25519 public key
    /// </summary>
    public static string ComputeFingerprint(byte[] publicKeyEd25519)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(publicKeyEd25519);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Gets or generates keys (ensures keys exist)
    /// </summary>
    public static (byte[] publicKeyEd25519, byte[] publicKeyX25519) EnsureKeys()
    {
        var keys = LoadKeys();
        if (keys.HasValue)
            return keys.Value;

        return GenerateAndSaveKeys();
    }
}
