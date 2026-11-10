using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace UzemBacsi;

public class BhopConfig
{
    [JsonPropertyName("Bekapcsolva")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("SebessegLimitNelkul")] public bool Unlimited { get; set; } = false;
}

public class ParachuteConfig
{
    [JsonPropertyName("Bekapcsolva")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("EsesiSebesseg")] public float FallSpeed { get; set; } = 150f;
}

public class AdsConfig
{
    [JsonPropertyName("Bekapcsolva")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("PercenkentEgyszer")] public float Minutes { get; set; } = 10f;
    [JsonPropertyName("Uzenetek")] public List<string> Messages { get; set; } = new()
    {
        "Discord: {lime}https://discord.gg/T7u8TYJapR"
    };
}

public class WelcomeConfig
{
    [JsonPropertyName("Bekapcsolva")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Uzenet")] public string Message { get; set; } =
        "Szia {lime}{nev}{default}! Skinek: {lime}!ws{default} | Rangod: {lime}!rank{default} | Discord: {lime}!dc";
}

public class DamageConfig
{
    [JsonPropertyName("SebzesSzamokTalalatkor")] public bool HitNumbers { get; set; } = true;
    [JsonPropertyName("OsszesitesHalalUtan")] public bool DeathReport { get; set; } = true;
}

public class TagConfig
{
    [JsonPropertyName("Bekapcsolva")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("Admin")] public string Admin { get; set; } = "{red}[ADMIN]";
    [JsonPropertyName("Vip")] public string Vip { get; set; } = "{gold}[VIP]";
    [JsonPropertyName("AdminTablazatban")] public string AdminClan { get; set; } = "[ADMIN]";
    [JsonPropertyName("VipTablazatban")] public string VipClan { get; set; } = "[VIP]";
}

public class RankTier
{
    [JsonPropertyName("Nev")] public string Name { get; set; } = "";
    [JsonPropertyName("Pont")] public int Points { get; set; }
}

public class StatsConfig
{
    [JsonPropertyName("Bekapcsolva")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("PontOlesert")] public int Kill { get; set; } = 2;
    [JsonPropertyName("PontFejlovesert")] public int Headshot { get; set; } = 1;
    [JsonPropertyName("PontAsszisztert")] public int Assist { get; set; } = 1;
    [JsonPropertyName("PontHalalert")] public int Death { get; set; } = -1;
    [JsonPropertyName("Rangok")] public List<RankTier> Ranks { get; set; } = new()
    {
        new() { Name = "Újonc", Points = 0 },
        new() { Name = "Bronz", Points = 100 },
        new() { Name = "Ezüst", Points = 300 },
        new() { Name = "Arany", Points = 600 },
        new() { Name = "Platina", Points = 1000 },
        new() { Name = "Gyémánt", Points = 1600 },
        new() { Name = "Legenda", Points = 2500 },
    };
}

public class DefuseConfig
{
    [JsonPropertyName("CsakHaNincsTerrorista")] public bool OnlyWhenNoT { get; set; } = true;
    [JsonPropertyName("MaxTavolsag")] public float MaxDistance { get; set; } = 100f;
    [JsonPropertyName("AutomatikusHaNincsTerrorista")] public bool AutoWhenNoT { get; set; } = true;
}

public class StreakConfig
{
    [JsonPropertyName("Bekapcsolva")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("OlesekSzama")] public int Kills { get; set; } = 4;
    [JsonPropertyName("Jutalom")] public string Reward { get; set; } = "weapon_healthshot";
}

public class UzemConfig : BasePluginConfig
{
    [JsonPropertyName("Elotag")] public string Prefix { get; set; } = "{lightblue}[Üzem-Bácsi]{default}";
    [JsonPropertyName("Discord")] public string Discord { get; set; } = "https://discord.gg/T7u8TYJapR";
    [JsonPropertyName("Bhop")] public BhopConfig Bhop { get; set; } = new();
    [JsonPropertyName("Ejtoernyo")] public ParachuteConfig Parachute { get; set; } = new();
    [JsonPropertyName("TiltottFegyverek")] public List<string> BannedWeapons { get; set; } = new()
    {
        "weapon_nova", "weapon_xm1014", "weapon_sawedoff", "weapon_mag7"
    };
    [JsonPropertyName("Hirdetes")] public AdsConfig Ads { get; set; } = new();
    [JsonPropertyName("Udvozles")] public WelcomeConfig Welcome { get; set; } = new();
    [JsonPropertyName("Sebzes")] public DamageConfig Damage { get; set; } = new();
    [JsonPropertyName("Cimkek")] public TagConfig Tags { get; set; } = new();
    [JsonPropertyName("Statisztika")] public StatsConfig Stats { get; set; } = new();
    [JsonPropertyName("Sorozat")] public StreakConfig Streak { get; set; } = new();
    [JsonPropertyName("Hatastalanitas")] public DefuseConfig Defuse { get; set; } = new();
    [JsonPropertyName("MaxJatekos")] public int MaxPlayers { get; set; } = 6;
    [JsonPropertyName("Botok")] public bool Bots { get; set; } = false;
    [JsonPropertyName("MeccsVegenUgyanazAPalya")] public bool KeepMap { get; set; } = true;
}
