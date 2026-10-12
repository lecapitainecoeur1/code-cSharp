public class Joueur
{
    public int vie; // vie du joueur
    public int force; // degat que fait le joueur
    public int exp;
    public int lvlCapExp;
    public int lvl;
    public int maxVie;
    public int potions;
    public int expAvantLvlUp;
    public int niveauSuivant;
    public Joueur(int nbVie, int nbForce, int nbExp, int nbLvlCapExp, int nbLvl, int nbPotion)
    {
        // actualisation des variables
        vie = nbVie;
        maxVie = nbVie;
        force = nbForce;
        exp = nbExp;
        lvlCapExp = nbLvlCapExp;
        lvl = nbLvl;
        potions = nbPotion;
        niveauSuivant = lvl + 1;
        expAvantLvlUp = lvlCapExp - exp;
    }
    public bool enVie()
    {
        return vie > 0;
    }
    public void GagnerExp(int experience)
    {
        exp += experience;
        while (exp >= lvlCapExp)
        {
            LvlUp();
        }
    }
    public void LvlUp()
    {
        exp -= lvlCapExp;
        lvl += 1;
        force += 1;
        maxVie += 2;
        lvlCapExp = lvlCapExp * 50/100;
        niveauSuivant += 1;
        expAvantLvlUp = lvlCapExp - exp;
        vie = maxVie;
    }
}