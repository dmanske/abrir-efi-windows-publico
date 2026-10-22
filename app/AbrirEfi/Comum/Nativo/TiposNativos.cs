using System.Runtime.InteropServices;

// Em programa elevado, procurar DLLs só em System32 evita carregar uma DLL plantada ao lado do .exe.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace AbrirEfi.Comum.Nativo
{
    /// <summary>POINT do Windows.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PontoNativo
    {
        public int X;
        public int Y;
    }
}
