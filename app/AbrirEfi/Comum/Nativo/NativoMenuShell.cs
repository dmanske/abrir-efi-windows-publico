using System;
using System.Runtime.InteropServices;

namespace AbrirEfi.Comum.Nativo
{
    /// <summary>Funções necessárias para obter e mostrar o menu de contexto do shell (IContextMenu).</summary>
    internal static class NativoMenuShell
    {
        public const uint TPM_RIGHTBUTTON = 0x0002;
        public const uint TPM_RETURNCMD = 0x0100;

        public const uint CMF_NORMAL = 0x0;
        public const uint CMF_EXPLORE = 0x4;
        public const uint CMF_EXTENDEDVERBS = 0x100;

        public const uint CMIC_MASK_UNICODE = 0x4000;
        public const uint CMIC_MASK_PTINVOKE = 0x20000000;

        /// <summary>CMINVOKECOMMANDINFOEX do SDK (alinhamento natural, igual em 32 e 64 bits).</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct CMINVOKECOMMANDINFOEX
        {
            public uint cbSize;
            public uint fMask;
            public IntPtr hwnd;
            public IntPtr lpVerb;
            public IntPtr lpParameters;
            public IntPtr lpDirectory;
            public int nShow;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr lpTitle;
            public IntPtr lpVerbW;
            public IntPtr lpParametersW;
            public IntPtr lpDirectoryW;
            public IntPtr lpTitleW;
            public PontoNativo ptInvoke;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHParseDisplayName(string nome, IntPtr pbc, out IntPtr pidl, uint atributosIn, out uint atributosOut);

        [DllImport("shell32.dll")]
        public static extern int SHBindToParent(IntPtr pidl, ref Guid riid, out IntPtr ppv, out IntPtr pidlFilho);

        [DllImport("shell32.dll")]
        public static extern void ILFree(IntPtr pidl);

        [DllImport("user32.dll")]
        public static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyMenu(IntPtr hmenu);

        [DllImport("user32.dll")]
        public static extern int TrackPopupMenuEx(IntPtr hmenu, uint flags, int x, int y, IntPtr hwnd, IntPtr parametros);
    }
}
