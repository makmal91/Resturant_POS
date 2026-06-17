using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using POSSystem.Application.License.Models;
using POSSystem.Application.License.Options;

namespace POSSystem.Infrastructure.License;

public sealed class LicenseFileDocument
{
    public int Version { get; set; } = 1;
    public string EncryptedPayload { get; set; } = string.Empty;
    public string Iv { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public static class LicenseCrypto
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static LicensePayload DecryptAndVerify(LicenseFileDocument document, LicenseOptions options)
    {
        if (document.Version != 1)
            throw new InvalidOperationException("Unsupported license file version.");

        if (string.IsNullOrWhiteSpace(document.EncryptedPayload) ||
            string.IsNullOrWhiteSpace(document.Iv) ||
            string.IsNullOrWhiteSpace(document.Signature))
        {
            throw new InvalidOperationException("License file is missing required fields.");
        }

        VerifySignature(document, options.PublicKeyPem);

        var aesKey = Convert.FromBase64String(options.AesKeyBase64);
        var iv = Convert.FromBase64String(document.Iv);
        var cipherBytes = Convert.FromBase64String(document.EncryptedPayload);

        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        var json = Encoding.UTF8.GetString(plainBytes);

        return JsonSerializer.Deserialize<LicensePayload>(json, JsonOptions)
            ?? throw new InvalidOperationException("License payload is invalid.");
    }

    public static LicenseFileDocument EncryptAndSign(LicensePayload payload, LicenseOptions options, RSA privateKey)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var plainBytes = Encoding.UTF8.GetBytes(json);

        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(options.AesKeyBase64);
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var document = new LicenseFileDocument
        {
            Version = 1,
            EncryptedPayload = Convert.ToBase64String(cipherBytes),
            Iv = Convert.ToBase64String(aes.IV)
        };

        document.Signature = SignDocument(document, privateKey);
        return document;
    }

    public static LicenseFileDocument ParseDocument(string content)
    {
        var document = JsonSerializer.Deserialize<LicenseFileDocument>(content, JsonOptions);
        return document ?? throw new InvalidOperationException("License file format is invalid.");
    }

    public static string SerializeDocument(LicenseFileDocument document)
        => JsonSerializer.Serialize(document, JsonOptions);

    private static void VerifySignature(LicenseFileDocument document, string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem))
            throw new InvalidOperationException("License public key is not configured.");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        var signatureBytes = Convert.FromBase64String(document.Signature);
        var signedContent = BuildSignedContent(document.EncryptedPayload, document.Iv);

        if (!rsa.VerifyData(signedContent, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            throw new InvalidOperationException("License signature verification failed.");
    }

    private static string SignDocument(LicenseFileDocument document, RSA privateKey)
    {
        var signedContent = BuildSignedContent(document.EncryptedPayload, document.Iv);
        var signatureBytes = privateKey.SignData(signedContent, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signatureBytes);
    }

    private static byte[] BuildSignedContent(string encryptedPayload, string iv)
        => Encoding.UTF8.GetBytes($"{encryptedPayload}.{iv}");
}
