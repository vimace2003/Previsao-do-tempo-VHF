using System;
using System.IO;
using System.IO.Ports;
using System.Net.Http;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using Newtonsoft.Json.Linq;
using NAudio.Wave;
using Microsoft.Extensions.Configuration;

class Program
{
    private static IConfiguration _configuration;

    static async Task Main(string[] args)
    {
        // Carregar as configurações
        LoadConfiguration();

        var apiKey = _configuration["ApiKey"];
        var serialPortName = _configuration["SerialPortName"];
        var callSign = _configuration["CallSign"];
        var speechRate = int.TryParse(_configuration["SpeechRate"], out var rate) ? rate : 0;
        var pttControlLine = _configuration["PttControlLine"]?.ToUpper() ?? "DTR";
        var cities = File.ReadAllLines("cities.txt");
        var synthesizer = new SpeechSynthesizer
        {
            Rate = speechRate
        };

        // Carregar mensagem personalizada
        var customMessage = LoadCustomMessage();

        Console.WriteLine("Configurações carregadas:");
        Console.WriteLine($"API Key: {apiKey}");
        Console.WriteLine($"Serial Port Name: {serialPortName}");
        Console.WriteLine($"Call Sign: {callSign}");
        Console.WriteLine($"Velocidade da fala: {speechRate}");
        Console.WriteLine($"Linha de controle PTT: {pttControlLine}");

        using (var serialPort = new SerialPort(serialPortName, 9600))
        {
            try
            {
                serialPort.Open();
                Console.WriteLine($"Porta serial {serialPortName} aberta.");

                var city = cities[new Random().Next(cities.Length)];
                var parts = city.Split(',');
                var cityName = parts[0];
                var lat = parts[1];
                var lon = parts[2];

                Console.WriteLine($"Consultando o tempo para {cityName}...");
                Console.WriteLine($"Latitude: {lat}, Longitude: {lon}");

                var weatherData = await GetWeatherDataAsync(lat, lon, apiKey);
                var forecastData = await GetForecastDataAsync(lat, lon, apiKey);

                if (weatherData != null)
                {
                    Console.WriteLine("Dados do tempo recebidos com sucesso.");

                    var tempKelvin = weatherData["main"]["temp"]?.Value<double>() ?? double.NaN;
                    var description = weatherData["weather"]?[0]["description"]?.Value<string>() ?? "sem descrição disponível";
                    var humidity = weatherData["main"]["humidity"]?.Value<int>() ?? 0;
                    var pressure = weatherData["main"]["pressure"]?.Value<int>() ?? 0;
                    var windSpeed = weatherData["wind"]["speed"]?.Value<double>() ?? 0;
                    var windDeg = weatherData["wind"]["deg"]?.Value<int>() ?? 0;
                    var clouds = weatherData["clouds"]["all"]?.Value<int>() ?? 0;
                    var rain = forecastData?["list"]?[0]["rain"]?["3h"]?.Value<double>() ?? 0;

                    var tempCelsius = tempKelvin - 273.15;
                    var tempFormatted = tempCelsius.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

                    var message = $"{callSign} Informa: A temperatura atual em {cityName} é {tempFormatted} graus Celsius. " +
                                  $"A Condição Atual é: {description}. Umidade: {humidity}%." +
                                  $" Pressão atmosférica: {pressure} hPa. Velocidade do vento: {windSpeed} Metros por segundo. " +
                                  $"Direção do vento: {windDeg} graus. Condições de nuvens: {clouds}%. Possível chuva nas próximas 3 horas: {rain} mm. " +
                                  $"{customMessage} " +
                                  $"Emissão Piloto de {callSign}. Geração de previsão do tempo com Tecnologia Microsoft Azure e Open Weather Map.";

                    var tempAudioFile = "temp.wav";
                    Console.WriteLine("Gerando áudio...");
                    synthesizer.SetOutputToWaveFile(tempAudioFile);
                    synthesizer.Speak(message);
                    synthesizer.SetOutputToNull();
                    Console.WriteLine("Áudio gerado.");

                    // Ativar linha de controle correta
                    if (pttControlLine == "RTS")
                    {
                        serialPort.RtsEnable = true;
                        Console.WriteLine("Sinal RTS ativado.");
                    }
                    else
                    {
                        serialPort.DtrEnable = true;
                        Console.WriteLine("Sinal DTR ativado.");
                    }

                    using (var audioFile = new WaveFileReader(tempAudioFile))
                    using (var audioOutput = new WaveOutEvent())
                    {
                        audioOutput.Init(audioFile);
                        audioOutput.Play();
                        Console.WriteLine("Reproduzindo áudio...");

                        while (audioOutput.PlaybackState == PlaybackState.Playing)
                        {
                            await Task.Delay(100);
                        }

                        Console.WriteLine("Áudio reproduzido.");
                    }

                    // Desativar linha de controle correta
                    if (pttControlLine == "RTS")
                    {
                        serialPort.RtsEnable = false;
                        Console.WriteLine("Sinal RTS desativado.");
                    }
                    else
                    {
                        serialPort.DtrEnable = false;
                        Console.WriteLine("Sinal DTR desativado.");
                    }
                }
                else
                {
                    Console.WriteLine("Não foi possível obter os dados do tempo.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao abrir a porta serial ou processar os dados: {ex.Message}");
            }
            finally
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    Console.WriteLine($"Porta serial {serialPortName} fechada.");
                }
            }
        }
    }

    private static void LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        _configuration = builder.Build();
    }

    private static async Task<JObject> GetWeatherDataAsync(string lat, string lon, string apiKey)
    {
        using (var httpClient = new HttpClient())
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={apiKey}&lang=pt_br";
            Console.WriteLine($"URL da API de tempo: {url}");
            try
            {
                var response = await httpClient.GetStringAsync(url);
                return JObject.Parse(response);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erro ao obter dados do tempo: {ex.Message}");
                return null;
            }
        }
    }

    private static async Task<JObject> GetForecastDataAsync(string lat, string lon, string apiKey)
    {
        using (var httpClient = new HttpClient())
        {
            var url = $"https://api.openweathermap.org/data/2.5/forecast?lat={lat}&lon={lon}&appid={apiKey}&lang=pt_br";
            Console.WriteLine($"URL da API de previsão: {url}");
            try
            {
                var response = await httpClient.GetStringAsync(url);
                return JObject.Parse(response);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erro ao obter dados de previsão: {ex.Message}");
                return null;
            }
        }
    }

    private static string LoadCustomMessage()
    {
        string customMessageFilePath = "custom_message.txt";
        if (File.Exists(customMessageFilePath))
        {
            return File.ReadAllText(customMessageFilePath);
        }
        else
        {
            Console.WriteLine("Arquivo de mensagem personalizada não encontrado. Usando mensagem padrão.");
            return string.Empty;
        }
    }
}
