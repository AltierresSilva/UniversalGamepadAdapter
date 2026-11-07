# Como contribuir

O UGA é um aplicativo Windows em C#/.NET 10 e WinForms, com interface em português. Contribuições podem incluir código, documentação, acessibilidade e testes de compatibilidade com controles reais.

## Preparar o ambiente

1. Use Windows 11 x64, Git e o SDK indicado em `global.json`.
2. Execute `./build.ps1` na raiz: restaura dependências, compila Release e executa testes. Preserve os arquivos `packages.lock.json`.
3. Para executar a aplicação, use `./publish.ps1` e abra o executável em `artifacts/Release/win-x64`. Inno Setup é necessário somente para gerar o instalador.
4. ViGEmBus é uma dependência externa para emulação real. Não instale drivers automaticamente em scripts de desenvolvimento ou CI. HidHide é opcional.

## Propor uma mudança

Abra uma discussão ou issue descrevendo o problema, o comportamento esperado e uma proposta pequena. Antes de mudanças grandes de arquitetura ou novas dependências, alinhe o escopo com o mantenedor. Trabalhe em uma branch, mantenha commits claros e envie um pull request focado.

No PR, explique o problema resolvido, a implementação, a validação e as limitações. Inclua capturas de tela para mudanças visuais, nos temas claro e escuro. Informe uso de IA conforme `docs/AI-DISCLOSURE.md`. A pessoa que envia o PR responde pela revisão do código e pelas afirmações de teste.

## Arquitetura e invariantes

- `src/UGA.Core`: contratos, mapeamento, motor e persistência.
- `src/UGA.Infrastructure`: SDL, identificação Windows, ViGEm e HidHide.
- `src/UGA.App`: interface WinForms, temas e desenhos dos controles.
- `tests/UGA.Tests`: simulações e testes nativos.

Leia `AGENTS.md` antes de editar. Mantenha operações SDL e de saída na thread dedicada; callbacks de feedback apenas armazenam dados. Pare e neutralize saídas ao desconectar, encerrar ou falhar. Não gere comandos de mouse/teclado, não altere segurança do Windows e não sobrescreva configurações de terceiros. Preserve backup e recuperação do HidHide.

## Testar e relatar resultados

Execute `./build.ps1`. Use `./build.ps1 -WithDriverTests` apenas em máquina Windows com ViGEmBus já instalado; esse teste cria e remove um Xbox virtual neutro. CI não deve exigir hardware nem instalar drivers. Adicione testes de comportamento para mudanças no motor, persistência e tratamento de falhas.

Para bugs, informe versão do Windows e do UGA, modelo/VID/PID do controle, USB/Bluetooth, passos, resultado esperado e observado. Remova caminhos pessoais, seriais e outros dados privados dos anexos. Não declare compatibilidade com um jogo ou dispositivo sem testar. Registre verificações reais em `docs/VALIDATION.md` e cenários em `docs/MANUAL-TESTS.md`.

## Publicação e licença

O código do UGA é disponibilizado sob a licença MIT em LICENSE. Ao enviar uma contribuição, você concorda com sua distribuição sob essa licença. Inclua somente material que você tem direito de contribuir. Não é exigido CLA. CI valida build, testes sem driver e publicação portátil em Windows; o instalador é gerado separadamente por publish.ps1. Dependências mantêm suas próprias licenças e avisos em `THIRD-PARTY-NOTICES.md`.
