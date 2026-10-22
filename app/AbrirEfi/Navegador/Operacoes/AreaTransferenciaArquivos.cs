using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Forms;
using AbrirEfi.Comum;

namespace AbrirEfi.Navegador.Operacoes
{
    /// <summary>Conteúdo de arquivos lido da área de transferência.</summary>
    public sealed class ConteudoArquivos
    {
        public string[] Caminhos { get; set; }

        /// <summary>True quando quem copiou pediu "recortar" (Preferred DropEffect = mover).</summary>
        public bool Mover { get; set; }
    }

    /// <summary>
    /// Copiar/recortar/colar arquivos pela área de transferência, no mesmo formato do Explorer:
    /// lista de arquivos (CF_HDROP) + "Preferred DropEffect" (5 = copiar, 2 = mover).
    /// A área de transferência não é bloqueada pelo UIPI, então funciona com o Explorer comum.
    /// </summary>
    public static class AreaTransferenciaArquivos
    {
        private const string FormatoEfeito = "Preferred DropEffect";
        private const int EfeitoCopiar = 5; // DROPEFFECT_COPY | DROPEFFECT_LINK, como o Explorer grava
        private const int EfeitoMover = 2;  // DROPEFFECT_MOVE

        public static void Colocar(IEnumerable<string> caminhos, bool recortar)
        {
            var lista = new StringCollection();
            foreach (var c in caminhos) lista.Add(c);
            if (lista.Count == 0) return;

            var dados = new DataObject();
            dados.SetFileDropList(lista);
            var efeito = BitConverter.GetBytes(recortar ? EfeitoMover : EfeitoCopiar);
            dados.SetData(FormatoEfeito, new MemoryStream(efeito));
            // copy=true deixa os dados na área de transferência depois que o app fechar; 10 tentativas por causa de bloqueios.
            Clipboard.SetDataObject(dados, true, 10, 100);
            Log.Escrever("{0} {1} item(ns) na área de transferência", recortar ? "Recortou" : "Copiou", lista.Count);
        }

        public static bool TemArquivos()
        {
            try { return Clipboard.ContainsFileDropList(); }
            catch (Exception) { return false; }
        }

        public static ConteudoArquivos Ler()
        {
            var lista = Clipboard.GetFileDropList();
            if (lista == null || lista.Count == 0) return null;

            var caminhos = new string[lista.Count];
            lista.CopyTo(caminhos, 0);

            var mover = false;
            var objeto = Clipboard.GetDataObject();
            var fluxo = objeto == null ? null : objeto.GetData(FormatoEfeito) as MemoryStream;
            if (fluxo != null && fluxo.Length >= 4)
            {
                var bytes = new byte[4];
                fluxo.Position = 0;
                fluxo.Read(bytes, 0, 4);
                var efeito = BitConverter.ToInt32(bytes, 0);
                mover = (efeito & EfeitoMover) != 0 && (efeito & 1) == 0; // mover só se COPY não estiver marcado
            }
            return new ConteudoArquivos { Caminhos = caminhos, Mover = mover };
        }

        public static void Limpar()
        {
            try { Clipboard.Clear(); }
            catch (Exception ex) { Log.Erro("limpar área de transferência", ex); }
        }
    }
}
