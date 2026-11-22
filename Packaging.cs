using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zip;

namespace ModPackagerUI
{
    /// <summary>
    /// Packaging logic ported from ModPackager 2.0.1 (Program.cs) so that the
    /// generated .mods files are byte-format compatible with the original tool.
    /// </summary>
    public static class Packaging
    {
        // key length in BYTES, not BITS
        private const int KeyLength = 32;

        private static readonly byte[] XorTable =
        {
            0x94, 0xce, 0xc3, 0xae, 0x73, 0xf9, 0xf1, 0xb9
        };

        /// <summary>
        /// Packs the contents of <paramref name="sourceFolder"/> (recursively) into a
        /// .mods package written to <paramref name="outFilePath"/>.
        /// </summary>
        public static void BuildPackage(string sourceFolder, string outFilePath, bool encrypt, Action<string> log)
        {
            log($"正在打包: {sourceFolder}");

            var masterKey = Array.Empty<byte>();
            using var ms = new MemoryStream();
            var packageHeader = new PackageHeader
            {
                Magic = 0x4459495A,
                CompilationTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                EncryptionEnabled = encrypt
            };

            if (encrypt)
            {
                packageHeader.KeyLength = KeyLength;
                masterKey = CryptUtil.GenerateKey(KeyLength);
            }

            BinaryUtils.MarshalStruct(ms, packageHeader);

            if (encrypt)
            {
                for (var i = 0; i < masterKey.Length; i++)
                {
                    ms.WriteByte((byte)(masterKey[i] ^ XorTable[i % XorTable.Length]));
                }
            }

            using (var zipFile = new ZipFile())
            {
                if (encrypt)
                {
                    zipFile.Encryption = EncryptionAlgorithm.WinZipAes256;
                    zipFile.Password = Encoding.ASCII.GetString(masterKey);
                }

                foreach (var item in Directory.GetFileSystemEntries(sourceFolder))
                {
                    var itemName = Path.GetFileName(item);

                    if (Directory.Exists(item))
                    {
                        log($"正在读取目录 {itemName}/ ...");
                        zipFile.AddDirectory(item, itemName);
                    }
                    else
                    {
                        zipFile.AddEntry(itemName.Replace('\\', '/'), File.ReadAllBytes(item));
                    }
                }

                log("正在压缩...");
                zipFile.Save(ms);
            }

            log($"正在写入 {outFilePath} ...");
            ms.Position = 0;
            using var fs = File.Open(outFilePath, FileMode.Create, FileAccess.Write);
            ms.CopyTo(fs);
        }

        /// <summary>
        /// Writes an index.json (same layout as the original build index) next to the package.
        /// </summary>
        public static void WriteIndex(string outDir, string packageName)
        {
            var checksum = CryptUtil.SHA1File(Path.Combine(outDir, packageName));
            var buildIndex = new BuildIndex
            {
                BuiltAt = DateTime.Now,
                Entries = new List<BuildIndexEntry>
                {
                    new BuildIndexEntry
                    {
                        Checksum = checksum,
                        Name = packageName
                    }
                }
            };

            File.WriteAllText(Path.Combine(outDir, "index.json"), Serialization.Serialize(buildIndex));
        }
    }
}
