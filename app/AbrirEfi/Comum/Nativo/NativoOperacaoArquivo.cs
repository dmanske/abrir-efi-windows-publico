using System;
using System.Runtime.InteropServices;

namespace AbrirEfi.Comum.Nativo
{
    /// <summary>
    /// SHFileOperation (copiar/mover/excluir com a janela de progresso e de conflitos do próprio Windows).
    /// A estrutura tem empacotamento diferente em 32 bits (Pack=1) e 64 bits (natural, 8) no SDK
    /// (shellapi.h usa pshpack1 só fora do _WIN64); por isso duas versões, escolhidas por IntPtr.Size.
    /// pFrom e pTo são listas de textos terminadas por DOIS caracteres nulos.
    /// </summary>
    internal static class NativoOperacaoArquivo
    {
        public const uint FO_MOVE = 0x1;
        public const uint FO_COPY = 0x2;
        public const uint FO_DELETE = 0x3;

        public const ushort FOF_RENAMEONCOLLISION = 0x0008;
        public const ushort FOF_NOCONFIRMATION = 0x0010;
        public const ushort FOF_NOCONFIRMMKDIR = 0x0200;

        [StructLayout(LayoutKind.Sequential)]
        private struct Estrutura64
        {
            public IntPtr hwnd;
            public uint wFunc;
            public IntPtr pFrom;
            public IntPtr pTo;
            public ushort fFlags;
            public int fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public IntPtr lpszProgressTitle;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Estrutura32
        {
            public IntPtr hwnd;
            public uint wFunc;
            public IntPtr pFrom;
            public IntPtr pTo;
            public ushort fFlags;
            public int fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public IntPtr lpszProgressTitle;
        }

        [DllImport("shell32.dll", EntryPoint = "SHFileOperationW", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation64(ref Estrutura64 operacao);

        [DllImport("shell32.dll", EntryPoint = "SHFileOperationW", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation32(ref Estrutura32 operacao);

        /// <summary>Executa a operação. Devolve o código do SHFileOperation (0 = ok) e se o usuário cancelou.</summary>
        public static int Executar(IntPtr dono, uint funcao, string[] origens, string destino, ushort flags, out bool cancelada)
        {
            var de = IntPtr.Zero;
            var para = IntPtr.Zero;
            try
            {
                de = Marshal.StringToHGlobalUni(string.Join("\0", origens) + "\0\0");
                if (destino != null) para = Marshal.StringToHGlobalUni(destino + "\0\0");

                if (IntPtr.Size == 8)
                {
                    var op = new Estrutura64 { hwnd = dono, wFunc = funcao, pFrom = de, pTo = para, fFlags = flags };
                    var r = SHFileOperation64(ref op);
                    cancelada = op.fAnyOperationsAborted != 0;
                    return r;
                }
                else
                {
                    var op = new Estrutura32 { hwnd = dono, wFunc = funcao, pFrom = de, pTo = para, fFlags = flags };
                    var r = SHFileOperation32(ref op);
                    cancelada = op.fAnyOperationsAborted != 0;
                    return r;
                }
            }
            finally
            {
                if (de != IntPtr.Zero) Marshal.FreeHGlobal(de);
                if (para != IntPtr.Zero) Marshal.FreeHGlobal(para);
            }
        }
    }
}
