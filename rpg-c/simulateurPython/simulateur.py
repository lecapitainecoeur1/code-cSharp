"""
Simulateur d'équilibrage pour ton RPG (copie en Python des règles de Joueur.cs, Ennemy.cs, Combat.cs, Shop.cs).

Utilisation :  python3 simulateur_rpg.py
Un "robot" joue des milliers de parties avec une stratégie fixe, et on compare les résultats.
Pour tester un réglage, modifie les valeurs dans REGLAGES_ACTUELS / REGLAGES_PROPOSES (ou crée-en un nouveau).

Limites : première vie uniquement (le bug de resetJoueur n'est pas simulé), pas de fuite sauf stratégie dédiée,
le robot joue mieux qu'un humain : compare les chiffres entre eux plutôt que de les lire comme des vérités.
"""
import random
import statistics

# ----------------------------------------------------------------------------
# 1) RÉGLAGES (tout ce que tu peux changer pour équilibrer le jeu)
# ----------------------------------------------------------------------------
# Ennemis : (PV, dégâts, XP donnée, pièces données)
ENNEMIS_ACTUELS = {'G': (18, 3, 5, 2), 'O': (34, 5, 9, 4), 'T': (60, 9, 18, 8)}
ENNEMIS_PROPOSES = {'G': (16, 3, 5, 2), 'O': (30, 5, 9, 4), 'T': (52, 8, 18, 8)}

# Niveau d'arrivée de chaque ennemi (sert à la progression des ennemis)
NIVEAU_ARRIVEE = {'G': 1, 'O': 5, 'T': 11}


def reglages(**changements):
    r = dict(
        vie_depart=20, force_depart=4, endurance_depart=20,
        vie_par_niveau=2, force_par_niveau=1,
        xp_premier_niveau=10, xp_croissance=40,          # le seuil d'XP augmente de 40 % par niveau
        gain_endurance_a1=5, cout_endurance_a2=10,
        chance_reussite_a2=70,                            # en %
        degats_a2_niveau1=175, degats_a2_ensuite=150,     # en % de la force (comme dans ton code)
        soin_potion=37,                                   # en % des PV max
        potion_gratuite_par_victoire=1,
        prix_potion=4, utilise_boutique=True,
        chance_fuite=50, chance_attaque_forte=40,         # en %
        ennemis=ENNEMIS_ACTUELS,
        progression_pv=0.0, progression_degats=0.0,       # ex. 0.07 = +7 % par niveau après l'arrivée
    )
    r.update(changements)
    return r


REGLAGES_ACTUELS = reglages()
REGLAGES_PROPOSES = reglages(ennemis=ENNEMIS_PROPOSES, progression_pv=0.07, progression_degats=0.05)


