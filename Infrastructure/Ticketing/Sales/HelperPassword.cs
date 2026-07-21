namespace RedAnts.Infrastructure.Ticketing.Sales;

public static class HelperPassword
{
    private static readonly string[] MasculineAdjectives =
    [
        "Roter", "Blauer", "Gruener", "Gelber", "Weisser", "Schwarzer", "Goldener", "Silberner", "Bunter", "Brauner",
        "Grauer", "Rosa", "Lila", "Tuerkiser", "Violetter", "Purpurner", "Beiger", "Himmelblauer", "Tiefblauer", "Hellblauer",
        "Dunkelblauer", "Dunkelroter", "Hellgruener", "Dunkelgruener", "Feuerroter", "Schneeweisser", "Blutroter", "Rostroter", "Sonnengelber", "Grasgruener",
        "Nachtblauer", "Silbriger", "Goldiger", "Glaenzender", "Matter", "Perliger", "Schimmernder", "Leuchtender", "Heller", "Dunkler",
        "Klarer", "Blasser", "Fahler", "Greller", "Grosser", "Kleiner", "Riesiger", "Winziger", "Breiter", "Schmaler",
        "Hoher", "Tiefer", "Langer", "Kurzer", "Weiter", "Enger", "Runder", "Eckiger", "Spitzer", "Flacher",
        "Steiler", "Zierlicher", "Schlanker", "Maechtiger", "Gewaltiger", "Staemmiger", "Kugeliger", "Ovaler", "Krummer", "Gerader",
        "Wuchtiger", "Bauchiger", "Weicher", "Harter", "Glatter", "Rauer", "Grober", "Feiner", "Seidiger", "Samtiger",
        "Flauschiger", "Wolliger", "Borstiger", "Stachliger", "Knuspriger", "Saftiger", "Trockener", "Nasser", "Feuchter", "Staubiger",
        "Schuppiger", "Federleichter", "Warmer", "Kalter", "Heisser", "Kuehler", "Lauer", "Frostiger", "Eisiger", "Gluehender",
        "Frischer", "Sonniger", "Wolkiger", "Regnerischer", "Nebliger", "Windiger", "Stuermischer", "Verschneiter", "Milder", "Feuriger",
        "Schneller", "Langsamer", "Flinker", "Rascher", "Hurtiger", "Behaender", "Flotter", "Fixer", "Traeger", "Gemaechlicher",
        "Fluechtiger", "Reger", "Agiler", "Wilder", "Zahmer", "Sanfter", "Ruhiger", "Stiller", "Lauter", "Munterer",
        "Lebhafter", "Verspielter", "Neugieriger", "Scheuer", "Frecher", "Kecker", "Mutiger", "Tapferer", "Kuehner", "Furchtloser",
        "Stolzer", "Treuer", "Braver", "Artiger", "Ungestuemer", "Wagemutiger", "Verwegener", "Gelassener", "Geduldiger", "Vertraeumter",
        "Verschmitzter", "Pfiffiger", "Schlauer", "Kluger", "Weiser", "Gewitzter", "Aufgeweckter", "Wacher", "Emsiger", "Fleissiger",
        "Eifriger", "Strebsamer", "Geselliger", "Gutmuetiger", "Sanftmuetiger", "Liebenswerter", "Charmanter", "Reizender", "Anmutiger", "Eleganter",
        "Vornehmer", "Edler", "Nobler", "Schicker", "Fescher", "Adretter", "Schmucker", "Putziger", "Drolliger", "Knuffiger",
        "Herziger", "Niedlicher", "Possierlicher", "Wonniger", "Suesser", "Reizvoller", "Saurer", "Bitterer", "Herber", "Scharfer",
        "Wuerziger", "Salziger", "Fruchtiger", "Aromatischer", "Koestlicher", "Leckerer", "Schmackhafter", "Delikater", "Cremiger", "Sahniger",
        "Honigsuesser", "Zuckersuesser", "Neuer", "Alter", "Junger", "Frueher", "Spaeter", "Reifer", "Zeitloser", "Ewiger",
        "Betagter", "Jugendlicher", "Guter", "Praechtiger", "Herrlicher", "Wunderbarer", "Grossartiger", "Prachtvoller", "Stattlicher", "Erhabener",
        "Koeniglicher", "Fuerstlicher", "Kostbarer", "Wertvoller", "Erlesener", "Vitaler", "Robuster", "Kraeftiger", "Starker", "Kraftvoller",
        "Ruestiger", "Gesunder", "Bluehender", "Strahlender", "Heiterer", "Froher", "Froehlicher", "Vergnuegter", "Lustiger", "Beschwingter",
        "Uebermuetiger", "Ausgelassener", "Seliger", "Gluecklicher", "Zufriedener", "Behaglicher", "Gemuetlicher", "Trauter", "Idyllischer", "Friedlicher",
        "Beschaulicher", "Zauberhafter", "Sagenhafter", "Traumhafter", "Fabelhafter", "Wundersamer", "Mystischer", "Magischer", "Schillernder", "Funkelnder",
        "Glitzernder", "Flimmernder", "Lodernder", "Spruehender", "Knisternder", "Rauschender", "Fluesternder", "Klingender", "Singender", "Tanzender",
        "Huepfender", "Springender", "Wirbelnder", "Schwebender", "Gleitender", "Fliegender", "Flatternder", "Kreisender", "Blinkender",
    ];

