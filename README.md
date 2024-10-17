# Previsão do Tempo VHF

Este projeto é um aplicativo de console em C# que consulta dados meteorológicos de uma API e gera uma mensagem de áudio com as informações do tempo. A mensagem é reproduzida e enviada via porta serial.

## Pré-requisitos

- .NET Core SDK
- Microsoft.Extensions.Configuration
- Newtonsoft.Json
- NAudio
- System.Speech

## Configuração

1. Clone o repositório para o seu ambiente local.
2. Crie um arquivo `appsettings.json` na raiz do projeto com o seguinte conteúdo:

    ```json
    {
      "ApiKey": "SUA_API_KEY",
      "SerialPortName": "NOME_DA_PORTA_SERIAL",
      "CallSign": "SEU_CALL_SIGN"
    }
    ```

3. Crie um arquivo `cities.txt` na raiz do projeto com a lista de cidades no seguinte formato:

    ```
    NomeDaCidade,Latitude,Longitude
    ```

4. (Opcional) Crie um arquivo `custom_message.txt` na raiz do projeto com uma mensagem personalizada que será adicionada ao final da mensagem de áudio.

## Como Usar

1. Abra um terminal e navegue até o diretório do projeto.
2. Execute o comando `dotnet run` para iniciar o aplicativo.
3. O aplicativo irá:
    - Carregar as configurações do arquivo `appsettings.json`.
    - Ler a lista de cidades do arquivo `cities.txt`.
    - Selecionar uma cidade aleatoriamente e consultar os dados meteorológicos.
    - Gerar uma mensagem de áudio com as informações do tempo.
    - Reproduzir a mensagem de áudio e enviar via porta serial.

## Estrutura do Código

- `Program.cs`: Contém a lógica principal do aplicativo.
- `appsettings.json`: Arquivo de configuração com a chave da API, nome da porta serial e call sign.
- `cities.txt`: Arquivo com a lista de cidades.
- `custom_message.txt`: Arquivo opcional com uma mensagem personalizada.

## Dependências

- [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/)
- [NAudio](https://www.nuget.org/packages/NAudio/)
- [System.Speech](https://www.nuget.org/packages/System.Speech/)

## Licença

Este projeto está licenciado sob os termos da licença MIT.
