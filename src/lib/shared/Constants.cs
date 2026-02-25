namespace WhoIsSpy.Lib.Shared;

/// <summary>Application-wide named constants to avoid magic values.</summary>
public static class Constants
{
    // ── Table names ──────────────────────────────────────────────────────────
    public const string TableRooms = "Rooms";
    public const string TablePlayers = "Players";
    public const string TableGameState = "GameState";
    public const string TableVotes = "Votes";

    // ── Environment variable keys ────────────────────────────────────────────
    public const string EnvAdminPass = "AdminPass";
    public const string EnvStorageAccount = "StorageAccountName";

    // ── Game rules ───────────────────────────────────────────────────────────
    public const int MinPlayers = 3;
    public const int MaxPlayers = 30;
    public const int TurnSeconds = 60;
    public const int VotingSeconds = 120;

    // ── Partition / row key helpers ──────────────────────────────────────────
    /// <summary>Builds the Votes partition key for a given room and round.</summary>
    public static string VotePartitionKey(string roomCode, int round) =>
        $"{roomCode}_R{round}";

    // ── Word pairs (civilian word, spy word) ─────────────────────────────────
    public static readonly IReadOnlyList<(string Civilian, string Spy)> WordPairs =
    [
        // Animals
        ("cat", "kitten"),
        ("lion", "tiger"),
        ("dolphin", "shark"),
        ("eagle", "hawk"),
        ("wolf", "fox"),
        ("horse", "donkey"),
        ("frog", "toad"),
        ("penguin", "seal"),
        ("crocodile", "alligator"),
        ("butterfly", "moth"),

        // Food & drink
        ("chocolate", "candy"),
        ("coffee", "tea"),
        ("apple", "pear"),
        ("pizza", "flatbread"),
        ("butter", "margarine"),
        ("wine", "champagne"),
        ("sushi", "sashimi"),
        ("hamburger", "hotdog"),
        ("ice cream", "gelato"),
        ("honey", "syrup"),

        // Nature & places
        ("ocean", "sea"),
        ("beach", "desert"),
        ("mountain", "hill"),
        ("forest", "jungle"),
        ("river", "stream"),
        ("volcano", "geyser"),
        ("cave", "tunnel"),
        ("island", "peninsula"),
        ("glacier", "iceberg"),
        ("swamp", "marsh"),

        // People & roles
        ("doctor", "nurse"),
        ("police", "soldier"),
        ("chef", "baker"),
        ("king", "queen"),
        ("teacher", "professor"),
        ("pilot", "astronaut"),
        ("judge", "lawyer"),
        ("carpenter", "blacksmith"),
        ("painter", "sculptor"),
        ("spy", "detective"),

        // Objects & tools
        ("sword", "knife"),
        ("piano", "guitar"),
        ("car", "truck"),
        ("airplane", "helicopter"),
        ("rocket", "missile"),
        ("telescope", "microscope"),
        ("compass", "map"),
        ("hammer", "wrench"),
        ("candle", "torch"),
        ("mirror", "window"),

        // Buildings & structures
        ("castle", "palace"),
        ("library", "bookstore"),
        ("church", "temple"),
        ("lighthouse", "watchtower"),
        ("bridge", "tunnel"),
        ("stadium", "arena"),
        ("prison", "fortress"),
        ("hospital", "clinic"),
        ("hotel", "hostel"),
        ("museum", "gallery"),

        // Mythology & fantasy
        ("dragon", "lizard"),
        ("ghost", "shadow"),
        ("robot", "android"),
        ("vampire", "werewolf"),
        ("wizard", "witch"),
        ("mermaid", "siren"),
        ("giant", "ogre"),
        ("unicorn", "pegasus"),
        ("zombie", "skeleton"),
        ("fairy", "elf"),

        // Science & space
        ("diamond", "ruby"),
        ("planet", "moon"),
        ("comet", "asteroid"),
        ("atom", "molecule"),
        ("black hole", "nebula"),
        ("DNA", "protein"),
        ("battery", "capacitor"),
        ("laser", "flashlight"),
        ("magnet", "compass"),
        ("satellite", "space station"),

        // Everyday life
        ("umbrella", "raincoat"),
        ("clock", "watch"),
        ("phone", "tablet"),
        ("newspaper", "magazine"),
        ("backpack", "suitcase"),
        ("carpet", "rug"),
        ("elevator", "escalator"),
        ("bank", "ATM"),
        ("cinema", "theatre"),
        ("playground", "park"),
    ];
}