    private static readonly string[] FeminineAdjectives =
    [
        "Rote", "Blaue", "Gruene", "Gelbe", "Weisse", "Schwarze", "Goldene", "Silberne", "Bunte", "Braune",
        "Graue", "Rosa", "Lila", "Tuerkise", "Violette", "Purpurne", "Beige", "Himmelblaue", "Tiefblaue", "Hellblaue",
        "Dunkelblaue", "Dunkelrote", "Hellgruene", "Dunkelgruene", "Feuerrote", "Schneeweisse", "Blutrote", "Rostrote", "Sonnengelbe", "Grasgruene",
        "Nachtblaue", "Silbrige", "Goldige", "Glaenzende", "Matte", "Perlige", "Schimmernde", "Leuchtende", "Helle", "Dunkle",
        "Klare", "Blasse", "Fahle", "Grelle", "Grosse", "Kleine", "Riesige", "Winzige", "Breite", "Schmale",
        "Hohe", "Tiefe", "Lange", "Kurze", "Weite", "Enge", "Runde", "Eckige", "Spitze", "Flache",
        "Steile", "Zierliche", "Schlanke", "Maechtige", "Gewaltige", "Staemmige", "Kugelige", "Ovale", "Krumme", "Gerade",
        "Wuchtige", "Bauchige", "Weiche", "Harte", "Glatte", "Raue", "Grobe", "Feine", "Seidige", "Samtige",
        "Flauschige", "Wollige", "Borstige", "Stachlige", "Knusprige", "Saftige", "Trockene", "Nasse", "Feuchte", "Staubige",
        "Schuppige", "Federleichte", "Warme", "Kalte", "Heisse", "Kuehle", "Laue", "Frostige", "Eisige", "Gluehende",
        "Frische", "Sonnige", "Wolkige", "Regnerische", "Neblige", "Windige", "Stuermische", "Verschneite", "Milde", "Feurige",
        "Schnelle", "Langsame", "Flinke", "Rasche", "Hurtige", "Behaende", "Flotte", "Fixe", "Traege", "Gemaechliche",
        "Fluechtige", "Rege", "Agile", "Wilde", "Zahme", "Sanfte", "Ruhige", "Stille", "Laute", "Muntere",
        "Lebhafte", "Verspielte", "Neugierige", "Scheue", "Freche", "Kecke", "Mutige", "Tapfere", "Kuehne", "Furchtlose",
        "Stolze", "Treue", "Brave", "Artige", "Ungestueme", "Wagemutige", "Verwegene", "Gelassene", "Geduldige", "Vertraeumte",
        "Verschmitzte", "Pfiffige", "Schlaue", "Kluge", "Weise", "Gewitzte", "Aufgeweckte", "Wache", "Emsige", "Fleissige",
        "Eifrige", "Strebsame", "Gesellige", "Gutmuetige", "Sanftmuetige", "Liebenswerte", "Charmante", "Reizende", "Anmutige", "Elegante",
        "Vornehme", "Edle", "Noble", "Schicke", "Fesche", "Adrette", "Schmucke", "Putzige", "Drollige", "Knuffige",
        "Herzige", "Niedliche", "Possierliche", "Wonnige", "Suesse", "Reizvolle", "Saure", "Bittere", "Herbe", "Scharfe",
        "Wuerzige", "Salzige", "Fruchtige", "Aromatische", "Koestliche", "Leckere", "Schmackhafte", "Delikate", "Cremige", "Sahnige",
        "Honigsuesse", "Zuckersuesse", "Neue", "Alte", "Junge", "Fruehe", "Spaete", "Reife", "Zeitlose", "Ewige",
        "Betagte", "Jugendliche", "Gute", "Praechtige", "Herrliche", "Wunderbare", "Grossartige", "Prachtvolle", "Stattliche", "Erhabene",
        "Koenigliche", "Fuerstliche", "Kostbare", "Wertvolle", "Erlesene", "Vitale", "Robuste", "Kraeftige", "Starke", "Kraftvolle",
        "Ruestige", "Gesunde", "Bluehende", "Strahlende", "Heitere", "Frohe", "Froehliche", "Vergnuegte", "Lustige", "Beschwingte",
        "Uebermuetige", "Ausgelassene", "Selige", "Glueckliche", "Zufriedene", "Behagliche", "Gemuetliche", "Traute", "Idyllische", "Friedliche",
        "Beschauliche", "Zauberhafte", "Sagenhafte", "Traumhafte", "Fabelhafte", "Wundersame", "Mystische", "Magische", "Schillernde", "Funkelnde",
        "Glitzernde", "Flimmernde", "Lodernde", "Spruehende", "Knisternde", "Rauschende", "Fluesternde", "Klingende", "Singende", "Tanzende",
        "Huepfende", "Springende", "Wirbelnde", "Schwebende", "Gleitende", "Fliegende", "Flatternde", "Kreisende", "Blinkende",
    ];

