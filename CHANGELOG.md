# Changelog

## 0.3.4

- Adiciona rolagem vertical ao painel principal quando a janela não comporta todas as seções.
- Preserva uma área útil para as listas de dispositivos HID e sensores da CPU.

## 0.3.3

- Detecta a ausência do PawnIO 2.2 e oferece a instalação explícita do driver oficial assinado.
- Confere o SHA-256 do instalador antes de executá-lo e remove o arquivo temporário ao terminar.
- Mantém a CPU em 250 ms, reduz a leitura de placa-mãe/Super I/O para 2 segundos e não consulta Embedded Controller, evitando travamentos de teclado causados por acesso excessivo ao barramento.
- Mantém o envio USB bloqueado enquanto o suporte de sensores não estiver instalado.

## 0.3.2

- Corrige o reinício sem acesso administrativo após atualizações ou atalhos externos.
- Tenta reinicializar uma vez o acesso de baixo nível quando a CPU é enumerada sem temperaturas.
- Diferencia sensor ausente de sensor detectado cuja leitura foi bloqueada pelo Windows.
- Mantém o envio HID bloqueado até existir uma temperatura válida.

## 0.3.1

- Corrige a abertura da Ajuda em diferentes escalas de DPI e tamanhos de janela.
- Moderniza o painel e a Ajuda com cards, espaçamento, cores e ícones nas ações.
- Adiciona um novo ícone do aplicativo inspirado no watercooler Aura Ice de três ventoinhas.

## 0.3.0

- Renomeia o aplicativo e o instalador para RM Aura Ice Display.
- Adiciona ícone dinâmico na bandeja com a temperatura exibida e menu Painel/Sair.
- O botão fechar agora oculta o painel; somente Sair encerra o processo.
- A inicialização com o Windows abre diretamente na bandeja.
- Remove o DevMode da interface; iniciar monitoramento passa a conectar e enviar automaticamente ao visor confirmado.
- Abre a primeira janela em 60% x 80% da tela e restaura posição, tamanho ou estado maximizado nas execuções seguintes.
- Adiciona menu Ajuda, manual navegável dentro do aplicativo, atalho F1 e documentação detalhada no repositório.
- Integra Velopack 1.2.0, botão Verificar atualizações, download com progresso, aplicação segura e reinício automático.
- Adiciona workflow manual para gerar Setup, pacotes incrementais e publicar GitHub Releases.
- Faz a tarefa agendada usar o lançador estável da instalação Velopack.
- Atualiza o perfil AA88:8666 para os relatórios reais de 11/11/0 bytes e descritores auxiliares confirmados.
- Define o pacote AuraIceV1 fixo de 11 bytes, com Report ID zero, limites independentes e arredondamento.
- Confirma o visor pelos critérios principais e impede que Possible/Unknown alcancem o transporte.
- Adiciona proteção térmica por Core Max/CPU Package e diagnóstico correspondente na interface.
- Adiciona envio manual de um único pacote com confirmação e desconexão obrigatória.
- Adiciona testes automatizados de protocolo, classificação, segurança, configurações e proteção térmica.

## 0.2.0

- reconhecimento por perfis externos de dispositivos;
- enumeração diagnóstica de todos os HID;
- pontuação e níveis de confiança;
- detecção por VID/PID, relatório, nomes e Usage;
- seleção automática somente quando há um único visor seguro;
- seleção manual para múltiplos candidatos;
- bloqueio de escrita em dispositivos desconhecidos;
- persistência sem DevicePath ou porta USB;
- modo de desenvolvimento reativado em toda inicialização;
- transporte HID agora usa o dispositivo selecionado em tempo de execução.

## 0.1.0

- protótipo inicial de leitura de sensores, suavização e pacote AuraIceV1.
