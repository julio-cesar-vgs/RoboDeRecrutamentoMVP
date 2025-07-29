// Program.cs

using Azure.Identity;
using Microsoft.Graph;
using RecrutamentoRobo.Core.Interfaces;
using RecrutamentoRobo.Infrastructure.Services;

namespace RecrutamentoRobo.Worker; // É uma boa prática usar um namespace

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Olá, mundo! Esta é a forma tradicional.");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IEmailService, GraphEmailService>();
                services.AddSingleton<GraphServiceClient>(provider =>
                {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var clientSecretCredential = new ClientSecretCredential(
                        configuration["AzureAd:TenantId"],
                        configuration["AzureAd:ClientId"],
                        configuration["AzureAd:ClientSecret"]
                    );
                    return new GraphServiceClient(clientSecretCredential);
                });
                services.AddHostedService<Worker>();
            })
            .Build();

        host.Run();
    }
}