    private static readonly string[] NeuterAdjectives =
    [
        "Rotes", "Blaues", "Gruenes", "Gelbes", "Weisses", "Schwarzes", "Goldenes", "Silbernes", "Buntes", "Braunes",
        "Graues", "Rosa", "Lila", "Tuerkises", "Violettes", "Purpurnes", "Beiges", "Himmelblaues", "Tiefblaues", "Hellblaues",
        "Dunkelblaues", "Dunkelrotes", "Hellgruenes", "Dunkelgruenes", "Feuerrotes", "Schneeweisses", "Blutrotes", "Rostrotes", "Sonnengelbes", "Grasgruenes",
        "Nachtblaues", "Silbriges", "Goldiges", "Glaenzendes", "Mattes", "Perliges", "Schimmerndes", "Leuchtendes", "Helles", "Dunkles",
        "Klares", "Blasses", "Fahles", "Grelles", "Grosses", "Kleines", "Riesiges", "Winziges", "Breites", "Schmales",
        "Hohes", "Tiefes", "Langes", "Kurzes", "Weites", "Enges", "Rundes", "Eckiges", "Spitzes", "Flaches",
        "Steiles", "Zierliches", "Schlankes", "Maechtiges", "Gewaltiges", "Staemmiges", "Kugeliges", "Ovales", "Krummes", "Gerades",
        "Wuchtiges", "Bauchiges", "Weiches", "Hartes", "Glattes", "Raues", "Grobes", "Feines", "Seidiges", "Samtiges",
        "Flauschiges", "Wolliges", "Borstiges", "Stachliges", "Knuspriges", "Saftiges", "Trockenes", "Nasses", "Feuchtes", "Staubiges",
        "Schuppiges", "Federleichtes", "Warmes", "Kaltes", "Heisses", "Kuehles", "Laues", "Frostiges", "Eisiges", "Gluehendes",
        "Frisches", "Sonniges", "Wolkiges", "Regnerisches", "Nebliges", "Windiges", "Stuermisches", "Verschneites", "Mildes", "Feuriges",
        "Schnelles", "Langsames", "Flinkes", "Rasches", "Hurtiges", "Behaendes", "Flottes", "Fixes", "Traeges", "Gemaechliches",
        "Fluechtiges", "Reges", "Agiles", "Wildes", "Zahmes", "Sanftes", "Ruhiges", "Stilles", "Lautes", "Munteres",
        "Lebhaftes", "Verspieltes", "Neugieriges", "Scheues", "Freches", "Keckes", "Mutiges", "Tapferes", "Kuehnes", "Furchtloses",
        "Stolzes", "Treues", "Braves", "Artiges", "Ungestuemes", "Wagemutiges", "Verwegenes", "Gelassenes", "Geduldiges", "Vertraeumtes",
        "Verschmitztes", "Pfiffiges", "Schlaues", "Kluges", "Weises", "Gewitztes", "Aufgewecktes", "Waches", "Emsiges", "Fleissiges",
        "Eifriges", "Strebsames", "Geselliges", "Gutmuetiges", "Sanftmuetiges", "Liebenswertes", "Charmantes", "Reizendes", "Anmutiges", "Elegantes",
        "Vornehmes", "Edles", "Nobles", "Schickes", "Fesches", "Adrettes", "Schmuckes", "Putziges", "Drolliges", "Knuffiges",
        "Herziges", "Niedliches", "Possierliches", "Wonniges", "Suesses", "Reizvolles", "Saures", "Bitteres", "Herbes", "Scharfes",
        "Wuerziges", "Salziges", "Fruchtiges", "Aromatisches", "Koestliches", "Leckeres", "Schmackhaftes", "Delikates", "Cremiges", "Sahniges",
        "Honigsuesses", "Zuckersuesses", "Neues", "Altes", "Junges", "Fruehes", "Spaetes", "Reifes", "Zeitloses", "Ewiges",
        "Betagtes", "Jugendliches", "Gutes", "Praechtiges", "Herrliches", "Wunderbares", "Grossartiges", "Prachtvolles", "Stattliches", "Erhabenes",
        "Koenigliches", "Fuerstliches", "Kostbares", "Wertvolles", "Erlesenes", "Vitales", "Robustes", "Kraeftiges", "Starkes", "Kraftvolles",
        "Ruestiges", "Gesundes", "Bluehendes", "Strahlendes", "Heiteres", "Frohes", "Froehliches", "Vergnuegtes", "Lustiges", "Beschwingtes",
        "Uebermuetiges", "Ausgelassenes", "Seliges", "Glueckliches", "Zufriedenes", "Behagliches", "Gemuetliches", "Trautes", "Idyllisches", "Friedliches",
        "Beschauliches", "Zauberhaftes", "Sagenhaftes", "Traumhaftes", "Fabelhaftes", "Wundersames", "Mystisches", "Magisches", "Schillerndes", "Funkelndes",
        "Glitzerndes", "Flimmerndes", "Loderndes", "Spruehendes", "Knisterndes", "Rauschendes", "Fluesterndes", "Klingendes", "Singendes", "Tanzendes",
        "Huepfendes", "Springendes", "Wirbelndes", "Schwebendes", "Gleitendes", "Fliegendes", "Flatterndes", "Kreisendes", "Blinkendes",
    ];

