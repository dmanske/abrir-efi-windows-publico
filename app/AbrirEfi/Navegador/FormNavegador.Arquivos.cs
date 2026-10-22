using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AbrirEfi.Comum;
using AbrirEfi.Navegador.Arrastar;
using AbrirEfi.Navegador.Operacoes;

namespace AbrirEfi.Navegador
{
    public sealed partial class FormNavegador
    {
        // ---------------- Abrir ----------------

        private void AbrirSelecionados()
        {
            var itens = Selecionados();
            if (itens.Count == 0) return;
            if (itens[0].EhPasta) { Navegar(itens[0].CaminhoCompleto); return; }
            foreach (var arquivo in itens.Where(i => !i.EhPasta)) AbrirArquivo(arquivo.CaminhoCompleto);
        }

        private void AbrirArquivo(string caminho)
        {
            try
            {
                Log.Escrever("Abrindo arquivo {0}", caminho);
                ServicoArquivos.AbrirArquivo(caminho);
            }
            catch (Exception ex)
            {
                TratarExcecao("abrir o arquivo " + caminho, ex);
            }
        }

        private void AbrirComSelecionado()
        {
            var itens = Selecionados();
            if (itens.Count != 1 || itens[0].EhPasta) return;
            try { AbrirCom.Escolher(Handle, itens[0].CaminhoCompleto); }
            catch (Exception ex) { TratarExcecao("abrir com outro programa", ex); }
        }

        // ---------------- Copiar / recortar / colar ----------------

        private void CopiarOuRecortar(bool recortar)
        {
            var itens = Selecionados();
            if (itens.Count == 0) return;
            try
            {
                AreaTransferenciaArquivos.Colocar(itens.Select(i => i.CaminhoCompleto), recortar);
                _recortados.Clear();
                if (recortar) foreach (var i in itens) _recortados.Add(i.CaminhoCompleto);
                Atualizar(); // reaplica o cinza nos recortados
            }
            catch (Exception ex)
            {
                TratarExcecao(recortar ? "recortar" : "copiar", ex);
            }
        }

        private void Colar()
        {
            ConteudoArquivos conteudo;
            try { conteudo = AreaTransferenciaArquivos.Ler(); }
            catch (Exception ex) { TratarExcecao("ler a área de transferência", ex); return; }
            if (conteudo == null) return;

            ReceberArquivos(conteudo.Caminhos, _pastaAtual, conteudo.Mover, "colar");
            if (conteudo.Mover) { _recortados.Clear(); AreaTransferenciaArquivos.Limpar(); }
        }

        private static bool AreaTransferenciaTemArquivos()
        {
            return AreaTransferenciaArquivos.TemArquivos();
        }

        /// <summary>Copia ou move arquivos para uma pasta, com a janela de progresso do Windows. Serve para colar e para soltar.</summary>
        private void ReceberArquivos(string[] origens, string destino, bool mover, string nomeAcao)
        {
            if (_pastaAtual == null || origens == null || origens.Length == 0) return;

            // Mover para a pasta onde já estão não faz nada.
            var lista = mover
                ? origens.Where(o => !ServicoArquivos.MesmoCaminho(Path.GetDirectoryName(o) ?? "", destino)).ToArray()
                : origens;
            if (lista.Length == 0) return;

            // Copiar para a mesma pasta cria "arquivo - Copia" em vez de perguntar se quer substituir ele mesmo.
            var naMesmaPasta = lista.Any(o => ServicoArquivos.MesmoCaminho(Path.GetDirectoryName(o) ?? "", destino));

            ExecutarOperacao(nomeAcao, () => mover
                ? OperacaoShell.Mover(Handle, lista, destino)
                : OperacaoShell.Copiar(Handle, lista, destino, naMesmaPasta));

            if (ServicoArquivos.MesmoCaminho(_pastaAtual, destino))
                Navegar(_pastaAtual, registrarHistorico: false, selecionar: lista.Select(Path.GetFileName).ToList());
            else
                Atualizar();
        }

        /// <summary>Executa uma operação protegida contra reentrada (e que bloqueia o fechamento da janela).</summary>
        private void ExecutarOperacao(string nome, Func<ResultadoOperacao> acao)
        {
            if (_operacaoEmAndamento) return;
            _operacaoEmAndamento = true;
            try
            {
                var r = acao();
                if (!r.Sucesso && !r.Cancelada) MostrarErro(r.Mensagem ?? ("Não deu para " + nome + "."));
            }
            catch (Exception ex)
            {
                TratarExcecao(nome, ex);
            }
            finally
            {
                _operacaoEmAndamento = false;
            }
        }

        // ---------------- Excluir ----------------

