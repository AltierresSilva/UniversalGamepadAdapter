# UGA — manutenção e continuação

## Escopo

Windows 11 x64, C# net10.0-windows, WinForms, SDL3-CS/SDL3-CS.Windows 3.4.20.0, Nefarius.ViGEm.Client 1.21.256. Um controle físico e um Xbox virtual. UI pt-BR. Não adicionar mouse/teclado, sensores, macros, rede em operação normal, kernel próprio ou alterações de segurança.

## Arquitetura

- `src/UGA.Core`: contratos IPhysicalControllerSource, IVirtualControllerBackend, IMappingService, IProfileStore; modelos, validação, MappingService, ControllerEngine, temporizador Windows de alta resolução e Storage/JsonStore.
- `src/UGA.Infrastructure/Input`: SDL3, identificação PnP somente leitura com SetupAPI/ConfigMgr. Eventos SDL desativados; UpdateGamepads, hotplug/reabertura, varredura a cada segundo. Filtra virtual SDL, ancestral ViGEm e VID/PID de saída 045E:028E (também exclui físicos com esses IDs).
- `src/UGA.Infrastructure/Virtual`: backend ViGEm, reports enviados explicitamente, alvo único e limpeza. Driver oficial externo, não incluído.
- `src/UGA.Infrastructure/HidHide`: CLI oficial instalada, confirmação exata de HID, registro/verificação do exe antes de ocultar, backup antes de mutação, undo por entradas próprias, sem sobrescrever listas globais ou modo inverso.
- `src/UGA.Infrastructure/Diagnostics`: conflitos por nome de processo e exportação sem caminhos/seriais/texto livre de erro.
- `src/UGA.App`: Program, MainForm (comportamento) e MainForm.Layout (shell com cinco páginas e navegação lateral), Theme/ModernButton/CardPanel/IconPainter, ControllerView, MappingWizard, bandeja, startup opcional do usuário por arquivo na pasta Inicializar. Temas Dark/Light em AppSettings.Theme; aplicar imediatamente e persistir a preferência. Sem conta/avatar/perfil de usuário na UI; MappingProfile é apenas a persistência interna do mapeamento por dispositivo.
- `tests/UGA.Tests`: simulações, persistência e testes nativos. DriverFact executa apenas com UGA_DRIVER_TESTS=1.
- `Installer/UGA.iss`: Program Files, admin somente no Setup, página guiada oficial de drivers, desinstalação preserva drivers e permite dados pessoais opcionais (backup HidHide pendente é preservado).

## Comandos

`./build.ps1`, `./build.ps1 -WithDriverTests`, `./publish.ps1`. Os scripts usam SDK local `../.tools/dotnet/dotnet.exe` se disponível, ou o SDK instalado. Inno local `../.tools/InnoSetup/ISCC.exe`, ou forneça `-IsccPath`. Não instalar drivers em build/test/publish.

Publicação em `artifacts/Release/win-x64`, Setup em `artifacts/Installer`, TRX em `artifacts/test-results`. Preservar packages.lock.json. `docs/VALIDATION.md` registra resultados reais, e `docs/MANUAL-TESTS.md` os cenários pendentes.

## Invariantes

Todas as operações SDL/backend ficam numa única thread dedicada. UI recebe cópias ~25 Hz; leitura alvo 125–250 Hz, sem busy-wait. Erro/desconexão/parada/Dispose neutralizam e desconectam. Falha do backend não tenta recriar continuamente; exige Iniciar. Mapeamento suspende saída. Perfil manual não assume layout DualShock; calibração mede neutro e extremos. Raw axis não associado nunca gera comandos. Listas e configurações de terceiros devem permanecer intactas.


Consulte docs/VALIDATION.md e docs/MANUAL-TESTS.md para separar verificações reais de pendências. Declare uso de IA nos PRs conforme docs/AI-DISCLOSURE.md. Não exponha dados pessoais em commits ou logs.
