using System;
using System.Collections.Generic;
using System.Linq;
using AbrirEfi.Comum;
using AbrirEfi.Comum.Nativo;

namespace AbrirEfi.Navegador.Operacoes
{
    /// <summary>
    /// Copiar, mover e excluir pelo SHFileOperation.
    /// Escolha: SHFileOperation em vez de IFileOperation porque é uma chamada única (sem COM/vtable),
    /// já traz a janela de progresso, de conflito ("substituir?") e de erro do próprio Windows, e é a
    /// mesma API que o Explorer usou por anos. IFileOperation é mais moderna (suporta caminhos longos),
    /// mas o ganho não compensa o risco de interop que não dá para testar daqui.
    /// Sem FOF_ALLOWUNDO de propósito: a EFI não tem lixeira e, com a flag, uma substituição tentaria
    /// mandar o arquivo antigo para uma lixeira que não existe e perguntaria "excluir permanentemente?".
    /// </summary>
    public static class OperacaoShell
    {
        public static ResultadoOperacao Copiar(IntPtr dono, IEnumerable<string> origens, string destino, bool renomearSeColidir)
        {
            ushort flags = NativoOperacaoArquivo.FOF_NOCONFIRMMKDIR;
            if (renomearSeColidir) flags |= NativoOperacaoArquivo.FOF_RENAMEONCOLLISION;
            return Executar(NativoOperacaoArquivo.FO_COPY, "copiar", dono, origens, destino, flags);
        }

        public static ResultadoOperacao Mover(IntPtr dono, IEnumerable<string> origens, string destino)
        {
            return Executar(NativoOperacaoArquivo.FO_MOVE, "mover", dono, origens, destino, NativoOperacaoArquivo.FOF_NOCONFIRMMKDIR);
        }

        /// <summary>Exclusão definitiva (a confirmação é feita antes, pela janela do app).</summary>
        public static ResultadoOperacao ExcluirDefinitivo(IntPtr dono, IEnumerable<string> origens)
        {
            return Executar(NativoOperacaoArquivo.FO_DELETE, "excluir", dono, origens, null, NativoOperacaoArquivo.FOF_NOCONFIRMATION);
        }

        private static ResultadoOperacao Executar(uint funcao, string nome, IntPtr dono, IEnumerable<string> origens, string destino, ushort flags)
        {
            var lista = origens.ToArray();
            if (lista.Length == 0) return new ResultadoOperacao { Sucesso = true };

            Log.Escrever("{0}: {1} item(ns) → {2}", nome, lista.Length, destino ?? "(sem destino)");
            foreach (var o in lista) Log.Escrever("  {0}", o);

            bool cancelada;
            int codigo;
            try
            {
                codigo = NativoOperacaoArquivo.Executar(dono, funcao, lista, destino, flags, out cancelada);
            }
            catch (Exception ex)
            {
                Log.Erro(nome, ex);
                return new ResultadoOperacao { Codigo = -1, Mensagem = "Não consegui chamar o Windows para " + nome + ": " + ex.Message };
            }
            Log.Escrever("{0}: código 0x{1:X}, cancelada={2}", nome, codigo, cancelada);

            if (codigo == 0 && !cancelada) return new ResultadoOperacao { Sucesso = true };
            if (cancelada || codigo == 0x75 || codigo == 0x4C7) return new ResultadoOperacao { Cancelada = true, Codigo = codigo };
            return new ResultadoOperacao { Codigo = codigo, Mensagem = Descrever(nome, codigo) };
        }

        private static string Descrever(string nome, int codigo)
        {
            string motivo;
            switch (codigo)
            {
                case 0x71: case 0x73: motivo = "origem e destino são o mesmo lugar."; break;
                case 0x74: motivo = "não dá para fazer isso com a pasta raiz."; break;
                case 0x76: motivo = "o destino fica dentro da própria origem."; break;
                case 0x78: motivo = "acesso negado."; break;
                case 0x79: case 0x7C: case 0x89: motivo = "caminho ou nome longo/inválido demais."; break;
                default: motivo = "o Windows devolveu o código 0x" + codigo.ToString("X", System.Globalization.CultureInfo.InvariantCulture) + "."; break;
            }
            return "Não deu para " + nome + ": " + motivo;
        }
    }
}
