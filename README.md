# Universal Gamepad Adapter 1.0

[![Windows CI](https://github.com/AltierresSilva/UniversalGamepadAdapter/actions/workflows/ci.yml/badge.svg)](https://github.com/AltierresSilva/UniversalGamepadAdapter/actions/workflows/ci.yml)

Repositório: https://github.com/AltierresSilva/UniversalGamepadAdapter · [Abrir issue](https://github.com/AltierresSilva/UniversalGamepadAdapter/issues/new/choose) · [Discussões](https://github.com/AltierresSilva/UniversalGamepadAdapter/discussions)

**Desenvolvido com assistência de IA (OpenAI Codex).** Leia a [declaração de transparência](docs/AI-DISCLOSURE.md) e o [guia de contribuição](CONTRIBUTING.md). A validação com hardware e jogos ainda está em andamento; consulte `docs/VALIDATION.md`.

Código sob [licença MIT](LICENSE). Contribuições e relatos de compatibilidade são bem-vindos. Veja o [roadmap](docs/ROADMAP.md), as [regras de convivência](CODE_OF_CONDUCT.md) e a [política de segurança](SECURITY.md). Este é um projeto comunitário experimental, sem vínculo oficial com Microsoft, Sony ou fabricantes de controles.

## Recursos e limites

- Um controle físico via SDL3 convertido em um Xbox 360 virtual XInput.
- Mapeamento, calibração de eixos, zona morta e diagnóstico local.
- Interface Windows com temas claro/escuro, bandeja e inicialização opcional.
- Cor do LED RGB e intensidade da vibração dos jogos, quando o dispositivo/conexão oferece suporte.
- HidHide opcional com backup e recuperação de alterações próprias.

Não há suporte universal garantido: modelos paralelos e transportes podem apresentar diferenças. ViGEmBus é uma dependência legada externa; drivers não são incluídos nem instalados pela CI. O projeto não emula mouse/teclado nem usa movimento físico para controlar a câmera.

## Para colaborar

Abra uma issue com reprodução do problema ou proposta, faça um fork, crie uma branch e envie um PR pequeno. O workflow **Windows CI** compila, executa testes sem instalação de drivers e produz um artefato portátil para revisão. Artefatos de CI não equivalem a releases aprovadas e são retidos por 14 dias. Testes de hardware/jogos e instalador continuam necessários; não são substituídos pelo resultado da CI.

Aplicação Windows 11 x64 em C#/.NET 10 e Windows Forms. Lê um controle físico por SDL3 e gera um Xbox 360 XInput via ViGEmBus. Funciona offline após instalar o driver. Interface em português; não envia mouse, teclado, giroscópio ou touchpad.

Interface reformulada com navegação lateral, cartões de status e temas **escuro e claro**. Alterne pelo botão no cabeçalho ou em Configurações → Aparência; a escolha é salva automaticamente. Sem conta, avatar ou perfil de usuário. O mapeamento e a calibração continuam salvos automaticamente por dispositivo ao aplicar/salvar seus ajustes. Os desenhos distinguem PlayStation (analógicos simétricos e touchpad) e Xbox 360 (analógicos deslocados).

Logo escolhida pelo usuário: controle verde neon com esfera azul/vermelha e X branco. A lateral usa `Assets/UGA-brand.png`, recortado com transparência externa e margens reduzidas, em proporção correta e alinhado ao nome UGA. Os ícones do executável, janela, bandeja, atalhos e Setup/Uninstall usam a esfera com X em destaque (UGA-symbol.png) em ICO multirresolução, para melhor leitura em tamanhos pequenos. A logo completa continua na lateral. O original está preservado em `Assets/UGA-logo-master.png`. Regenerar os formatos Windows com `tools/create-icon.ps1`. [Origem e preparo da logo](docs/LOGO.md).

## Executar

Em **Configurações → LED e vibração**, escolha uma cor RGB, marque **Aplicar minha cor ao conectar o controle** e clique **Aplicar e salvar**. Ajuste a vibração de 0 a 100% (0 desativa); a intensidade vale para o retorno dos jogos no Xbox virtual e para **Testar vibração** (0,5 segundo). **Parar vibração** interrompe o efeito atual; novos comandos de um jogo podem retomá-lo. Os recursos são habilitados conforme o suporte reportado pelo SDL para o controle/conexão. Desmarcar a cor personalizada interrompe a aplicação automática, mantendo a última cor física até outro comando/reconexão. Outros remapeadores podem disputar LED e motores; feche-os antes de testar.

Use `artifacts/Release/win-x64/UniversalGamepadAdapter.exe` ou instale `artifacts/Installer/UGA-1.0.0-Setup.exe`. A publicação inclui o runtime .NET e SDL3 nativo. O aplicativo roda sem administrador; somente o instalador em Program Files e a instalação oficial dos drivers podem solicitar elevação.

1. Feche DS4Windows e outros remapeadores manualmente. Pare a emulação deles antes de testar.
2. Emparelhe o Wireless Controller no Bluetooth do Windows. Abra o UGA e selecione o controle físico.
3. Confira o layout SDL na tela Mapeamento. Se estiver errado ou indisponível, execute **Assistente completo** com o corpo do controle imóvel. Capture neutro, comandos e extremos; clique **Salvar mapeamento**.
4. Na Visão geral, clique **Iniciar emulação**. Abra `joy.cpl` e confira o Xbox 360 virtual.
5. Confira RX/RY na tela Calibração: só o analógico direito deve alterar esses valores. Use zona morta inicial de 8% e clique **Aplicar ajustes**. **Calibrar eixos e gatilhos** permite recapturar apenas os seis eixos.
6. Se houver entrada duplicada, use o HidHide opcional com cuidado. Leia [recuperação e drivers](docs/DRIVERS.md). Reconecte o controle após ocultá-lo.
7. Só então teste Edge/Chrome e Xbox Cloud Gaming. Opera é experimental. A compatibilidade efetiva com cada jogo precisa ser validada no seu computador.

Fechar a janela mantém o UGA na bandeja por padrão. Para liberar o virtual e encerrar, use **Sair do UGA** na lateral ou **Sair** no ícone da bandeja. Desative essa preferência para encerrar pelo X. **Desativar emulação** neutraliza e remove o virtual.

## Compilar

Clone com `git clone https://github.com/AltierresSilva/UniversalGamepadAdapter.git` e entre na pasta do projeto. Requer SDK .NET 10 e, para Setup.exe, Inno Setup 6+.

```powershell
.\build.ps1
.\publish.ps1
# Opcional: teste real do driver instalado, usando apenas estados neutros:
.\build.ps1 -WithDriverTests
```

Os scripts procuram o SDK local em `../.tools/dotnet` e depois o SDK instalado. Não instalam drivers. Comandos equivalentes:

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet publish src/UGA.App/UGA.App.csproj -c Release -r win-x64 --self-contained true -o artifacts/Release/win-x64
ISCC.exe Installer/UGA.iss
```

Dados pessoais locais: `%LocalAppData%\UniversalGamepadAdapter\` (preferências, perfis, calibração, logs limitados e backup HidHide). JSON inválido é preservado com sufixo `.invalid-*`; salvamento usa arquivo temporário e substituição. Relatórios exportados excluem caminhos, seriais e mensagens livres de erro.

## Estado verificado e pendências

Consulte [VALIDATION.md](docs/VALIDATION.md) para resultados reais e [MANUAL-TESTS.md](docs/MANUAL-TESTS.md) para o roteiro de aceitação. ViGEmBus é legado e descontinuado; `IVirtualControllerBackend` permite substituí-lo. O UGA não inclui instaladores de drivers. O executável e o Setup não têm assinatura de publicador própria; não foi fornecido certificado de assinatura.

Não há suporte a vibração, múltiplos controles, sensores, macros, injeção, overlay ou atualizador. Controles Xbox 360 com VID/PID `045E:028E` são excluídos também por segurança contra realimentação; isso inclui controles físicos que usem esses mesmos IDs.
