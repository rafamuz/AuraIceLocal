# Manual do usuário — RM Aura Ice Display 0.3

O RM Aura Ice Display monitora sensores do computador e envia informações de temperatura e utilização para o visor do water cooler Rise Mode Aura Ice. Este manual descreve cada parte do painel, o motivo de ela existir e os cuidados necessários para usar o aplicativo com segurança.

O manual também está disponível dentro do programa pelo menu **Ajuda > Manual do usuário** ou pela tecla **F1**.

## Visão geral

O aplicativo realiza três trabalhos principais:

- identifica com segurança o visor HID compatível;
- lê temperaturas e percentuais de utilização do computador;
- monta e envia ao visor um relatório HID de exatamente 11 bytes, uma vez por segundo.

O RM Aura Ice Display foi validado com o dispositivo:

```text
VID: AA88
PID: 8666
Produto: 温度显示HID设备
Fabricante: 铭研科技
Saída: 11 bytes
Entrada: 11 bytes
Feature: 0 bytes
Usage Page: 0x0001
Usage: 0x0000
```

O programa precisa ser executado como administrador para acessar todos os sensores e o dispositivo HID. Ele não deve ser usado ao mesmo tempo que o software oficial da Rise Mode.

## Primeiros passos

1. Conecte o cabo USB interno do visor.
2. Feche completamente o software oficial da Rise Mode.
3. Abra o RM Aura Ice Display como administrador.
4. Clique em **Procurar visores**.
5. Confirme que o visor AA88:8666 aparece como **Confirmado** e com saída de 11 bytes.
6. Mantenha **Core Average** como sensor de exibição ou escolha outro sensor da CPU.
7. Clique em **Iniciar monitoramento**.

Ao clicar em **Iniciar monitoramento**, o aplicativo passa a enviar dados reais ao USB. Não é apenas uma visualização local.

## Menu Ajuda

### Manual do usuário

Abre esta documentação dentro do aplicativo. A lista à esquerda separa os assuntos e o painel à direita mostra as instruções. O atalho **F1** abre o mesmo manual.

O manual é incorporado ao executável durante a compilação. Isso garante que a ajuda instalada corresponda à versão do aplicativo.

### Sobre o RM Aura Ice Display

Mostra a versão instalada, a finalidade do programa e a identificação básica do perfil HID suportado. Essa informação será útil para conferir a versão antes de procurar ou instalar atualizações.

## Seleção do visor LCD

### Lista Visor LCD

Mostra os dispositivos HID que apresentam alguma evidência relacionada aos perfis conhecidos. Cada opção informa classificação, nome, VID/PID e tamanho do relatório de saída.

Quando existe exatamente um dispositivo confirmado, ele pode ser selecionado automaticamente. Se houver mais de um, o usuário precisa escolher o correto.

Trocar o visor selecionado interrompe qualquer autorização de escrita existente, desconecta o transporte e, se necessário, para o monitoramento. Isso impede que a seleção mude durante um envio.

### Botão Procurar visores

Faz uma nova enumeração dos dispositivos HID conectados. Use este botão quando:

- o visor foi conectado depois que o programa abriu;
- o cabo USB foi removido e reconectado;
- o visor foi trocado de porta USB;
- a lista não mostra o dispositivo esperado;
- os dados de entrada, saída ou fabricante precisam ser atualizados.

O caminho HID é utilizado somente durante a execução atual. Ele não é salvo nas configurações.

### Indicação Perfis

Mostra de onde vieram os perfis usados no reconhecimento. O perfil distribuído acompanha o aplicativo em `device-profiles.json`. Um perfil personalizado em `%LOCALAPPDATA%\AuraIceLocal\device-profiles.json` tem prioridade.

Perfis personalizados devem ser usados somente depois de confirmar que o novo dispositivo utiliza exatamente o protocolo AuraIceV1.

## Classificação dos dispositivos

### Confirmado

É a única classificação autorizada a chegar ao transporte USB. Para o perfil Rise Mode Aura Ice, exige:

- VID AA88 e PID 8666;
- relatório de saída com exatamente 11 bytes;
- perfil compatível com o protocolo AuraIceV1.

Entrada, Feature, Usage Page, Usage, produto e fabricante aumentam a pontuação e ajudam no diagnóstico, mas não substituem os requisitos principais.

