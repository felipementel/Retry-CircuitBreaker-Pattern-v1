using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CircuitBreakerRetryClient
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public Worker(ILogger<Worker> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var retryPolicy = Policy
                    .Handle<HttpRequestException>()//ex => ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: i => TimeSpan.FromSeconds(2),
                    onRetry: (ex, ts, retryCount, context) =>
                    {
                        Console.WriteLine($"RT - Tentativa {retryCount} - Erro {ex.Message}");
                    });

                var circuitBreakerPolicy = Policy.Handle<HttpRequestException>()
                    .CircuitBreakerAsync(3, TimeSpan.FromSeconds(2),
                    onBreak: (ex, ts) => Console.WriteLine($"CB - {ex.Message} - Tempo {ts} - curcuito aberto"),
                    onHalfOpen: () => Console.WriteLine("CB - Tentativa"),
                    onReset: () => Console.WriteLine("CB - Requisacao feita com sucesso - curcuito fechado"));


                //await retryPolicy.ExecuteAsync(async () => {

                //});                

                await retryPolicy.ExecuteAsync(
                    () => circuitBreakerPolicy.ExecuteAsync(
                        () => BuscarCliente()));

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task<HttpResponseMessage> BuscarCliente()
        {
            using (var client = _httpClientFactory.CreateClient("Local"))
            {
                var response = await client.GetAsync("/Livros");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNameCaseInsensitive = true
                    };

                    var livros = JsonSerializer.Deserialize<IEnumerable<Livro>>(json, options);

                    foreach (var item in livros)
                    {
                        Console.WriteLine($"Sucesso - {item.Nome}");
                    }
                }

                return response;
            }
        }
    }

    public class Livro
    {
        public int Id { get; set; }

        public string Nome { get; set; }
    }
}
