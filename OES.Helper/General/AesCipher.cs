using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text;

namespace OES.Helper.General
{
    public static class AesCipher
    {
        private const string HardcodedKey = "ThisIsA32ByteSecretKeyForAES256!"; // <-- MUST BE 32 characters

        // The IV must be 16 bytes for AES.
        private const string HardcodedIv = "ThisIsA16ByteIV!"; // <-- MUST BE 16 characters

        private static readonly byte[] KeyBytes = Encoding.UTF8.GetBytes(HardcodedKey);
        private static readonly byte[] IvBytes = Encoding.UTF8.GetBytes(HardcodedIv);

        /// <summary>
        /// Encrypts plain-text using AES-256 in CBC mode with PKCS7 padding.
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            // 1. Create the Bouncy Castle cipher engine.
            var engine = new RijndaelEngine();
            var blockCipher = new CbcBlockCipher(engine);
            var cipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());

            // 2. Initialize the cipher for encryption with our key and IV.
            var keyParam = new KeyParameter(KeyBytes);
            var keyParamWithIv = new ParametersWithIV(keyParam, IvBytes);
            cipher.Init(true, keyParamWithIv);

            // 3. Encrypt the data.
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = new byte[cipher.GetOutputSize(plainBytes.Length)];
            int length = cipher.ProcessBytes(plainBytes, 0, plainBytes.Length, encryptedBytes, 0);
            cipher.DoFinal(encryptedBytes, length);

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Decrypts a Base64 encoded AES-256 payload.
        /// </summary>
        public static string Decrypt(string encryptedBase64Text)
        {
            if (string.IsNullOrEmpty(encryptedBase64Text))
                return encryptedBase64Text;

            try
            {
                // 1. Create the Bouncy Castle cipher engine (same as encryption).
                var engine = new RijndaelEngine();
                var blockCipher = new CbcBlockCipher(engine);
                var cipher = new PaddedBufferedBlockCipher(blockCipher, new Pkcs7Padding());

                // 2. Initialize the cipher for DEcryption.
                var keyParam = new KeyParameter(KeyBytes);
                var keyParamWithIv = new ParametersWithIV(keyParam, IvBytes);
                cipher.Init(false, keyParamWithIv);

                // 3. Decrypt the data.
                var encryptedBytes = Convert.FromBase64String(encryptedBase64Text);
                var decryptedBytes = new byte[cipher.GetOutputSize(encryptedBytes.Length)];
                int length = cipher.ProcessBytes(encryptedBytes, 0, encryptedBytes.Length, decryptedBytes, 0);
                int finalLength = cipher.DoFinal(decryptedBytes, length);

                // 4. Create the final plain byte array of the correct size.
                byte[] finalPlainBytes = new byte[length + finalLength];
                Array.Copy(decryptedBytes, 0, finalPlainBytes, 0, finalPlainBytes.Length);

                return Encoding.UTF8.GetString(finalPlainBytes);
            }
            catch (Exception ex) // Catch potential padding errors, etc.
            {
                // If decryption fails, it could be not-encrypted data.
                Console.WriteLine($"[AesCipher] Decryption failed (possibly not-encrypted data): {ex.GetType().Name} - {ex.Message}");
                return encryptedBase64Text;
            }
        }
    }
}