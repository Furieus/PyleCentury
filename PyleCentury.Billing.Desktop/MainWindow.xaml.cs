using System.Globalization;
using System.Windows;
using PyleCentury.Shared;

namespace PyleCentury.Billing.Desktop;

public partial class MainWindow : Window
{
    private LaunchContext? _launchContext;
    private readonly CustomerLookupService _customerLookupService;
    private readonly ShipmentApiService _shipmentApiService;

    private CustomerRecord? _currentConsignee;

    public MainWindow()
    {
        _launchContext = ModuleLaunchGuard.RequireMenuLaunch("Billing");
        if (_launchContext is null) return;
        if (_launchContext.BillingPermission == "none")
        {
            MessageBox.Show("Your access level does not allow Billing access.", "Access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            Application.Current.Shutdown();
            return;
        }

        InitializeComponent();
        Title = $"Billing · {_launchContext.HomeTerminalCode} · {_launchContext.DisplayName}";

        var config = AppConfig.Load();
        var apiBaseUrl = !string.IsNullOrWhiteSpace(_launchContext.ApiBaseUrl)
            ? _launchContext.ApiBaseUrl
            : config.EffectiveApiBaseUrl;

        _customerLookupService = new CustomerLookupService(apiBaseUrl);
        _shipmentApiService = new ShipmentApiService(apiBaseUrl);

        StatusText.Text = string.IsNullOrWhiteSpace(apiBaseUrl)
            ? "Ready. ApiBaseUrl is blank. PRO generation/save/load requires Render/Postgres."
            : $"Ready. Connected to Render/Postgres API: {apiBaseUrl} · Employee {_launchContext.EmployeeNumber} · Terminal {_launchContext.HomeTerminalCode}";
    }

    private async void LookupCustomer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var accountCode = CustomerCodeBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(accountCode))
            {
                SetStatus("Enter a customer account code first.", true);
                return;
            }

            var customer = await _customerLookupService.LookupAsync(accountCode);

            if (customer is null)
            {
                ClearConsigneeFields();
                SetStatus($"No customer found for account code {accountCode}.", true);
                return;
            }

