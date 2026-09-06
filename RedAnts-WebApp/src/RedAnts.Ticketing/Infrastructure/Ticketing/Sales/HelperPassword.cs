namespace RedAnts.Infrastructure.Ticketing.Sales;

public static class HelperPassword
{
    private static readonly string[] MasculineAdjectives =
    [
        "Roter", "Blauer", "Gelber", "Bunter", "Grauer", "Rosa", "Lila", "Beiger", "Matter", "Heller",
        "Klarer", "Fahler", "Hoher", "Tiefer", "Langer", "Kurzer", "Weiter", "Enger", "Runder", "Ovaler",
        "Harter", "Rauer", "Grober", "Feiner", "Nasser", "Warmer", "Kalter", "Lauer", "Milder", "Fixer",
        "Reger", "Agiler", "Wilder", "Zahmer", "Lauter", "Kecker", "Treuer", "Braver", "Kluger", "Weiser",
        "Wacher", "Edler", "Nobler", "Saurer", "Herber", "Neuer", "Alter", "Junger", "Reifer", "Ewiger",
        "Guter", "Froher",
    ];

    private static readonly string[] FeminineAdjectives =
    [
        "Rote", "Blaue", "Gruene", "Gelbe", "Weisse", "Bunte", "Braune", "Graue", "Rosa", "Lila",
        "Beige", "Matte", "Helle", "Dunkle", "Klare", "Blasse", "Fahle", "Grelle", "Grosse", "Kleine",
        "Breite", "Hohe", "Tiefe", "Lange", "Kurze", "Weite", "Enge", "Runde", "Eckige", "Spitze",
        "Flache", "Steile", "Ovale", "Krumme", "Gerade", "Weiche", "Harte", "Glatte", "Raue", "Grobe",
        "Feine", "Nasse", "Warme", "Kalte", "Heisse", "Kuehle", "Laue", "Eisige", "Milde", "Flinke",
        "Rasche", "Flotte", "Fixe", "Traege", "Rege", "Agile", "Wilde", "Zahme", "Sanfte", "Ruhige",
        "Stille", "Laute", "Scheue", "Freche", "Kecke", "Mutige", "Kuehne", "Stolze", "Treue", "Brave",
        "Artige", "Kluge", "Weise", "Wache", "Emsige", "Edle", "Noble", "Fesche", "Suesse", "Saure",
        "Herbe", "Neue", "Alte", "Junge", "Fruehe", "Spaete", "Reife", "Ewige", "Gute", "Vitale",
        "Starke", "Frohe", "Selige", "Traute",
    ];

    private static readonly string[] NeuterAdjectives =
    [
        "Rotes", "Blaues", "Gelbes", "Buntes", "Graues", "Rosa", "Lila", "Beiges", "Mattes", "Helles",
        "Klares", "Fahles", "Hohes", "Tiefes", "Langes", "Kurzes", "Weites", "Enges", "Rundes", "Ovales",
        "Hartes", "Raues", "Grobes", "Feines", "Nasses", "Warmes", "Kaltes", "Laues", "Mildes", "Fixes",
        "Reges", "Agiles", "Wildes", "Zahmes", "Lautes", "Keckes", "Treues", "Braves", "Kluges", "Weises",
        "Waches", "Edles", "Nobles", "Saures", "Herbes", "Neues", "Altes", "Junges", "Reifes", "Ewiges",
        "Gutes", "Frohes",
    ];

