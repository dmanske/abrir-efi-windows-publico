#!/bin/bash
# Atualiza o repositório público (dmanske/abrir-efi-windows-publico) a partir deste, o privado.
#   bash scripts/publicar.sh            copia, faz commit e push no público
#   bash scripts/publicar.sh --release  também cria a Release v${VERSAO} com o AbrirEFI.exe compilado
#
# O público recebe só o que está no git daqui (commit atual), menos o que é só nosso (FORA_DO_PUBLICO).
# O README e a licença do público ficam em publico/. Passo a passo: docs/operacao/PUBLICAR.md.
set -euo pipefail

RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
REPO_PUBLICO="dmanske/abrir-efi-windows-publico"
PASTA_PUBLICO="${RAIZ}/../abrir-efi-windows-publico"
CSPROJ="${RAIZ}/app/AbrirEfi/AbrirEfi.csproj"
EXE="${RAIZ}/app/AbrirEfi/bin/Release/net48/AbrirEFI.exe"
VERSAO="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "${CSPROJ}" | head -1)"
[ -n "${VERSAO}" ] || { echo "ERRO: <Version> não encontrada em ${CSPROJ}"; exit 1; }

FORA_DO_PUBLICO=(
    CLAUDE.md
    .kiro
    publico                                # vira a raiz do público (README, LICENSE)
    docs/planos                            # análise interna
    docs/produto                           # PRD interno
    docs/_arquivo
    docs/operacao/ENGENHARIA_REVERSA.md
    docs/operacao/WINDOWS.md               # tem caminhos pessoais; o README público já cobre compilar e o log
    docs/operacao/PUBLICAR.md              # fluxo privado -> público
)
CRIAR_RELEASE=0
[ "${1:-}" = "--release" ] && CRIAR_RELEASE=1

[ -z "$(git -C "${RAIZ}" status --porcelain)" ] || { echo "ERRO: há mudanças sem commit aqui; faça o commit antes."; exit 1; }
COMMIT="$(git -C "${RAIZ}" rev-parse --short HEAD)"

if [ ! -d "${PASTA_PUBLICO}/.git" ]; then
    if ! gh repo view "${REPO_PUBLICO}" >/dev/null 2>&1; then
        echo "ERRO: o repositório ${REPO_PUBLICO} ainda não existe no GitHub. Crie com:"
        echo "  gh repo create ${REPO_PUBLICO} --public --description \"Abrir EFI: abre a partição EFI no Windows\""
        exit 1
    fi
    echo "==> Clonando ${REPO_PUBLICO} em ${PASTA_PUBLICO}"
    gh repo clone "${REPO_PUBLICO}" "${PASTA_PUBLICO}"
fi

echo "==> Copiando os arquivos do commit ${COMMIT}"
TEMP="$(mktemp -d)"
git -C "${RAIZ}" archive HEAD | tar -x -C "${TEMP}"
for f in "${FORA_DO_PUBLICO[@]}"; do rm -rf "${TEMP:?}/${f}"; done
cp "${RAIZ}/publico/README.md" "${TEMP}/README.md"
cp "${RAIZ}/publico/LICENSE" "${TEMP}/LICENSE"
mkdir -p "${TEMP}/docs"
cp "${RAIZ}/publico/DOCS_README.md" "${TEMP}/docs/README.md"   # o índice daqui aponta para pastas que não vão
# --delete apaga no público o que saiu daqui; .git fica
rsync -a --delete --exclude .git "${TEMP}/" "${PASTA_PUBLICO}/"
rm -rf "${TEMP:?}"

cd "${PASTA_PUBLICO}"
git add -A
if git diff --cached --quiet; then
    echo "(nada mudou no público)"
else
    git commit -q -m "Abrir EFI ${VERSAO} (privado ${COMMIT})"
    git push -q -u origin HEAD:main
    echo "==> Publicado: https://github.com/${REPO_PUBLICO}"
fi

if [ "${CRIAR_RELEASE}" = 1 ]; then
    [ -f "${EXE}" ] || { echo "ERRO: ${EXE} não existe (dotnet build app/AbrirEfi/AbrirEfi.csproj -c Release)"; exit 1; }
    gh release create "v${VERSAO}" "${EXE}" --repo "${REPO_PUBLICO}" --title "Abrir EFI ${VERSAO}" \
        --notes-file "${RAIZ}/publico/NOTAS_DA_VERSAO.md"
fi
