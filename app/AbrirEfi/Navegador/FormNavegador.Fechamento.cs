using System.Windows.Forms;
using AbrirEfi.Comum;
using AbrirEfi.Navegador.Dialogos;

namespace AbrirEfi.Navegador
{
    public sealed partial class FormNavegador
    {
        private void AoFechar(object sender, FormClosingEventArgs e)
        {
            if (_operacaoEmAndamento)
            {
                e.Cancel = true;
                MostrarAviso("Há uma operação de arquivos em andamento. Espere terminar (ou cancele na janela de progresso) antes de fechar.");
                return;
            }

            if (Configuracao.NaoPerguntarAoFechar) return;

            var aviso = _montagem.MontadaPeloApp
                ? "Ao fechar, a letra " + _montagem.Letra + ": da EFI será removida.\n\n" +
                  "Se algum arquivo da EFI estiver aberto em outro programa (ProperTree, Bloco de Notas...), " +
                  "salve e feche antes, senão a alteração pode se perder.\n\nFechar agora?"
                : "A EFI já estava montada antes de abrir o app, então a letra " + _montagem.Letra + ": será mantida.\n\n" +
                  "Se algum arquivo da EFI estiver aberto em outro programa, salve antes de continuar.\n\nFechar agora?";

            using (var dialogo = new DialogoAvisoFechar(aviso))
            {
                var resposta = dialogo.ShowDialog(this);
                if (dialogo.NaoPerguntarDeNovo && resposta == DialogResult.Yes)
                    Configuracao.NaoPerguntarAoFechar = true;
                if (resposta != DialogResult.Yes) e.Cancel = true;
            }
        }
    }
}
