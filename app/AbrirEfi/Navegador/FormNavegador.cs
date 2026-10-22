using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using AbrirEfi.Comum;
using AbrirEfi.Comum.Nativo;
using AbrirEfi.Navegador.Arrastar;
using AbrirEfi.Navegador.Controles;
using AbrirEfi.Navegador.Icones;
using AbrirEfi.Navegador.Shell;
using MontagemEfi = AbrirEfi.Montagem.Montagem;

namespace AbrirEfi.Navegador
{
    /// <summary>
    /// Navegador de arquivos próprio do app, rodando como administrador dentro da EFI
    /// (o Explorer do Windows se recusa a abrir a partição EFI).
    /// A classe é dividida em arquivos por responsabilidade:
    /// FormNavegador.cs (campos e ciclo de vida), .Construcao.cs (montagem da tela), .Navegacao.cs,
    /// .Arquivos.cs (copiar/colar/excluir/renomear...), .Menus.cs (menu de contexto, prompt, atalho),
    /// .Atalhos.cs (teclado) e .Fechamento.cs.
    /// </summary>
    public sealed partial class FormNavegador : Form
    {
        private readonly MontagemEfi _montagem;
        private readonly string _raiz;
        private readonly Stack<string> _historico = new Stack<string>();
        private readonly ProvedorIcones _icones = new ProvedorIcones();
        private readonly HashSet<string> _recortados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private string _pastaAtual;
        private bool _operacaoEmAndamento;
        private bool _editandoNome;

        public FormNavegador(MontagemEfi montagem)
        {
            _montagem = montagem;
            _raiz = montagem.Raiz;

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Text = "Abrir EFI (Administrador) — " + _raiz;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(960, 600);
            MinimumSize = new Size(560, 340);
            KeyPreview = true;

            ConstruirTela();

            Load += (s, e) =>
            {
                AjustarColunasAoDpi(96, DeviceDpi);
                Navegar(_raiz, registrarHistorico: false);
            };
            DpiChanged += (s, e) => AjustarColunasAoDpi(e.DeviceDpiOld, e.DeviceDpiNew);
            Shown += (s, e) => { Activate(); _lista.Focus(); };
            FormClosing += AoFechar;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Mesma liberação do UIPI na janela principal (o arrastar passa por ela antes de chegar na lista).
            NativoUipi.LiberarMensagensDeArrasto(Handle, "Form");
        }

        protected override void WndProc(ref Message m)
        {
            // Enquanto o menu do shell está aberto, ele precisa destas mensagens para desenhar submenus e itens.
            if (MenuContextoShell.TentarTratar(ref m)) return;
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dicas.Dispose();
                _menuContexto.Dispose();
            }
            base.Dispose(disposing);
        }

        private void AjustarColunasAoDpi(int dpiAntigo, int dpiNovo)
        {
            if (dpiAntigo <= 0 || dpiNovo <= 0 || dpiAntigo == dpiNovo) return;
            foreach (ColumnHeader coluna in _lista.Columns)
                coluna.Width = coluna.Width * dpiNovo / dpiAntigo;
        }

        private void MostrarErro(string mensagem)
        {
            MessageBox.Show(this, mensagem, "Abrir EFI", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void MostrarAviso(string mensagem)
        {
            MessageBox.Show(this, mensagem, "Abrir EFI", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>Registra a exceção e mostra uma mensagem amigável. Nunca relança: o app não cai por erro de arquivo.</summary>
        private void TratarExcecao(string contexto, Exception ex)
        {
            Log.Erro(contexto, ex);
            string explicacao;
            if (ex is UnauthorizedAccessException) explicacao = "O Windows negou o acesso (arquivo somente leitura, em uso ou protegido).";
            else if (ex is System.IO.PathTooLongException) explicacao = "O caminho é longo demais.";
            else if (ex is System.IO.FileNotFoundException || ex is System.IO.DirectoryNotFoundException) explicacao = "O arquivo ou a pasta não existe mais. Atualize a lista (F5).";
            else if (ex is System.IO.IOException) explicacao = "Erro de leitura/gravação (o arquivo pode estar aberto em outro programa).";
            else explicacao = "Erro inesperado.";
            MostrarErro("Não deu para " + contexto + ".\n\n" + explicacao + "\n\nDetalhe: " + ex.Message + "\n\nO registro está em:\n" + Log.CaminhoArquivo);
        }
    }
}
