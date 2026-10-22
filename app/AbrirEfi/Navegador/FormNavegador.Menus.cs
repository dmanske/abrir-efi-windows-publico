using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AbrirEfi.Comum;
using AbrirEfi.Navegador.Operacoes;
using AbrirEfi.Navegador.Shell;

namespace AbrirEfi.Navegador
{
    public sealed partial class FormNavegador
    {
        private Point _posicaoMenu;

        private void AtualizarMenuContexto()
        {
            _posicaoMenu = Cursor.Position;
            var itens = Selecionados();
            var umItem = itens.Count == 1;
            var temSelecao = itens.Count > 0;

            _mAbrir.Enabled = temSelecao;
            _mAbrirCom.Visible = umItem && !itens[0].EhPasta;
            _mRecortar.Enabled = temSelecao;
            _mCopiar.Enabled = temSelecao;
            _mColar.Enabled = AreaTransferenciaTemArquivos();
            _mRenomear.Enabled = umItem;
            _mExcluir.Enabled = temSelecao;
        }

        /// <summary>Menu de contexto completo do Windows para a seleção (ou para a pasta atual, se nada está selecionado).</summary>
        private void MostrarMenuDoWindows()
        {
            var caminhos = Selecionados().Select(i => i.CaminhoCompleto).ToList();
            if (caminhos.Count == 0 && _pastaAtual != null) caminhos.Add(_pastaAtual);
            try
            {
                var ok = MenuContextoShell.Mostrar(this, caminhos, _posicaoMenu, (ModifierKeys & Keys.Shift) == Keys.Shift);
                if (!ok) MostrarErro("Não consegui abrir o menu do Windows para este item.\nDetalhes no log:\n" + Log.CaminhoArquivo);
                Atualizar();
            }
            catch (Exception ex)
            {
                TratarExcecao("abrir o menu do Windows", ex);
            }
        }

        /// <summary>cmd.exe já na pasta atual; herda o administrador deste app.</summary>
        private void AbrirPrompt()
        {
            try
            {
                Log.Escrever("Abrindo o Prompt em {0}", _pastaAtual);
                using (Process.Start(new ProcessStartInfo
                {
                    FileName = ExecutorNativo.NoSystem32("cmd.exe"),
                    Arguments = "/k title Prompt na EFI (Administrador)",
                    WorkingDirectory = _pastaAtual ?? _raiz,
                    UseShellExecute = false,
                })) { }
            }
            catch (Exception ex)
            {
                TratarExcecao("abrir o Prompt de Comando", ex);
            }
        }

        private void CriarAtalho()
        {
            try
            {
                var caminho = AtalhoAreaDeTrabalho.Criar();
                Log.Escrever("Atalho criado em {0}", caminho);
                MostrarAviso("Atalho criado na Área de Trabalho:\n" + caminho);
            }
            catch (Exception ex)
            {
                TratarExcecao("criar o atalho", ex);
            }
        }

        private void AbrirLog()
        {
            try { ServicoArquivos.AbrirArquivo(Log.CaminhoArquivo); }
            catch (Exception ex) { TratarExcecao("abrir o log", ex); }
        }
    }
}
