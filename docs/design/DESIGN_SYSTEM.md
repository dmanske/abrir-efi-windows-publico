# Design system — Abrir EFI (Windows)

Janela nativa do Windows (WinForms): o objetivo é parecer o Explorer, não ter identidade própria.

## Tipografia
- Fonte **Segoe UI**: 9 pt na janela de arquivos, 10 pt na janela de escolha da partição.
- Sem negrito, exceto títulos curtos de diálogo.

## Cores
- Só cores do sistema (`SystemColors`): respeitam o tema de contraste do Windows.
- Itens ocultos e itens recortados: `SystemColors.GrayText`.
- Sem cores próprias, sem tema escuro próprio (o WinForms não tem; ficam como o Windows desenha).

## Ícones
- Os do próprio Windows (`SHGetFileInfo` + image list do sistema), os mesmos do Explorer, por tipo de arquivo.
- Botões da barra de ferramentas usam texto (com setas Unicode ← ↑ ⟳), sem imagens.

## Espaçamento
- Medidas escritas em pixels a 96 DPI (100%); `AutoScaleMode = Dpi` escala o resto.
- Margem de 6 px nas barras, 12 px nos diálogos; botões de diálogo 100x28.
- Larguras das colunas são reescaladas manualmente quando o DPI do monitor muda (`AjustarColunasAoDpi`).

## DPI
- Manifesto com `PerMonitorV2` (com `true/pm` de reserva): nítido ao mover a janela entre monitores de escalas
  diferentes.
- Limitação: a image list pequena do sistema pode ter ícones de 16 px mesmo em 150%; é aceitável.

## Textos
- Português do Brasil, tom direto, sem jargão: "Excluir definitivamente", "A EFI não tem lixeira".
- Ações destrutivas têm confirmação com o botão "Não" como padrão.