### Reconhecido

O VID/PID corresponde ao perfil, mas algum requisito necessário para o transporte não corresponde, como o tamanho do relatório. O dispositivo aparece para diagnóstico, porém nenhuma escrita é permitida.

### Possível

Existem semelhanças auxiliares, mas não há confirmação suficiente. Nenhum pacote pode ser enviado.

### Desconhecido

Não corresponde de maneira segura aos perfis conhecidos. Nenhum pacote pode ser enviado.

## Sensor da CPU e suavização

### Lista Sensor da CPU

Escolhe qual temperatura será usada como valor normal do visor. As opções principais são:

- **Core Average:** média dos núcleos; é o padrão e costuma representar melhor a temperatura geral;
- **CPU Package:** temperatura do encapsulamento do processador;
- **Core Max:** maior temperatura instantânea entre os núcleos.

O painel de sensores pode mostrar outros sensores encontrados pelo LibreHardwareMonitor, mas o perfil padrão permanece **Core Average**.

### Campo Suavização

Define em segundos a resposta do filtro exponencial. O valor padrão é aproximadamente 3 segundos.

- valor menor: o visor reage mais rapidamente, mas oscila mais;
- valor maior: o visor fica mais estável, mas demora mais para acompanhar mudanças normais;
- zero: praticamente remove a suavização normal.

A suavização não atrasa a proteção térmica. Em temperatura crítica, o maior valor de proteção é exibido imediatamente.

## Botões de monitoramento

### Iniciar monitoramento

Valida o visor selecionado, verifica se o software oficial está fechado, conecta ao HID e começa a leitura contínua.

Durante o monitoramento:

- os sensores são lidos a cada 250 ms;
- a temperatura normal passa pelo filtro de suavização;
- Core Max e CPU Package são observados pela proteção térmica;
- o visor recebe um pacote de 11 bytes aproximadamente uma vez por segundo;
- o painel e o ícone da bandeja são atualizados.

O botão muda para **Parar monitoramento** enquanto o processo está ativo.

### Parar monitoramento

Cancela o laço de leitura, retira a autorização de escrita e desconecta o transporte HID. Parar o monitoramento não fecha o aplicativo e não desativa as opções de inicialização automática.

### Enviar um pacote de teste

Envia exatamente um relatório e não inicia monitoramento contínuo. O botão começa desativado e só é habilitado quando:

- o dispositivo está **Confirmado**;
- o relatório de saída tem 11 bytes;
- não existe monitoramento contínuo ativo;
- o software oficial da Rise Mode está fechado.

Antes do envio, uma confirmação mostra VID/PID, produto, tamanho e todos os bytes do pacote. Depois da tentativa, o transporte é desconectado. Cancelar a confirmação não envia nada.

## Automação

### Iniciar com o Windows

Cria uma tarefa agendada chamada `RM Aura Ice Display`, executada no logon com privilégios elevados. Quando aberto por essa tarefa, o aplicativo inicia somente na bandeja, sem mostrar o painel. Instalações anteriores que ainda tenham a tarefa legada `AuraIceLocal` são migradas ao reativar essa opção.

Na versão portátil atual, a tarefa usa o caminho absoluto do executável que estava aberto quando a opção foi marcada. Se a pasta for movida, desative e ative novamente a opção para registrar o novo caminho.

O instalador e o atualizador planejados usarão um lançador estável, evitando que o caminho mude entre versões.

### Monitorar e enviar ao abrir

Quando marcada, inicia automaticamente a leitura e o envio assim que o aplicativo abre, desde que exista um visor confirmado e o software oficial esteja fechado.

Quando combinada com **Iniciar com o Windows**, o fluxo é totalmente automático: o Windows abre o RM Aura Ice Display na bandeja e o aplicativo começa a monitorar e enviar sem que o painel precise ser aberto.

Quando desmarcada, o aplicativo abre parado e aguarda o botão **Iniciar monitoramento**.

## Atualizações do aplicativo

### Botão Verificar atualizações

Consulta as Releases públicas de `https://github.com/rafamuz/AuraIceLocal` e compara a versão disponível com a versão instalada.

Na cópia instalada pelo Setup oficial, o fluxo é:

