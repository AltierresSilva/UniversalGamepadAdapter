# Logo UGA

Ferramenta: Imagegen integrado, sem API/CLI externa. Referência: última imagem circular metálica enviada pelo usuário. A forma circular substitui a caixa anterior; azul e vermelho foram mantidos conforme o pedido textual anterior. O texto UGA aparece na interface, separado do símbolo, para preservar a legibilidade dos ícones pequenos.

Arquivos: `src/UGA.App/Assets/UGA-circle-master.png` (1254×1254, transparência real), `UGA.png` (256×256) e `UGA.ico` (16/24/32/48/64/128/256). `tools/create-icon.ps1` gera as versões de distribuição a partir do PNG mestre. A imagem gerada foi copiada para o projeto; não depende do cache do Codex.

Prompt final usado:

```text
Use case: logo-brand / precise-object-edit. Input image is the user's reference for the Universal Gamepad Adapter Windows application logo. Create a square transparent-background application icon derived from ONLY the circular metallic emblem with the luminous curved X in the upper center of the reference. Preserve its recognizable round brushed-metal medallion, silver beveled ring, dark gunmetal face, and sweeping four-armed X shape. Important user color requirement from this conversation: use electric BLUE and RED accents together (blue upper-left/lower-right X arms, red upper-right/lower-left X arms), with a restrained blue/red glow and polished silver outer rim. Center the circular emblem alone, filling 88–92% of the square, face-on, balanced and sharply legible at small Windows icon sizes. Remove all scenery, smoke, particles, UI toolbar buttons, UGA lettering, subtitle and every other text element. No background plate or square container; real alpha transparency outside the circular emblem. No external cast shadow, no scattered flares. Professional production icon, finely rendered metal but simplify unnecessary tiny decorations for readability. One emblem only. This supersedes the previous square/box icon in the project.
```
