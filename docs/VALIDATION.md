# Resultados reais — 7 de novembro de 2026

## Reformulação visual

Interface refeita a partir da referência do usuário, mantendo Windows Forms e a janela nativa do Windows (redimensionar, maximizar e minimizar). Sem perfil de usuário/avatar. Navegação lateral: Visão geral, Mapeamento, Calibração, Diagnóstico e Configurações. Temas Dark/Light em JSON, alternância imediata pelo cabeçalho ou Aparência. Reabertura real observada no modo claro escolhido anteriormente; tema também coberto pelo teste automatizado de persistência.

Desenhos em GDI+ sem imagens web: PS4 com analógicos simétricos, touchpad, símbolos PlayStation e ombros L1/R1; Xbox 360 com analógicos deslocados, D-pad inferior esquerdo e botões ABXY. Genéricos não recebem touchpad ou identificação de modelo não confirmado. Prévia de entrada física separada da saída Xbox. Calibração possui tela própria e assistente opcional só para os seis eixos.

Release compila com 0 erros e 0 avisos; 58 testes aprovados, incluindo o teste nativo do ViGEmBus. Verificação visual executada nas telas reformuladas e nos dois temas. Bateria, sinal, latência em ms e percentuais de saúde fictícios da referência não foram incluídos: os cartões usam somente dados reais disponíveis. O medidor em Hz indica leitura, não latência ponta a ponta.

As medições de CPU/memória abaixo pertencem à interface anterior; não foram repetidas para a reformulada. Testes de jogo/xCloud e recuperação física do HidHide continuam pendentes.

Logo integrada: o original do usuário permanece preservado byte a byte, mas a UI usa recorte com transparência externa e margens reduzidas (UGA-brand.png), preparado com Imagegen integrado para remover a aparência de bloco preto. Corpo escuro, controle verde e esfera azul/vermelha com X branco preservados. LogoView renderiza o PNG diretamente e mantém a proporção; ícones Windows usam a esfera com X em destaque, sem margem adicional, para preencher a área disponível na barra de título/bandeja. A moldura nativa do Windows foi mantida. Build e verificações disponíveis registrados abaixo; prompt e assets em LOGO.md.

## Ambiente e build

- Windows 11 x64, SDK .NET 10.0.403 instalado localmente em `../.tools/dotnet` (SDK global anterior era 9.0.305).
- Pacotes restaurados e APIs efetivamente compiladas: SDL3-CS 3.4.20.0, SDL3-CS.Windows 3.4.20.0 e Nefarius.ViGEm.Client 1.21.256. NuGet normaliza 3.4.20.0 para 3.4.20 no lock file.
- `build.ps1 -WithDriverTests`: restore aprovado; Release com **0 erros, 0 avisos**; **58 testes aprovados, 0 falhas, 0 ignorados**. Resultado em `artifacts/test-results/UGA-tests.trx`.
- Testes incluem botões, normalização/deadzone/inversão, gatilhos, hats/D-pad, neutro/entradas desconhecidas, validação/conflitos, JSON/perfil/calibração, desconexão e falhas simuladas de leitura/backend, descarte de recursos.
- Sete testes HidHide usam exclusivamente um executor e uma identidade simulados: permissão antes de ocultação, preservação de terceiros, undo, listas preexistentes, modo inverso, dispositivo virtual recusado, falha parcial e ativação global preservada quando outro dispositivo foi acrescentado.
- Publicação `win-x64 --self-contained true`, sem single-file/trimming: aproximadamente 122 MiB, incluindo coreclr, Windows Forms, SDL3.dll, bindings e cliente ViGEm (nativo incorporado ao assembly). Hashes em `artifacts/publish-sha256.json`.
- Inno Setup **6.7.4**: Setup.exe gerado com sucesso, aproximadamente 36,8 MiB. O compilador avisa sobre uso de área por usuário em instalador administrativo: limpeza da Inicialização/configurações refere-se ao usuário que executa a desinstalação. Se elevar usando outra conta, os dados da conta original podem permanecer; desative a inicialização e desfaça HidHide no UGA antes de desinstalar. Não foi suprimido o aviso.
- A ferramenta Inno foi obtida da página oficial `https://jrsoftware.org/isdl.php`, release oficial GitHub; assinatura Authenticode **Valid**, publicador **Pyrsys B.V.**; SHA256 do instalador da ferramenta: `A549929319F9120716677C685535CA4671E4B937B5FDC87E2585F32D8EB30E8C`.
- Nenhum driver foi instalado, atualizado ou removido durante o trabalho. O UGA e seu Setup não são assinados com certificado próprio.

## Verificações executadas no computador

