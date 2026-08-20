using System.Text.Json;
using LexmarkMonitor.Models;
using LexmarkMonitor.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<MonitorImpresorasService>();

using IHost host = builder.Build();

Console.WriteLine("🚀 Monitor en modo escucha permanente iniciado.");
await host.RunAsync();

public class MonitorImpresorasService : BackgroundService
{
    private const string TriggerFile = @"C:\Proyectos\LexmarkMonitor\Logs\update.trigger";
    private const string PrintersFile = @"C:\Proyectos\LexmarkMonitor\printers.json";
    private const string OutputFile = @"C:\Proyectos\LexmarkMonitor\Logs\dashboard.json";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory("Logs");
        
        // Ejecutamos un escaneo inicial al arrancar
        await RealizarMonitoreo();

        int segundosAcumulados = 0;
        const int intervaloRutinarioSegundos = 300; // 5 minutos

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. ESCUCHA PERMANENTE: Si existe el trigger, actualizamos de inmediato
                if (File.Exists(TriggerFile))
                {
                    Console.WriteLine("⚡ ¡Señal detectada! Actualizando impresoras de inmediato...");
                    File.Delete(TriggerFile);
                    await RealizarMonitoreo();
                    
                    segundosAcumulados = 0; 
                }

                // 2. CONTROL DE RUTINA (Cada 5 minutos)
                segundosAcumulados += 2;
                if (segundosAcumulados >= intervaloRutinarioSegundos)
                {
                    await RealizarMonitoreo();
                    segundosAcumulados = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en ciclo de escucha: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task RealizarMonitoreo()
    {
        if (!File.Exists(PrintersFile)) return;

        var jsonContent = await File.ReadAllTextAsync(PrintersFile);
        var configs = JsonSerializer.Deserialize<List<PrinterConfig>>(jsonContent) ?? new List<PrinterConfig>();
        var results = new List<PrinterStatus>();

        foreach (var config in configs)
        {
            try
            {
                PrinterStatus status = config.Model.Equals("MX511de", StringComparison.OrdinalIgnoreCase)
                    ? await new LexmarkMx511Client(config.Name, config.Ip).GetStatusAsync()
                    : await new LexmarkClient(config.Name, config.Ip).GetStatusAsync();

                status.Floor = config.Floor;
                results.Add(status);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"❌ Error en {config.Name}: {ex.Message}");
                results.Add(new PrinterStatus { Printer = config.Name, Ip = config.Ip, Floor = config.Floor, WasteToner = "Error" });
            }
        }

        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(OutputFile + ".tmp", json);
        File.Move(OutputFile + ".tmp", OutputFile, overwrite: true);
    }
}