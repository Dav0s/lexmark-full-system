using System.Text.RegularExpressions;
using LexmarkMonitor.Models;

namespace LexmarkMonitor.Services;

public class LexmarkMx511Client : IPrinterClient
{
    private readonly string _printerName;
    private readonly string _ip;

    public LexmarkMx511Client(
        string printerName,
        string ip)
    {
        _printerName = printerName;
        _ip = ip;
    }

    public async Task<PrinterStatus> GetStatusAsync()
    {
        using var client = new HttpClient();

        var html =
            await client.GetStringAsync(
                $"http://{_ip}/cgi-bin/dynamic/printer/PrinterStatus.html");

        return new PrinterStatus
        {
            Printer = _printerName,
            Ip = _ip,
            UpdatedAt = DateTime.Now,

            Black = ExtractToner(html),

            Cyan = null,
            Magenta = null,
            Yellow = null,

            Fuser = ExtractMaintenanceKit(html),

            BlackImageUnit = ExtractImageUnit(html),

            ColorImageKit = null,
            TransferModule = null,

            WasteToner = "N/A"
        };
    }

    private static int ExtractToner(
        string html)
    {
        var match = Regex.Match(
            html,
            @"Cartucho negro\s*~(\d+)%",
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return int.Parse(
                match.Groups[1].Value);
        }

        return 0;
    }

    private static int? ExtractMaintenanceKit(
        string html)
    {
        var match = Regex.Match(
            html,
            @"Kit mantenimient Duración restante:</b></td><td>(\d+)%",
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return int.Parse(
                match.Groups[1].Value);
        }

        return null;
    }

    private static int? ExtractImageUnit(
        string html)
    {
        var match = Regex.Match(
            html,
            @"Unidad imagen Duración restante:</b></td><td>(\d+)%",
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            return int.Parse(
                match.Groups[1].Value);
        }

        return null;
    }
}