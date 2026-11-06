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
    public int attaqueFaible;
    public int degats;
    public int soins;
    private int reductionDegats;
    private int attaqueEnnemiBonnus;

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
        degats = force * 175/100;
        attaqueFaible = force * 25/100;
        soins = maxVie * 25 / 100;
        reductionDegats = 1;
        attaqueEnnemiBonnus = 1;
    }

    public bool enVie()
    {
        return vie > 0;
    }

    public void GagnerExp(int experience)
    {
        exp += experience;
        expAvantLvlUp = lvlCapExp - exp;
        while (exp >= lvlCapExp)
        {
            LvlUp();
        }
    }

    public void LvlUp()
    {
        exp -= lvlCapExp;
        lvl += 1;
        force += 1; // valeur a modifier pour augmenter proportionnelement
        maxVie += 2; // valeur a modifier pour augmenter proportionnelement
        lvlCapExp += lvlCapExp * 50 / 100;
        niveauSuivant += 1;
        expAvantLvlUp = lvlCapExp - exp;
        vie = maxVie;
        attaqueFaible = force * 25 / 100;
        degats = force * 150/100;
    }
    public void PrendreDegats(string nomEnnemi, int degatsPrit, bool bloque, bool strongEnnemyAttack)
    {
        if (bloque)
        {
            reductionDegats = 50 / 100;
        }
        else if (strongEnnemyAttack)
        {
            attaqueEnnemiBonnus = 150/100;
        }
        vie -= degatsPrit * reductionDegats * attaqueEnnemiBonnus;
        reductionDegats = 1;
        attaqueEnnemiBonnus = 1;
    }
}