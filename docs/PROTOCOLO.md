# Protocolo e reconhecimento do LCD Aura Ice

## Perfil conhecido

```text
Perfil: rise-mode-aura-ice-v1
Protocolo: AuraIceV1
VID: 0xAA88
PID: 0x8666
Relatório esperado: exatamente 11 bytes (incluindo o Report ID na posição 0)
```

O VID/PID identifica o controlador conhecido, mas o caminho completo do dispositivo não é persistido porque pode mudar ao trocar de porta USB, reiniciar o Windows ou reinstalar o driver.

## Estrutura do relatório

| Byte | Campo |
|---:|---|
| 0 | Report ID (`0`) |
| 1 | temperatura da CPU (0–125 °C) |
| 2 | temperatura da GPU (0–125 °C) |
| 3 | uso da CPU (0–100%) |
| 4 | uso da RAM (0–100%) |
| 5 | temperatura da placa-mãe (0–125 °C) |
| 6 | uso da GPU (0–100%) |
| 7–10 | reservado, sempre zero |

Valores são arredondados ao inteiro mais próximo; leituras indisponíveis são zero. O comprimento real é consultado no descritor HID pelo `GetMaxOutputReportLength()`, e o transporte bloqueia a escrita se ele não for exatamente igual aos 11 bytes do pacote.

## Reconhecimento

A detecção enumera todos os HID e coleta, quando o driver disponibiliza:

- VID e PID;
- fabricante;
- nome do produto;
- número de série;
- tamanhos dos relatórios de entrada, saída e feature;
- Usage Page e Usage;
- caminho atual apenas para reabrir o dispositivo durante a sessão.

### Pontuação atual

| Evidência | Pontos |
|---|---:|
| VID/PID exatos | 100 |
| comprimento de saída esperado | 40 |
| relatório de saída diferente, mas existente | 5 |
| comprimento de entrada compatível | 10 |
| comprimento de feature compatível | 5 |
| nome do produto compatível | até 20 |
| fabricante compatível | 10 |
| Usage Page compatível | 15 |
| Usage compatível | 15 |

Classificação:

- **Confirmado:** VID/PID exatos, saída de 11 bytes e capacidade de escrita indicada pelo relatório de saída;
- **Reconhecido:** VID/PID exatos, mas o relatório de saída não corresponde ao protocolo confirmado;
- **Possível:** heurísticas compatíveis sem VID/PID confirmado;
- **Desconhecido:** sem evidência suficiente.

Somente `Confirmado`, com relatório de saída válido e protocolo `AuraIceV1`, pode receber dados. `Reconhecido`, `Possível` e `Desconhecido` permanecem bloqueados no transporte USB.

## Seleção e persistência

São persistidos:

- ID do perfil;
- VID/PID;
- série, quando disponível;
- nome do produto, quando disponível.

Não são persistidos:

- DevicePath;
- InstanceId completo;
- porta ou hub USB.

Quando há mais de um dispositivo indistinguível sem número de série, o usuário precisa selecionar novamente na sessão.
