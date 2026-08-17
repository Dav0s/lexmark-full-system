using LexmarkMonitor.Models;

namespace LexmarkMonitor.Services;

public interface IPrinterClient
{
    Task<PrinterStatus> GetStatusAsync();
}