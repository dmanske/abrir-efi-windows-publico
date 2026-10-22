# Arquitetura — Abrir EFI (Windows)

## Visão geral

Aplicativo WinForms em C# para **.NET Framework 4.8** (já instalado no Windows 10/11; nada a instalar),
interface montada em código (sem Designer). Um único `AbrirEFI.exe`. O manifesto exige administrador
(`requireAdministrator`), então o UAC aparece ao abrir e tudo que o app inicia herda o administrador.

## Fluxo

1. `Program.Main`: trata exceções não previstas (mensagem + log) e chama o fluxo.
2. `ServicoParticoes.Listar`: WMI (`root\Microsoft\Windows\Storage`, `MSFT_Partition` / `MSFT_Disk`),
   filtrando o tipo GPT da ESP. Para cada uma: disco, partição, caminho do volume, letra, tamanho, se é a do Windows.
3. `FormEscolhaEfi` só aparece se houver mais de uma.
4. `ServicoMontagem.Montar`: se já tem letra, usa e não remove depois; senão pega a primeira letra livre
   (Z, Y, X...) e roda `mountvol Z: \\?\Volume{...}\`; se falhar, `diskpart` (select disk / select partition /
   assign letter). Espera a raiz aparecer (até 5 s).
5. `FormNavegador` (a janela de arquivos) roda até fechar.
6. `ServicoMontagem.Desmontar`: `mountvol Z: /D`, com `diskpart remove letter` de reserva. Só se o app montou.

## Estrutura de pastas (`app/AbrirEfi/`)

```
Program.cs                      orquestra o fluxo
app.manifest                    requireAdministrator, PerMonitorV2, Common Controls 6
AbrirEfi.csproj                 net48, WinExe, AnyCPU 64 bits, saída AbrirEFI.exe
Comum/
  Log.cs                        log em %LOCALAPPDATA%\AbrirEFI\abrir-efi.log
  Configuracao.cs               config.ini (NaoPerguntarAoFechar)
  ExecutorNativo.cs, ResultadoNativo.cs   executa mountvol/diskpart com captura de saída
  AtalhoAreaDeTrabalho.cs       atalho .lnk via WScript.Shell
  Dpi.cs
  Nativo/                       P/Invoke, um arquivo por assunto
    TiposNativos.cs             POINT + DefaultDllImportSearchPaths(System32)
    NativoJanelas.cs            SendMessage, PostMessage, SetWindowTheme, constantes WM_*
    NativoUipi.cs               ChangeWindowMessageFilterEx
    NativoArrastar.cs           DragAcceptFiles, DragQueryFileW, DragQueryPoint, DragFinish
    NativoIcones.cs             SHGetFileInfo
    NativoOperacaoArquivo.cs    SHFileOperationW (estruturas 32 e 64 bits)
    NativoAbrirCom.cs           SHOpenWithDialog
    NativoMenuShell.cs          SHParseDisplayName, SHBindToParent, menus, TrackPopupMenuEx
Particoes/                      ParticaoEfi, ServicoParticoes (WMI)
Montagem/                       Montagem, ServicoMontagem (mountvol/diskpart)
EscolhaEfi/                     FormEscolhaEfi
Navegador/
  FormNavegador.cs              campos, ciclo de vida, WndProc, tratamento de exceções
  FormNavegador.Construcao.cs   monta toolbar, endereço, lista, status, menu de contexto
  FormNavegador.Navegacao.cs    navegar, preencher lista, status
  FormNavegador.Arquivos.cs     abrir, copiar/recortar/colar, excluir, renomear, nova pasta, arrastar
  FormNavegador.Menus.cs        menu de contexto, menu do Windows, Prompt, atalho, log
  FormNavegador.Atalhos.cs      teclado (ProcessCmdKey)
  FormNavegador.Fechamento.cs   aviso ao fechar, bloqueio durante operação
  ItemArquivo.cs, ServicoArquivos.cs
  Controles/ListaArquivos.cs    ListView com image list do sistema e WM_DROPFILES
  Icones/ProvedorIcones.cs      ícone e tipo por extensão (cache)
  Operacoes/                    OperacaoShell (SHFileOperation), AreaTransferenciaArquivos,
                                ValidadorNome, AbrirCom, ResultadoOperacao
  Arrastar/                     LeitorDeArrasto, ArquivosSoltosEventArgs
  Shell/                        InterfacesShell (COM), MenuContextoShell
  Dialogos/                     DialogoAvisoFechar
