using System;
using System.Collections.Generic;
using System.Drawing;
using AbrirEfi.Comum.Nativo;

namespace AbrirEfi.Navegador.Arrastar
{
    /// <summary>Lê o conteúdo de um HDROP (wParam do WM_DROPFILES) e o libera com DragFinish.</summary>
    internal static class LeitorDeArrasto
    {
        public static string[] LerELiberar(IntPtr hDrop, out Point ponto)
        {
            ponto = Point.Empty;
            try
            {
                PontoNativo p;
                if (NativoArrastar.DragQueryPoint(hDrop, out p)) ponto = new Point(p.X, p.Y);

                var total = NativoArrastar.DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                var caminhos = new List<string>();
                for (uint i = 0; i < total; i++)
                {
                    var tamanho = NativoArrastar.DragQueryFile(hDrop, i, null, 0);
                    var texto = new char[tamanho + 1];
                    var copiados = NativoArrastar.DragQueryFile(hDrop, i, texto, tamanho + 1);
                    caminhos.Add(new string(texto, 0, (int)copiados));
                }
                return caminhos.ToArray();
            }
            finally
            {
                NativoArrastar.DragFinish(hDrop);
            }
        }
    }
}