1. procurar uma versão estável mais nova;
2. pedir autorização para baixar;
3. baixar em segundo plano, mostrando o percentual no painel;
4. manter o monitoramento funcionando durante o download;
5. pedir uma segunda confirmação antes de aplicar;
6. parar o monitoramento e desconectar o USB;
7. salvar preferências e posição da janela;
8. fechar, substituir os arquivos e reiniciar automaticamente;
9. retomar o monitoramento se **Monitorar e enviar ao abrir** estiver marcado.

Uma cópia `Debug` ou portátil informa que a atualização integrada só fica disponível depois da instalação pelo Setup. Isso evita tentar substituir arquivos de desenvolvimento ou pastas movidas manualmente.

O RM Aura Ice Display não instala uma atualização sem confirmação. O download pode ocorrer com o painel aberto, mas a substituição dos arquivos exige um reinício rápido porque o Windows mantém o executável e as DLLs em uso enquanto o processo está aberto.

O Velopack valida o pacote baixado antes da aplicação. Releases preliminares não são oferecidas pelo canal estável.

## Resumo de estado

### Estado

Informa se o programa está parado, iniciando, monitorando ou se ocorreu algum erro. Mensagens de bloqueio também aparecem aqui.

### LCD USB

Mostra se existe visor compatível selecionado e se ele está reconhecido com segurança. Uma mensagem de envio bloqueado significa que os requisitos do transporte não foram satisfeitos.

### Sensor de exibição

É o nome do sensor escolhido para a leitura normal, como Core Average.

### Temperatura bruta

É a leitura mais recente do sensor de exibição antes do filtro. Ela reage rapidamente e pode oscilar entre amostras.

### Temperatura suavizada

É o resultado do filtro exponencial aplicado à temperatura bruta. Esse é o valor normal usado no visor quando a proteção térmica não está ativa.

### Temperatura exibida

É o valor efetivamente colocado no campo de temperatura da CPU do próximo pacote. Normalmente corresponde à temperatura suavizada; durante proteção térmica, passa a ser o maior valor crítico.

### Sensor de proteção

Mostra qual sensor entre Core Max e CPU Package está fornecendo a maior leitura de proteção.

### Proteção térmica

Mostra um dos seguintes estados:

- **Normal — suavização ativa:** o visor usa a temperatura suavizada;
- **ATIVA — valor imediato:** Core Max ou CPU Package atingiu pelo menos 80 °C;
- **ATIVA — aguardando 5 s abaixo de 75 °C:** a temperatura caiu, mas o período seguro ainda não terminou.

### Pacote

Apresenta os valores preparados para o LCD e sua representação hexadecimal. Quando ocorre um envio de teste, o painel identifica explicitamente que o pacote foi enviado uma vez.

## Proteção térmica

A proteção térmica é independente do sensor escolhido para exibição.

Se **Core Max** ou **CPU Package** atingir 80 °C ou mais, a suavização é ignorada imediatamente e o visor recebe o maior valor encontrado. Isso evita que um filtro lento esconda uma elevação crítica.

Para retornar ao modo normal, os sensores de proteção precisam permanecer abaixo de 75 °C durante 5 segundos contínuos. Se a temperatura voltar a 75 °C ou mais nesse intervalo, a contagem é reiniciada.

Essa função é apenas informativa. Ela não substitui proteções da BIOS, controle de ventoinhas ou mecanismos térmicos do processador.

## Pacote HID AuraIceV1

Cada envio contém exatamente 11 bytes:

```text
Índice 0: Report ID, sempre 0
Índice 1: temperatura da CPU
Índice 2: temperatura da GPU
Índice 3: utilização da CPU
Índice 4: utilização da RAM
Índice 5: temperatura da placa-mãe
Índice 6: utilização da GPU
Índices 7 a 10: zero
```

Temperaturas são arredondadas para o inteiro mais próximo e limitadas entre 0 e 125 °C. Percentuais são arredondados e limitados entre 0 e 100%. Valores indisponíveis ou inválidos se tornam zero.

Antes de escrever, o transporte compara o tamanho do pacote com `GetMaxOutputReportLength()` do dispositivo. Qualquer diferença bloqueia o envio.

## Abas de diagnóstico

### Dispositivos HID / diagnóstico

Exibe os dispositivos encontrados e os dados disponíveis:

