namespace AbrirEfi.Comum
{
    /// <summary>Resultado de um programa do Windows executado pelo <see cref="ExecutorNativo"/>.</summary>
    public sealed class ResultadoNativo
    {
        public int Codigo { get; set; }
        public string Saida { get; set; } = "";
        public string Erros { get; set; } = "";
        public bool EstourouTempo { get; set; }

        public bool Sucesso => Codigo == 0 && !EstourouTempo;

        /// <summary>Saída + erros numa linha só, separados por " | " (para o log).</summary>
        public string Resumo()
        {
            return ExecutorNativo.EmUmaLinha(Saida + "\n" + Erros);
        }
    }
}
