using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharedHelper.Common;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace OES.Infrastructure.ValueConverters
{
    internal class SecureCompressedStringConverter : ValueConverter<string, string>
    {
        private static readonly byte[] Key = DeriveMySQLKey(Secrets.EncryptionKey, 32);
        private static readonly byte[] IV = DeriveMySQLKey(Secrets.EncryptionIV, 16);

        public SecureCompressedStringConverter()
            : base(
                v => CompressAndEncrypt(v),
                v => DecryptAndDecompress(v))
        { }

        private static byte[] DeriveMySQLKey(string keyString, int keyLength = 32)
        {
            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            var derivedKey = new byte[keyLength];

            for (int i = 0; i < keyBytes.Length; i++)
            {
                derivedKey[i % keyLength] ^= keyBytes[i];
            }

            return derivedKey;
        }

        private static string CompressAndEncrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            if (IsAlreadyEncrypted(value)) return value;

            var compressedData = Compress(value);
            var encryptedData = Encrypt(compressedData, Key, IV);

            return Convert.ToBase64String(encryptedData);
        }

        private static string DecryptAndDecompress(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            byte[] decryptedData;

            try
            {
                var encryptedData = Convert.FromBase64String(value);
                decryptedData = Decrypt(encryptedData, Key, IV);
            }
            catch
            {
                return value;
            }

            try
            {
                return Decompress(decryptedData);
            }
            catch
            {
                return Encoding.UTF8.GetString(decryptedData);
            }
        }

        private static byte[] Compress(string text)
        {
            var rawBytes = Encoding.UTF8.GetBytes(text);
            var rawLength = rawBytes.Length;

            using var memoryStream = new MemoryStream();

            // 1. Add 4 bytes to represent the original length (crucial for MySQL UNCOMPRESS)
            memoryStream.Write(BitConverter.GetBytes(rawLength), 0, 4);

            // 2. Compress the actual data using Zlib
            using (var zlibStream = new ZLibStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlibStream.Write(rawBytes, 0, rawLength);
            }

            return memoryStream.ToArray();
        }

        private static string Decompress(byte[] compressedData)
        {
            if (compressedData.Length < 4)
                throw new InvalidDataException("Compressed data is too short.");

            // Decompress the remaining array starting from byte index 4
            using var memoryStream = new MemoryStream(compressedData, 4, compressedData.Length - 4);
            using var zlibStream = new ZLibStream(memoryStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();

            zlibStream.CopyTo(resultStream);

            return Encoding.UTF8.GetString(resultStream.ToArray());
        }

        private static byte[] Encrypt(byte[] plainData, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var memoryStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainData, 0, plainData.Length);
            }
            return memoryStream.ToArray();
        }

        private static byte[] Decrypt(byte[] cipherData, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var memoryStream = new MemoryStream(cipherData);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var resultStream = new MemoryStream();
            cryptoStream.CopyTo(resultStream);
            return resultStream.ToArray();
        }

        private static bool IsAlreadyEncrypted(string value)
        {
            if (!IsBase64(value)) return false;

            try
            {
                var decoded = Convert.FromBase64String(value);

                var decrypted = Decrypt(decoded, Key, IV);

                var decompressed = Decompress(decrypted);

                return !string.IsNullOrEmpty(decompressed);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsBase64(string value)
        {
            value = value.Trim();
            if (value.Length % 4 != 0) return false;

            Span<byte> buffer = new Span<byte>(new byte[value.Length]);

            return Convert.TryFromBase64String(value, buffer, out _);
        }
    }
}