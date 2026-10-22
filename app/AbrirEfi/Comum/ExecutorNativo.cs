using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AbrirEfi.Comum
{
    /// <summary>
    /// Executa programas do Windows (mountvol.exe, diskpart.exe) sem janela, capturando a saída
    /// e o código de saída. Tudo é registrado no log. Toda chamada a .exe do app passa por aqui.
    /// </summary>
    public static class ExecutorNativo
    {
        /// <summary>Tempo máximo de espera por um programa (o diskpart pode demorar alguns segundos).</summary>
        private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(60);

        /// <summary>Caminho completo de um programa em System32 (evita pegar um .exe homônimo no PATH).</summary>
        public static string NoSystem32(string exe)
        {
            return Path.Combine(Environment.SystemDirectory, exe);
        }

        public static ResultadoNativo Executar(string exe, params string[] argumentos)
        {
            var linhaArgumentos = string.Join(" ", argumentos.Select(Aspas));
            var info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = linhaArgumentos,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                WorkingDirectory = Environment.SystemDirectory,
            };

            // mountvol e diskpart escrevem na code page OEM do console (ex.: 850 no Brasil).
            var codificacao = CodificacaoOem();
            info.StandardOutputEncoding = codificacao;
            info.StandardErrorEncoding = codificacao;

            var resultado = new ResultadoNativo();
            var nomeCurto = Path.GetFileName(exe);
            try
            {
                using (var processo = new Process { StartInfo = info })
                {
                    processo.Start();
                    // Nada a mandar pela entrada; fechar evita que o programa fique esperando.
                    processo.StandardInput.Close();

                    // Lê as duas saídas em paralelo para não travar se um dos buffers encher.
                    var tarefaSaida = processo.StandardOutput.ReadToEndAsync();
                    var tarefaErros = processo.StandardError.ReadToEndAsync();

                    if (!processo.WaitForExit((int)TempoLimite.TotalMilliseconds))
                    {
                        resultado.EstourouTempo = true;
                        try { processo.Kill(); } catch { /* já pode ter terminado */ }
                    }
                    processo.WaitForExit();

                    resultado.Saida = tarefaSaida.Result ?? "";
                    resultado.Erros = tarefaErros.Result ?? "";
                    resultado.Codigo = resultado.EstourouTempo ? -1 : processo.ExitCode;
                }
            }
            catch (Exception ex)
            {
                resultado.Codigo = -1;
                resultado.Erros = ex.Message;
                Log.Erro("executar " + nomeCurto, ex);
            }

            Log.Escrever("{0} {1} -> código {2}{3}: {4}",
                nomeCurto, linhaArgumentos, resultado.Codigo,
                resultado.EstourouTempo ? " (tempo esgotado)" : "",
                resultado.Resumo());
            return resultado;
        }

        /// <summary>
        /// Roda uma lista de comandos no diskpart por um arquivo de script temporário (/s),
        /// que é apagado no fim.
        /// </summary>
        public static ResultadoNativo ExecutarDiskpart(IEnumerable<string> comandos)
        {
            var lista = comandos.ToList();
            var arquivo = Path.Combine(Path.GetTempPath(), "abrir-efi-diskpart-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                File.WriteAllLines(arquivo, lista.Concat(new[] { "exit" }), Encoding.ASCII);
                Log.Escrever("diskpart [{0}]", string.Join("; ", lista));
                return Executar(NoSystem32("diskpart.exe"), "/s", arquivo);
            }
            catch (Exception ex)
            {
                Log.Erro("preparar script do diskpart", ex);
                return new ResultadoNativo { Codigo = -1, Erros = ex.Message };
            }
            finally
            {
                try { File.Delete(arquivo); } catch { /* arquivo temporário; não é crítico */ }
            }
        }

        /// <summary>Junta as linhas não vazias com " | ".</summary>
        public static string EmUmaLinha(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            var linhas = texto.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(l => l.Trim())
                              .Where(l => l.Length > 0);
            return string.Join(" | ", linhas);
        }

        private static Encoding CodificacaoOem()
        {
            try
            {
                return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
            }
            catch
            {
                return Encoding.Default;
            }
        }

        /// <summary>Põe aspas só quando o argumento tem espaço (ex.: caminho do script temporário).</summary>
        private static string Aspas(string argumento)
        {
            if (string.IsNullOrEmpty(argumento)) return "\"\"";
            if (argumento.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return argumento;
            // Barra invertida antes da aspa de fechamento precisaria ser dobrada; os nossos
            // argumentos com espaço são caminhos de arquivo, que não terminam em "\".
            return "\"" + argumento.Replace("\"", "\\\"") + "\"";
        }
    }
}
