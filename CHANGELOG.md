# Changelog

## 0.3.0

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
