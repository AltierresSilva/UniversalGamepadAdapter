# Teste de aceitação no computador do usuário

Registre data, versão Windows, modelo/VID/PID, transporte, driver e resultado. Casos abaixo NÃO são presumidos aprovados por passar nos testes simulados.

| Caso | Procedimento e resultado esperado |
|---|---|
| Sem controle | Abrir UGA sem controle. Janela responsiva, aguardando controle; sem virtual extra. |
| Sem ViGEmBus | Em uma máquina/VM sem o driver, abrir UGA. Instruções claras; Iniciar falha sem crash. Não desinstale o driver compartilhado apenas para esse teste. |
| Wireless Controller Bluetooth | Emparelhar pelo Windows e selecionar no UGA. Nome, IDs disponíveis e conexão visíveis. SDL pode informar somente “sem fio”, sem distinguir Bluetooth de receptor. |
| Layout automático | Conferir A/B/X/Y, ombros, cliques, Start/Back, D-pad, dois analógicos e gatilhos na prévia. Não presumir layout interno de DualShock original. |
| Mapeamento bruto | Assistente completo: corpo imóvel, neutro, botões/hat, extremos dos dois sticks e gatilhos soltos/pressionados. Perfil incompleto não inicia emulação. Entradas repetidas são recusadas. Salvar, sair, reabrir; perfil/calibração devem reaparecer. |
| Deadzone e inversão | Testar 0%, 8%, 30%, inversão individual e restauração. Centro deve ficar em zero; extremos devem atingir faixa completa. |
| XInput | Iniciar e abrir joy.cpl. Um Xbox 360 virtual; pressionar controles e conferir cada comando, diagonais, gatilhos separados. Iniciar repetidamente não duplica. |
| Reconexão | Enquanto segura botão/stick, desligar Bluetooth/controle. Saída deve neutralizar e desaparecer. Reconectar: leitura reabre sem threads extras; reinício automático apenas conforme preferência/solicitação vigente. |
| Câmera | Em jogo, deixar stick direito parado, girar/inclinar o corpo do controle e tocar no touchpad. UGA não deve alterar RX/RY nem enviar mouse. Observar origem RX/RY ao vivo. Se o próprio jogo ou outro programa usa sensores, revisar sua configuração. |
| Conflito | Abrir DS4Windows ou outro remapeador. Aviso não invasivo no diagnóstico/controle. Encerrar o outro software manualmente e clicar Verificar driver e conflitos. UGA nunca encerra processos. |
| HidHide | Confirmar exe real permitido antes de ocultar somente o HID físico; reconectar. Jogos veem virtual, UGA ainda vê físico. Preservar entradas de outros aplicativos. Testar Desfazer e recuperação manual do guia. |
| Edge / Chrome | Após Iniciar, abrir xCloud e entrar num jogo. Confirmar navegação, botões, D-pad, sticks e gatilhos. Repetir por navegador/jogo; registrar resultado. |
| Opera | Mesmo roteiro; compatibilidade experimental, dependente do navegador e serviço. |
| Aplicativo Xbox | Abrir sessão de nuvem/jogo compatível e repetir. Não presumir equivalência com navegador. |
| Parada e saída | Parar remove virtual. Fechar janela com opção bandeja mantém execução; Sair na bandeja libera tudo. Com bandeja desativada, X encerra. |
| Instalador | Em VM Windows 11 x64 limpa: Setup em Program Files, atalho opcional, sem runtime instalado. Confirmar app abre, driver ausente é tratado; testar preservar/remover configurações na desinstalação. Drivers compartilhados devem permanecer. |
| Recursos | Medir CPU média por pelo menos 60s, memória e taxa real, em repouso e com controle ativo. Medir latência com hardware apropriado; não inferir latência ponta a ponta da frequência alvo. |

Para defeitos, exporte o JSON na aba Diagnóstico. O relatório não inclui caminhos HID, seriais, perfil do Windows ou texto livre dos erros. Os caminhos ficam visíveis apenas no diagnóstico local e no backup HidHide local.
# LED e vibração

- Em Configurações, conferir suporte RGB/rumble com controle USB e Bluetooth; verificar opções desabilitadas em controle incompatível/ausente.
- Salvar uma cor, observar LED, reabrir UGA e reconectar controle. Desmarcar a cor impede reaplicação automática; não restaura uma cor padrão.
- Testar vibração em 0/25/50/100%, confirmar duração de 0,5 s, parada imediata e ausência de efeito preso após desconectar/sair.
- Em jogo com Xbox virtual ativo, conferir motores distintos e intensidade; parar a emulação e verificar que a vibração termina. Fechar outros remapeadores para evitar disputa.
