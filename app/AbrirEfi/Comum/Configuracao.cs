using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AbrirEfi.Comum
{
    /// <summary>
    /// Configurações simples em %LOCALAPPDATA%\AbrirEFI\config.ini (linhas "chave=valor").
    /// Qualquer falha de leitura/gravação é registrada no log e ignorada.
    /// </summary>
    public static class Configuracao
    {
        private const string ChaveNaoPerguntarAoFechar = "NaoPerguntarAoFechar";

        public static string CaminhoArquivo
        {
            get
            {
                var pastaLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(pastaLocal, "AbrirEFI", "config.ini");
            }
        }

        /// <summary>Se true, o aviso ao fechar a janela não aparece mais.</summary>
        public static bool NaoPerguntarAoFechar
        {
            get { return Ler(ChaveNaoPerguntarAoFechar) == "1"; }
            set { Gravar(ChaveNaoPerguntarAoFechar, value ? "1" : "0"); }
        }

        private static Dictionary<string, string> Carregar()
        {
            var valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(CaminhoArquivo)) return valores;
                foreach (var linha in File.ReadAllLines(CaminhoArquivo, Encoding.UTF8))
                {
                    var i = linha.IndexOf('=');
                    if (i <= 0) continue;
                    valores[linha.Substring(0, i).Trim()] = linha.Substring(i + 1).Trim();
                }
            }
            catch (Exception ex)
            {
                Log.Erro("ler configuração", ex);
            }
            return valores;
        }

        private static string Ler(string chave)
        {
            string valor;
            return Carregar().TryGetValue(chave, out valor) ? valor : null;
        }

        private static void Gravar(string chave, string valor)
        {
            try
            {
                var valores = Carregar();
                valores[chave] = valor;
                var linhas = new List<string>();
                foreach (var par in valores) linhas.Add(par.Key + "=" + par.Value);
                Directory.CreateDirectory(Path.GetDirectoryName(CaminhoArquivo));
                File.WriteAllLines(CaminhoArquivo, linhas, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log.Erro("gravar configuração", ex);
            }
        }
    }
}
