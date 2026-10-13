public class Ennemy
{
    public string nom;
    public int vie;
    public int degats;
    public int expDrop;

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
    }
    private void Orc()
    {
        nom = "Orc";
        vie = 40;
        degats = 6;
        expDrop = 10;
    }
    private void Troll(){
        nom = "Troll";
        vie = 80;
        degats = 12;
        expDrop = 20;
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
}