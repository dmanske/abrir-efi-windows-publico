using System;
using System.IO;

namespace AbrirEfi.Navegador
{
    /// <summary>Um arquivo ou pasta mostrado no navegador.</summary>
    public sealed class ItemArquivo
    {
        public string Nome { get; set; }
        public string CaminhoCompleto { get; set; }
        public bool EhPasta { get; set; }

        /// <summary>Tamanho em bytes (null para pastas).</summary>
        public long? Tamanho { get; set; }

        /// <summary>Data de modificação em hora LOCAL.</summary>
        public DateTime Modificado { get; set; }

        public string Tipo { get; set; }

        /// <summary>Índice do ícone na image list pequena do sistema (-1 se desconhecido).</summary>
        public int IndiceIcone { get; set; } = -1;

        public FileAttributes Atributos { get; set; }

        public bool Oculto => (Atributos & FileAttributes.Hidden) != 0;
    }
}
