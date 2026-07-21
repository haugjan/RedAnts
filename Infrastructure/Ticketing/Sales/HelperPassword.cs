namespace RedAnts.Infrastructure.Ticketing.Sales;

public static class HelperPassword
{
    private static readonly string[] Adjectives =
    [
        "Rote", "Blaue", "Gruene", "Gelbe", "Weisse", "Schwarze", "Goldene", "Silberne", "Bunte", "Helle",
        "Dunkle", "Warme", "Kalte", "Frische", "Klare", "Reine", "Feine", "Grobe", "Weiche", "Harte",
        "Glatte", "Runde", "Eckige", "Spitze", "Flache", "Steile", "Hohe", "Tiefe", "Weite", "Enge",
        "Breite", "Schmale", "Kurze", "Lange", "Grosse", "Kleine", "Schwere", "Leichte", "Dicke", "Duenne",
        "Schnelle", "Flinke", "Rasche", "Ruhige", "Stille", "Leise", "Laute", "Wilde", "Zahme", "Sanfte",
        "Starke", "Kraftvolle", "Robuste", "Zarte", "Muntere", "Frohe", "Lustige", "Freche", "Kecke", "Nette",
        "Kluge", "Schlaue", "Weise", "Mutige", "Tapfere", "Kuehne", "Stolze", "Treue", "Edle", "Gute",
        "Suesse", "Saure", "Scharfe", "Milde", "Wuerzige", "Neue", "Alte", "Junge", "Fruehe", "Spaete",
        "Wackre", "Kesse", "Heitere", "Fesche", "Schicke", "Coole", "Flotte", "Fixe", "Wache", "Pfiffige",
        "Goldige", "Putzige", "Drollige", "Knuffige", "Herzige", "Emsige", "Fleissige", "Eifrige", "Muntre", "Wonnige",
        "Sonnige", "Windige", "Stuermische", "Neblige", "Frostige", "Eisige", "Glutrote", "Tuerkise", "Lila", "Rosa",
        "Braune", "Graue", "Beige", "Cremige", "Perlige", "Glaenzende", "Matte", "Sammtige", "Seidige", "Federleichte"
    ];

    private static readonly string[] MasculineNouns =
    [
        "Hund", "Fuchs", "Hase", "Igel", "Otter", "Biber", "Dachs", "Wolf", "Baer", "Loewe",
        "Tiger", "Panda", "Esel", "Adler", "Falke", "Rabe", "Spatz", "Storch", "Reiher", "Kranich",
        "Pinguin", "Delfin", "Hai", "Wal", "Krebs", "Seestern", "Berg", "Fluss", "Bach", "See",
        "Strand", "Wald", "Baum", "Busch", "Klee", "Ahorn", "Kaktus", "Farn", "Pilz", "Stein",
        "Fels", "Sand", "Kiesel", "Kristall", "Mond", "Stern", "Komet", "Regen", "Schnee", "Wind",
        "Sturm", "Blitz", "Donner", "Nebel", "Frost", "Funke", "Kuchen", "Keks", "Honig", "Zucker",
        "Reis", "Apfel", "Anker", "Kompass"
    ];

    private static readonly string[] FeminineNouns =
    [
        "Pizza", "Katze", "Giraffe", "Amsel", "Meise", "Eule", "Robbe", "Muschel", "Koralle", "Qualle",
        "Insel", "Wiese", "Blume", "Rose", "Tulpe", "Nelke", "Lilie", "Distel", "Eiche", "Buche",
        "Birke", "Tanne", "Kiefer", "Palme", "Sonne", "Wolke", "Flamme", "Glut", "Torte", "Waffel",
        "Butter", "Sahne", "Suppe", "Nudel", "Bohne", "Erbse", "Linse", "Gurke", "Tomate", "Birne",
        "Pflaume", "Kirsche", "Traube", "Banane", "Zitrone", "Melone", "Beere", "Nuss", "Mandel", "Kastanie",
        "Gitarre", "Trommel", "Floete", "Geige", "Rakete", "Laterne"
    ];

    private static readonly string[] NeuterNouns =
    [
        "Zebra", "Kamel", "Pony", "Fohlen", "Tal", "Meer", "Feld", "Moos", "Feuer", "Brot"
    ];

    private static readonly (string Word, char Gender)[] Nouns =
        MasculineNouns.Select(n => (Word: n, Gender: 'm'))
            .Concat(FeminineNouns.Select(n => (Word: n, Gender: 'f')))
            .Concat(NeuterNouns.Select(n => (Word: n, Gender: 'n')))
            .ToArray();

    public static string Generate()
    {
        var adjective = Adjectives[Random.Shared.Next(Adjectives.Length)];
        var (word, gender) = Nouns[Random.Shared.Next(Nouns.Length)];
        return Decline(adjective, gender) + word;
    }

    private static string Decline(string feminine, char gender)
    {
        if (gender == 'f' || !feminine.EndsWith('e')) return feminine;
        var stem = feminine[..^1];
        return gender == 'm' ? stem + "er" : stem + "es";
    }
}