    private static readonly string[] MasculineNouns =
    [
        "Hund", "Kater", "Fuchs", "Hase", "Igel", "Biber", "Dachs", "Wolf", "Baer", "Loewe",
        "Tiger", "Panda", "Hengst", "Elch", "Luchs", "Marder", "Elefant", "Leopard", "Jaguar", "Puma",
        "Panther", "Gorilla", "Schimpanse", "Lemur", "Koala", "Stier", "Widder", "Maulwurf", "Hamster", "Bueffel",
        "Waschbaer", "Adler", "Falke", "Habicht", "Bussard", "Rabe", "Fink", "Star", "Storch", "Reiher",
        "Kranich", "Uhu", "Kauz", "Specht", "Kuckuck", "Schwan", "Fasan", "Pfau", "Hahn", "Papagei",
        "Sittich", "Kakadu", "Pinguin", "Flamingo", "Pelikan", "Tukan", "Kolibri", "Zaunkoenig", "Delfin", "Wal",
        "Hai", "Seehund", "Seeloewe", "Krebs", "Hummer", "Seestern", "Tintenfisch", "Hecht", "Karpfen", "Aal",
        "Hering", "Thunfisch", "Gecko", "Leguan", "Alligator", "Frosch", "Salamander", "Kaefer", "Marienkaefer", "Falter",
        "Grashuepfer", "Skorpion", "Drache", "Phoenix", "Kobold", "Troll", "Ahorn", "Baum", "Busch", "Strauch",
        "Farn", "Kaktus", "Bambus", "Loewenzahn", "Mohn", "Krokus", "Klee", "Flieder", "Jasmin", "Holunder",
        "Apfel", "Pfirsich", "Granatapfel", "Knoblauch", "Kuerbis", "Rettich", "Mais", "Spinat", "Salat", "Kohl",
        "Blumenkohl", "Brokkoli", "Lauch", "Spargel", "Pilz", "Champignon", "Kuchen", "Keks", "Krapfen", "Muffin",
        "Lutscher", "Honig", "Zucker", "Kaese", "Knoedel", "Pfannkuchen", "Pudding", "Lebkuchen", "Stollen", "Zimtstern",
        "Kakao", "Saft", "Sirup", "Sprudel", "Punsch", "Most", "Nektar", "Berg", "Gipfel", "Huegel",
        "Gletscher", "Vulkan", "Fluss", "Bach", "Strom", "Teich", "Weiher", "Tuempel", "Ozean", "Strand",
        "Wald", "Hain", "Wasserfall", "Pfad", "Steg", "Kiesel", "Sand", "Lehm", "Ton", "Mond",
        "Stern", "Komet", "Planet", "Regen", "Schnee", "Hagel", "Wind", "Sturm", "Blitz", "Donner",
        "Nebel", "Frost", "Reif", "Regenbogen", "Sonnenstrahl", "Stein", "Rubin", "Smaragd", "Saphir", "Bernstein",
        "Opal", "Quarz", "Marmor", "Granit", "Basalt", "Amethyst", "Schiefer", "Feuerstein", "Anker", "Kompass",
        "Kessel", "Krug", "Becher", "Loeffel", "Faden", "Knopf", "Schluessel", "Korb", "Hammer", "Bohrer",
        "Pinsel", "Schlitten", "Karren", "Kahn", "Ballon", "Drachen", "Waggon", "Mantel", "Schal", "Handschuh",
        "Stiefel", "Schuh", "Guertel", "Ring", "Talisman", "Umhang", "Turm", "Herd", "Ofen", "Brunnen",
        "Balkon", "Keller", "Speicher", "Leuchtturm", "Fruehling", "Sommer", "Herbst", "Winter", "Abend", "Mut",
        "Traum", "Wunsch", "Zauber", "Frieden", "Rhythmus", "Klang", "Welpe", "Rotfuchs", "Polarfuchs", "Schneehase",
        "Feldhase", "Delphin", "Albatros", "Eisvogel", "Thymian", "Rosmarin", "Schnittlauch", "Dill", "Koriander", "Fenchel",
        "Ingwer", "Safran", "Muskat", "Wacholder", "Grat", "Sattel", "Steilhang", "Abgrund", "Krater", "Fjord",
        "Strudel", "Sog", "Auwald", "Urwald", "Tannenwald", "Buchenwald", "Mischwald", "Waldrand", "Waldboden", "Morgennebel",
        "Bodennebel", "Windhauch", "Luftzug", "Wirbel", "Raureif", "Eiszapfen", "Vollmond", "Halbmond", "Neumond", "Morgenstern",
        "Abendstern", "Fixstern", "Mondschein", "Kreisel", "Tuerklopfer", "Moerser", "Fingerhut", "Kittel", "Pantoffel", "Holzschuh",
        "Kragen", "Zauberer",
    ];

