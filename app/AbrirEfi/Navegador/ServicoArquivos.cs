using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AbrirEfi.Navegador
{
    /// <summary>
    /// Lógica de arquivos do navegador, separada da janela: listar, formatar e abrir.
    /// Copiar/mover/excluir ficam em <c>Operacoes/</c>.
    /// </summary>
    public static class ServicoArquivos
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        /// <summary>Lista o conteúdo de uma pasta: pastas primeiro, depois arquivos, em ordem alfabética.</summary>
        public static List<ItemArquivo> Listar(string pasta)
        {
            var info = new DirectoryInfo(pasta);
            if (!info.Exists) throw new DirectoryNotFoundException("Pasta não encontrada: " + pasta);

            var itens = new List<ItemArquivo>();
            foreach (var entrada in info.EnumerateFileSystemInfos())
            {
                try
                {
                    var ehPasta = (entrada.Attributes & FileAttributes.Directory) != 0;
                    itens.Add(new ItemArquivo
                    {
                        Nome = entrada.Name,
                        CaminhoCompleto = entrada.FullName,
                        EhPasta = ehPasta,
                        Tamanho = ehPasta ? (long?)null : ((FileInfo)entrada).Length,
                        Modificado = entrada.LastWriteTime,
                        Atributos = entrada.Attributes,
                    });
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // Uma entrada ilegível não impede de listar as outras.
                    Comum.Log.Erro("ler entrada " + entrada.FullName, ex);
                }
            }

            return itens
                .OrderByDescending(i => i.EhPasta)
                .ThenBy(i => i.Nome, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Pasta acima de <paramref name="pasta"/>; null se já está na raiz de uma unidade
        /// (o navegador pode sair da EFI, mas não existe "Este Computador" aqui).
        /// </summary>
        public static string PastaAcima(string pasta)
        {
            if (string.IsNullOrEmpty(pasta)) return null;
            var completo = Path.GetFullPath(pasta);
            if (string.Equals(Path.GetPathRoot(completo), completo, StringComparison.OrdinalIgnoreCase)) return null;
            return Directory.GetParent(completo.TrimEnd('\\'))?.FullName;
        }

        /// <summary>Compara dois caminhos ignorando maiúsculas e a barra final.</summary>
        public static bool MesmoCaminho(string a, string b)
        {
            if (a == null || b == null) return false;
            return string.Equals(Normalizar(a), Normalizar(b), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Abre o arquivo com o programa associado do Windows (herda o administrador deste app).</summary>
        public static void AbrirArquivo(string caminho)
        {
            var info = new ProcessStartInfo
            {
                FileName = caminho,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(caminho) ?? "",
            };
            using (Process.Start(info)) { }
        }

        public static string FormatarTamanho(long? bytes)
        {
            if (!bytes.HasValue) return "";
            var b = bytes.Value;
            if (b < 1024) return b.ToString("N0", PtBr) + " bytes";
            double valor = b;
            string[] unidades = { "KB", "MB", "GB", "TB" };
            var i = -1;
            do { valor /= 1024; i++; } while (valor >= 1024 && i < unidades.Length - 1);
            return valor.ToString(valor < 10 ? "N1" : "N0", PtBr) + " " + unidades[i];
        }

        public static string FormatarData(DateTime data)
        {
            return data.ToString("dd/MM/yyyy HH:mm", PtBr);
        }

        /// <summary>Transforma "Z:" em "Z:\", expande %variáveis% e devolve o caminho completo.</summary>
        public static string NormalizarEndereco(string texto)
        {
            var t = Environment.ExpandEnvironmentVariables((texto ?? "").Trim().Trim('"'));
            if (t.Length == 2 && t[1] == ':') t += "\\";
            return t.Length == 0 ? t : Path.GetFullPath(t);
        }

        private static string Normalizar(string caminho)
        {
            var completo = Path.GetFullPath(caminho);
            return completo.Length > 3 ? completo.TrimEnd('\\') : completo;
        }
    }
}
