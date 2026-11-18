public class Ennemy
{
    public string nom;
    public int vie;
    public int degats;
    public int expDrop;
    public int monaieDrop;

    public Ennemy(string unNom, int nbVie, int nbDegats, int nbExpDrop)
    {
        nom = unNom;
        vie = nbVie;
        degats = nbDegats;
        expDrop = nbExpDrop;
    }
    private void Gobelin()
    {
        nom = "Gobelin";
        vie = 18;
        degats = 3;
        expDrop = 5;
        monaieDrop = 2;
    }
    private void Orc()
    {
        nom = "Orc";
        vie = 34;
        degats = 5;
        expDrop = 9;
        monaieDrop = 4;
    }
    private void Troll(){
        nom = "Troll";
        vie = 60;
        degats = 9;
        expDrop = 18;
        monaieDrop = 8;
    }
    public Ennemy(Joueur joueur, Random rnd)
    {
        if (joueur.lvl < 5)
        {
            Gobelin();
        }
        else if (joueur.lvl <= 10)
        {
            if (rnd.Next(0, 2) == 0)
            {
                Gobelin();
            }
            else
            {
                Orc();
            }
        }
        else
        {
            
            if (rnd.Next(0, 2) == 0)
            {
                Orc();
            }
            else
            {
                Troll();
            }
        }
    }

    public bool enVie()
    {
        return vie > 0;
    }
    public void PrendreDegats(int degats)
    {
        vie -= degats;
    }
}