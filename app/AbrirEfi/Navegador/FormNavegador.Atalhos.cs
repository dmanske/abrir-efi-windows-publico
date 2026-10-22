using System.Windows.Forms;

namespace AbrirEfi.Navegador
{
    public sealed partial class FormNavegador
    {
        /// <summary>
        /// Atalhos de teclado. Enter (abrir) é tratado pelo próprio ListView via ItemActivate.
        /// Os atalhos da lista só valem com a lista em foco e fora da edição de nome, para não roubar
        /// Ctrl+C/Ctrl+V/Delete/Backspace da caixa de endereço ou do campo de renomear.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_editandoNome) return base.ProcessCmdKey(ref msg, keyData);

            switch (keyData)
            {
                case Keys.F5:
                    Atualizar();
                    return true;
                case Keys.Alt | Keys.Left:
                    Voltar();
                    return true;
                case Keys.Control | Keys.L:
                case Keys.Alt | Keys.D:
                    _endereco.Focus();
                    _endereco.SelectAll();
                    return true;
                case Keys.Control | Keys.Shift | Keys.N:
                    NovaPasta();
                    return true;
            }

            if (_lista.Focused)
            {
                switch (keyData)
                {
                    case Keys.Back:
                        Subir();
                        return true;
                    case Keys.F2:
                        Renomear();
                        return true;
                    case Keys.Delete:
                    case Keys.Shift | Keys.Delete:
                        ExcluirSelecionados();
                        return true;
                    case Keys.Control | Keys.C:
                        CopiarOuRecortar(false);
                        return true;
                    case Keys.Control | Keys.X:
                        CopiarOuRecortar(true);
                        return true;
                    case Keys.Control | Keys.V:
                        Colar();
                        return true;
                    case Keys.Control | Keys.A:
                        foreach (ListViewItem item in _lista.Items) item.Selected = true;
                        return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
