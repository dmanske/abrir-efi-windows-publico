using System;
using System.Runtime.InteropServices;

namespace AbrirEfi.Comum.Nativo
{
    /// <summary>SHOpenWithDialog: a janela "Abrir com..." do Windows (Windows 7 ou mais novo).</summary>
    internal static class NativoAbrirCom
    {
        public const uint OAIF_ALLOW_REGISTRATION = 0x1;
        public const uint OAIF_EXEC = 0x4;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct OPENASINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pcszFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pcszClass;
            public uint oaifInFlags;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHOpenWithDialog(IntPtr hwndParent, ref OPENASINFO info);
    }
}
