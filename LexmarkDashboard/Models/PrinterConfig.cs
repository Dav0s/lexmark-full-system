namespace LexmarkDashboard.Models;

public class PrinterConfig
{
    public string Name { get; set; } = "";

    public string Model { get; set; } = "";

    public string Ip { get; set; } = "";

    // Campos necesarios para los modelos CX725dhe y CX735adse
    public string SessionId { get; set; } = "";
    public string SessionKey { get; set; } = "";
    public string SessionName { get; set; } = "";
}