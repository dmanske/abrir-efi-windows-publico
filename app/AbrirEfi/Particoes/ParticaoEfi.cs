namespace AbrirEfi.Particoes
{
    /// <summary>Uma partição EFI (GPT "EFI System Partition") encontrada em algum disco.</summary>
    public sealed class ParticaoEfi
    {
        public int NumeroDisco { get; set; }
        public int NumeroParticao { get; set; }

        /// <summary>Caminho do volume, ex.: \\?\Volume{1234...}\ (pode ser null se o Windows não informar).</summary>
        public string CaminhoVolume { get; set; }

        /// <summary>Letra atual (ex.: 'Z') ou null se a partição não tem letra.</summary>
        public char? Letra { get; set; }

        /// <summary>True se é a EFI que inicia o Windows (IsSystem).</summary>
        public bool DoWindows { get; set; }

        public long TamanhoMb { get; set; }
        public string NomeDisco { get; set; }

        /// <summary>Texto mostrado na lista de escolha e no log.</summary>
        public string Descricao { get; set; }

        public override string ToString() => Descricao ?? base.ToString();
    }
}
