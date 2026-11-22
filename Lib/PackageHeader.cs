using System.Runtime.InteropServices;

namespace ModPackagerUI
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PackageHeader
    {
        public uint Magic;

        public bool EncryptionEnabled;

        public long CompilationTimestamp;

        public int KeyLength;
    }
}
