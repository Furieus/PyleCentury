using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PyleCentury.Billing.Desktop;

public sealed class AppConfig
{
    public string ApiBaseUrl { get; set; } = "";
    public string CustomerApiBaseUrl { get; set; } = ""; // legacy support

    public string EffectiveApiBaseUrl =>
        !string.IsNullOrWhiteSpace(ApiBaseUrl)
            ? ApiBaseUrl
            : CustomerApiBaseUrl;

    public static AppConfig Load()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
            {
                return new AppConfig();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }
}

public sealed class CustomerLookupService
{
    private readonly string _apiBaseUrl;
    private readonly HttpClient _httpClient = new();

    public CustomerLookupService(string apiBaseUrl)
    {
        _apiBaseUrl = apiBaseUrl.Trim().TrimEnd('/');
    }

    public async Task<CustomerRecord?> LookupAsync(string accountCode)
    {
        accountCode = accountCode.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(_apiBaseUrl))
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/customers/{Uri.EscapeDataString(accountCode)}");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("customer", out var customer))
                {
                    return new CustomerRecord
                    {
                        AccountCode = GetString(customer, "account_code"),
                        BusinessName = GetString(customer, "business_name"),
                        Address1 = GetString(customer, "address1"),
                        Address2 = GetString(customer, "address2"),
                        City = GetString(customer, "city"),
                        State = GetString(customer, "state"),
                        ZipCode = GetString(customer, "zip_code"),
                        Phone = GetString(customer, "phone"),
                        ContactName = GetString(customer, "contact_name"),
                        ContactEmail = GetString(customer, "contact_email"),
                        RequiresLiftgate = GetBool(customer, "requires_liftgate"),
                        RequiresStraightTruck = GetBool(customer, "requires_straight_truck"),
                        AppointmentRequired = GetBool(customer, "appointment_required"),
                        LimitedAccess = GetBool(customer, "limited_access"),
                        CallBeforeDelivery = GetBool(customer, "call_before_delivery"),
                        InsideDelivery = GetBool(customer, "inside_delivery"),
                        DockAvailable = GetBool(customer, "dock_available"),
                        ForkliftAvailable = GetBool(customer, "forklift_available"),
                        PalletJackRequired = GetBool(customer, "pallet_jack_required"),
                        IsHazmat = GetBool(customer, "is_hazmat")
                    };
                }
            }

            return null;
        }

        return DummyCustomerLookup(accountCode);
    }

    private static CustomerRecord? DummyCustomerLookup(string accountCode)
    {
        var records = new Dictionary<string, CustomerRecord>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = new()
            {
                AccountCode = "",
                BusinessName = "A Duie Pyle",
                Address1 = "87 Aircraft Rd",
                City = "Southington",
                State = "CT",
                ZipCode = "06489",
                Phone = "860-555-0100",
                ContactName = "Terminal Dispatch",
                ContactEmail = "dispatch@example.com",
                DockAvailable = true,
                ForkliftAvailable = true
            },
            ["ABME17"] = new()
            {
                AccountCode = "ABME17",
                BusinessName = "ABC Supply",
                Address1 = "123 Main St",
                City = "Meriden",
                State = "CT",
                ZipCode = "06450",
                Phone = "203-555-1212",
                ContactName = "John Smith",
                ContactEmail = "john.smith@example.com",
                RequiresLiftgate = true,
                AppointmentRequired = true,
                CallBeforeDelivery = true,
                DockAvailable = true,
                ForkliftAvailable = true,
                IsHazmat = true
            }
        };

        return records.TryGetValue(accountCode, out var customer) ? customer : null;
    }

    private static string GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString() ?? ""
            : "";

    private static bool GetBool(JsonElement element, string name)
        => element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.True;
}

