using System;
using System.Drawing;
using System.Windows.Forms;
using AbrirEfi.Comum;
using AbrirEfi.Comum.Nativo;
using AbrirEfi.Navegador.Arrastar;

namespace AbrirEfi.Navegador.Controles
{
    /// <summary>
    /// ListView da lista de arquivos, com três diferenças do padrão:
    /// 1) usa a image list pequena do sistema para os ícones (LVM_SETIMAGELIST);
    /// 2) aceita arquivos arrastados de programas NÃO elevados (Explorer) via WM_DROPFILES
    ///    (o OLE do WinForms é bloqueado pelo UIPI; AllowDrop fica false de propósito);
    /// 3) visual "Explorer" e sem tremulação.
    /// </summary>
    public sealed class ListaArquivos : ListView
    {
        private IntPtr _imageListSistema;

        public ListaArquivos()
        {
            DoubleBuffered = true;
            AllowDrop = false;
        }

        /// <summary>Disparado (fora do loop do arrastar) quando arquivos são soltos na lista.</summary>
        public event EventHandler<ArquivosSoltosEventArgs> ArquivosSoltos;

        /// <summary>Image list do sistema (HIMAGELIST). Pode ser definida antes ou depois de o handle existir.</summary>
        public IntPtr ImageListSistema
        {
            get { return _imageListSistema; }
            set
            {
                _imageListSistema = value;
                AplicarImageList();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AplicarImageList();
            try { var hr = NativoJanelas.SetWindowTheme(Handle, "Explorer", null); if (hr != 0) Log.Escrever("SetWindowTheme devolveu 0x{0:X}", hr); } catch (Exception ex) { Log.Erro("SetWindowTheme", ex); }

            // Aceitar arrastar do Explorer comum: precisa liberar as mensagens no UIPI e declarar que aceita arquivos.
            NativoUipi.LiberarMensagensDeArrasto(Handle, "ListView");
            NativoArrastar.DragAcceptFiles(Handle, true);
        }

        private void AplicarImageList()
        {
            if (_imageListSistema != IntPtr.Zero && IsHandleCreated)
                NativoJanelas.SendMessage(Handle, NativoIcones.LVM_SETIMAGELIST, (IntPtr)NativoIcones.LVSIL_SMALL, _imageListSistema);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativoJanelas.WM_DROPFILES)
            {
                Point ponto;
                var caminhos = LeitorDeArrasto.LerELiberar(m.WParam, out ponto);
                var mover = (ModifierKeys & Keys.Shift) == Keys.Shift;
                m.Result = IntPtr.Zero;
                Log.Escrever("WM_DROPFILES na lista: {0} item(ns), mover={1}", caminhos.Length, mover);

                // Processa depois de devolver o controle: o Explorer (origem) fica esperando o fim do arrastar.
                var handler = ArquivosSoltos;
                if (handler != null && caminhos.Length > 0)
                    BeginInvoke((Action)(() => handler(this, new ArquivosSoltosEventArgs(caminhos, ponto, mover))));
                return;
            }
            base.WndProc(ref m);
        }
    }
}
