using AbrirEfi.Particoes;

namespace AbrirEfi.Montagem
{
    /// <summary>Resultado de dar uma letra para a partição EFI.</summary>
    public sealed class Montagem
    {
        /// <summary>Letra usada (ex.: 'Z').</summary>
        public char Letra { get; set; }

        /// <summary>Raiz da unidade (ex.: "Z:\").</summary>
        public string Raiz => Letra + @":\";

        /// <summary>
        /// True se foi o app que deu a letra — só nesse caso a letra é removida no fim.
        /// False quando a EFI já estava montada antes.
        /// </summary>
        public bool MontadaPeloApp { get; set; }

        public ParticaoEfi Particao { get; set; }
    }
}
