# Drivers oficiais e recuperação HidHide

## ViGEmBus obrigatório para a saída XInput

Fornecedor: https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0

Consulte também https://docs.nefarius.at/projects/ViGEm/End-of-Life/ e as instruções de instalação do fornecedor. ViGEmBus foi descontinuado. Não use sites alternativos nem atualizadores antigos. O UGA não baixa, inclui ou instala drivers automaticamente e não substitui o driver já instalado.

Baixe o pacote pela página oficial e verifique sua assinatura digital válida e o publicador Nefarius nas propriedades do arquivo antes de executá-lo. Se quiser registrar o SHA256 local, use `Get-FileHash -Algorithm SHA256` e compare com o publicado pelo fornecedor, quando disponível. Não execute binários com origem ou assinatura não verificáveis. Se o instalador oficial solicitar elevação/reinicialização, siga suas instruções.

O UGA abre mesmo sem o driver. O diagnóstico verifica acesso ao barramento; a funcionalidade de criação é testada ao iniciar emulação. Se a criação falhar, o virtual é liberado e o erro aparece sem encerrar o aplicativo. Feche outros remapeadores manualmente, tente novamente e, se persistir, reinicie/verifique o driver pelo fornecedor. Nunca desative a segurança do Windows.

## HidHide opcional

Fornecedor: https://github.com/nefarius/HidHide/releases

Documentação CLI usada: https://docs.nefarius.at/projects/HidHide/CLI-Usage/

1. Instale a distribuição oficial assinada e reinicie se solicitado.
2. Execute o UGA pelo caminho em que ficará instalado. Feche o HidHide Configuration Client para permitir o acesso exclusivo pela CLI.
3. Selecione o controle físico no UGA e revise o nome, VID/PID e caminho HID exibidos no consentimento.
4. **Configurar HidHide com segurança** primeiro registra o executável real, confirma a permissão, depois adiciona somente o HID físico confirmado no inventário oficial. Nunca esconde o Xbox virtual. Configurações de outros aplicativos são preservadas.
5. O UGA não altera o modo de lista inversa. Se há dispositivos ocultos preexistentes com a ocultação global desligada, ele pede revisão manual, pois ativá-la afetaria os outros dispositivos.
6. Reconecte o Bluetooth. Jogos devem enxergar apenas o Xbox virtual; o UGA deve continuar lendo o físico.

Se a CLI, formato do inventário, caminho HID ou identificação exata forem incompatíveis, o UGA recusa a automação. Continue usando o aplicativo sem ocultação ou configure manualmente no cliente oficial: adicione `UniversalGamepadAdapter.exe` em Applications antes de marcar SOMENTE o controle físico em Devices. Não selecione o Xbox 360 virtual nem dispositivos de teclado/mouse. Preserve as outras entradas existentes.

## Recuperação

- Clique **Desfazer somente alterações UGA**. O UGA remove somente a permissão e o dispositivo que adicionou. Desativa a ocultação global apenas se a ativou e não há mais dispositivos ocultos, preservando as configurações de outros programas.
- Backup e intenção de alteração: `%LocalAppData%\UniversalGamepadAdapter\hidhide-changes.json`. É salvo antes da primeira mudança, inclusive para recuperar de uma falha parcial.
- Se o controle ficar invisível, abra o cliente oficial e desmarque apenas o HID físico registrado no backup. Reconecte ou reinicie. Não apague listas globais nem remova drivers compartilhados.
- Ao mover o executável, desfaça a configuração antiga antes de configurar o novo caminho. A permissão HidHide corresponde ao caminho real do programa.
- Antes de desinstalar UGA, desfaça suas alterações. Se o backup ainda existir, o desinstalador preserva os dados e informa a recuperação manual.
- O UGA não termina DS4Windows, Steam ou outros processos e não altera o Registro para configurar HidHide.
