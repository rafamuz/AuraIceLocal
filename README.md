# RM Aura Ice Display

Aplicativo local experimental para o LCD do **Rise Mode Aura Ice Black 360 mm ARGB — RM-WAIB-06-ARGB**.

O manual detalhado de operação está em [docs/MANUAL_DO_USUARIO.md](docs/MANUAL_DO_USUARIO.md) e também pode ser aberto dentro do aplicativo em **Ajuda > Manual do usuário** ou pela tecla **F1**.

As versões instaláveis e as atualizações são publicadas em [GitHub Releases](https://github.com/rafamuz/AuraIceLocal/releases). A instalação oficial usa Velopack; o botão **Verificar atualizações** baixa a nova versão, encerra o transporte USB com segurança, aplica os arquivos e reinicia o aplicativo.

## Versão 0.3 — protocolo real de 11 bytes e proteção térmica

O LCD não é mais tratado como um caminho USB fixo. O aplicativo agora:

- enumera os dispositivos HID instalados;
- reconhece perfis por VID/PID, comprimento do relatório, fabricante, produto e, quando disponível, Usage Page/Usage;
- atribui pontuação e confiança (`Confirmado`, `Reconhecido`, `Possível` ou `Desconhecido`);
- seleciona automaticamente somente quando existe um único visor reconhecido com segurança;
- exige seleção manual quando existem vários visores compatíveis;
- bloqueia totalmente a escrita em dispositivos desconhecidos;
- guarda perfil, VID/PID, série e nome do produto, mas **nunca salva DevicePath ou porta USB**;
- reencontra o dispositivo mesmo depois de trocar a porta USB;
- usa perfis externos em `device-profiles.json`, permitindo adicionar revisões futuras sem alterar o código.

O perfil conhecido inicial é:

```text
Rise Mode Aura Ice / AuraIceV1
VID: AA88
PID: 8666
Relatórios confirmados: saída 11 bytes, entrada 11 bytes e feature 0 bytes.

Descritor auxiliar confirmado: Usage Page `0x0001`, Usage `0x0000`, produto `温度显示HID设备` e fabricante `铭研科技`. Os textos são apenas evidências adicionais e não impedem a confirmação se VID/PID, saída de 11 bytes e capacidade de escrita estiverem presentes.
```

## Monitoramento de temperatura

Também estão implementados:

- leitura pelo LibreHardwareMonitor;
- escolha entre `Core Average`, `CPU Package`, `Core Max` ou outro sensor encontrado;
- suavização exponencial configurável;
- arredondamento correto para o inteiro mais próximo;
- temperatura crítica exibida imediatamente;
- montagem do relatório HID do LCD;
- exibição do pacote enviado em hexadecimal.

## Segurança

Ao iniciar o monitoramento, o aplicativo conecta e envia ao USB automaticamente, mas somente quando:

1. selecionar um dispositivo `Confirmado` por um perfil `AuraIceV1`;
2. fechar completamente o programa oficial da Rise Mode;
3. o relatório de saída real tem exatamente 11 bytes.

O RM Aura Ice Display não envia pacotes a dispositivos apenas classificados como `Possível` ou `Desconhecido`.

Nenhum arquivo de log é criado. Apenas preferências são guardadas em:

```text
%LOCALAPPDATA%\AuraIceLocal\settings.json
```

## Perfis de dispositivos

O perfil distribuído fica em:

```text
device-profiles.json
```

Para testar um perfil personalizado sem modificar a instalação, crie:

```text
%LOCALAPPDATA%\AuraIceLocal\device-profiles.json
```

O arquivo personalizado tem prioridade. Exemplo:

```json
{
  "schemaVersion": 2,
  "profiles": [
    {
      "id": "rise-mode-aura-ice-v1",
      "name": "Rise Mode Aura Ice",
      "protocol": "AuraIceV1",
      "vendorId": "AA88",
      "productId": "8666",
      "outputReportLength": 11,
      "inputReportLength": 11,
      "featureReportLength": 0,
      "usagePage": "0x0001",
      "usage": "0x0000",
      "productNameContains": ["温度显示HID设备"],
      "manufacturerContains": ["铭研科技"]
    }
  ]
}
```

Um novo VID/PID deve ser adicionado somente depois de confirmar que usa o mesmo protocolo. O transporte AuraIceV1 exige correspondência exata entre o pacote de 11 bytes e o relatório de saída do dispositivo; dispositivos Possível ou Desconhecido nunca chegam ao transporte USB.

O monitor usa `Core Average` por padrão, lê CPU/GPU/memória a cada 250 ms, consulta placa-mãe/Super I/O a cada 2 segundos, não consulta o Embedded Controller, aplica EMA de aproximadamente 3 segundos e atualiza o LCD uma vez por segundo. `Core Max` e `CPU Package` alimentam uma proteção independente: a partir de 80 °C o maior valor é exibido imediatamente e a suavização só retorna após 5 segundos contínuos abaixo de 75 °C.

O LibreHardwareMonitor 0.9.6 requer o driver PawnIO 2.2 ou superior para obter as temperaturas. Quando ele não está instalado, o painel oferece **Instalar suporte de sensores**. A instalação só começa após confirmação explícita, usa o arquivo oficial assinado, confere o SHA-256 e remove o download temporário ao terminar.

O botão **Enviar um pacote de teste** permanece desativado até que todas as travas de segurança sejam atendidas. Ele exige confirmação explícita, envia no máximo um relatório e desconecta o transporte após a tentativa.

## Inicialização e monitoramento automáticos

A interface oferece duas opções independentes:

- **Iniciar com o Windows:** registra uma tarefa de logon com privilégios elevados e abre o aplicativo minimizado;
- **Monitorar e enviar ao abrir:** inicia a leitura dos sensores e o envio ao visor confirmado assim que a interface é carregada.

Quando **Monitorar e enviar ao abrir** está marcado, o aplicativo inicia a leitura e o envio ao visor confirmado automaticamente. Desmarcado, ele abre parado e aguarda o botão **Iniciar monitoramento**.

Durante suspensão ou hibernação, o aplicativo fecha temporariamente os acessos aos sensores e ao HID. Depois da retomada, aguarda os drivers, enumera novamente o visor e reinicia o monitoramento somente se ele estava ativo antes, com até cinco tentativas e todas as validações de segurança antes da primeira escrita.

Na primeira execução, o painel abre centralizado com aproximadamente 60% da largura e 80% da altura útil da tela. Posição, tamanho e estado maximizado são lembrados nas execuções seguintes. A tarefa agendada usa o caminho absoluto do executável que criou o registro; em uma instalação portátil, desative e ative novamente a inicialização automática depois de mover a pasta.

## Bandeja do sistema

O ícone da bandeja exibe a temperatura atualmente escolhida para o visor e muda de cor conforme a faixa térmica. O número é atualizado quando a temperatura inteira muda.

- fechar a janela esconde o painel, sem parar o monitoramento;
- **Painel** reabre a janela;
- **Sair** encerra o monitoramento, desconecta o transporte e fecha o processo;
- quando iniciado pelo Windows, o RM Aura Ice Display abre diretamente na bandeja.

## Compilação

Requisitos:

- Windows 11 x64;
- SDK do .NET 8; ou
- Visual Studio 2022 atualizado com **Desenvolvimento para desktop com .NET**.

No PowerShell, dentro desta pasta:

```powershell
dotnet restore
dotnet build .\AuraIceLocal.sln -c Debug
dotnet run --project .\src\AuraIceLocal\AuraIceLocal.csproj
```

Para gerar os arquivos self-contained usados pelo instalador:

```powershell
dotnet publish .\src\AuraIceLocal\AuraIceLocal.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=false
```

O workflow **Publicar instalador** recebe uma versão SemVer manualmente, executa os testes, gera o `Setup.exe` e os pacotes incrementais com Velopack 1.2.0 e publica uma GitHub Release.

## Primeiro teste

1. Execute como administrador.
2. Clique em **Procurar visores**.
3. Confira se aparece `Confirmado` para `AA88:8666` com saída de 11 bytes.
4. Na aba de diagnóstico, confirme os demais descritores HID.
5. Feche completamente o software oficial da Rise Mode.
6. Clique em **Iniciar monitoramento**; a partir desse momento haverá envio real ao visor confirmado.

## Dependências

- LibreHardwareMonitorLib 0.9.6 — MPL-2.0;
- HidSharp 2.6.4 — Apache-2.0.
- Velopack 1.2.0 — MIT.
- PawnIO 2.2 ou superior — driver externo oficial assinado, instalado separadamente após confirmação do usuário.

As dependências são restauradas pelo NuGet. Nenhuma DLL extraída do programa oficial é redistribuída.
