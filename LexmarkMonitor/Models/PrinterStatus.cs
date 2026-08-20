namespace LexmarkMonitor.Models;

public class PrinterStatus
{
    public string Printer { get; set; } = "";
    public string Ip { get; set; } = "";
    
    public string Floor { get; set; } = "";
    public DateTime UpdatedAt { get; set; }

    public int? Black { get; set; }

    public int? Cyan { get; set; }
    public int? Magenta { get; set; }
    public int? Yellow { get; set; }

    public int? Fuser { get; set; }
    public int? BlackImageUnit { get; set; }
    public int? ColorImageKit { get; set; }
    public int? TransferModule { get; set; }

    public string WasteToner { get; set; } = "OK";
}