    private static readonly string[] MasculineNouns =
    [
        "Hund", "Kater", "Fuchs", "Hase", "Igel", "Biber", "Dachs", "Wolf", "Baer", "Loewe",
        "Tiger", "Panda", "Hengst", "Elch", "Luchs", "Marder", "Jaguar", "Puma", "Lemur", "Koala",
        "Stier", "Widder", "Adler", "Falke", "Rabe", "Fink", "Star", "Storch", "Reiher", "Uhu",
        "Kauz", "Specht", "Schwan", "Fasan", "Pfau", "Hahn", "Kakadu", "Tukan", "Delfin", "Wal",
        "Hai", "Krebs", "Hummer", "Hecht", "Aal", "Hering", "Gecko", "Leguan", "Frosch", "Kaefer",
        "Falter", "Drache", "Kobold", "Troll", "Ahorn", "Baum", "Busch", "Farn", "Kaktus", "Bambus",
        "Mohn", "Krokus", "Klee", "Jasmin", "Apfel", "Mais", "Spinat", "Salat", "Kohl", "Lauch",
        "Pilz", "Kuchen", "Keks", "Muffin", "Honig", "Zucker", "Kaese", "Kakao", "Saft", "Sirup",
        "Punsch", "Most", "Nektar", "Berg", "Gipfel", "Huegel", "Vulkan", "Fluss", "Bach", "Strom",
        "Teich", "Weiher", "Ozean", "Strand", "Wald", "Hain", "Pfad", "Steg", "Kiesel", "Sand",
        "Lehm", "Ton", "Mond", "Stern", "Komet", "Planet", "Regen", "Schnee", "Hagel", "Wind",
        "Sturm", "Blitz", "Donner", "Nebel", "Frost", "Reif", "Stein", "Rubin", "Saphir", "Opal",
        "Quarz", "Marmor", "Granit", "Basalt", "Anker", "Kessel", "Krug", "Becher", "Faden", "Knopf",
        "Korb", "Hammer", "Bohrer", "Pinsel", "Karren", "Kahn", "Ballon", "Waggon", "Mantel", "Schal",
        "Schuh", "Ring", "Umhang", "Turm", "Herd", "Ofen", "Balkon", "Keller", "Sommer", "Herbst",
        "Winter", "Abend", "Mut", "Traum", "Wunsch", "Zauber", "Klang", "Welpe", "Dill", "Ingwer",
        "Safran", "Muskat", "Grat", "Sattel", "Krater", "Fjord", "Sog", "Auwald", "Urwald", "Wirbel",
        "Kittel", "Kragen", "Garten", "Hunger", "Finger", "Koffer", "Fehler", "Zeiger", "Bagger", "Tunnel",
        "Kaefig", "Faktor", "Sektor", "Doktor", "Tresor", "Zirkus", "Kanton", "Braten", "Deckel", "Bengel",
        "Muskel", "Kaviar", "Rektor", "Meteor", "Reifen", "Kaiser", "Ritter", "Vetter", "Bruder", "Gockel",
        "Kummer", "Jammer", "Sockel", "Riegel", "Panzer", "Wecker", "Rappen", "Batzen", "Gulden", "Saebel",
        "Kolben", "Bolzen", "Zirkel", "Winkel", "Amboss", "Koeder", "Roller", "Vogel", "Esel", "Bock",
        "Ochse", "Ruede", "Lachs", "Wurm", "Ast", "Zweig", "Stamm", "Fels", "Sumpf", "Himmel",
        "Hafen", "Tempel", "Palast", "Boden", "Zaun", "Weg", "Tisch", "Stuhl", "Sessel", "Topf",
        "Eimer", "Besen", "Wagen", "Zug", "Kutter",
    ];

    private static readonly string[] FeminineNouns =
    [
        "Katze", "Stute", "Kraehe", "Elster", "Dohle", "Amsel", "Meise", "Eule", "Lerche", "Taube",
        "Moewe", "Ente", "Robbe", "Krabbe", "Auster", "Viper", "Kobra", "Biene", "Hummel", "Wespe",
        "Ameise", "Motte", "Raupe", "Grille", "Zikade", "Spinne", "Muecke", "Fliege", "Fee", "Nixe",
        "Eiche", "Buche", "Birke", "Tanne", "Fichte", "Kiefer", "Ulme", "Linde", "Pappel", "Weide",
        "Erle", "Esche", "Zeder", "Palme", "Olive", "Hecke", "Ranke", "Distel", "Rose", "Tulpe",
        "Nelke", "Lilie", "Primel", "Dahlie", "Aster", "Birne", "Traube", "Banane", "Melone", "Ananas",
        "Mango", "Feige", "Dattel", "Nuss", "Mandel", "Quitte", "Beere", "Tomate", "Gurke", "Moehre",
        "Ruebe", "Bohne", "Erbse", "Linse", "Semmel", "Brezel", "Torte", "Waffel", "Sahne", "Suppe",
        "Nudel", "Pizza", "Milch", "Klippe", "Hoehle", "Grotte", "Quelle", "Bucht", "Lagune", "Insel",
        "Kueste", "Duene", "Wueste", "Oase", "Wiese", "Au", "Steppe", "Wolke", "Brise", "Boee",
        "Perle", "Bronze", "Kohle", "Kreide", "Geige", "Harfe", "Pauke", "Floete", "Tuba", "Orgel",
        "Glocke", "Rassel", "Leier", "Kerze", "Lampe", "Lupe", "Kanne", "Tasse", "Kelle", "Gabel",
        "Schere", "Nadel", "Truhe", "Zange", "Saege", "Feile", "Harke", "Sense", "Sichel", "Angel",
        "Feder", "Tinte", "Rakete", "Muetze", "Jacke", "Socke", "Krone", "Kette", "Robe", "Burg",
        "Huette", "Villa", "Pforte", "Treppe", "Muehle", "Nacht", "Freude", "Stille", "Ruhe", "Weite",
        "Ferne", "Minze", "Marone", "Kuppe", "Lawine", "Woge", "Kappe", "Haube", "Weste", "Kirche",
        "Fabrik", "Mimose", "Kamera", "Pfanne", "Leiter", "Fliese", "Kachel", "Schnur", "Kordel", "Kaelte",
        "Waerme", "Naesse", "Orange", "Natter", "Kabine", "Kammer", "Kanzel", "Kuppel", "Saeule", "Maus",
        "Kuh", "Ziege", "Gans", "Made", "Qualle", "Blume", "Wurzel", "Bluete", "Frucht", "Sonne",
        "Erde", "Welle", "Flut", "Ebbe", "Heide", "Gasse", "Mauer", "Tuer", "Wand", "Decke",
        "Stufe", "Kueche", "Diele", "Kiste", "Tonne", "Klinge", "Muenze", "Fahne", "Flagge", "Note",
    ];