            _currentConsignee = customer;
            PopulateConsignee(customer);
            ApplyCustomerRestrictions(customer);
            SetStatus($"Loaded consignee {customer.BusinessName} using account code {customer.AccountCode}.");
        }
        catch (Exception ex)
        {
            SetStatus($"Customer lookup failed: {ex.Message}", true);
        }
    }

    private void PopulateConsignee(CustomerRecord customer)
    {
        CustomerCodeBox.Text = customer.AccountCode;
        ConsigneeNameBox.Text = customer.BusinessName;
        ConsigneeAddressBox.Text = string.IsNullOrWhiteSpace(customer.Address2)
            ? customer.Address1
            : $"{customer.Address1}, {customer.Address2}";
        ConsigneeCityStateZipBox.Text = $"{customer.City}, {customer.State} {customer.ZipCode}".Trim();
        ConsigneePhoneBox.Text = customer.Phone;
        ConsigneeContactBox.Text = string.IsNullOrWhiteSpace(customer.ContactEmail)
            ? customer.ContactName
            : $"{customer.ContactName} · {customer.ContactEmail}";
    }

    private void ApplyCustomerRestrictions(CustomerRecord customer)
    {
        LiftgateCheck.IsChecked = customer.RequiresLiftgate;
        StraightTruckCheck.IsChecked = customer.RequiresStraightTruck;
        AppointmentCheck.IsChecked = customer.AppointmentRequired;
        LimitedAccessCheck.IsChecked = customer.LimitedAccess;
        CallBeforeCheck.IsChecked = customer.CallBeforeDelivery;
        InsideDeliveryCheck.IsChecked = customer.InsideDelivery;
        HazmatCheck.IsChecked = customer.IsHazmat;
    }

    private async void GeneratePro_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProNumberText.Text = await _shipmentApiService.GenerateProAsync();
            SetStatus($"Generated PRO {ProNumberText.Text} from Render/Supabase.");
        }
        catch (Exception ex)
        {
            SetStatus($"PRO generation failed: {ex.Message}", true);
        }
    }

    private async void SaveShipment_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentConsignee is null)
            {
                SetStatus("Load a consignee before saving.", true);
                return;
            }

            if (!IsGeneratedPro(ProNumberText.Text))
            {
                SetStatus("Generate a 9-digit PRO before saving.", true);
                return;
            }

            if (string.IsNullOrWhiteSpace(ProductDescriptionBox.Text))
            {
                SetStatus("Enter a product description before saving.", true);
                return;
            }

            if (HazmatCheck.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(UnNumberBox.Text) || string.IsNullOrWhiteSpace(HazmatClassBox.Text))
                {
                    SetStatus("Hazmat shipments require at least a UN/NA number and class.", true);
                    return;
                }
            }

            var shipment = BuildShipmentRecord();
            await _shipmentApiService.SaveAsync(shipment);

            SetStatus($"Saved shipment PRO {shipment.ProNumber} to Render/Supabase.");
        }
        catch (Exception ex)
        {
            SetStatus($"Save failed: {ex.Message}", true);
        }
    }

    private ShipmentRecord BuildShipmentRecord()
    {
        return new ShipmentRecord
        {
            ProNumber = ProNumberText.Text.Trim(),

            ShipperName = ShipperNameBox.Text,
            ShipperAddress1 = ShipperAddressBox.Text,
            ShipperCityStateZip = ShipperCityStateZipBox.Text,
            ShipperPhone = ShipperPhoneBox.Text,

            ConsigneeAccountCode = CustomerCodeBox.Text.Trim().ToUpperInvariant(),
            ConsigneeName = ConsigneeNameBox.Text,
            ConsigneeAddress1 = _currentConsignee?.Address1 ?? "",
            ConsigneeAddress2 = _currentConsignee?.Address2 ?? "",
            ConsigneeCity = _currentConsignee?.City ?? "",
            ConsigneeState = _currentConsignee?.State ?? "",
            ConsigneeZipCode = _currentConsignee?.ZipCode ?? "",
            ConsigneePhone = _currentConsignee?.Phone ?? "",
            ConsigneeContactName = _currentConsignee?.ContactName ?? "",
            ConsigneeContactEmail = _currentConsignee?.ContactEmail ?? "",

            ProductDescription = ProductDescriptionBox.Text.Trim(),
            Pieces = ParseInt(PiecesBox.Text),
            Skids = ParseInt(SkidsBox.Text),
            WeightPounds = ParseDecimal(WeightBox.Text),
            FreightClass = FreightClassBox.Text.Trim(),
            NMFC = NmfcBox.Text.Trim(),

            IsHazardous = HazmatCheck.IsChecked == true,
            UNNumber = HazmatCheck.IsChecked == true ? UnNumberBox.Text.Trim().ToUpperInvariant() : "",
            HazmatClass = HazmatCheck.IsChecked == true ? HazmatClassBox.Text.Trim() : "",
            PackingGroup = HazmatCheck.IsChecked == true ? PackingGroupBox.Text.Trim().ToUpperInvariant() : "",
            ContainerType = HazmatCheck.IsChecked == true ? ContainerTypeBox.Text.Trim() : "",
            ProperShippingName = HazmatCheck.IsChecked == true ? ProperShippingNameBox.Text.Trim() : "",

            IsFoodstuffs = FoodstuffsCheck.IsChecked == true,
            RequiresLiftgate = LiftgateCheck.IsChecked == true,
            RequiresStraightTruck = StraightTruckCheck.IsChecked == true,
            AppointmentRequired = AppointmentCheck.IsChecked == true,
            LimitedAccess = LimitedAccessCheck.IsChecked == true,
            CallBeforeDelivery = CallBeforeCheck.IsChecked == true,
            InsideDelivery = InsideDeliveryCheck.IsChecked == true,

            SpecialInstructions = SpecialInstructionsBox.Text.Trim(),
            BillingStatus = "Created",
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private async void LoadPro_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var pro = ProLookupBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(pro))
            {
                SetStatus("Enter a PRO number to load.", true);
                return;
            }

            var shipment = await _shipmentApiService.FindByProAsync(pro);

            if (shipment is null)
            {
                SetStatus($"No saved shipment found for PRO {pro}.", true);
                return;
            }

            PopulateFromShipment(shipment);
            SetStatus($"Loaded saved shipment PRO {shipment.ProNumber} from Render/Supabase.");
        }
        catch (Exception ex)
        {
            SetStatus($"PRO lookup failed: {ex.Message}", true);
        }
    }

    private void PopulateFromShipment(ShipmentRecord shipment)
    {
        ProNumberText.Text = shipment.ProNumber;

        CustomerCodeBox.Text = shipment.ConsigneeAccountCode;
        ConsigneeNameBox.Text = shipment.ConsigneeName;
        ConsigneeAddressBox.Text = string.IsNullOrWhiteSpace(shipment.ConsigneeAddress2)
            ? shipment.ConsigneeAddress1
            : $"{shipment.ConsigneeAddress1}, {shipment.ConsigneeAddress2}";
        ConsigneeCityStateZipBox.Text = $"{shipment.ConsigneeCity}, {shipment.ConsigneeState} {shipment.ConsigneeZipCode}".Trim();
        ConsigneePhoneBox.Text = shipment.ConsigneePhone;
        ConsigneeContactBox.Text = string.IsNullOrWhiteSpace(shipment.ConsigneeContactEmail)
            ? shipment.ConsigneeContactName
            : $"{shipment.ConsigneeContactName} · {shipment.ConsigneeContactEmail}";

        _currentConsignee = new CustomerRecord
        {
            AccountCode = shipment.ConsigneeAccountCode,
            BusinessName = shipment.ConsigneeName,
            Address1 = shipment.ConsigneeAddress1,
            Address2 = shipment.ConsigneeAddress2,
            City = shipment.ConsigneeCity,
            State = shipment.ConsigneeState,
            ZipCode = shipment.ConsigneeZipCode,
            Phone = shipment.ConsigneePhone,
            ContactName = shipment.ConsigneeContactName,
            ContactEmail = shipment.ConsigneeContactEmail
        };

        ProductDescriptionBox.Text = shipment.ProductDescription;
        PiecesBox.Text = shipment.Pieces.ToString(CultureInfo.InvariantCulture);
        SkidsBox.Text = shipment.Skids.ToString(CultureInfo.InvariantCulture);
        WeightBox.Text = shipment.WeightPounds.ToString(CultureInfo.InvariantCulture);
        FreightClassBox.Text = shipment.FreightClass;
        NmfcBox.Text = shipment.NMFC;
        SpecialInstructionsBox.Text = shipment.SpecialInstructions;

        HazmatCheck.IsChecked = shipment.IsHazardous;
        UnNumberBox.Text = shipment.UNNumber;
        HazmatClassBox.Text = shipment.HazmatClass;
        PackingGroupBox.Text = shipment.PackingGroup;
        ContainerTypeBox.Text = shipment.ContainerType;
        ProperShippingNameBox.Text = shipment.ProperShippingName;

        FoodstuffsCheck.IsChecked = shipment.IsFoodstuffs;
        LiftgateCheck.IsChecked = shipment.RequiresLiftgate;
        StraightTruckCheck.IsChecked = shipment.RequiresStraightTruck;
        AppointmentCheck.IsChecked = shipment.AppointmentRequired;
        LimitedAccessCheck.IsChecked = shipment.LimitedAccess;
        CallBeforeCheck.IsChecked = shipment.CallBeforeDelivery;
        InsideDeliveryCheck.IsChecked = shipment.InsideDelivery;
    }

    private void ClearForm_Click(object sender, RoutedEventArgs e)
    {
        ProNumberText.Text = "Not Generated";
        ProLookupBox.Text = "";

        CustomerCodeBox.Text = "";
        ClearConsigneeFields();

        ProductDescriptionBox.Text = "";
        PiecesBox.Text = "";
        SkidsBox.Text = "";
        WeightBox.Text = "";
        FreightClassBox.Text = "";
        NmfcBox.Text = "";
        SpecialInstructionsBox.Text = "";

        FoodstuffsCheck.IsChecked = false;
        LiftgateCheck.IsChecked = false;
        StraightTruckCheck.IsChecked = false;
        AppointmentCheck.IsChecked = false;
        LimitedAccessCheck.IsChecked = false;
        CallBeforeCheck.IsChecked = false;
        InsideDeliveryCheck.IsChecked = false;

        HazmatCheck.IsChecked = false;
        UnNumberBox.Text = "";
        HazmatClassBox.Text = "";
        PackingGroupBox.Text = "";
        ContainerTypeBox.Text = "";
        ProperShippingNameBox.Text = "";

        _currentConsignee = null;

        SetStatus("Form cleared.");
    }

    private void ClearConsigneeFields()
    {
        _currentConsignee = null;
        ConsigneeNameBox.Text = "";
        ConsigneeAddressBox.Text = "";
        ConsigneeCityStateZipBox.Text = "";
        ConsigneePhoneBox.Text = "";
        ConsigneeContactBox.Text = "";
    }

    private void HazmatCheck_Changed(object sender, RoutedEventArgs e)
    {
        var enabled = HazmatCheck.IsChecked == true;
        HazmatPanel.IsEnabled = enabled;
        HazmatPanel.Opacity = enabled ? 1.0 : 0.38;
    }

    private static bool IsGeneratedPro(string value)
    {
        return value.Length == 9 && value.All(char.IsDigit);
    }

    private static int ParseInt(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? System.Windows.Media.Brushes.OrangeRed
            : System.Windows.Media.Brushes.LimeGreen;
    }
}
