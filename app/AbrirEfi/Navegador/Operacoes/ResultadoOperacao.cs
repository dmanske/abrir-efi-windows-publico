namespace AbrirEfi.Navegador.Operacoes
{
    /// <summary>Resultado de copiar/mover/excluir.</summary>
    public sealed class ResultadoOperacao
    {
        public bool Sucesso { get; set; }

        /// <summary>O usuário cancelou na janela do Windows (não é erro).</summary>
        public bool Cancelada { get; set; }

        public int Codigo { get; set; }

        /// <summary>Texto amigável quando falhou.</summary>
        public string Mensagem { get; set; }
    }
}
