using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using LexmarkDashboard.Models;

namespace LexmarkDashboard.Controllers;

public class PrintersController : Controller
{
    private const string PrintersFile = @"C:\Proyectos\LexmarkMonitor\printers.json";
    private const string TriggerFile = @"C:\Proyectos\LexmarkMonitor\Logs\update.trigger";

    // 1. MÉTODO GET: Este carga la vista cuando entras a localhost/Printers (Gestionar)
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewBag.Models = GetModels();
        var printers = await LoadPrintersAsync();
        return View(printers);
    }

    // 2. MÉTODO POST: Este procesa el formulario cuando agregas una impresora
    [HttpPost]
    public async Task<IActionResult> Index(PrinterConfig printer, string? returnUrl)
    {
        var printers = await LoadPrintersAsync();

        var exists = printers.Any(x => x.Ip.Equals(printer.Ip, StringComparison.OrdinalIgnoreCase));
        
        if (!exists && !string.IsNullOrWhiteSpace(printer.Name) && !string.IsNullOrWhiteSpace(printer.Ip))
        {
            printers.Add(printer);
            await SavePrintersAsync(printers);
            
            // Envío de señal al monitor
            System.IO.File.WriteAllText(TriggerFile, "actualizar");
            
            TempData["SuccessMessage"] = "✅ Impresora agregada. Actualizando...";
        }
        else
        {
            TempData["ErrorMessage"] = "❌ Error: Datos inválidos o IP duplicada.";
        }

        return returnUrl == "Dashboard" ? RedirectToAction("Index", "Home") : RedirectToAction(nameof(Index));
    }

    private static List<string> GetModels()
    {
        return new List<string>
        {
            "CX725dhe",
            "CX735adse",
            "MX511de"
        };
    }

    private static async Task<List<PrinterConfig>> LoadPrintersAsync()
    {
        if (!System.IO.File.Exists(PrintersFile)) return new List<PrinterConfig>();
        var json = await System.IO.File.ReadAllTextAsync(PrintersFile);
        return JsonSerializer.Deserialize<List<PrinterConfig>>(json) ?? new List<PrinterConfig>();
    }

    private static async Task SavePrintersAsync(List<PrinterConfig> printers)
    {
        var json = JsonSerializer.Serialize(printers, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(PrintersFile, json);
    }
}