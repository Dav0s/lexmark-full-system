using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using LexmarkDashboard.Models;

namespace LexmarkDashboard.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private const string DashboardJsonFile = @"C:\Proyectos\LexmarkMonitor\Logs\dashboard.json";

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // Cambiado a Async para no bloquear hilos ni archivos en red
    public async Task<IActionResult> Index()
    {
        if (!System.IO.File.Exists(DashboardJsonFile))
        {
            return View(new List<PrinterStatus>());
        }

        try
        {
            // Lectura asíncrona del archivo compartido
            var json = await System.IO.File.ReadAllTextAsync(DashboardJsonFile);

            // Configuramos las opciones para ignorar mayúsculas/minúsculas en las llaves del JSON
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var printers = JsonSerializer.Deserialize<List<PrinterStatus>>(json, options)
                           ?? new List<PrinterStatus>();

            return View(printers);
        }
        catch (IOException)
        {
            // Si el monitor está escribiendo el archivo justo ahora, atrapamos el error 
            // y devolvemos una lista vacía temporal en lugar de romper la app
            return View(new List<PrinterStatus>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportToCsv()
    {
        if (!System.IO.File.Exists(DashboardJsonFile))
        {
            return NotFound("No hay datos de monitoreo disponibles para exportar.");
        }

        try
        {
            var json = await System.IO.File.ReadAllTextAsync(DashboardJsonFile);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var printers = JsonSerializer.Deserialize<List<PrinterStatus>>(json, options) ?? new List<PrinterStatus>();

            var csv = new StringBuilder();
            
            // Cabeceras del CSV (delimitadas por punto y coma para Excel en español)
            csv.AppendLine("Impresora;IP;Negro (%);Cian (%);Magenta (%);Amarillo (%);Fusor (%);Imagen Negra (%);Kit Color (%);Transferencia (%);Desecho / Estado;Actualizado");

            foreach (var p in printers)
            {
                csv.AppendLine($"\"{p.Printer}\";\"{p.Ip}\";\"{p.Black}\";\"{p.Cyan}\";\"{p.Magenta}\";\"{p.Yellow}\";\"{p.Fuser}\";\"{p.BlackImageUnit}\";\"{p.ColorImageKit}\";\"{p.TransferModule}\";\"{p.WasteToner}\";\"{p.UpdatedAt:yyyy-MM-dd HH:mm:ss}\"");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            
            return File(bytes, "text/csv", $"Reporte_Impresoras_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al generar el reporte CSV: {ex.Message}");
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}