using System;
using System.Runtime.InteropServices;

namespace AbrirEfi.Comum.Nativo
{
    /// <summary>
    /// UIPI: um programa elevado (administrador) ignora mensagens enviadas por programas comuns,
    /// inclusive o arrastar-e-soltar do Explorer. ChangeWindowMessageFilterEx abre exceções por janela.
    /// </summary>
    internal static class NativoUipi
    {
        private const uint MSGFLT_ALLOW = 1;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr hwnd, uint message, uint action, IntPtr pChangeFilterStruct);

        /// <summary>
        /// Libera WM_DROPFILES, WM_COPYDATA e WM_COPYGLOBALDATA para a janela. Registra no log o resultado de cada uma.
        /// </summary>
        public static void LiberarMensagensDeArrasto(IntPtr hwnd, string descricao)
        {
            foreach (var mensagem in new[] { NativoJanelas.WM_DROPFILES, NativoJanelas.WM_COPYDATA, NativoJanelas.WM_COPYGLOBALDATA })
            {
                try
                {
                    var ok = ChangeWindowMessageFilterEx(hwnd, mensagem, MSGFLT_ALLOW, IntPtr.Zero);
                    var erro = ok ? 0 : Marshal.GetLastWin32Error();
                    Log.Escrever("ChangeWindowMessageFilterEx({0}, 0x{1:X}) = {2}{3}", descricao, mensagem, ok,
                                 ok ? "" : " (erro Win32 " + erro + ")");
                }
                catch (Exception ex)
                {
                    Log.Erro("ChangeWindowMessageFilterEx " + descricao, ex);
                }
            }
        }
    }
}
