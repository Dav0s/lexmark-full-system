using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using LexmarkMonitor.Models;

namespace LexmarkMonitor.Services;

public class LexmarkClient : IPrinterClient
{
    private readonly string _printerName;
    private readonly string _ip;

    public LexmarkClient(string printerName, string ip)
    {
        _printerName = printerName;
        _ip = ip;
    }

    public async Task<PrinterStatus> GetStatusAsync()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Host", _ip);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36");

        string html = string.Empty;
        bool isColorPublic = false;

        // ESTRATEGIA 1: Lectura desde el Endpoint de Consumibles Tradicional
        try
        {
            html = await client.GetStringAsync($"http://{_ip}/webglue/content?c=Status");
            
            if (html.Contains("title=\"") && (html.Contains("negro") || html.Contains("Negro") || html.Contains("Black")))
            {
                isColorPublic = true;
            }
        }
        catch 
        {
            // Petición fallida: continúa a estrategia secundaria
        }

        // ESTRATEGIA 2: Ruta alternativa de Datos
        if (!isColorPublic)
        {
            try
            {
                html = await client.GetStringAsync($"http://{_ip}/cgi-bin/dynamic/printer/config/reports/device_info.html");
                
                if (string.IsNullOrEmpty(html) || !html.Contains("%"))
                {
                    html = await client.GetStringAsync($"http://{_ip}/webglue/content?page=supplies");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fallo definitivo en la comunicación con {_printerName} ({_ip}): {ex.Message}");
            }
        }

        // PARSEO DE VALORES
        return new PrinterStatus
        {
            Printer = _printerName,
            Ip = _ip,
            UpdatedAt = DateTime.Now,

            Black = ExtractPercentFlexible(html, "Cartucho negro", "Negro", "Black"),
            Cyan = ExtractPercentFlexible(html, "Cartucho cian", "Cian", "Cyan"),
            Magenta = ExtractPercentFlexible(html, "Cartucho magenta", "Magenta"),
            Yellow = ExtractPercentFlexible(html, "Cartucho amarillo", "Amarillo", "Yellow"),

            Fuser = ExtractPercentNullable(html, "Fusor", "Kit mantenimiento", "Fuser"),
            BlackImageUnit = ExtractPercentNullable(html, "Unidad de imagen de tinta negra", "Unidad de imágenes de tinta negra", "Imaging Unit"),
            ColorImageKit = ExtractPercentNullable(html, "Kit de imagen de color", "Unidad de imagen de color"),
            TransferModule = ExtractPercentNullable(html, "Módulo de transferencia", "Transfer")
        };
    }

    private static int? ExtractPercentNullable(string html, params string[] labels)
    {
        foreach (var label in labels)
        {
            var value = ExtractPercentFlexible(html, label);
            if (value >= 0) return value;
        }
        return null;
    }

    private static int ExtractPercentFlexible(string html, params string[] labels)
    {
        foreach (var label in labels)
        {
            var patternA = $"{Regex.Escape(label)}.*?title=\"(\\d+)%\"";
            var matchA = Regex.Match(html, patternA, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (matchA.Success) return int.Parse(matchA.Groups[1].Value);

            var patternB = $"{Regex.Escape(label)}.*?(\\d+)\\s*%";
            var matchB = Regex.Match(html, patternB, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (matchB.Success) return int.Parse(matchB.Groups[1].Value);
        }
        return -1;
    }
}