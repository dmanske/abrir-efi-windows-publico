using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using AbrirEfi.Comum;
using AbrirEfi.Particoes;

namespace AbrirEfi.Montagem
{
    /// <summary>
    /// Dá e tira a letra de unidade da partição EFI.
    /// Primeiro tenta o mountvol com o caminho do volume (não depende da numeração do diskpart);
    /// se falhar, usa o diskpart (o mesmo do jeito manual).
    /// </summary>
    public static class ServicoMontagem
    {
        private const string LetrasCandidatas = "ZYXWVUTSRQPONMLKJIHGFED";
        private static readonly TimeSpan EsperaRaiz = TimeSpan.FromSeconds(5);

        [DllImport("kernel32.dll")]
        private static extern uint GetLogicalDrives();

        public static Montagem Montar(ParticaoEfi particao)
        {
            if (particao == null) throw new ArgumentNullException(nameof(particao));

            if (particao.Letra.HasValue)
            {
                Log.Escrever("Já estava montada em {0}:", particao.Letra.Value);
                return new Montagem { Letra = particao.Letra.Value, MontadaPeloApp = false, Particao = particao };
            }

            var letra = LetraLivre();
            var montagem = new Montagem { Letra = letra, MontadaPeloApp = true, Particao = particao };
            var montou = false;
            var algumComandoDeuCerto = false;

            if (!string.IsNullOrEmpty(particao.CaminhoVolume))
            {
                var r = ExecutorNativo.Executar(ExecutorNativo.NoSystem32("mountvol.exe"), letra + ":", particao.CaminhoVolume);
                algumComandoDeuCerto |= r.Sucesso;
                montou = r.Sucesso && EsperarRaiz(montagem.Raiz);
            }
            else
            {
                Log.Escrever("Partição sem caminho de volume; indo direto para o diskpart.");
            }

            if (!montou)
            {
                Log.Escrever("Tentando pelo diskpart.");
                var r = ExecutorNativo.ExecutarDiskpart(new[]
                {
                    "select disk " + particao.NumeroDisco,
                    "select partition " + particao.NumeroParticao,
                    "assign letter=" + letra,
                });
                algumComandoDeuCerto |= r.Sucesso;
                montou = r.Sucesso && EsperarRaiz(montagem.Raiz);
            }

            if (!montou)
            {
                // A letra pode ter sido dada mesmo sem a raiz aparecer: não deixar pendurada.
                if (algumComandoDeuCerto) Desmontar(montagem);

                throw new InvalidOperationException(string.Format(
                    "Não consegui dar a letra {0}: para a EFI do disco {1} (partição {2}).",
                    letra, particao.NumeroDisco, particao.NumeroParticao));
            }

            Log.Escrever("Montada em {0}", montagem.Raiz);
            return montagem;
        }

        /// <summary>Remove a letra. Não lança exceção: falhas vão para o log e o retorno diz se deu certo.</summary>
        public static bool Desmontar(Montagem montagem)
        {
            if (montagem == null) return true;
            if (!montagem.MontadaPeloApp)
            {
                Log.Escrever("Mantida em {0}: (já estava montada antes)", montagem.Letra);
                return true;
            }

            var r = ExecutorNativo.Executar(ExecutorNativo.NoSystem32("mountvol.exe"), montagem.Letra + ":", "/D");
            if (!r.Sucesso)
            {
                Log.Escrever("mountvol /D falhou; tentando pelo diskpart.");
                r = ExecutorNativo.ExecutarDiskpart(new[]
                {
                    "select disk " + montagem.Particao.NumeroDisco,
                    "select partition " + montagem.Particao.NumeroParticao,
                    "remove letter=" + montagem.Letra,
                });
            }

            if (r.Sucesso) Log.Escrever("Letra {0}: removida.", montagem.Letra);
            else Log.Escrever("Não consegui remover a letra {0}:.", montagem.Letra);
            return r.Sucesso;
        }

        /// <summary>Primeira letra livre de Z até D.</summary>
        private static char LetraLivre()
        {
            var usadas = new HashSet<char>();

            try
            {
                foreach (var unidade in DriveInfo.GetDrives())
                {
                    if (!string.IsNullOrEmpty(unidade.Name))
                        usadas.Add(char.ToUpperInvariant(unidade.Name[0]));
                }
            }
            catch (Exception ex)
            {
                Log.Erro("listar unidades (DriveInfo)", ex);
            }

            // GetLogicalDrives também pega letras que o DriveInfo pode não devolver.
            var mascara = GetLogicalDrives();
            for (var i = 0; i < 26; i++)
            {
                if ((mascara & (1u << i)) != 0) usadas.Add((char)('A' + i));
            }

            foreach (var letra in LetrasCandidatas)
            {
                if (!usadas.Contains(letra)) return letra;
            }
            throw new InvalidOperationException("Não há nenhuma letra de unidade livre (de D a Z).");
        }

        private static bool EsperarRaiz(string raiz)
        {
            var relogio = Stopwatch.StartNew();
            while (relogio.Elapsed < EsperaRaiz)
            {
                if (Directory.Exists(raiz)) return true;
                Thread.Sleep(250);
            }
            Log.Escrever("A raiz {0} não apareceu em {1} s.", raiz, EsperaRaiz.TotalSeconds);
            return false;
        }
    }
}
