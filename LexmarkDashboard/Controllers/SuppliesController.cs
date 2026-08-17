using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using LexmarkDashboard.Models;

namespace LexmarkDashboard.Controllers;

public class SuppliesController : Controller
{
    private const string SuppliesFile = @"C:\Proyectos\LexmarkMonitor\supplies.json";
    private readonly IWebHostEnvironment _env;

    public SuppliesController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var supplies = await LoadSuppliesAsync();
        ViewBag.Models = new List<string> { "CX725dhe", "CX735adse", "MX511de" };
        return View(supplies);
    }

    [HttpPost]
    public async Task<IActionResult> Add(SuppliesPurchase supply, List<SupplyItem> items, IFormFile? invoiceFile)
    {
        supply.Items = items?.Where(i => !string.IsNullOrWhiteSpace(i.ItemName)).ToList() ?? new List<SupplyItem>();

        if (string.IsNullOrWhiteSpace(supply.Model) || !supply.Items.Any())
        {
            TempData["ErrorMessage"] = "❌ Error: Debes seleccionar un modelo y agregar al menos un insumo.";
            return RedirectToAction(nameof(Index));
        }

        // Procesar archivo adjunto si existe
        if (invoiceFile != null && invoiceFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "invoices");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid().ToString().Substring(0, 6)}_{Path.GetFileName(invoiceFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await invoiceFile.CopyToAsync(stream);
            }

            supply.InvoiceFile = $"/uploads/invoices/{uniqueFileName}";
        }

        var supplies = await LoadSuppliesAsync();
        supplies.Add(supply);
        await SaveSuppliesAsync(supplies);

        TempData["SuccessMessage"] = "✅ Compra e factura adjunta registradas con éxito.";
        return RedirectToAction(nameof(Index));
    }

    private static async Task<List<SuppliesPurchase>> LoadSuppliesAsync()
    {
        if (!System.IO.File.Exists(SuppliesFile)) return new List<SuppliesPurchase>();
        var json = await System.IO.File.ReadAllTextAsync(SuppliesFile);
        return JsonSerializer.Deserialize<List<SuppliesPurchase>>(json) ?? new List<SuppliesPurchase>();
    }

    private static async Task SaveSuppliesAsync(List<SuppliesPurchase> supplies)
    {
        var json = JsonSerializer.Serialize(supplies, new JsonSerializerOptions { WriteIndented = true });
        await System.IO.File.WriteAllTextAsync(SuppliesFile, json);
    }
}