```

## Decisões

- **net48 sem instalar nada**: já vem no Windows. Compila no Mac com o pacote de assemblies de referência
  (só em tempo de compilação, não vai junto com o .exe).
- **Manifesto de administrador**: montar a EFI e escrever nela exige; os programas abertos pela janela herdam.
- **WMI** (`MSFT_Partition`) em vez de `diskpart list`: devolve dados estruturados (tipo GPT, IsSystem, caminho
  do volume) sem interpretar texto. É o mesmo provedor dos cmdlets `Get-Partition`.
- **mountvol, com diskpart de reserva**: mountvol usa o caminho do volume (não depende da numeração do disco);
  o diskpart é o caminho manual conhecido.
- **Janela de arquivos própria** em vez de depender de um gerenciador de arquivos de terceiros: nada para
  baixar nem conferir; o app controla o momento de desmontar e entrega o que o usuário pediu (arrastar, menu do
  Windows, prompt).
- **Ícones**: `SHGetFileInfo(SYSICONINDEX | SMALLICON)` e `LVM_SETIMAGELIST` com a image list do sistema.
  Cache por extensão com `USEFILEATTRIBUTES` (não lê o disco); `.exe`, `.lnk`, `.ico` etc. por caminho.
  A coluna Tipo vem de `SHGFI_TYPENAME`.
- **Copiar/mover/excluir: `SHFileOperation`** em vez de `IFileOperation`: chamada única sem COM, traz a janela
  de progresso e de conflito do Windows. `IFileOperation` aceita caminhos longos, mas o ganho não compensa o
  risco de interop que não dá para testar daqui. Sem `FOF_ALLOWUNDO` (a EFI não tem lixeira; com a flag, uma
  substituição tentaria reciclar e perguntaria "excluir permanentemente?"). A estrutura tem alinhamento diferente em
  32 bits (Pack=1) e 64 bits (natural), por isso há duas versões. `pFrom`/`pTo` terminam com dois nulos.
- **Área de transferência**: `DataObject` com `SetFileDropList` + "Preferred DropEffect" (5 copiar, 2 mover),
  igual ao Explorer; a área de transferência não é bloqueada pelo UIPI.
- **Arrastar de fora para dentro**: o OLE é bloqueado pelo UIPI entre programa comum e elevado. A lista fica
  com `AllowDrop = false`, chama `ChangeWindowMessageFilterEx` (MSGFLT_ALLOW) para `WM_DROPFILES`,
  `WM_COPYDATA` e `WM_COPYGLOBALDATA` na janela e na lista, `DragAcceptFiles(lista, true)` e trata
  `WM_DROPFILES` com `DragQueryFileW` + `DragFinish`. O resultado de cada chamada vai para o log. O processamento
  é adiado (`BeginInvoke`) para não segurar o Explorer. **Suposição não confirmada: precisa ser testado no Windows.**
- **Arrastar de dentro para fora**: `ItemDrag` → `DoDragDrop` com `DataFormats.FileDrop` (elevado → comum é permitido).
  O app não apaga a origem; quem recebe move.
- **Menu do Windows**: `SHParseDisplayName` → `SHBindToParent` → `IShellFolder.GetUIObjectOf(IContextMenu)` →
  `QueryContextMenu` → `TrackPopupMenuEx(TPM_RETURNCMD)` → `InvokeCommand`. Enquanto aberto, `WM_INITMENUPOPUP`,
  `WM_DRAWITEM`, `WM_MEASUREITEM` e `WM_MENUCHAR` do formulário vão para `IContextMenu3/2`. As interfaces COM
  repetem os métodos das bases, na ordem da vtable do SDK.
- **Fechamento**: durante uma operação a janela não fecha; fora disso mostra aviso Sim/Não (desligável).
- **Segurança de DLL**: `DefaultDllImportSearchPaths(System32)`, porque o app roda elevado.

## Limitações

- Testado no Windows em 22/10/2026 (uso geral: abrir, montar, usar a janela, fechar). O arrastar a partir do
  Explorer comum e o menu "Mais opções do Windows…" ainda não foram confirmados um a um.
- Sem atualização automática da lista (F5), sem ordenar por coluna, sem desfazer.
- Os ícones usam a image list pequena do sistema (16 px), que pode ficar pequena em escalas altas.
- Extensões de terceiros do menu do Windows rodam dentro do processo elevado.
- Caminhos muito longos (> 260) dependem do `SHFileOperation`/.NET Framework e podem falhar.
- `Delete` é sempre definitivo, também fora da EFI.
