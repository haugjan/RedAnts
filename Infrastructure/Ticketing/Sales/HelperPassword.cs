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

    private static readonly string[] Nouns =
    [
        "Pizza", "Katze", "Hund", "Fuchs", "Hase", "Igel", "Otter", "Biber", "Dachs", "Wolf",
        "Baer", "Loewe", "Tiger", "Panda", "Zebra", "Giraffe", "Kamel", "Esel", "Pony", "Fohlen",
        "Adler", "Falke", "Rabe", "Amsel", "Meise", "Spatz", "Storch", "Reiher", "Kranich", "Eule",
        "Pinguin", "Robbe", "Delfin", "Hai", "Wal", "Krebs", "Muschel", "Koralle", "Qualle", "Seestern",
        "Berg", "Tal", "Fluss", "Bach", "See", "Meer", "Insel", "Strand", "Wald", "Wiese",
        "Feld", "Baum", "Busch", "Blume", "Rose", "Tulpe", "Nelke", "Lilie", "Distel", "Klee",
        "Ahorn", "Eiche", "Buche", "Birke", "Tanne", "Kiefer", "Palme", "Kaktus", "Farn", "Moos",
        "Pilz", "Stein", "Fels", "Sand", "Kiesel", "Kristall", "Sonne", "Mond", "Stern", "Komet",
        "Wolke", "Regen", "Schnee", "Wind", "Sturm", "Blitz", "Donner", "Nebel", "Frost", "Feuer",
        "Flamme", "Funke", "Glut", "Brot", "Kuchen", "Torte", "Keks", "Waffel", "Honig", "Zucker",
        "Butter", "Sahne", "Suppe", "Nudel", "Reis", "Bohne", "Erbse", "Linse", "Gurke", "Tomate",
        "Apfel", "Birne", "Pflaume", "Kirsche", "Traube", "Banane", "Zitrone", "Melone", "Beere", "Nuss",
        "Mandel", "Kastanie", "Gitarre", "Trommel", "Floete", "Geige", "Rakete", "Anker", "Kompass", "Laterne"
    ];

    public static string Generate() =>
        Adjectives[Random.Shared.Next(Adjectives.Length)] + Nouns[Random.Shared.Next(Nouns.Length)];
}
