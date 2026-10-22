using System;
using System.Diagnostics;
using AbrirEfi.Comum;
using AbrirEfi.Comum.Nativo;

namespace AbrirEfi.Navegador.Operacoes
{
    /// <summary>"Abrir com...": a janela de escolha de programa do Windows.</summary>
    public static class AbrirCom
    {
        public static void Escolher(IntPtr dono, string arquivo)
        {
            try
            {
                var info = new NativoAbrirCom.OPENASINFO
                {
                    pcszFile = arquivo,
                    pcszClass = null,
                    oaifInFlags = NativoAbrirCom.OAIF_ALLOW_REGISTRATION | NativoAbrirCom.OAIF_EXEC,
                };
                var hr = NativoAbrirCom.SHOpenWithDialog(dono, ref info);
                Log.Escrever("SHOpenWithDialog({0}) = 0x{1:X}", arquivo, hr);
                if (hr >= 0 || hr == unchecked((int)0x800704C7)) return; // ok ou cancelado pelo usuário
            }
            catch (Exception ex) when (ex is EntryPointNotFoundException || ex is DllNotFoundException)
            {
                Log.Erro("SHOpenWithDialog indisponível", ex);
            }

            // Plano B: o mesmo diálogo pelo rundll32.
            Log.Escrever("Abrir com: usando rundll32 como alternativa");
            using (Process.Start(new ProcessStartInfo
            {
                FileName = ExecutorNativo.NoSystem32("rundll32.exe"),
                Arguments = "shell32.dll,OpenAs_RunDLL \"" + arquivo + "\"",
                UseShellExecute = false,
            })) { }
        }
    }
}
