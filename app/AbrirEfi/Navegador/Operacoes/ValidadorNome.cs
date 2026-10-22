using System;
using System.IO;
using System.Linq;

namespace AbrirEfi.Navegador.Operacoes
{
    /// <summary>Valida nomes de arquivo/pasta digitados pelo usuário.</summary>
    public static class ValidadorNome
    {
        private static readonly string[] Reservados =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        };

        /// <summary>Devolve null se o nome é válido, ou a mensagem de erro para mostrar ao usuário.</summary>
        public static string Validar(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return "O nome não pode ficar vazio.";
            if (nome.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return "O nome não pode ter nenhum destes caracteres:\n\\ / : * ? \" < > |";
            if (nome.EndsWith(".", StringComparison.Ordinal) || nome.EndsWith(" ", StringComparison.Ordinal))
                return "O nome não pode terminar com ponto ou espaço.";
            var semExtensao = nome.Split('.')[0].Trim();
            if (Reservados.Contains(semExtensao, StringComparer.OrdinalIgnoreCase))
                return "\"" + semExtensao + "\" é um nome reservado do Windows.";
            return null;
        }

        /// <summary>"Nova pasta", "Nova pasta (2)", ... o primeiro que ainda não existe em <paramref name="pai"/>.</summary>
        public static string NomeLivre(string pai, string baseNome)
        {
            var candidato = baseNome;
            for (var n = 2; File.Exists(Path.Combine(pai, candidato)) || Directory.Exists(Path.Combine(pai, candidato)); n++)
                candidato = baseNome + " (" + n + ")";
            return candidato;
        }
    }
}