        private void ExcluirSelecionados()
        {
            var itens = Selecionados();
            if (itens.Count == 0) return;

            var o = itens.Count == 1 ? "\"" + itens[0].Nome + "\"" : itens.Count + " itens";
            var resposta = MessageBox.Show(this,
                "Excluir " + o + " definitivamente?\n\n" +
                "A EFI não tem lixeira: não dá para desfazer." +
                (itens.Any(i => i.EhPasta) ? "\nO conteúdo das pastas também será apagado." : ""),
                "Excluir definitivamente", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (resposta != DialogResult.Yes) return;

            ExecutarOperacao("excluir", () => OperacaoShell.ExcluirDefinitivo(Handle, itens.Select(i => i.CaminhoCompleto)));
            Atualizar();
        }

        // ---------------- Renomear / nova pasta ----------------

        private void Renomear()
        {
            if (_lista.SelectedItems.Count != 1) return;
            _lista.SelectedItems[0].BeginEdit();
        }

        private void AoTerminarEdicaoNome(object sender, LabelEditEventArgs e)
        {
            _editandoNome = false;
            var item = e.Item >= 0 && e.Item < _lista.Items.Count ? _lista.Items[e.Item].Tag as ItemArquivo : null;
            if (e.Label == null || item == null) return; // o usuário cancelou (Esc)

            e.CancelEdit = true; // a lista é recarregada por nós em seguida; não deixa o ListView mexer no texto
            var novoNome = e.Label;
            if (novoNome == item.Nome) return;

            var erro = ValidadorNome.Validar(novoNome);
            if (erro != null) { MostrarErro(erro); return; }

            var novoCaminho = Path.Combine(Path.GetDirectoryName(item.CaminhoCompleto) ?? "", novoNome);
            var soMudouMaiusculas = string.Equals(novoNome, item.Nome, StringComparison.OrdinalIgnoreCase);
            if (!soMudouMaiusculas && (File.Exists(novoCaminho) || Directory.Exists(novoCaminho)))
            {
                MostrarErro("Já existe um item chamado \"" + novoNome + "\" nesta pasta.");
                return;
            }

            try
            {
                Log.Escrever("Renomear {0} → {1}", item.CaminhoCompleto, novoNome);
                if (item.EhPasta) Directory.Move(item.CaminhoCompleto, novoCaminho);
                else File.Move(item.CaminhoCompleto, novoCaminho);
            }
            catch (Exception ex)
            {
                TratarExcecao("renomear", ex);
                return;
            }
            // Recarrega fora do evento (mexer na lista dentro do AfterLabelEdit é arriscado).
            BeginInvoke((Action)(() => Navegar(_pastaAtual, registrarHistorico: false, selecionar: new[] { novoNome })));
        }

        private void NovaPasta()
        {
            if (_pastaAtual == null) return;
            string nome;
            try
            {
                nome = ValidadorNome.NomeLivre(_pastaAtual, "Nova pasta");
                Directory.CreateDirectory(Path.Combine(_pastaAtual, nome));
                Log.Escrever("Nova pasta: {0}", Path.Combine(_pastaAtual, nome));
            }
            catch (Exception ex)
            {
                TratarExcecao("criar a pasta", ex);
                return;
            }

            Navegar(_pastaAtual, registrarHistorico: false, selecionar: new[] { nome });
            _lista.Focus();
            if (_lista.SelectedItems.Count == 1) _lista.SelectedItems[0].BeginEdit(); // já entra no modo de digitar o nome
        }

        private void CopiarCaminho()
        {
            var itens = Selecionados();
            var caminhos = itens.Count > 0 ? itens.Select(i => i.CaminhoCompleto) : new[] { _pastaAtual };
            try { Clipboard.SetText(string.Join(Environment.NewLine, caminhos)); }
            catch (Exception ex) { TratarExcecao("copiar o caminho", ex); }
        }

        // ---------------- Arrastar ----------------

        /// <summary>De dentro para fora (para o Explorer ou outro programa): elevado → comum é permitido.</summary>
        private void AoArrastarParaFora(object sender, ItemDragEventArgs e)
        {
            var caminhos = Selecionados().Select(i => i.CaminhoCompleto).ToArray();
            if (caminhos.Length == 0) return;
            try
            {
                var dados = new DataObject(DataFormats.FileDrop, caminhos);
                // Quem recebe faz a cópia/movimentação; o app não apaga nada sozinho depois.
                var efeito = _lista.DoDragDrop(dados, DragDropEffects.Copy | DragDropEffects.Move);
                Log.Escrever("Arrastar para fora: {0} item(ns), resultado {1}", caminhos.Length, efeito);
                if (efeito != DragDropEffects.None) Atualizar();
            }
            catch (Exception ex)
            {
                TratarExcecao("arrastar os arquivos", ex);
            }
        }

        /// <summary>De fora para dentro (WM_DROPFILES): copia para a pasta atual, ou para a pasta sob o mouse; Shift move.</summary>
        private void AoSoltarArquivos(object sender, ArquivosSoltosEventArgs e)
        {
            var destino = _pastaAtual;
            var alvo = _lista.GetItemAt(e.PosicaoNaLista.X, e.PosicaoNaLista.Y);
            var itemAlvo = alvo == null ? null : alvo.Tag as ItemArquivo;
            if (itemAlvo != null && itemAlvo.EhPasta) destino = itemAlvo.CaminhoCompleto;

            Log.Escrever("Soltar: {0} item(ns) em {1} (mover={2})", e.Caminhos.Length, destino, e.Mover);
            ReceberArquivos(e.Caminhos, destino, e.Mover, e.Mover ? "mover" : "copiar");
        }
    }
}