    private static readonly string[] NeuterNouns =
    [
        "Zebra", "Fohlen", "Pferd", "Reh", "Wiesel", "Kalb", "Lamm", "Schaf", "Ferkel", "Kueken",
        "Python", "Moos", "Brot", "Butter", "Eis", "Tal", "Meer", "Riff", "Feld", "Moor",
        "Ufer", "Gras", "Schilf", "Gold", "Silber", "Kupfer", "Eisen", "Cello", "Horn", "Kanu",
        "Segel", "Diadem", "Glueck", "Wunder", "Echo", "Kitz", "Watt", "Delta", "Platin", "Zinn",
        "Jojo", "Siegel", "Zimmer", "Kissen", "Paddel", "Muster", "Messer", "Wasser", "Metall", "Papier",
        "Wetter", "Gitter", "Futter", "Pulver", "Fieber", "Ventil", "Signal", "Symbol", "Objekt", "Talent",
        "Umfeld", "Umland", "Ticket", "Budget", "Fossil", "Modell", "Ritual", "Lineal", "Diktat", "Rezept",
        "Format", "Insekt", "Gewand", "Gebiet", "Genick", "Gehirn", "Gewehr", "Getier", "Ei", "Haus",
        "Dach", "Tor", "Bett", "Sofa", "Regal", "Brett", "Fass", "Seil", "Netz", "Rad",
        "Boot", "Auto", "Schiff", "Ruder", "Kabel", "Blech", "Rohr", "Glas", "Salz", "Mehl",
        "Obst", "Kraut", "Laub", "Holz", "Beet", "Land", "Loch", "Nest", "Fell", "Maul",
        "Auge", "Ohr", "Herz", "Bein", "Knie", "Haar", "Hemd", "Kleid", "Tuch", "Spiel",
        "Buch", "Heft", "Blatt", "Bild", "Foto", "Radio", "Handy", "Video", "Kino", "Zelt",
        "Feuer", "Licht", "Wachs", "Gift", "Oel", "Vieh", "Wild", "Kamel", "Pony", "Huhn",
        "Baby", "Kind",
    ];


    private static readonly (string Word, char Gender)[] Nouns =
        MasculineNouns.Select(n => (Word: n, Gender: 'm'))
            .Concat(FeminineNouns.Select(n => (Word: n, Gender: 'f')))
            .Concat(NeuterNouns.Select(n => (Word: n, Gender: 'n')))
            .ToArray();

    public static string Generate()
    {
        var (word, gender) = Nouns[Random.Shared.Next(Nouns.Length)];
        var adjectives = gender switch
        {
            'm' => MasculineAdjectives,
            'f' => FeminineAdjectives,
            _ => NeuterAdjectives
        };
        return adjectives[Random.Shared.Next(adjectives.Length)] + word;
    }
}
