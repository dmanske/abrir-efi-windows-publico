using System;
using System.Runtime.InteropServices;

namespace AbrirEfi.Comum.Nativo
{
    /// <summary>Funções genéricas de janela (user32/uxtheme) usadas pelo navegador.</summary>
    internal static class NativoJanelas
    {
        public const uint WM_NULL = 0x0000;
        public const uint WM_DRAWITEM = 0x002B;
        public const uint WM_MEASUREITEM = 0x002C;
        public const uint WM_COPYDATA = 0x004A;
        public const uint WM_COPYGLOBALDATA = 0x0049;
        public const uint WM_INITMENUPOPUP = 0x0117;
        public const uint WM_MENUCHAR = 0x0120;
        public const uint WM_DROPFILES = 0x0233;

        [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "PostMessageW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>Aplica o visual "Explorer" (seleção azul clara, cabeçalhos modernos) a um controle.</summary>
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hwnd, string subAppName, string subIdList);
    }
}
