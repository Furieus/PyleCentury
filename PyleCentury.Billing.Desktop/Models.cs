namespace PyleCentury.Billing.Desktop;

public sealed class CustomerRecord
{
    public string AccountCode { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string Address1 { get; set; } = "";
    public string Address2 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Phone { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string ContactEmail { get; set; } = "";

    public bool RequiresLiftgate { get; set; }
    public bool RequiresStraightTruck { get; set; }
    public bool AppointmentRequired { get; set; }
    public bool LimitedAccess { get; set; }
    public bool CallBeforeDelivery { get; set; }
    public bool InsideDelivery { get; set; }
    public bool DockAvailable { get; set; }
    public bool ForkliftAvailable { get; set; }
    public bool PalletJackRequired { get; set; }
    public bool IsHazmat { get; set; }
}

public sealed class ShipmentRecord
{
    public string ProNumber { get; set; } = "";

    public string ShipperName { get; set; } = "";
    public string ShipperAddress1 { get; set; } = "";
    public string ShipperCityStateZip { get; set; } = "";
    public string ShipperPhone { get; set; } = "";

    public string ConsigneeAccountCode { get; set; } = "";
    public string ConsigneeName { get; set; } = "";
    public string ConsigneeAddress1 { get; set; } = "";
    public string ConsigneeAddress2 { get; set; } = "";
    public string ConsigneeCity { get; set; } = "";
    public string ConsigneeState { get; set; } = "";
    public string ConsigneeZipCode { get; set; } = "";
    public string ConsigneePhone { get; set; } = "";
    public string ConsigneeContactName { get; set; } = "";
    public string ConsigneeContactEmail { get; set; } = "";

    public string ProductDescription { get; set; } = "";
    public int Pieces { get; set; }
    public int Skids { get; set; }
    public decimal WeightPounds { get; set; }
    public string FreightClass { get; set; } = "";
    public string NMFC { get; set; } = "";

    public bool IsHazardous { get; set; }
    public string UNNumber { get; set; } = "";
    public string HazmatClass { get; set; } = "";
    public string PackingGroup { get; set; } = "";
    public string ContainerType { get; set; } = "";
    public string ProperShippingName { get; set; } = "";

    public bool IsFoodstuffs { get; set; }
    public bool RequiresLiftgate { get; set; }
    public bool RequiresStraightTruck { get; set; }
    public bool AppointmentRequired { get; set; }
    public bool LimitedAccess { get; set; }
    public bool CallBeforeDelivery { get; set; }
    public bool InsideDelivery { get; set; }

    public string SpecialInstructions { get; set; } = "";
    public string BillingStatus { get; set; } = "Created";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
