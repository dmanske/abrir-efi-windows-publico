using System;
using System.Collections.Generic;
using System.IO;
using AbrirEfi.Comum.Nativo;

namespace AbrirEfi.Navegador.Icones
{
    /// <summary>Ícone (índice na image list do sistema) e nome do tipo de um arquivo.</summary>
    public sealed class InfoIcone
    {
        public int Indice { get; set; } = -1;
        public string Tipo { get; set; }
    }

    /// <summary>
    /// Ícones e nomes de tipo vindos do próprio Windows (SHGetFileInfo), como no Explorer.
    /// Usa a image list pequena do sistema (SHGFI_SYSICONINDEX); o ListView a recebe por LVM_SETIMAGELIST.
    /// Para a maioria das extensões usa SHGFI_USEFILEATTRIBUTES (não toca no disco) e guarda em cache por
    /// extensão. Extensões com ícone próprio em cada arquivo (.exe, .lnk, .ico...) são consultadas por caminho.
    /// </summary>
    public sealed class ProvedorIcones
    {
        private static readonly HashSet<string> ExtensoesComIconePorArquivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".lnk", ".ico", ".cur", ".ani", ".scr", ".msi", ".url", ".appref-ms", ".dll",
        };

        private readonly Dictionary<string, InfoIcone> _cache = new Dictionary<string, InfoIcone>(StringComparer.OrdinalIgnoreCase);
        private IntPtr _listaSistema;
        private bool _listaConsultada;

        /// <summary>Handle (HIMAGELIST) da image list pequena do sistema; IntPtr.Zero se não foi possível obter.</summary>
        public IntPtr ListaSistemaPequena
        {
            get
            {
                if (!_listaConsultada)
                {
                    _listaConsultada = true;
                    var info = new NativoIcones.SHFILEINFO();
                    _listaSistema = NativoIcones.SHGetFileInfo("pasta", NativoIcones.FILE_ATTRIBUTE_DIRECTORY, ref info,
                        (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativoIcones.SHFILEINFO)),
                        NativoIcones.SHGFI_SYSICONINDEX | NativoIcones.SHGFI_SMALLICON | NativoIcones.SHGFI_USEFILEATTRIBUTES);
                }
                return _listaSistema;
            }
        }

        /// <summary>Preenche o ícone e o tipo do item.</summary>
        public void Preencher(ItemArquivo item)
        {
            InfoIcone info;
            try
            {
                info = Obter(item.CaminhoCompleto, item.Nome, item.EhPasta);
            }
            catch (Exception ex)
            {
                Comum.Log.Erro("ícone de " + item.Nome, ex);
                info = new InfoIcone();
            }
            item.IndiceIcone = info.Indice;
            item.Tipo = string.IsNullOrEmpty(info.Tipo) ? TipoReserva(item.Nome, item.EhPasta) : info.Tipo;
        }

        private InfoIcone Obter(string caminho, string nome, bool ehPasta)
        {
            string chave;
            string alvo;
            uint atributos;
            uint flags = NativoIcones.SHGFI_SYSICONINDEX | NativoIcones.SHGFI_SMALLICON | NativoIcones.SHGFI_TYPENAME;

            if (ehPasta)
            {
                chave = "<pasta>";
                alvo = "pasta";
                atributos = NativoIcones.FILE_ATTRIBUTE_DIRECTORY;
                flags |= NativoIcones.SHGFI_USEFILEATTRIBUTES;
            }
            else
            {
                var extensao = Path.GetExtension(nome);
                if (!string.IsNullOrEmpty(extensao) && ExtensoesComIconePorArquivo.Contains(extensao))
                {
                    chave = "arq:" + caminho;
                    alvo = caminho;
                    atributos = 0;
                }
                else
                {
                    chave = string.IsNullOrEmpty(extensao) || extensao == "." ? "<sem extensão>" : extensao;
                    alvo = "arquivo" + (chave[0] == '.' ? chave : "");
                    atributos = NativoIcones.FILE_ATTRIBUTE_NORMAL;
                    flags |= NativoIcones.SHGFI_USEFILEATTRIBUTES;
                }
            }

            InfoIcone achado;
            if (_cache.TryGetValue(chave, out achado)) return achado;

            var resultado = Consultar(alvo, atributos, flags);
            if (resultado == null && (flags & NativoIcones.SHGFI_USEFILEATTRIBUTES) == 0)
            {
                // Arquivo ilegível: cai para o ícone genérico por extensão.
                resultado = Consultar("arquivo" + Path.GetExtension(nome), NativoIcones.FILE_ATTRIBUTE_NORMAL,
                                      flags | NativoIcones.SHGFI_USEFILEATTRIBUTES);
            }
            resultado = resultado ?? new InfoIcone();
            _cache[chave] = resultado;
            return resultado;
        }

        private static InfoIcone Consultar(string alvo, uint atributos, uint flags)
        {
            var info = new NativoIcones.SHFILEINFO();
            var r = NativoIcones.SHGetFileInfo(alvo, atributos, ref info,
                (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativoIcones.SHFILEINFO)), flags);
            if (r == IntPtr.Zero) return null;
            return new InfoIcone { Indice = info.iIcon, Tipo = info.szTypeName };
        }

        private static string TipoReserva(string nome, bool ehPasta)
        {
            if (ehPasta) return "Pasta de arquivos";
            var extensao = Path.GetExtension(nome);
            if (string.IsNullOrEmpty(extensao) || extensao == ".") return "Arquivo";
            return "Arquivo " + extensao.TrimStart('.').ToUpperInvariant();
        }
    }
}
