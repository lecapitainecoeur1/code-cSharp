Random rnd = new Random();
LanguageService lang = new LanguageService();
Config config = Config.Load();

if (config.language != "fr" && config.language != "en")
{
    Console.Write("Choose language (fr/en): ");
    string? choice = Console.ReadLine();
    if (string.IsNullOrEmpty(choice)){
        choice = "en";
    }
    else if (choice != "en" && choice != "fr")
    {
        choice = "en";
    }
    config.language = choice;
    config.Save();
    lang.Load(config.language ?? "en");
}
else
{
    lang.Load(config.language ?? "en");
}
string langue = config.language;
Console.WriteLine(lang.Get("demandePseudo"));
string? pseudoJoueur = Console.ReadLine();
if (string.IsNullOrEmpty(pseudoJoueur))
{
    if (langue == "fr")
        pseudoJoueur = "Joueur";
    else if (langue == "en")
        pseudoJoueur = "Player";
    else
        pseudoJoueur = "Player";
}
var variables = new Dictionary<string, string>{
    { "pseudo", pseudoJoueur},
};
//Console.WriteLine(lang.Get("narration"));
//Console.WriteLine(lang.Get("narration2", variables));
Joueur joueur1 = new Joueur(20, 4, 0, 10, 1, 0, 0);
Combat combat = new Combat(rnd, pseudoJoueur, joueur1);
Shop shop = new Shop();

bool wentContinue = true;
while(wentContinue){
    string fin = combat.LancerCombats();
    string restart = Console.ReadLine();
    if (fin == "ennemyMort" || fin == "fuite")
    {
        if (restart == "1")
        {
            
        }
        else if (restart == "2")
        {
            shop.OuvrirShop(joueur1, combat);
        }
        else
        {
            Environment.Exit(0);
        }    
    }
    else if (fin == "joueurMort")
    {
        if (restart == "1")
        {
            joueur1.resetJoueur();
        }
        else
        {
            Environment.Exit(0);
        }
    }
    
}