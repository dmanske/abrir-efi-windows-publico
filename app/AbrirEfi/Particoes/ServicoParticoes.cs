using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using AbrirEfi.Comum;

namespace AbrirEfi.Particoes
{
    /// <summary>
    /// Lista as partições EFI de todos os discos pelo WMI de armazenamento do Windows
    /// (root\Microsoft\Windows\Storage — o mesmo usado pelos cmdlets Get-Partition / Get-Disk).
    /// Só discos GPT: EFI em disco MBR não aparece.
    /// </summary>
    public static class ServicoParticoes
    {
        private const string NamespaceArmazenamento = @"root\Microsoft\Windows\Storage";
        private const string GuidTipoEfi = "{c12a7328-f81f-11d2-ba4b-00a0c93ec93b}";

        public static List<ParticaoEfi> Listar()
        {
            var escopo = new ManagementScope(NamespaceArmazenamento);
            escopo.Connect();

            var nomesDiscos = LerNomesDiscos(escopo);
            var resultado = new List<ParticaoEfi>();

            using (var busca = new ManagementObjectSearcher(escopo, new ObjectQuery("SELECT * FROM MSFT_Partition")))
            using (var particoes = busca.Get())
            {
                foreach (ManagementBaseObject mo in particoes)
                {
                    using (mo)
                    {
                        var tipo = mo["GptType"] as string;
                        if (!string.Equals(tipo, GuidTipoEfi, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var numeroDisco = ParaInt(mo["DiskNumber"]);
                        string nomeDisco;
                        if (!nomesDiscos.TryGetValue(numeroDisco, out nomeDisco) || string.IsNullOrWhiteSpace(nomeDisco))
                            nomeDisco = "disco desconhecido";

                        var particao = new ParticaoEfi
                        {
                            NumeroDisco = numeroDisco,
                            NumeroParticao = ParaInt(mo["PartitionNumber"]),
                            CaminhoVolume = LerCaminhoVolume(mo["AccessPaths"]),
                            Letra = LerLetra(mo["DriveLetter"]),
                            DoWindows = mo["IsSystem"] is bool sistema && sistema,
                            TamanhoMb = (long)Math.Round(ParaDouble(mo["Size"]) / (1024 * 1024)),
                            NomeDisco = nomeDisco,
                        };
                        particao.Descricao = MontarDescricao(particao);
                        resultado.Add(particao);
                    }
                }
            }

            return resultado
                .OrderBy(p => p.NumeroDisco)
                .ThenBy(p => p.NumeroParticao)
                .ToList();
        }

        private static Dictionary<int, string> LerNomesDiscos(ManagementScope escopo)
        {
            var nomes = new Dictionary<int, string>();
            try
            {
                using (var busca = new ManagementObjectSearcher(escopo, new ObjectQuery("SELECT Number, FriendlyName FROM MSFT_Disk")))
                using (var discos = busca.Get())
                {
                    foreach (ManagementBaseObject mo in discos)
                    {
                        using (mo)
                        {
                            if (mo["Number"] == null) continue;
                            nomes[ParaInt(mo["Number"])] = mo["FriendlyName"] as string;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Sem o nome do disco a lista ainda funciona.
                Log.Erro("ler nomes dos discos (MSFT_Disk)", ex);
            }
            return nomes;
        }

        private static string MontarDescricao(ParticaoEfi p)
        {
            var texto = string.Format("Disco {0} ({1}) - EFI de {2} MB", p.NumeroDisco, p.NomeDisco, p.TamanhoMb);
            if (p.DoWindows) texto += " - a que inicia o Windows";
            if (p.Letra.HasValue) texto += string.Format(" - já montada em {0}:", p.Letra.Value);
            return texto;
        }

        /// <summary>Pega em AccessPaths o caminho do tipo \\?\Volume{...}\.</summary>
        private static string LerCaminhoVolume(object accessPaths)
        {
            var caminhos = accessPaths as string[];
            if (caminhos == null) return null;
            var volume = caminhos.FirstOrDefault(c => c != null && c.StartsWith(@"\\?\Volume{", StringComparison.OrdinalIgnoreCase));
            if (volume != null && !volume.EndsWith(@"\")) volume += @"\";
            return volume;
        }

        /// <summary>
        /// DriveLetter é Char16 no WMI: pode chegar como char, ushort ou string conforme o caminho.
        /// O valor 0 significa "sem letra".
        /// </summary>
        private static char? LerLetra(object valor)
        {
            char letra;
            switch (valor)
            {
                case null: return null;
                case char c: letra = c; break;
                case ushort u: letra = (char)u; break;
                case string s when s.Length > 0: letra = s[0]; break;
                default:
                    try { letra = (char)Convert.ToUInt16(valor); }
                    catch { return null; }
                    break;
            }
            if (letra == '\0' || !char.IsLetter(letra)) return null;
            return char.ToUpperInvariant(letra);
        }

        private static int ParaInt(object valor)
        {
            return valor == null ? -1 : Convert.ToInt32(valor);
        }

        private static double ParaDouble(object valor)
        {
            return valor == null ? 0 : Convert.ToDouble(valor);
        }
    }
}
