using System;
using System.Runtime.InteropServices;

namespace AbrirEfi.Comum.Nativo
{
    /// <summary>SHGetFileInfo: índice do ícone na image list do sistema e nome do tipo ("Documento de Texto").</summary>
    internal static class NativoIcones
    {
        public const uint SHGFI_TYPENAME = 0x400;
        public const uint SHGFI_SYSICONINDEX = 0x4000;
        public const uint SHGFI_SMALLICON = 0x1;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        public const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        public const uint LVM_SETIMAGELIST = 0x1003;
        public const int LVSIL_SMALL = 1;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }

        [DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfo(string caminho, uint atributos, ref SHFILEINFO info, uint tamanhoInfo, uint flags);
    }
}