# ----------------------------------------------------------------------------
# 2) LE JEU (même logique que ton C#, divisions entières comprises)
# ----------------------------------------------------------------------------
def nouveau_joueur(r, niveau=1):
    force = r['force_depart'] + (niveau - 1) * r['force_par_niveau']
    vie_max = r['vie_depart'] + (niveau - 1) * r['vie_par_niveau']
    pct = r['degats_a2_niveau1'] if niveau == 1 else r['degats_a2_ensuite']
    return dict(niveau=niveau, exp=0, seuil=r['xp_premier_niveau'], vie=vie_max, vie_max=vie_max,
                force=force, endurance=r['endurance_depart'], degats=force * pct // 100,
                attaque_faible=force * 25 // 100, potions=0, pieces=0)


def monter_niveaux(j, r):
    while j['exp'] >= j['seuil']:
        j['exp'] -= j['seuil']
        j['niveau'] += 1
        j['force'] += r['force_par_niveau']
        j['vie_max'] += r['vie_par_niveau']
        j['seuil'] += j['seuil'] * r['xp_croissance'] // 100
        j['vie'] = j['vie_max']                                   # soin complet à la montée de niveau
        j['attaque_faible'] = j['force'] * 25 // 100
        j['degats'] = j['force'] * r['degats_a2_ensuite'] // 100


def ennemis_possibles(niveau):
    if niveau < 5:
        return ['G']
    return ['G', 'O'] if niveau <= 10 else ['O', 'T']


def combat(j, type_ennemi, strategie, rng, r):
    """Joue un combat. Renvoie 'victoire', 'mort' ou 'fuite'."""
    pv, degats_e, xp, pieces = r['ennemis'][type_ennemi]
    k = max(0, j['niveau'] - NIVEAU_ARRIVEE[type_ennemi])        # niveaux depuis l'arrivée de l'ennemi
    pv = round(pv * (1 + r['progression_pv'] * k))
    degats_e = max(1, round(degats_e * (1 + r['progression_degats'] * k)))

    while True:
        attaque_forte = type_ennemi == 'O' and rng.random() * 100 < r['chance_attaque_forte']
        action = strategie(j, pv, degats_e, attaque_forte, rng, r, type_ennemi)
        bloque = False

        if action == 'a1':
            pv -= j['force']
            j['endurance'] += r['gain_endurance_a1']
        elif action == 'a2':                                      # un échec ne coûte pas d'endurance (comme ton code)
            if rng.random() * 100 < r['chance_reussite_a2']:
                pv -= j['degats']
                j['endurance'] -= r['cout_endurance_a2']
        elif action == 'blocage':
            bloque = True
            pv -= j['attaque_faible']
        elif action == 'potion':
            j['potions'] -= 1
            j['vie'] += j['vie_max'] * r['soin_potion'] // 100
        elif action == 'fuite':
            if rng.random() * 100 < r['chance_fuite']:
                j['vie'] = j['vie_max']
                return 'fuite'

        if action in ('a1', 'a2', 'blocage') and pv <= 0:
            j['potions'] += r['potion_gratuite_par_victoire']
            j['exp'] += xp
            j['pieces'] += pieces
            monter_niveaux(j, r)
            if r['utilise_boutique']:
                n = j['pieces'] // r['prix_potion']
                j['potions'] += n
                j['pieces'] -= n * r['prix_potion']
            j['vie'] = min(j['vie'], j['vie_max'])
            return 'victoire'

        # tour de l'ennemi : le blocage annule l'attaque forte (ton code : if bloque ... else if forte)
        degats = degats_e // 2 if bloque else (degats_e * 150 // 100 if attaque_forte else degats_e)
        j['vie'] -= degats
        if j['vie'] <= 0:
            return 'mort'
        j['vie'] = min(j['vie'], j['vie_max'])


# ----------------------------------------------------------------------------
# 3) LES STRATÉGIES DU ROBOT
# ----------------------------------------------------------------------------
def strategie_reflechie(style='achever', seuil_potion=0.4, seuil_fuite=None):
    """style : 'a1' (attaque 1 seulement), 'a2' (attaque 2 dès que possible), 'achever' (attaque 1 + attaque 2 pour finir)."""
    def choisir(j, pv_ennemi, degats_e, forte, rng, r, type_ennemi):
        if pv_ennemi <= j['force']:
            return 'a1'                                           # coup sûr qui termine le combat
        if seuil_fuite is not None and j['vie'] <= seuil_fuite * j['vie_max']:
            return 'fuite'
        if seuil_fuite is None and j['potions'] > 0 and j['vie'] <= seuil_potion * j['vie_max']:
            return 'potion'
        if forte:
            return 'blocage'
        if style == 'a2' and j['endurance'] > 0:
            return 'a2'
        if style == 'achever' and j['endurance'] > 0 and pv_ennemi <= j['degats']:
            return 'a2'
        return 'a1'
    return choisir


def strategie_hasard(j, pv_ennemi, degats_e, forte, rng, r, type_ennemi):
    actions = ['a1', 'blocage', 'fuite']
    if j['endurance'] > 0:
        actions.append('a2')
    if j['potions'] > 0:
        actions.append('potion')
    return rng.choice(actions)


STRATEGIES = [
    ("a1 + a2 pour achever", strategie_reflechie('achever')),
    ("a2 dès que possible", strategie_reflechie('a2')),
    ("a1 seulement", strategie_reflechie('a1')),
    ("fuit sous 50% de PV", strategie_reflechie('a2', seuil_fuite=0.5)),
    ("hasard", strategie_hasard),
]


# ----------------------------------------------------------------------------
# 4) LES MESURES
# ----------------------------------------------------------------------------
def courbe_de_difficulte(r, strategie, niveaux=(1, 3, 5, 8, 11, 14, 18, 22), essais=3000):
    """Un seul combat, joueur plein de PV, sans potion. Renvoie (niveau, % de PV perdus, % de morts)."""
    rng = random.Random(1)
    lignes = []
    for niveau in niveaux:
        pertes, morts, total = [], 0, 0
        types = ennemis_possibles(niveau)
        for t in types:
            for _ in range(essais // len(types)):
                j = nouveau_joueur(r, niveau)
                j['seuil'] = 10 ** 9                              # évite le soin de montée de niveau pendant la mesure
                depart = j['vie']
                res = combat(j, t, strategie, rng, r)
                morts += res == 'mort'
                total += 1
                pertes.append(depart if res == 'mort' else depart - j['vie'])
        lignes.append((niveau, 100 * statistics.mean(pertes) / depart, 100 * morts / total))
    return lignes


def carriere(r, strategie, parties=700, combats_max=250):
    """Une partie = des combats jusqu'à la mort. Renvoie (victoires moyennes, médiane, % qui tiennent 250 combats, % morts avant niv. 5)."""
    rng = random.Random(3)
    victoires, survivants, morts_precoces = [], 0, 0
    for _ in range(parties):
        j = nouveau_joueur(r)
        gagnes, mort = 0, False
        for _ in range(combats_max):
            types = ennemis_possibles(j['niveau'])
            res = combat(j, types[rng.randrange(len(types))], strategie, rng, r)
            if res == 'mort':
                mort = True
                break
            gagnes += res == 'victoire'
        victoires.append(gagnes)
        survivants += not mort
        morts_precoces += mort and j['niveau'] <= 4
    n = parties
    return statistics.mean(victoires), statistics.median(victoires), 100 * survivants / n, 100 * morts_precoces / n


def rapport(titre, r):
    print("=" * 84)
    print(titre)
    print("-" * 84)
    print("Difficulté d'un combat isolé (robot « a1 + a2 pour achever ») :")
    lignes = courbe_de_difficulte(r, strategie_reflechie('achever'))
    print("  niveau    : " + "  ".join(f"{l[0]:>5}" for l in lignes))
    print("  PV perdus : " + "  ".join(f"{l[1]:4.0f}%" for l in lignes))
    print("  mort      : " + "  ".join(f"{l[2]:4.1f}%" for l in lignes))
    print("\nParties complètes (victoires avant la mort, 250 combats maximum) :")
    for nom, strat in STRATEGIES:
        moy, med, surv, tot = carriere(r, strat)
        print(f"  {nom:<24} moyenne {moy:6.1f}  médiane {med:4.0f}  tient 250 combats {surv:5.1f}%  mort avant niv.5 {tot:5.1f}%")


if __name__ == '__main__':
    rapport("RÉGLAGES ACTUELS (ton dépôt)", REGLAGES_ACTUELS)
    rapport("RÉGLAGES PROPOSÉS (Gobelin 16, Orc 30, Troll 52/8, ennemis +7 % PV et +5 % dégâts par niveau)", REGLAGES_PROPOSES)