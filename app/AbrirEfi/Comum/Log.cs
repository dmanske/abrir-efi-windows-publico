using System;
using System.IO;
using System.Text;

namespace AbrirEfi.Comum
{
    /// <summary>
    /// Log em %LOCALAPPDATA%\AbrirEFI\abrir-efi.log, com data e hora LOCAIS.
    /// Qualquer falha ao gravar é engolida: o log nunca pode impedir de abrir a EFI.
    /// </summary>
    public static class Log
    {
        private static readonly object Trava = new object();

        /// <summary>Caminho completo do arquivo de log (mostrado nas mensagens de erro).</summary>
        public static string CaminhoArquivo
        {
            get
            {
                var pastaLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(pastaLocal, "AbrirEFI", "abrir-efi.log");
            }
        }

        public static void Escrever(string mensagem)
        {
            try
            {
                var arquivo = CaminhoArquivo;
                var linha = string.Format("{0:yyyy-MM-dd HH:mm:ss}  {1}{2}", DateTime.Now, mensagem, Environment.NewLine);
                lock (Trava)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(arquivo));
                    // UTF-8 com BOM (o BOM só é gravado quando o arquivo é criado).
                    File.AppendAllText(arquivo, linha, Encoding.UTF8);
                }
            }
            catch
            {
                // Falha no log nunca deve derrubar o app.
            }
        }

        public static void Escrever(string formato, params object[] argumentos)
        {
            string mensagem;
            try { mensagem = string.Format(formato, argumentos); }
            catch { mensagem = formato; }
            Escrever(mensagem);
        }

        /// <summary>Registra uma exceção com o tipo, a mensagem e a pilha.</summary>
        public static void Erro(string contexto, Exception ex)
        {
            Escrever("ERRO ({0}): {1}", contexto, ex);
        }
    }
}
