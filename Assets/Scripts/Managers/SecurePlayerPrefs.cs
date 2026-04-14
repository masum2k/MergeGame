using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Lightweight encrypted wrapper over PlayerPrefs with legacy migration support.
/// - New writes are stored as encrypted + integrity protected payloads.
/// - Legacy plain PlayerPrefs keys are read once and migrated in place.
/// </summary>
public static class SecurePlayerPrefs
{
    private const string SecureKeyPrefix = "__secure_v1__";
    private const string PayloadPrefix = "encv1";
    private const int Pbkdf2Iterations = 10000;

    private static readonly byte[] Salt =
    {
        0x1A, 0x4F, 0x92, 0xC3, 0x0D, 0xB7, 0x62, 0x7E,
        0x18, 0x5A, 0xD1, 0x39, 0x84, 0x2C, 0xF0, 0x6B
    };

    private static byte[] _encryptionKey;
    private static byte[] _macKey;

    public static bool HasKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return PlayerPrefs.HasKey(ToSecureKey(key)) || PlayerPrefs.HasKey(key);
    }

    public static void DeleteKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        PlayerPrefs.DeleteKey(ToSecureKey(key));
        PlayerPrefs.DeleteKey(key);
        SaveCoordinator.MarkDirty();
    }

    public static void SetInt(string key, int value)
    {
        WriteEncryptedString(key, value.ToString(CultureInfo.InvariantCulture));
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        if (TryGetEncryptedValue(key, out string secureValue, out bool tampered))
        {
            if (int.TryParse(secureValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                return parsed;

            return defaultValue;
        }

        if (tampered)
            return defaultValue;

        if (PlayerPrefs.HasKey(key))
        {
            int legacy = PlayerPrefs.GetInt(key, defaultValue);
            SetInt(key, legacy);
            return legacy;
        }

        return defaultValue;
    }

    public static void SetFloat(string key, float value)
    {
        WriteEncryptedString(key, value.ToString("R", CultureInfo.InvariantCulture));
    }

    public static float GetFloat(string key, float defaultValue = 0f)
    {
        if (TryGetEncryptedValue(key, out string secureValue, out bool tampered))
        {
            if (float.TryParse(secureValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float parsed))
                return parsed;

            return defaultValue;
        }

        if (tampered)
            return defaultValue;

        if (PlayerPrefs.HasKey(key))
        {
            float legacy = PlayerPrefs.GetFloat(key, defaultValue);
            SetFloat(key, legacy);
            return legacy;
        }

        return defaultValue;
    }

    public static void SetString(string key, string value)
    {
        WriteEncryptedString(key, value ?? string.Empty);
    }

    public static string GetString(string key, string defaultValue = "")
    {
        if (TryGetEncryptedValue(key, out string secureValue, out bool tampered))
            return secureValue;

        if (tampered)
            return defaultValue;

        if (PlayerPrefs.HasKey(key))
        {
            string legacy = PlayerPrefs.GetString(key, defaultValue);
            SetString(key, legacy);
            return legacy;
        }

        return defaultValue;
    }

    private static void WriteEncryptedString(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        string encrypted = Encrypt(key, value ?? string.Empty);
        PlayerPrefs.SetString(ToSecureKey(key), encrypted);

        // Remove plain legacy key to avoid plain-text persistence on disk.
        if (PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.DeleteKey(key);
        }

        SaveCoordinator.MarkDirty();
    }

    private static bool TryGetEncryptedValue(string key, out string value, out bool tampered)
    {
        value = string.Empty;
        tampered = false;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        string secureKey = ToSecureKey(key);
        if (!PlayerPrefs.HasKey(secureKey))
            return false;

        string payload = PlayerPrefs.GetString(secureKey, string.Empty);
        if (TryDecrypt(key, payload, out value))
            return true;

        tampered = true;
        Debug.LogWarning("[SecurePlayerPrefs] Invalid or tampered save payload for key: " + key);
        return false;
    }

    private static string Encrypt(string key, string plainText)
    {
        EnsureCryptoKeys();

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText ?? string.Empty);
        byte[] iv = new byte[16];

        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }

        byte[] cipherBytes;
        using (Aes aes = Aes.Create())
        {
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Key = _encryptionKey;
            aes.IV = iv;

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }
        }

        byte[] macInput = BuildMacInput(key, iv, cipherBytes);
        byte[] macBytes;
        using (HMACSHA256 hmac = new HMACSHA256(_macKey))
        {
            macBytes = hmac.ComputeHash(macInput);
        }

        return PayloadPrefix + ":" +
               Convert.ToBase64String(iv) + ":" +
               Convert.ToBase64String(cipherBytes) + ":" +
               Convert.ToBase64String(macBytes);
    }

    private static bool TryDecrypt(string key, string payload, out string plainText)
    {
        plainText = string.Empty;

        if (string.IsNullOrWhiteSpace(payload))
            return false;

        string[] parts = payload.Split(':');
        if (parts.Length != 4 || parts[0] != PayloadPrefix)
            return false;

        byte[] iv;
        byte[] cipherBytes;
        byte[] expectedMac;

        try
        {
            iv = Convert.FromBase64String(parts[1]);
            cipherBytes = Convert.FromBase64String(parts[2]);
            expectedMac = Convert.FromBase64String(parts[3]);
        }
        catch
        {
            return false;
        }

        EnsureCryptoKeys();

        byte[] macInput = BuildMacInput(key, iv, cipherBytes);
        byte[] actualMac;
        using (HMACSHA256 hmac = new HMACSHA256(_macKey))
        {
            actualMac = hmac.ComputeHash(macInput);
        }

        if (!FixedTimeEquals(actualMac, expectedMac))
            return false;

        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Key = _encryptionKey;
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    plainText = Encoding.UTF8.GetString(plainBytes);
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }
    }

    private static string ToSecureKey(string key)
    {
        return SecureKeyPrefix + key;
    }

    private static byte[] BuildMacInput(string key, byte[] iv, byte[] cipherBytes)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key ?? string.Empty);
        byte[] result = new byte[keyBytes.Length + 1 + iv.Length + cipherBytes.Length];

        Buffer.BlockCopy(keyBytes, 0, result, 0, keyBytes.Length);
        result[keyBytes.Length] = 0x7C; // '|'
        Buffer.BlockCopy(iv, 0, result, keyBytes.Length + 1, iv.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, keyBytes.Length + 1 + iv.Length, cipherBytes.Length);

        return result;
    }

    private static void EnsureCryptoKeys()
    {
        if (_encryptionKey != null && _macKey != null)
            return;

        string seed = Application.identifier + "|game3_secureprefs_v1";
        using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(seed, Salt, Pbkdf2Iterations, HashAlgorithmName.SHA256))
        {
            byte[] full = kdf.GetBytes(64);
            _encryptionKey = new byte[32];
            _macKey = new byte[32];

            Buffer.BlockCopy(full, 0, _encryptionKey, 0, 32);
            Buffer.BlockCopy(full, 32, _macKey, 0, 32);
        }
    }

    private static bool FixedTimeEquals(byte[] a, byte[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return false;

        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }

        return diff == 0;
    }
}
