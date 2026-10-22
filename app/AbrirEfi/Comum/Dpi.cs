using System.Windows.Forms;

namespace AbrirEfi.Comum
{
    /// <summary>Converte medidas pensadas em 100% de escala (96 DPI) para o DPI do controle.</summary>
    public static class Dpi
    {
        public static int Escalar(Control controle, int pixels96)
        {
            var dpi = controle != null && controle.DeviceDpi > 0 ? controle.DeviceDpi : 96;
            return pixels96 * dpi / 96;
        }
    }
}
