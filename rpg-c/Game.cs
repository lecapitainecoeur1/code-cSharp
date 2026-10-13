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
Console.WriteLine(lang.Get("get_pseudo"));
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
Joueur joueur1 = new Joueur(20, 4, 0, 10, 1, 0);
Combat combat = new Combat(rnd, pseudoJoueur, joueur1);

bool wentContinue = true;
while(wentContinue){
    combat.LancerCombats();
    Console.WriteLine(lang.Get("restart"));
    string restart = Console.ReadLine();
    if (restart == "1")
    {
    }
    else
    {
        Environment.Exit(0);
    }
}