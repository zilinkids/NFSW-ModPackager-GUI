using System;
using System.IO;
using System.Windows.Forms;

namespace ModPackagerUI
{
    internal static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            // Headless mode: ModPackagerUI.exe <mod名称> <源文件夹> <输出目录> [encrypt:true|false] [index:true|false]
            if (args.Length >= 3)
            {
                try
                {
                    var modName = string.Join("", args[0].Split(Path.GetInvalidFileNameChars())).Trim();
                    var sourceFolder = args[1];
                    var outDir = args[2];
                    var encrypt = args.Length >= 4 && string.Equals(args[3], "true", StringComparison.OrdinalIgnoreCase);
                    var genIndex = args.Length >= 5 && string.Equals(args[4], "true", StringComparison.OrdinalIgnoreCase);

                    if (string.IsNullOrEmpty(modName))
                        throw new Exception("Mod 名称无效。");
                    if (!Directory.Exists(sourceFolder))
                        throw new Exception($"找不到源文件夹: {sourceFolder}");

                    Directory.CreateDirectory(outDir);
                    var packageName = modName + ".mods";
                    var outFilePath = Path.Combine(outDir, packageName);

                    Packaging.BuildPackage(sourceFolder, outFilePath, encrypt, msg => Console.WriteLine(msg));

                    if (genIndex)
                    {
                        Packaging.WriteIndex(outDir, packageName);
                        Console.WriteLine("已生成 index.json");
                    }

                    Console.WriteLine($"完成: {outFilePath}");
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"错误: {ex.Message}");
                    return 1;
                }
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
            return 0;
        }
    }
}