- confiança e pontuação;
- perfil associado;
- VID/PID;
- produto, fabricante e número de série;
- tamanhos de saída, entrada e Feature;
- Usage Page e Usage;
- justificativas da classificação;
- caminho HID da execução atual.

O caminho é mostrado apenas para diagnóstico e não é persistido.

### Sensores de temperatura da CPU

Lista os sensores de temperatura enumerados, seu identificador interno e o valor atual. Essa aba ajuda a confirmar a presença de Core Average, CPU Package e Core Max e a diagnosticar computadores com nomes diferentes.

## Bandeja do sistema

Fechar a janela pelo botão **X** não encerra o RM Aura Ice Display. O painel é escondido e o monitoramento continua.

O ícone da bandeja mostra a temperatura inteira atualmente destinada ao visor. O número e a cor mudam conforme a temperatura é atualizada.

O menu da bandeja oferece:

- **Painel:** mostra e traz a janela principal para frente;
- **Sair:** para o monitoramento, desconecta o HID e encerra realmente o aplicativo.

Use **Sair** antes de atualizar manualmente arquivos da versão portátil.

## Janela e preferências

Na primeira execução, a janela abre centralizada com aproximadamente 60% da largura e 80% da altura útil da tela.

O aplicativo memoriza:

- posição da janela;
- largura e altura;
- estado normal ou maximizado;
- sensor escolhido;
- suavização;
- opções de automação;
- identidade não sensível do visor selecionado.

Se um monitor for removido ou a resolução mudar, a janela salva é trazida de volta para uma área visível.

## Segurança e concorrência USB

O RM Aura Ice Display aplica as seguintes travas em todas as escritas:

- somente dispositivos **Confirmado** chegam ao transporte;
- o pacote precisa ter exatamente o tamanho exigido pelo HID;
- o Report ID zero precisa estar na primeira posição;
- o software oficial da Rise Mode precisa estar fechado;
- envio e desconexão usam sincronização para não ocorrerem ao mesmo tempo;
- trocar o visor selecionado cancela a autorização anterior;
- testes automatizados não abrem o transporte USB.

Não execute o software oficial e o RM Aura Ice Display escrevendo simultaneamente. Dois programas tentando controlar o mesmo HID podem causar falhas, dados alternados ou bloqueio do dispositivo.

## Dados armazenados e privacidade

As preferências ficam em:

```text
%LOCALAPPDATA%\AuraIceLocal\settings.json
```

O RM Aura Ice Display não registra histórico de sensores nem conteúdo USB. O instalador/atualizador Velopack pode manter seu próprio registro técnico de instalação, atualização ou falha. Esse registro não contém histórico de temperatura nem os pacotes enviados ao LCD.

O aplicativo não persiste:

- DevicePath;
- InstanceId;
- porta USB;
- histórico de temperaturas;
- conteúdo enviado ao visor.

A identidade salva contém apenas perfil, VID/PID e, quando disponíveis, série e nome do produto. Isso permite reencontrar o visor mesmo se ele mudar de porta.

## Solução de problemas

### O visor não aparece

- confira o cabo USB interno;
- clique em **Procurar visores**;
- teste outra porta USB;
- verifique a aba de dispositivos HID;
- confirme se o Windows enumera AA88:8666.

### O visor aparece, mas o envio está bloqueado

- confirme que a classificação é **Confirmado**;
- verifique se a saída é exatamente 11 bytes;
- feche completamente o programa oficial da Rise Mode;
- procure os visores novamente.

### O monitoramento automático não inicia

- confirme **Monitorar e enviar ao abrir**;
- selecione um visor confirmado;
- feche o software oficial;
- confira a mensagem do campo Estado.

### O aplicativo não abre no logon

- desative e ative novamente **Iniciar com o Windows**;
- se a versão portátil foi movida, registre novamente a tarefa;
- confira a tarefa `RM Aura Ice Display` no Agendador de Tarefas.

### A temperatura não aparece

- abra a aba de sensores;
- confirme que Core Average, CPU Package ou Core Max possuem valor;
- execute o programa como administrador;
- atualize drivers do chipset e sensores se o LibreHardwareMonitor não encontrar leituras.

### Quero encerrar completamente

Abra o menu do ícone na bandeja e clique em **Sair**. O botão X apenas esconde o painel.
