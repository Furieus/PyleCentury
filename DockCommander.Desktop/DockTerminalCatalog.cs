namespace DockCommander.Desktop;

public sealed record DockTerminal(
    string Code,
    string Name,
    string State,
    string DisplayName,
    bool HasDockLayout = false)
{
    public DockTerminal(
        string code,
        string name,
        string state,
        string displayName,
        string? ignoredLegacyValue,
        bool hasDockLayout = false)
        : this(code, name, state, displayName, hasDockLayout)
    {
    }

    public string LayoutStatus => HasDockLayout ? "Mapped" : "No map";
    public string DisplayWithStatus => HasDockLayout ? DisplayName : $"{DisplayName}   (no dock map)";
}

public static class DockTerminalCatalog
{
    public static IReadOnlyList<DockTerminal> All { get; } = new List<DockTerminal>
    {
        new("ALE", "Allentown", "PA", "ALE - ALLENTOWN, PA"),
        new("ALT", "Altoona", "PA", "ALT - ALTOONA, PA"),
        new("BAL", "Baltimore", "MD", "BAL - BALTIMORE, MD"),
        new("COL", "Columbus", "OH", "COL - COLUMBUS, OH"),

        // CON is the default/original Dock Commander layout.
        new("CON", "Southington", "CT", "CON - SOUTHINGTON, CT", true),

        new("EXV-CLE", "Expeditors - Cleveland", "OH", "EXV - CLE - EXPEDITORS - CLEVELAND"),
        new("EXV-CMH", "Expeditors - Columbus", "OH", "EXV - CMH - EXPEDITORS - COLUMBUS"),
        new("EXV-JFK", "Expeditors - Carteret", "NJ", "EXV - JFK - EXPEDITORS - CARTERET"),
        new("EXV-PHL", "Expeditors - Philadelphia", "PA", "EXV - PHL - EXPEDITORS - PHILADELPHIA"),
        new("MAS", "Northborough", "MA", "MAS - NORTHBOROUGH, MA"),
        new("MAW", "Westfield", "MA", "MAW - WESTFIELD, MA"),
        new("MDH", "Hagerstown", "MD", "MDH - HAGERSTOWN, MD"),
        new("MEB", "Pittsfield", "ME", "MEB - PITTSFIELD, ME"),
        new("MEP", "Portland", "ME", "MEP - PORTLAND, ME"),
        new("NHC", "Concord", "NH", "NHC - CONCORD, NH"),
        new("NJC", "Carteret", "NJ", "NJC - CARTERET, NJ"),
        new("NJE", "East Brunswick", "NJ", "NJE - EAST BRUNSWICK, NJ"),
        new("NJW", "Westampton", "NJ", "NJW - WESTAMPTON, NJ"),
        new("NYA", "Albany", "NY", "NYA - ALBANY, NY"),
        new("NYB", "Buffalo", "NY", "NYB - BUFFALO, NY"),
        new("NYM", "Maspeth", "NY", "NYM - MASPETH, NY"),

        // NYN has its own actual Dock Commander layout profile.
        new("NYN", "Newburgh", "NY", "NYN - NEWBURGH, NY", null, true),

        new("NYR", "Rochester", "NY", "NYR - ROCHESTER, NY"),
        new("NYS", "Syracuse", "NY", "NYS - SYRACUSE, NY"),
        new("NYX", "Bronx", "NY", "NYX - BRONX, NY"),
        new("PAC", "Camp Hill", "PA", "PAC - CAMP HILL, PA"),
        new("PAE", "Erie", "PA", "PAE - ERIE, PA"),
        new("PAW", "Allentown Integrated", "PA", "PAW - ALLENTOWN INTEGRATED"),
        new("PGH", "Pittsburgh", "PA", "PGH - PITTSBURGH, PA"),
        new("RIT", "Johnston", "RI", "RIT - JOHNSTON, RI"),
        new("STB", "Streetsboro", "OH", "STB - STREETSBORO, OH"),
        new("TOL", "Toledo", "OH", "TOL - TOLEDO, OH"),
        new("VAH", "Harrisonburg Remote", "VA", "VAH - HARRISONBURG, VA REMOTE"),
        new("VAM", "Manassas", "VA", "VAM - MANASSAS, VA"),
        new("VAO", "Roanoke", "VA", "VAO - ROANOKE, VA"),
        new("VAR", "Richmond", "VA", "VAR - RICHMOND, VA"),
        new("VTB", "Burlington", "VT", "VTB - BURLINGTON, VT"),
        new("WCH", "West Chester", "PA", "WCH - WEST CHESTER, PA"),
        new("WKB", "Wilkes Barre", "PA", "WKB - WILKES BARRE, PA"),
        new("WVB", "Bridgeport", "WV", "WVB - BRIDGEPORT, WV"),
        new("WVC", "Charleston", "WV", "WVC - CHARLESTON, WV"),
        new("YRK", "York", "PA", "YRK - YORK, PA")
    };

    public static DockTerminal? Find(string code)
        => All.FirstOrDefault(t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
}
