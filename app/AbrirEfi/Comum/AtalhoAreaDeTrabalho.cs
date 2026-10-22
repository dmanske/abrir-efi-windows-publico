using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace AbrirEfi.Comum
{
    /// <summary>Cria um atalho (.lnk) do próprio AbrirEFI.exe na Área de Trabalho, usando o WScript.Shell (COM).</summary>
    public static class AtalhoAreaDeTrabalho
    {
        /// <summary>Cria o atalho e devolve o caminho do .lnk. Lança exceção se não conseguir.</summary>
        public static string Criar()
        {
            var tipo = Type.GetTypeFromProgID("WScript.Shell");
            if (tipo == null) throw new InvalidOperationException("O WScript.Shell não está disponível neste Windows.");

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var caminhoLnk = Path.Combine(desktop, "Abrir EFI.lnk");
            var exe = Application.ExecutablePath;

            var shell = Activator.CreateInstance(tipo);
            var atalho = tipo.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { caminhoLnk });
            var tipoAtalho = atalho.GetType();
            Definir(tipoAtalho, atalho, "TargetPath", exe);
            Definir(tipoAtalho, atalho, "WorkingDirectory", Path.GetDirectoryName(exe));
            Definir(tipoAtalho, atalho, "Description", "Abre a partição EFI do Windows (como administrador)");
            Definir(tipoAtalho, atalho, "IconLocation", exe + ",0");
            tipoAtalho.InvokeMember("Save", BindingFlags.InvokeMethod, null, atalho, null);
            return caminhoLnk;
        }

        private static void Definir(Type tipo, object objeto, string propriedade, string valor)
        {
            tipo.InvokeMember(propriedade, BindingFlags.SetProperty, null, objeto, new object[] { valor });
        }
    }
}
