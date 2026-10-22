using System;
using System.Drawing;

namespace AbrirEfi.Navegador.Arrastar
{
    /// <summary>Arquivos soltos na lista a partir de outro programa (ex.: Explorer), via WM_DROPFILES.</summary>
    public sealed class ArquivosSoltosEventArgs : EventArgs
    {
        public ArquivosSoltosEventArgs(string[] caminhos, Point posicaoNaLista, bool mover)
        {
            Caminhos = caminhos;
            PosicaoNaLista = posicaoNaLista;
            Mover = mover;
        }

        public string[] Caminhos { get; private set; }

        /// <summary>Onde o mouse estava, em coordenadas da lista (para saber se caiu em cima de uma pasta).</summary>
        public Point PosicaoNaLista { get; private set; }

        /// <summary>True se a tecla Shift estava apertada: mover em vez de copiar.</summary>
        public bool Mover { get; private set; }
    }
}
