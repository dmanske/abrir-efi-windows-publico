using System;
using System.Drawing;
using System.Windows.Forms;
using AbrirEfi.Navegador.Controles;

namespace AbrirEfi.Navegador
{
    public sealed partial class FormNavegador
    {
        private ToolStrip _barra;
        private ToolStripButton _btnVoltar, _btnSubir, _btnAtualizar, _btnNovaPasta, _btnColar, _btnPrompt;
        private TextBox _endereco;
        private Button _ir;
        private ListaArquivos _lista;
        private ToolStripStatusLabel _statusItens, _statusLivre, _statusLetra;
        private readonly ToolTip _dicas = new ToolTip();

        private ContextMenuStrip _menuContexto;
        private ToolStripMenuItem _mAbrir, _mAbrirCom, _mRecortar, _mCopiar, _mColar, _mRenomear, _mExcluir, _mNovaPasta,
                                  _mCopiarCaminho, _mAtualizar, _mMaisOpcoes;

        private void ConstruirTela()
        {
            // ---- Barra de ferramentas ----
            _barra = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top, RenderMode = ToolStripRenderMode.System };
            _btnVoltar = Botao("← Voltar", "Voltar para a pasta anterior (Alt+←)", Voltar);
            _btnSubir = Botao("↑ Subir", "Ir para a pasta de cima (Backspace)", Subir);
            _btnAtualizar = Botao("⟳ Atualizar", "Recarregar a pasta (F5)", Atualizar);
            _btnNovaPasta = Botao("Nova pasta", "Criar uma pasta (Ctrl+Shift+N)", NovaPasta);
            _btnColar = Botao("Colar", "Colar arquivos copiados ou recortados (Ctrl+V)", Colar);
            _btnPrompt = Botao("Prompt (admin)", "Abrir o Prompt de Comando como administrador nesta pasta", AbrirPrompt);

            var mais = new ToolStripDropDownButton("Mais") { ToolTipText = "Outras opções" };
            mais.DropDownItems.Add("Criar atalho na Área de Trabalho", null, (s, e) => CriarAtalho());
            mais.DropDownItems.Add("Abrir o arquivo de log", null, (s, e) => AbrirLog());

            _barra.Items.AddRange(new ToolStripItem[]
            {
                _btnVoltar, _btnSubir, _btnAtualizar, new ToolStripSeparator(),
                _btnNovaPasta, _btnColar, new ToolStripSeparator(),
                _btnPrompt, mais,
            });

            // ---- Endereço ----
            _endereco = new TextBox { Dock = DockStyle.Fill };
            _endereco.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    IrParaEndereco();
                }
            };
            _ir = new Button { Text = "Ir", Dock = DockStyle.Right, AutoSize = true, UseVisualStyleBackColor = true };
            _ir.Click += (s, e) => IrParaEndereco();
            _dicas.SetToolTip(_endereco, "Endereço (Ctrl+L). Pode digitar qualquer pasta, como C:\\");

            var painelEndereco = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(6, 4, 6, 2) };
            painelEndereco.Controls.Add(_endereco);
            painelEndereco.Controls.Add(_ir);

            // ---- Lista ----
            _lista = new ListaArquivos
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = true,
                AllowColumnReorder = true,
                LabelEdit = true,
            };
            _lista.Columns.Add("Nome", 360);
            _lista.Columns.Add("Tamanho", 100, HorizontalAlignment.Right);
            _lista.Columns.Add("Data de modificação", 140);
            _lista.Columns.Add("Tipo", 180);
            _lista.ImageListSistema = _icones.ListaSistemaPequena;
            _lista.ItemActivate += (s, e) => AbrirSelecionados();
            _lista.SelectedIndexChanged += (s, e) => AtualizarStatus();
            _lista.BeforeLabelEdit += (s, e) => _editandoNome = true;
            _lista.AfterLabelEdit += AoTerminarEdicaoNome;
            _lista.ItemDrag += AoArrastarParaFora;
            _lista.ArquivosSoltos += AoSoltarArquivos;

            ConstruirMenuContexto();
            _lista.ContextMenuStrip = _menuContexto;

            // ---- Barra de status ----
            _statusItens = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            _statusLivre = new ToolStripStatusLabel { BorderSides = ToolStripStatusLabelBorderSides.Left };
            _statusLetra = new ToolStripStatusLabel
            {
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                Text = "EFI montada em " + _montagem.Letra + ":" + (_montagem.MontadaPeloApp ? "" : " (já estava montada)"),
            };
            var barraStatus = new StatusStrip();
            barraStatus.Items.AddRange(new ToolStripItem[] { _statusItens, _statusLivre, _statusLetra });

            // Ordem no Dock: o último adicionado fica mais externo. Fill primeiro; depois endereço, ferramentas, status.
            Controls.Add(_lista);
            Controls.Add(painelEndereco);
            Controls.Add(_barra);
            Controls.Add(barraStatus);
        }

        private static ToolStripButton Botao(string texto, string dica, Action acao)
        {
            var botao = new ToolStripButton(texto) { DisplayStyle = ToolStripItemDisplayStyle.Text, ToolTipText = dica };
            botao.Click += (s, e) => acao();
            return botao;
        }

        private void ConstruirMenuContexto()
        {
            _menuContexto = new ContextMenuStrip();
            _mAbrir = Item("Abrir", AbrirSelecionados, Keys.Enter);
            _mAbrirCom = Item("Abrir com…", AbrirComSelecionado);
            _mRecortar = Item("Recortar", () => CopiarOuRecortar(true), Keys.Control | Keys.X);
            _mCopiar = Item("Copiar", () => CopiarOuRecortar(false), Keys.Control | Keys.C);
            _mColar = Item("Colar", Colar, Keys.Control | Keys.V);
            _mRenomear = Item("Renomear", Renomear, Keys.F2);
            _mExcluir = Item("Excluir", ExcluirSelecionados, Keys.Delete);
            _mNovaPasta = Item("Nova pasta", NovaPasta, Keys.Control | Keys.Shift | Keys.N);
            _mCopiarCaminho = Item("Copiar caminho", CopiarCaminho);
            _mAtualizar = Item("Atualizar", Atualizar, Keys.F5);
            _mMaisOpcoes = Item("Mais opções do Windows…", MostrarMenuDoWindows);

            _menuContexto.Items.AddRange(new ToolStripItem[]
            {
                _mAbrir, _mAbrirCom, new ToolStripSeparator(),
                _mRecortar, _mCopiar, _mColar, new ToolStripSeparator(),
                _mRenomear, _mExcluir, new ToolStripSeparator(),
                _mNovaPasta, _mCopiarCaminho, _mAtualizar, new ToolStripSeparator(),
                _mMaisOpcoes,
            });
            _menuContexto.Opening += (s, e) => AtualizarMenuContexto();
        }

        // Só exibe o texto do atalho no menu; as teclas de verdade são tratadas em ProcessCmdKey (sem duplicar).
        private static ToolStripMenuItem Item(string texto, Action acao, Keys atalho = Keys.None)
        {
            var item = new ToolStripMenuItem(texto);
            if (atalho != Keys.None) item.ShortcutKeyDisplayString = DescreverAtalho(atalho);
            item.Click += (s, e) => acao();
            return item;
        }

        private static string DescreverAtalho(Keys atalho)
        {
            var texto = "";
            if ((atalho & Keys.Control) != 0) texto += "Ctrl+";
            if ((atalho & Keys.Shift) != 0) texto += "Shift+";
            var tecla = atalho & Keys.KeyCode;
            return texto + (tecla == Keys.Delete ? "Del" : tecla.ToString());
        }
    }
}
