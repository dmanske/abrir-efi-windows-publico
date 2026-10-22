using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AbrirEfi.Comum;

namespace AbrirEfi.Navegador
{
    public sealed partial class FormNavegador
    {
        /// <summary>
        /// Mostra o conteúdo de <paramref name="pasta"/>. Devolve false se não deu para abrir.
        /// <paramref name="selecionar"/>: nomes a selecionar depois de listar (ex.: o que acabou de ser colado).
        /// </summary>
        private bool Navegar(string pasta, bool registrarHistorico = true, ICollection<string> selecionar = null)
        {
            List<ItemArquivo> itens;
            try
            {
                itens = ServicoArquivos.Listar(pasta);
            }
            catch (Exception ex)
            {
                TratarExcecao("abrir a pasta " + pasta, ex);
                _endereco.Text = _pastaAtual ?? "";
                return false;
            }

            if (registrarHistorico && _pastaAtual != null && !ServicoArquivos.MesmoCaminho(_pastaAtual, pasta))
                _historico.Push(_pastaAtual);

            _pastaAtual = pasta;
            _endereco.Text = pasta;
            Preencher(itens, selecionar);
            AtualizarBotoes();
            return true;
        }

        private void Preencher(List<ItemArquivo> itens, ICollection<string> selecionar)
        {
            _lista.BeginUpdate();
            try
            {
                _lista.Items.Clear();
                ListViewItem primeiroSelecionado = null;
                foreach (var item in itens)
                {
                    _icones.Preencher(item);
                    var linha = new ListViewItem(item.Nome) { Tag = item, ImageIndex = item.IndiceIcone };
                    linha.SubItems.Add(ServicoArquivos.FormatarTamanho(item.Tamanho));
                    linha.SubItems.Add(ServicoArquivos.FormatarData(item.Modificado));
                    linha.SubItems.Add(item.Tipo);
                    if (item.Oculto) linha.ForeColor = System.Drawing.SystemColors.GrayText;
                    if (_recortados.Contains(item.CaminhoCompleto)) linha.ForeColor = System.Drawing.SystemColors.GrayText;
                    _lista.Items.Add(linha);

                    if (selecionar != null && selecionar.Contains(item.Nome, StringComparer.OrdinalIgnoreCase))
                    {
                        linha.Selected = true;
                        if (primeiroSelecionado == null) primeiroSelecionado = linha;
                    }
                }
                if (primeiroSelecionado != null) primeiroSelecionado.EnsureVisible();
            }
            finally
            {
                _lista.EndUpdate();
            }
            AtualizarStatus();
        }

        private void Voltar()
        {
            while (_historico.Count > 0)
            {
                var anterior = _historico.Pop();
                if (Navegar(anterior, registrarHistorico: false)) break;
            }
            AtualizarBotoes();
        }

        private void Subir()
        {
            if (_pastaAtual == null) return;
            var acima = ServicoArquivos.PastaAcima(_pastaAtual);
            if (acima != null) Navegar(acima);
        }

        private void Atualizar()
        {
            if (_pastaAtual != null) Navegar(_pastaAtual, registrarHistorico: false);
        }

        private void IrParaEndereco()
        {
            string destino;
            try
            {
                destino = ServicoArquivos.NormalizarEndereco(_endereco.Text);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                MostrarErro("Endereço inválido:\n" + _endereco.Text);
                _endereco.Text = _pastaAtual ?? "";
                return;
            }
            if (destino.Length == 0) return;

            if (Directory.Exists(destino))
            {
                Navegar(destino);
                _lista.Focus();
            }
            else if (File.Exists(destino))
            {
                AbrirArquivo(destino);
            }
            else
            {
                MostrarErro("Endereço não encontrado:\n" + destino);
                _endereco.Text = _pastaAtual ?? "";
            }
        }

        // ---------------- Tela ----------------

        private void AtualizarBotoes()
        {
            _btnVoltar.Enabled = _historico.Count > 0;
            _btnSubir.Enabled = _pastaAtual != null && ServicoArquivos.PastaAcima(_pastaAtual) != null;
            _btnColar.Enabled = AreaTransferenciaTemArquivos();
            Text = "Abrir EFI (Administrador) — " + _pastaAtual;
            AtualizarEspacoLivre();
        }

        private void AtualizarStatus()
        {
            var total = _lista.Items.Count;
            var selecionados = _lista.SelectedItems.Count;
            var texto = total == 1 ? "1 item" : total + " itens";
            if (selecionados > 0)
                texto += selecionados == 1 ? " — 1 selecionado" : " — " + selecionados + " selecionados";
            _statusItens.Text = texto;
        }

        private void AtualizarEspacoLivre()
        {
            try
            {
                var raiz = Path.GetPathRoot(_pastaAtual ?? _raiz);
                var unidade = new DriveInfo(raiz);
                _statusLivre.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, "Livre em {0} {1} de {2}", raiz.TrimEnd('\\'),
                    ServicoArquivos.FormatarTamanho(unidade.AvailableFreeSpace),
                    ServicoArquivos.FormatarTamanho(unidade.TotalSize));
            }
            catch (Exception)
            {
                _statusLivre.Text = ""; // ex.: caminho de rede; o espaço livre é só informativo
            }
        }

        private List<ItemArquivo> Selecionados()
        {
            return _lista.SelectedItems.Cast<ListViewItem>().Select(i => i.Tag as ItemArquivo).Where(i => i != null).ToList();
        }
    }
}
