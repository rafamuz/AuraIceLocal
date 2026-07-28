# Validação da versão 0.3 no Codex

Abra o Codex na pasta raiz `AuraIceLocal` e envie:

```text
Analise e valide este projeto C#/.NET 8 WinForms. Execute dotnet restore e dotnet build na solução AuraIceLocal.sln. Corrija somente erros reais de compilação, APIs incompatíveis do HidSharp 2.6.4 ou problemas claros de concorrência/segurança, sem remover o reconhecedor de dispositivos.

Regras obrigatórias:
- nenhum pacote pode ser enviado a dispositivos que não estejam Confirmed;
- não persista DevicePath, InstanceId nem porta USB;
- mantenha o perfil conhecido AA88:8666 em device-profiles.json;
- não crie arquivos de log;
- não execute o programa oficial da Rise Mode e o AuraIceLocal escrevendo ao mesmo tempo.

Execute o aplicativo sem iniciar o monitoramento. Clique em Procurar visores e verifique se o HID AA88:8666 aparece como Confirmado, mostrando o comprimento de saída e os demais dados disponíveis. Verifique também se Core Average, CPU Package e Core Max são enumerados para o Intel Core Ultra 7 265K. Não clique em Iniciar monitoramento durante uma validação que não autorize escrita USB.

Ao final, informe comandos executados, arquivos alterados, erros corrigidos, classificação do visor, comprimento do relatório e sensores encontrados.
```
