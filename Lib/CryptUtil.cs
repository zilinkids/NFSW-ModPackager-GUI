using System;
using System.IO;
using System.Security.Cryptography;

namespace ModPackagerUI
{
    public static class CryptUtil
    {
        /// <summary>
        /// Generates a random encryption key of the given length.
        /// </summary>
        public static byte[] GenerateKey(int length)
        {
            byte[] key = new byte[length];
            Random rng = new Random(Guid.NewGuid().GetHashCode());
            for (int i = 0; i < length; i++)
            {
                key[i] = (byte) rng.Next(32, 127);
            }
            return key;
        }

        public static string SHA1File(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open))
            using (var bs = new BufferedStream(fs))
            {
                using (var sha1 = SHA1.Create())
                {
                    return BitConverter.ToString(sha1.ComputeHash(bs)).Replace("-", "").ToLower();
                }
            }
        }
    }
}
