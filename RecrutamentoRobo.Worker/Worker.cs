using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RecrutamentoRobo.Core.Interfaces;

namespace RecrutamentoRobo.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IEmailService emailService, IConfiguration configuration)
    {
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Robô de Recrutamento iniciado em: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Iniciando ciclo de verificação de e-mails.");

            var emails = await _emailService.GetUnreadEmailsAsync();

            if (emails.Any())
            {
                _logger.LogInformation("Processando {count} novos e-mails.", emails.Count());

                foreach (var email in emails)
                {
                    _logger.LogInformation("--- E-mail encontrado: {subject} ---", email.Subject);

                    // Por enquanto, vamos apenas testar a extração de texto do primeiro anexo
                    // A lógica de match com as palavras-chave virá na próxima etapa.

                    // Esta chamada ainda não está implementada no GetUnreadEmailsAsync,
                    // vamos precisar ajustar para pegar os anexos.

                    // --- LÓGICA DE MATCH E DOWNLOAD ENTRARÁ AQUI ---
                }
            }
            else
            {
                _logger.LogInformation("Nenhum e-mail novo encontrado.");
            }

            _logger.LogInformation("Ciclo finalizado. Aguardando 5 minutos para o próximo.");
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}