    private static readonly string[] FeminineNouns =
    [
        "Katze", "Giraffe", "Stute", "Antilope", "Kraehe", "Elster", "Dohle", "Amsel", "Meise", "Eule",
        "Schwalbe", "Lerche", "Nachtigall", "Drossel", "Taube", "Moewe", "Ente", "Wachtel", "Robbe", "Garnele",
        "Krabbe", "Muschel", "Auster", "Koralle", "Forelle", "Makrele", "Sardine", "Schildkroete", "Eidechse", "Schlange",
        "Viper", "Kobra", "Biene", "Hummel", "Wespe", "Hornisse", "Ameise", "Libelle", "Motte", "Raupe",
        "Grille", "Heuschrecke", "Zikade", "Spinne", "Muecke", "Fliege", "Fee", "Nixe", "Meerjungfrau", "Eiche",
        "Buche", "Birke", "Tanne", "Fichte", "Kiefer", "Laerche", "Kastanie", "Ulme", "Linde", "Pappel",
        "Weide", "Erle", "Esche", "Zeder", "Zypresse", "Palme", "Olive", "Hecke", "Ranke", "Distel",
        "Rose", "Tulpe", "Nelke", "Lilie", "Narzisse", "Sonnenblume", "Margerite", "Kornblume", "Glockenblume", "Primel",
        "Hyazinthe", "Orchidee", "Dahlie", "Aster", "Ringelblume", "Kamille", "Alpenrose", "Seerose", "Birne", "Pflaume",
        "Kirsche", "Aprikose", "Traube", "Banane", "Zitrone", "Mandarine", "Melone", "Ananas", "Mango", "Feige",
        "Dattel", "Erdbeere", "Himbeere", "Brombeere", "Heidelbeere", "Stachelbeere", "Preiselbeere", "Nuss", "Walnuss", "Haselnuss",
        "Mandel", "Kokosnuss", "Quitte", "Beere", "Tomate", "Gurke", "Karotte", "Moehre", "Kartoffel", "Ruebe",
        "Bohne", "Erbse", "Linse", "Artischocke", "Semmel", "Brezel", "Torte", "Waffel", "Praline", "Schokolade",
        "Marmelade", "Sahne", "Suppe", "Nudel", "Pizza", "Zuckerwatte", "Limonade", "Milch", "Klippe", "Schlucht",
        "Hoehle", "Grotte", "Quelle", "Bucht", "Lagune", "Insel", "Kueste", "Duene", "Wueste", "Oase",
        "Wiese", "Au", "Steppe", "Lichtung", "Wolke", "Brise", "Boee", "Daemmerung", "Perle", "Bronze",
        "Kohle", "Kreide", "Gitarre", "Geige", "Harfe", "Trommel", "Pauke", "Floete", "Trompete", "Posaune",
        "Klarinette", "Tuba", "Orgel", "Mandoline", "Glocke", "Rassel", "Leier", "Laterne", "Kerze", "Lampe",
        "Lupe", "Sanduhr", "Kanne", "Tasse", "Schuessel", "Kelle", "Gabel", "Schere", "Nadel", "Truhe",
        "Zange", "Saege", "Feile", "Schaufel", "Harke", "Sense", "Sichel", "Angel", "Feder", "Tinte",
        "Spindel", "Kutsche", "Rakete", "Lokomotive", "Muetze", "Jacke", "Socke", "Krone", "Kette", "Brosche",
        "Robe", "Burg", "Huette", "Villa", "Pforte", "Treppe", "Scheune", "Muehle", "Mitternacht", "Nacht",
        "Freude", "Hoffnung", "Freiheit", "Stille", "Ruhe", "Weite", "Ferne", "Sehnsucht", "Melodie", "Harmonie",
        "Wildkatze", "Fledermaus", "Minze", "Petersilie", "Kurkuma", "Vanille", "Brennnessel", "Weintraube", "Maulbeere", "Cranberry",
        "Pistazie", "Pekannuss", "Paranuss", "Marone", "Anhoehe", "Kuppe", "Lawine", "Meerenge", "Sandbank", "Brandung",
        "Woge", "Muendung", "Baumkrone", "Nebelbank", "Schneeflocke", "Galaxie", "Spieluhr", "Seifenblase", "Landkarte", "Sonnenuhr",
        "Wasseruhr", "Klingel", "Teekanne", "Zuckerdose", "Bratpfanne", "Backform", "Stricknadel", "Schuerze", "Kappe", "Haube",
        "Weste", "Sandale", "Wollsocke", "Manschette", "Zauberin",
    ];

