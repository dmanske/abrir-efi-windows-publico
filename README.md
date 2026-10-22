# Abrir EFI

Abre a **partição EFI no Windows** com dois cliques, com acesso total: copiar, colar, recortar, apagar, renomear,
criar pastas, arrastar arquivos e abrir/editar com qualquer programa (`config.plist`, kexts, drivers).

O Explorer do Windows se recusa a abrir a partição EFI. O Abrir EFI dá uma letra a ela, abre **a própria janela de
arquivos** (rodando como administrador) dentro dela e remove a letra quando você fecha. É o equivalente, no Windows,
da Ação Rápida "Mount EFI" do Mac.

**Para quem é:** quem tem Hackintosh com OpenCore, ou qualquer pessoa que precisa mexer na partição EFI pelo Windows.

## O que faz

- Um único arquivo, **`AbrirEFI.exe`**: não instala nada. Pede permissão de administrador (UAC) ao abrir.
- Lista as partições EFI do computador (a que inicia o Windows já vem marcada) e monta a escolhida com uma letra.
- Janela de arquivos própria: navegar, copiar / recortar / colar, **arrastar e soltar**, excluir, renomear, nova pasta,
  menu de contexto do Windows ("Mais opções do Windows…"), botão **Prompt (admin)** que abre o `cmd.exe` na pasta atual.
- Ao fechar, desmonta a letra (se a EFI já tinha letra antes, o app usa essa letra e não a remove).

## Baixar

Baixe o **`AbrirEFI.exe`** em **[Releases](https://github.com/dmanske/abrir-efi-windows-publico/releases)**.

Requisitos: Windows 10 ou 11 de 64 bits. O .NET Framework 4.8 já vem no Windows: não há nada a instalar.

## Como usar

1. Copie `AbrirEFI.exe` para qualquer pasta e dê dois cliques. Confirme a permissão de administrador.
2. Se houver mais de uma partição EFI, escolha na lista.
3. Use a janela como o Explorer:
   - Navegar: duplo clique, Backspace, Alt+←, barra de endereço (Ctrl+L / Alt+D).
   - Copiar / Recortar / Colar: Ctrl+C / Ctrl+X / Ctrl+V (inclusive arquivos copiados no Explorer normal).
   - Excluir: Delete. Renomear: F2. Nova pasta: Ctrl+Shift+N. Atualizar: F5.
4. Feche a janela: a letra da EFI é removida sozinha.

Opcional: menu **Mais > Criar atalho na Área de Trabalho**.

## Dicas

- **Arrastar entre um programa comum e a janela de administrador:** o Windows bloqueia isso por segurança. O app tenta
  liberar o arrastar do Explorer normal para dentro da janela (copia; com **Shift** move). Se não funcionar no seu
  Windows, copie no Explorer normal e cole (Ctrl+V) na janela do Abrir EFI.
- **Copiar e colar** funciona com arquivos copiados em qualquer programa.
- **Exclusão é definitiva:** a EFI não tem lixeira. O app pergunta antes de apagar.
- Programas abertos pela janela (ProperTree, Bloco de Notas…) também rodam como administrador e conseguem salvar na
  EFI. Salve e feche os arquivos antes de fechar a janela da EFI.
- O aviso ao fechar pode ser desligado marcando "Não perguntar de novo" (fica em `%LOCALAPPDATA%\AbrirEFI\config.ini`).

## Se der erro

Tudo é registrado em `%LOCALAPPDATA%\AbrirEFI\abrir-efi.log` (partições encontradas, comandos `mountvol`/`diskpart`,
operações de arquivos). Também abre pelo menu **Mais > Abrir o arquivo de log**. Ao abrir um problema, anexe esse arquivo.

## Aviso

> O programa **roda como administrador e mexe na partição de boot**. Apagar ou alterar arquivos errados na EFI pode
> impedir o computador de iniciar. **Faça backup da EFI antes de editar.** Use por sua conta e risco (licença MIT, sem
> garantia).

## Compilar

Precisa do SDK do .NET 8, no Windows, no macOS ou no Linux (o `.exe` só roda no Windows):

```
dotnet build app\AbrirEfi\AbrirEfi.csproj -c Release
```

(no Mac/Linux: `dotnet build app/AbrirEfi/AbrirEfi.csproj -c Release`). O resultado é
`app/AbrirEfi/bin/Release/net48/AbrirEFI.exe`. Detalhes de código em [`docs/ARQUITETURA.md`](docs/ARQUITETURA.md).

## Licença

[MIT](LICENSE) — Copyright (c) 2026 Daniel Manske.

## English summary

**Abrir EFI** ("Open EFI") is a single-file Windows tool (C#, .NET Framework 4.8) that mounts the EFI System Partition
with a drive letter and opens it in its own administrator-level file window (copy/paste, drag and drop, delete, rename,
Windows context menu, admin prompt), then unmounts it when you close it. Aimed at Hackintosh/OpenCore users and anyone
who needs to edit the EFI partition from Windows. Download `AbrirEFI.exe` from
[Releases](https://github.com/dmanske/abrir-efi-windows-publico/releases); requires Windows 10/11 64-bit. It runs as
administrator and touches the boot partition: **back up your EFI before editing**. Build with
`dotnet build app/AbrirEfi/AbrirEfi.csproj -c Release` (.NET SDK 8). Log: `%LOCALAPPDATA%\AbrirEFI\abrir-efi.log`.
MIT license.