public sealed class ShipmentApiService
{
    private readonly string _apiBaseUrl;
    private readonly HttpClient _httpClient = new();

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiBaseUrl);

    public ShipmentApiService(string apiBaseUrl)
    {
        _apiBaseUrl = apiBaseUrl.Trim().TrimEnd('/');
    }

    public async Task<string> GenerateProAsync()
    {
        EnsureConfigured();

        var response = await _httpClient.GetAsync($"{_apiBaseUrl}/shipments/generate-pro");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"PRO generation failed: {body}");
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("pro_number").GetString() ?? "";
    }

    public async Task SaveAsync(ShipmentRecord shipment)
    {
        EnsureConfigured();

        var payload = new
        {
            pro_number = shipment.ProNumber,

            shipper_name = shipment.ShipperName,
            shipper_address1 = shipment.ShipperAddress1,
            shipper_city_state_zip = shipment.ShipperCityStateZip,
            shipper_phone = shipment.ShipperPhone,

            consignee_account_code = shipment.ConsigneeAccountCode,
            consignee_name = shipment.ConsigneeName,
            consignee_address1 = shipment.ConsigneeAddress1,
            consignee_address2 = shipment.ConsigneeAddress2,
            consignee_city = shipment.ConsigneeCity,
            consignee_state = shipment.ConsigneeState,
            consignee_zip_code = shipment.ConsigneeZipCode,
            consignee_phone = shipment.ConsigneePhone,
            consignee_contact_name = shipment.ConsigneeContactName,
            consignee_contact_email = shipment.ConsigneeContactEmail,

            product_description = shipment.ProductDescription,
            pieces = shipment.Pieces,
            skids = shipment.Skids,
            weight_pounds = shipment.WeightPounds,
            freight_class = shipment.FreightClass,
            nmfc = shipment.NMFC,

            is_hazardous = shipment.IsHazardous,
            un_number = shipment.UNNumber,
            hazmat_class = shipment.HazmatClass,
            packing_group = shipment.PackingGroup,
            container_type = shipment.ContainerType,
            proper_shipping_name = shipment.ProperShippingName,

            is_foodstuffs = shipment.IsFoodstuffs,
            requires_liftgate = shipment.RequiresLiftgate,
            requires_straight_truck = shipment.RequiresStraightTruck,
            appointment_required = shipment.AppointmentRequired,
            limited_access = shipment.LimitedAccess,
            call_before_delivery = shipment.CallBeforeDelivery,
            inside_delivery = shipment.InsideDelivery,

            special_instructions = shipment.SpecialInstructions,
            billing_status = shipment.BillingStatus
        };

        var json = JsonSerializer.Serialize(payload);
        var response = await _httpClient.PostAsync(
            $"{_apiBaseUrl}/shipments",
            new StringContent(json, Encoding.UTF8, "application/json"));

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Shipment save failed: {body}");
        }
    }

    public async Task<ShipmentRecord?> FindByProAsync(string proNumber)
    {
        EnsureConfigured();

        var response = await _httpClient.GetAsync($"{_apiBaseUrl}/shipments/{Uri.EscapeDataString(proNumber.Trim())}");
        var body = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"PRO lookup failed: {body}");
        }

        using var doc = JsonDocument.Parse(body);

        if (!doc.RootElement.TryGetProperty("shipment", out var s))
        {
            return null;
        }

        return new ShipmentRecord
        {
            ProNumber = GetString(s, "pro_number"),

            ShipperName = GetString(s, "shipper_name"),
            ShipperAddress1 = GetString(s, "shipper_address1"),
            ShipperCityStateZip = GetString(s, "shipper_city_state_zip"),
            ShipperPhone = GetString(s, "shipper_phone"),

            ConsigneeAccountCode = GetString(s, "consignee_account_code"),
            ConsigneeName = GetString(s, "consignee_name"),
            ConsigneeAddress1 = GetString(s, "consignee_address1"),
            ConsigneeAddress2 = GetString(s, "consignee_address2"),
            ConsigneeCity = GetString(s, "consignee_city"),
            ConsigneeState = GetString(s, "consignee_state"),
            ConsigneeZipCode = GetString(s, "consignee_zip_code"),
            ConsigneePhone = GetString(s, "consignee_phone"),
            ConsigneeContactName = GetString(s, "consignee_contact_name"),
            ConsigneeContactEmail = GetString(s, "consignee_contact_email"),

            ProductDescription = GetString(s, "product_description"),
            Pieces = GetInt(s, "pieces"),
            Skids = GetInt(s, "skids"),
            WeightPounds = GetDecimal(s, "weight_pounds"),
            FreightClass = GetString(s, "freight_class"),
            NMFC = GetString(s, "nmfc"),

            IsHazardous = GetBool(s, "is_hazardous"),
            UNNumber = GetString(s, "un_number"),
            HazmatClass = GetString(s, "hazmat_class"),
            PackingGroup = GetString(s, "packing_group"),
            ContainerType = GetString(s, "container_type"),
            ProperShippingName = GetString(s, "proper_shipping_name"),

            IsFoodstuffs = GetBool(s, "is_foodstuffs"),
            RequiresLiftgate = GetBool(s, "requires_liftgate"),
            RequiresStraightTruck = GetBool(s, "requires_straight_truck"),
            AppointmentRequired = GetBool(s, "appointment_required"),
            LimitedAccess = GetBool(s, "limited_access"),
            CallBeforeDelivery = GetBool(s, "call_before_delivery"),
            InsideDelivery = GetBool(s, "inside_delivery"),

            SpecialInstructions = GetString(s, "special_instructions"),
            BillingStatus = GetString(s, "billing_status")
        };
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl))
        {
            throw new InvalidOperationException("ApiBaseUrl is blank in appsettings.json. Set it to your Render service URL.");
        }
    }

    private static string GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString() ?? ""
            : "";

    private static bool GetBool(JsonElement element, string name)
        => element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.True;

    private static int GetInt(JsonElement element, string name)
        => element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetInt32()
            : 0;

    private static decimal GetDecimal(JsonElement element, string name)
        => element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number
            ? property.GetDecimal()
            : 0;
}