    private static readonly string[] NeuterNouns =
    [
        "Kaninchen", "Zebra", "Fohlen", "Pferd", "Reh", "Wiesel", "Frettchen", "Murmeltier", "Faultier", "Nashorn",
        "Nilpferd", "Kaenguru", "Kalb", "Lamm", "Schaf", "Ferkel", "Chinchilla", "Erdmaennchen", "Kueken", "Rotkehlchen",
        "Walross", "Seepferdchen", "Chamaeleon", "Python", "Krokodil", "Einhorn", "Moos", "Veilchen", "Radieschen", "Brot",
        "Broetchen", "Butter", "Omelett", "Eis", "Marzipan", "Tal", "Meer", "Riff", "Feld", "Moor",
        "Ufer", "Dickicht", "Gras", "Schilf", "Nordlicht", "Morgenrot", "Abendrot", "Gewitter", "Gold", "Silber",
        "Kupfer", "Eisen", "Cello", "Horn", "Klavier", "Fernrohr", "Schloss", "Zahnrad", "Kanu", "Segel",
        "Karussell", "Riesenrad", "Diadem", "Armband", "Amulett", "Fenster", "Gewoelbe", "Glueck", "Wunder", "Echo",
        "Kitz", "Basilikum", "Geroell", "Watt", "Delta", "Blaetterdach", "Unterholz", "Wurzelwerk", "Wolkenband", "Hagelkorn",
        "Sternbild", "Daemmerlicht", "Zwielicht", "Platin", "Messing", "Zinn", "Xylophon", "Glockenspiel", "Tamburin", "Windrad",
        "Jojo", "Siegel", "Buegeleisen", "Halstuch",
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
