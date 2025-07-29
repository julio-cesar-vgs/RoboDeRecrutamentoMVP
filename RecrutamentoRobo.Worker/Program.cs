        // Program.cs
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Hosting;
        using RecrutamentoRobo.Core.Interfaces;
        using RecrutamentoRobo.Infrastructure.Services;

        namespace RecrutamentoRobo.ConsoleApp // É uma boa prática usar um namespace
        {
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine("Olá, mundo! Esta é a forma tradicional.");

                    IHost host = Host.CreateDefaultBuilder(args)
                        .ConfigureServices((hostContext, services) =>
                        {
                            services.AddSingleton<IEmailService, GraphEmailService>();
                            services.AddHostedService<Worker.Worker>();
                        })
                        .Build();

                    host.Run();
                }
            }
        }