# Logo UGA integrada à interface

A imagem escolhida pelo usuário é `Controle neon verde com esfera X luminosa.png`: controle verde neon com esfera azul/vermelha e X branco.

## Arquivos

- `src/UGA.App/Assets/UGA-logo-master.png`: original 1254×1254 preservado byte a byte, com fundo preto. SHA256 `6CAAA77FBDF8A7809AEC406CE5A05346C71F0CA79CAAFBCB68121BE44C2BF9DB`.
- `src/UGA.App/Assets/UGA-brand.png`: recorte 1536×1024 com transparência externa e margens reduzidas, preparado com Imagegen integrado. O corpo escuro dentro do controle continua opaco. Este é o PNG embutido e usado na lateral.
- `src/UGA.App/Assets/UGA-symbol.png`: recorte compacto 1254×1254 da esfera azul/vermelha com X branco, usado para tornar o ícone pequeno legível.
- `UGA.png` / `UGA.ico`: derivados do símbolo compacto para Windows. O ICO contém 16/24/32/48/64/128/256 px, sem margem adicional. `tools/create-icon.ps1` mantém as proporções e preenche o quadro do ícone.

Na lateral, o PNG é renderizado diretamente com antialiasing e ajuste proporcional em 68×48, alinhado ao texto UGA. Não é uma ampliação do ícone pequeno. O fundo do controle foi removido somente na região externa para integrar a marca aos temas claro e escuro.

Marcas anteriores e ícones com fundo preto estão preservados em `Assets/Previous`.

## Prompt usado no Imagegen integrado

```text
Use case: background-extraction. Asset: the EXISTING user-chosen UGA logo for a Windows application. Edit target: attached image, a black gamepad silhouette edged in bright neon green with a glossy blue/red orb and white X in its center. This is NOT a redesign. Preserve the EXACT recognizable controller outline, handles and shoulder shapes, centered blue/red sphere, white X, relative proportions, colors, and original glossy lighting. Remove ONLY the opaque black background OUTSIDE the controller silhouette, including the large empty top/bottom margins. Preserve the solid black/dark controller face INSIDE its green perimeter as an opaque part of the logo, do NOT make the interior transparent. Preserve the sphere entirely. Clean true alpha transparency outside the silhouette, with a very subtle soft green glow confined to the contour; no black matte, black rectangle, white halo, extra shadow, background smoke or gradients. Crop/enlarge the subject to occupy about 92% of a tight LANDSCAPE 3:2 canvas with small even transparent padding; do not squeeze, stretch, crop off handles, add buttons, add sticks, alter the X, change perspective or reimagine the logo. No text, no initials, no extra symbols. The result must look like this SAME logo cut out cleanly so it can sit naturally on either a light or a dark Windows UI.
```

## Ícone compacto para barra de título e bandeja

O Windows define o tamanho físico do ícone na barra de título. Para aumentar a presença visual sem mudar a moldura nativa, o aplicativo usa o recorte da esfera e X preenchendo o espaço disponível. A logo completa do controle permanece na lateral como PNG transparente. O recorte compacto foi preparado pelo Imagegen integrado, com este prompt:

```text
Use case: background-extraction / precise-object-edit. Edit target is the user-selected UGA logo image. Extract ONLY the EXISTING glossy sphere in the center of the gamepad, as the compact Windows titlebar/tray icon for this SAME brand. Preserve the blue left hemisphere, red right hemisphere, curved metallic WHITE X and exact recognizable look of that original sphere. Do not redesign or recolor it. Remove the green controller, all black background and all empty margins. Output one clean circular sphere on real alpha transparency. Square canvas, the sphere should fill 94–96% of width and height, centered, with no attached ring, no exterior halo, no external shadow, no text, no extra symbols. Maintain a bold white X and clean edges that will remain clearly legible at 16×16 and 32×32 pixels. The full controller logo remains elsewhere in the UI; this is solely a tight crop of its central emblem for small icons.
```
