public class Combat
{

    LanguageService lang = new LanguageService();
    Config config = Config.Load();
    
    Random rnd;
    string pseudoJoueur;
    Joueur joueur1;

    public Combat(Random rnd1, string nomJoueur, Joueur joueur){
        rnd = rnd1;
        pseudoJoueur = nomJoueur;
        joueur1 = joueur;
        lang.Load(config.language ?? "en");
    }


    public void LancerCombats(){

        bool finCombat = false;

        Ennemy ennemy = new Ennemy(joueur1, rnd); 

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
                Console.WriteLine(lang.Get("replyFalse", variables));
            }
        }

        Dictionary<string, string> CreerVariables()
        {
            var variables = new Dictionary<string, string>
                {
                    { "pseudo", pseudoJoueur},
                    { "joueur1.vie", joueur1.vie.ToString() },
                    { "joueur1.force", joueur1.force.ToString() },
                    { "joueur1.degats", joueur1.degats.ToString() },
                    { "attaqueFaible", joueur1.attaqueFaible.ToString() },
                    { "Ennemy.nom", ennemy.nom },
                    { "Ennemy.vie", ennemy.vie.ToString() },
                    { "Ennemy.degats", ennemy.degats.ToString() },
                    { "exp", joueur1.exp.ToString() },
                    { "expRestant", joueur1.expAvantLvlUp.ToString() },
                    { "niveau", joueur1.niveauSuivant.ToString() },
                    { "expDrop", ennemy.expDrop.ToString() },
                };
            return variables;
        }

        Console.WriteLine(lang.Get("playerStats", CreerVariables()));
        Console.WriteLine(lang.Get("ennemiStats", CreerVariables()));

        bool bloque = false;
        bool strongEnnemyAttack = false;

        void CheckVie()
        {
            if (!joueur1.enVie())
            {
                Console.WriteLine(lang.Get("lose"));
                finCombat = true;
                joueur1 = new Joueur(20, 4, 0, 10, 1, 0);
            }
            else if (!ennemy.enVie())
            {
                Console.WriteLine(lang.Get("win", CreerVariables()));
                int lastLevel = joueur1.lvl;
                joueur1.GagnerExp(ennemy.expDrop);
                if (lastLevel != joueur1.lvl)
                {
                    Console.WriteLine(lang.Get("lvlUp"));
                }
                finCombat = true;
            }
        }
        void PlayerTurn()
        {
            if (rnd.Next(0, 100) < 40)
            {
                strongEnnemyAttack = true;
                Console.WriteLine(lang.Get("strongEnnemyAttack"));
            }

            Console.WriteLine(lang.Get("tour", CreerVariables()));

            int choixAction = LireEntier(lang.Get("choice"), 1, 4);

            if (choixAction == 1)
            {
                Console.WriteLine(lang.Get("choixAttaque", CreerVariables()));
                int choixAttack = LireEntier(lang.Get("choice"),1,2);
                if (choixAttack == 1)
                {
                    ennemy.vie -= joueur1.force;
                    Console.WriteLine(lang.Get("player_attack", CreerVariables()));
                    CheckVie();
                }
                else if (choixAttack == 2)
                {
                    if (rnd.Next(0, 100) < 70)
                    {
                        
                        ennemy.vie -= joueur1.degats;
                        Console.WriteLine(lang.Get("player_attack", CreerVariables()));
                    }
                    else
                    {
                        Console.WriteLine(lang.Get("attackFail"));
                    }
                    CheckVie();
                }
            }
            else if (choixAction == 2)
            {
                bloque = true;
                Console.WriteLine(lang.Get("blocage", CreerVariables()));
                ennemy.vie -= joueur1.attaqueFaible;
                CheckVie();
            }
            else if (choixAction == 3)
            {
                if (rnd.Next(0, 2) == 1)
                {
                    finCombat = true;
                    Console.WriteLine(lang.Get("fuite"));
                }   
                else
                {
                    Console.WriteLine(lang.Get("fuiteFail"));
                }
            }
            else if (choixAction == 4){
                joueur1.exp = joueur1.lvlCapExp;
                joueur1.GagnerExp(0);
            }
        }
        void EnnemyTurn()
        {
            if (bloque)
            {
                if (strongEnnemyAttack)
                {
                    joueur1.vie -= (int)(ennemy.degats * 1.5 * 50 / 100);
                    strongEnnemyAttack = false;
                }
                else
                {
                    joueur1.vie -= ennemy.degats * 50/100;
                }
                bloque = false;
            }
            else if (strongEnnemyAttack)
            {
                joueur1.vie -= (int)(ennemy.degats * 1.5);
                strongEnnemyAttack = false;
            }
            else {joueur1.vie -= ennemy.degats;}
            Console.WriteLine(lang.Get("ennemie_attack", CreerVariables()));
            CheckVie();
        }
        while (joueur1.enVie() && ennemy.enVie() && !finCombat)
        {
            PlayerTurn();
            if (!finCombat)
            {
                EnnemyTurn();
            }
        }
    }
}