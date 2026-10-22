using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using AbrirEfi.Particoes;

namespace AbrirEfi.EscolhaEfi
{
    /// <summary>
    /// Lista as partições EFI para o usuário escolher qual abrir.
    /// Já deixa selecionada a EFI que inicia o Windows. Clique duplo = Abrir.
    /// </summary>
    public sealed class FormEscolhaEfi : Form
    {
        private readonly IList<ParticaoEfi> _particoes;
        private readonly ListBox _lista;
        private readonly Button _abrir;

        /// <summary>Partição escolhida (null se cancelou).</summary>
        public ParticaoEfi Escolhida { get; private set; }

        /// <summary>Mostra a janela e devolve a escolhida, ou null se o usuário cancelar.</summary>
        public static ParticaoEfi Escolher(IList<ParticaoEfi> particoes)
        {
            using (var form = new FormEscolhaEfi(particoes))
            {
                return form.ShowDialog() == DialogResult.OK ? form.Escolhida : null;
            }
        }

        public FormEscolhaEfi(IList<ParticaoEfi> particoes)
        {
            _particoes = particoes;

            // Medidas abaixo são em 96 DPI; o WinForms escala conforme a tela.
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            Text = "Abrir EFI - escolha a partição";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            Font = new Font("Segoe UI", 10F);
            ClientSize = new Size(620, 280);
            Padding = new Padding(12);

            var titulo = new Label
            {
                Text = "Escolha qual partição EFI abrir:",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6),
            };

            _lista = new ListBox
            {
                Dock = DockStyle.Fill,
                HorizontalScrollbar = true,
                IntegralHeight = false,
            };
            foreach (var p in particoes) _lista.Items.Add(p.Descricao);
            _lista.DoubleClick += (s, e) => Confirmar();
            _lista.SelectedIndexChanged += (s, e) => _abrir.Enabled = _lista.SelectedIndex >= 0;

            _abrir = new Button { Text = "Abrir", AutoSize = true, MinimumSize = new Size(90, 32) };
            _abrir.Click += (s, e) => Confirmar();

            var cancelar = new Button
            {
                Text = "Cancelar",
                AutoSize = true,
                MinimumSize = new Size(90, 32),
                DialogResult = DialogResult.Cancel,
            };

            var botoes = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 10, 0, 0),
            };
            botoes.Controls.Add(cancelar);
            botoes.Controls.Add(_abrir);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(titulo, 0, 0);
            layout.Controls.Add(_lista, 0, 1);
            layout.Controls.Add(botoes, 0, 2);
            Controls.Add(layout);

            AcceptButton = _abrir;
            CancelButton = cancelar;

            SelecionarInicial();

            // Depois do UAC a janela pode nascer atrás de outras; traz para a frente.
            Shown += (s, e) => Activate();
        }

        private void SelecionarInicial()
        {
            if (_particoes.Count == 0) return;
            var indice = 0;
            for (var i = 0; i < _particoes.Count; i++)
            {
                if (_particoes[i].DoWindows) { indice = i; break; }
            }
            _lista.SelectedIndex = indice;
        }

        private void Confirmar()
        {
            var indice = _lista.SelectedIndex;
            if (indice < 0 || indice >= _particoes.Count) return;
            Escolhida = _particoes[indice];
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
