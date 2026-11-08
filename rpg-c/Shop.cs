public class Shop
{
    LanguageService lang = new LanguageService();
    Config config = Config.Load();
    public Shop()
    {
        lang.Load(config.language ?? "en");
    }
    public void OuvrirShop(Joueur joueur1, Combat combat)
    {
        int LireEntier(string message, int min, int max)
        {
            while (true)
            {
                Console.Write(message);

                if (int.TryParse(Console.ReadLine(), out int valeur)
                    && valeur >= min
                    && valeur <= max)
                {
                    return valeur;
                }
                var variables = new Dictionary<string, string>
                {
                    { "min", min.ToString() },
                    { "max", max.ToString() }
                };
                Console.WriteLine(lang.Get("reponseIncorrecte", variables));
            }
        }
        choix:
        int choixAction = LireEntier(lang.Get("shop"), 1, 3);

        var variables = new Dictionary<string, string>
        {
            { "coins", joueur1.monaie.ToString() }
        };

        if (choixAction == 1)
        {
            int choixAchats = LireEntier(lang.Get("achatShop", variables), 1, 2);
            if (choixAchats == 1)
            {
                if (joueur1.monaie >= 4)
                {
                    joueur1.monaie -= 4;
                    joueur1.potions += 1;
                    Console.WriteLine(lang.Get("achats"));
                    goto choix;
                }
                else
                {   
                    
                    Console.WriteLine(lang.Get("pasMonaie"));
                    goto choix;
                }
            }
            else if (choixAchats == 2)
            {
                goto choix;
            }
        }
        else if (choixAction == 2)
        {
            combat.LancerCombats();
        }
        else if (choixAction == 3)
        {
            Environment.Exit(0);
        }
    }
}