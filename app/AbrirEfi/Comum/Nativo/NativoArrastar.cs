using System;
using System.Runtime.InteropServices;

namespace AbrirEfi.Comum.Nativo
{
    /// <summary>API clássica de arrastar arquivos (HDROP): DragAcceptFiles / WM_DROPFILES.</summary>
    internal static class NativoArrastar
    {
        [DllImport("shell32.dll")]
        public static extern void DragAcceptFiles(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool aceitar);

        [DllImport("shell32.dll", EntryPoint = "DragQueryFileW", CharSet = CharSet.Unicode)]
        public static extern uint DragQueryFile(IntPtr hDrop, uint indice, char[] texto, uint tamanho);

        [DllImport("shell32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DragQueryPoint(IntPtr hDrop, out PontoNativo ponto);

        [DllImport("shell32.dll")]
        public static extern void DragFinish(IntPtr hDrop);
    }
}
