using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using AbrirEfi.Comum;
using AbrirEfi.EscolhaEfi;
using AbrirEfi.Montagem;
using AbrirEfi.Navegador;
using AbrirEfi.Particoes;

namespace AbrirEfi
{
    /// <summary>
    /// Ponto de entrada: só orquestra o fluxo.
    /// listar EFIs → (escolher, se houver mais de uma) → montar → navegador → desmontar (se fomos nós que montamos).
    /// O UAC é pedido pelo app.manifest (requireAdministrator).
    /// </summary>
    internal static class Program
    {
        private const string TituloApp = "Abrir EFI";

        [STAThread]
        private static int Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Qualquer exceção não tratada vira mensagem + log, em vez da janela padrão do .NET.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => MostrarErroFatal(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex) MostrarErroFatal(ex);
            };

            Log.Escrever("--- Início ---");
            try
            {
                return Executar();
            }
            catch (Exception ex)
            {
                MostrarErroFatal(ex);
                return 1;
            }
            finally
            {
                Log.Escrever("--- Fim ---");
            }
        }

        private static int Executar()
        {
            List<ParticaoEfi> particoes = ServicoParticoes.Listar();
            Log.Escrever("Partições EFI encontradas: {0}", particoes.Count);
            foreach (var p in particoes)
                Log.Escrever("  {0} | disco={1} partição={2} volume={3}", p.Descricao, p.NumeroDisco, p.NumeroParticao, p.CaminhoVolume);

            if (particoes.Count == 0)
            {
                MostrarErro("Nenhuma partição EFI foi encontrada neste computador.\n\n" +
                            "(Só discos GPT são procurados.)");
                return 1;
            }

            var escolhida = particoes.Count == 1 ? particoes[0] : FormEscolhaEfi.Escolher(particoes);
            if (escolhida == null)
            {
                Log.Escrever("Cancelado pelo usuário.");
                return 0;
            }
            Log.Escrever("Escolhida: {0}", escolhida.Descricao);

            // "Montagem" aqui é o namespace AbrirEfi.Montagem; a classe tem o mesmo nome.
            Montagem.Montagem montagem = ServicoMontagem.Montar(escolhida);
            try
            {
                Log.Escrever("Abrindo o navegador em {0}", montagem.Raiz);
                using (var navegador = new FormNavegador(montagem))
                {
                    Application.Run(navegador);
                }
                Log.Escrever("Navegador fechado.");
            }
            finally
            {
                if (!ServicoMontagem.Desmontar(montagem))
                {
                    MostrarErro(string.Format(
                        "Não consegui remover a letra {0}: da EFI.\n\n" +
                        "Ela continua visível; reiniciar o Windows também remove.\n\nDetalhes no log:\n{1}",
                        montagem.Letra, Log.CaminhoArquivo));
                }
            }
            return 0;
        }

        private static int _jaMostrouErroFatal;

        private static void MostrarErroFatal(Exception ex)
        {
            Log.Erro("não tratado", ex);
            // Evita empilhar várias mensagens se o erro se repetir.
            if (Interlocked.Exchange(ref _jaMostrouErroFatal, 1) == 1) return;
            MostrarErro(string.Format("Não deu para abrir a EFI.\n\n{0}\n\nDetalhes no log:\n{1}",
                                      ex.Message, Log.CaminhoArquivo));
        }

        private static void MostrarErro(string mensagem)
        {
            try
            {
                MessageBox.Show(mensagem, TituloApp, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // Sem interface disponível: o log já registrou.
            }
        }
    }
}