- Executável publicado aberto sem controle conectado: janela responsiva, botões/abas acessíveis, status aguardando, driver acessível e aviso DS4Windows.
- Navegação visual pelas telas Controle, Mapeamento, Diagnóstico e Configurações. A revisão encontrou/alinhou textos do desenho e preservou os tamanhos dos títulos. A versão final adicionou botão Sair e identificação Bluetooth pela árvore PnP.
- Durante o trabalho, o Windows passou a apresentar **Wireless Controller** por Bluetooth, e SDL o abriu como **PS4 Controller**, VID `054C`, PID `09CC`. Não se presume que esses IDs comprovem autenticidade do fabricante.
- Leitura real: sticks inicialmente 0, gatilhos brutos −32768 e gatilhos SDL 0. Pequena variação de centro observada (128), dentro da deadzone de 8%. Origem RX/RY visível na interface. A versão final identifica o transporte como Bluetooth.
- Iniciar emulação pela interface resultou em **Xbox 360 virtual ativo**; o Windows apresentou **Controlador XBOX 360 para Windows**, status OK. Após parar, não havia Xbox 360 presente na consulta PnP. Nenhum comando de teclado/mouse foi implementado.
- Teste nativo automatizado no ViGEmBus instalado **1.21.442.0**, assinado: cria um alvo, chama Connect novamente sem duplicar, envia estado neutro, verifica incremento de um slot via XInput e retorno à contagem inicial ao liberar. SDL exclui o Xbox gerado.
- Houve uma primeira falha transitória real ao adicionar o alvo: `Win32Exception` com mensagem nativa “A operação foi concluída com êxito”. Causa não confirmada. Uma chamada diagnóstica ao cliente oficial e as execuções nativas/completas posteriores passaram. A UI agora explica falhas de criação e orienta revisão dos remapeadores/driver/reinicialização, preservando o erro original.
- HidHide instalado detectado (driver 1.4.181.0). **Não foi ocultado nenhum controle real**, nem alteradas as listas HidHide ou o DS4Windows. O DS4Windows permaneceu aberto e foi apontado como possível conflito.

## Medição de recursos

Medição de 60,20 segundos, controle conectado, emulação parada e janela aberta, máquina com 8 processadores lógicos e outros aplicativos em execução:

| Medida | Resultado |
|---|---:|
| CPU média em relação à capacidade total | 1,85% |
| CPU equivalente a um núcleo | 14,82% |
| Working set médio | 86,98 MiB |
| Working set máximo observado | 87,21 MiB |
| Threads observadas no fim | 19 |

Dados em `artifacts/performance.json`. A contagem inclui threads do CLR, SDL, UI e bibliotecas; há uma única thread de processamento criada pelo motor. Frequência indicada na UI variou sob carga; 125 Hz é alvo, não garantia em toda atualização. A espera usa timer Windows de alta resolução sem busy-wait. Esses números não medem latência ponta a ponta nem garantem consumo em outro cenário/dispositivo.

## Pendências de aceitação manual

- LED/vibração (07/11/2026): compilação Release sem erros/avisos, **66 testes aprovados**, incluindo ViGEmBus real neutro. Oito novos testes cobrem cor/reconexão, suporte ausente, intensidade dos dois motores, 0%, teste limitado, parada/Dispose, feedback tardio, falhas isoladas e persistência/validação. LED e vibração físicos ainda precisam de confirmação visual/tátil; não foi executado jogo para validar retorno real. Comandos SDL ficam na thread dedicada; callback ViGEm somente armazena feedback. Pulsos dos jogos expiram em 250 ms e são renovados a cada 100 ms; teste manual expira em 500 ms. Falha de saída física não desativa leitura/emulação nem repete tentativas a cada poll.

- Conferir cada botão, eixo, gatilho e limites com o controle físico; executar o assistente inteiro e conferir associações físicas salvas pela UI.
- Desconexão/reconexão Bluetooth real enquanto há botão pressionado; o caminho de desconexão foi testado por simulação.
- Movimento físico/giroscópio/touchpad durante jogo e ausência de câmera indevida; o código não lê sensores nem envia mouse/teclado, mas o comportamento do jogo e de outros remapeadores não foi testado.
- Ocultação/recuperação HidHide com dispositivo real, permissões e reconexão; somente o algoritmo foi testado por simulação.
- Edge/Chrome/Opera, aplicativo Xbox e jogos/xCloud. Nenhuma sessão de jogo foi executada.
- Instalação/desinstalação em uma VM limpa, elevação por outra conta, reinicializações e configuração do driver ausente. O Setup foi compilado, mas não instalado no computador do usuário durante a implementação.
- Latência com instrumentação e consumo em emulação ativa prolongada; ainda não medidos.

Use `MANUAL-TESTS.md` para registrar esses resultados. A entrega técnica foi produzida, mas não representa certificação de compatibilidade com todos os controles, navegadores ou jogos.
