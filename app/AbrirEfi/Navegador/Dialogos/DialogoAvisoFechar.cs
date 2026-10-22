using System.Drawing;
using System.Windows.Forms;

namespace AbrirEfi.Navegador.Dialogos
{
    /// <summary>Aviso ao fechar: Sim/Não, com a opção "Não perguntar de novo".</summary>
    public sealed class DialogoAvisoFechar : Form
    {
        private readonly CheckBox _naoPerguntar;

        public bool NaoPerguntarDeNovo => _naoPerguntar.Checked;

        public DialogoAvisoFechar(string mensagem)
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Abrir EFI";
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 190);

            var texto = new Label { Text = mensagem, Left = 16, Top = 16, Width = 428, Height = 100, AutoSize = false };
            _naoPerguntar = new CheckBox { Text = "Não perguntar de novo", Left = 16, Top = 124, Width = 428, AutoSize = true };
            var sim = new Button { Text = "&Sim, fechar", DialogResult = DialogResult.Yes, Left = 232, Top = 152, Width = 100, Height = 28 };
            var nao = new Button { Text = "&Não", DialogResult = DialogResult.No, Left = 344, Top = 152, Width = 100, Height = 28 };
            Controls.AddRange(new Control[] { texto, _naoPerguntar, sim, nao });
            AcceptButton = nao;   // Enter = continuar com a janela aberta (o lado seguro)
            CancelButton = nao;
        }
    }
}
