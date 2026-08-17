namespace LexmarkDashboard.Models;

public class SuppliesPurchase
{
    public string Id { get; set; } = Guid.NewGuid().ToString().Substring(0, 8);
    public string Model { get; set; } = "";
    public string Supplier { get; set; } = "";
    public DateTime PurchaseDate { get; set; } = DateTime.Now;
    
    // Ruta o nombre del archivo de la factura adjunta
    public string? InvoiceFile { get; set; }

    public List<SupplyItem> Items { get; set; } = new List<SupplyItem>();
}

public class SupplyItem
{
    public string ItemName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}