# 📜 LORE.md — Bible du monde de Reflets de Lignée

> **Source de vérité du lore.** Créé le 2026-09-24 (phase 0 de la restructuration).
> **Sources :** `/home/aymeric/Miror.txt` (528 lignes) et la carte `/home/aymeric/Downloads/The_Mirror_Legacy_Map.webp` (798×541).
> **Source complémentaire :** wiki du roman, https://the-mirror-legacy.fandom.com/wiki/The_Mirror_Legacy_Wiki (📚).
> **Statut :** ✅ **Lexique de renommage validé par l'utilisateur le 2026-09-24** (§13). Les recommandations 💡 de §4 et §11 attendent encore sa validation.

---

## 0. Décisions et conventions

| # | Décision (2026-09-24) | Pourquoi |
|---|---|---|
| D1 | **Structure fidèle, noms propres renommés.** Les systèmes, les règles et la géographie relative suivent la source ; les noms propres et les termes inventés par le roman sont remplacés (§13). La carte source est une œuvre de fan (signée @百里彤雲) : **référence uniquement, jamais intégrée au jeu** — la carte du jeu sera redessinée. | Une sortie commerciale est prévue (NOT_DONE #69). Les noms et la carte du roman sont un risque de propriété intellectuelle. |
| D2 | **Rien n'est simplifié.** Les 7 royaumes avec tous leurs sous-niveaux, le système de Fruition, les 5 voies et toutes les lignées nommées sont intégrés. Là où la source est implicite ou abîmée, on **ajoute une explication** et on signale l'interprétation (🔎). | Demande explicite de l'utilisateur. |
| D3 | **Orifice spirituel héréditaire** : ~3/1000 sans parent doté, ~30-50 % si un parent en a un (réglable dans `BalanceConfig`), Graines de Sceau en rattrapage. | Le taux strict de 3/1000 viderait le clan de cultivateurs. |
| D5 | **Trois sectes et huit portes.** Le lore est aligné sur la carte (8 portes) ; le wiki liste lui aussi huit portes. | Décision de l'utilisateur (2026-09-24). |
| D6 | **Noms descriptifs conservés** (ex. *Rupture des Liens*, *Âmes Descendantes*, noms des Qi de talisman) : ils décrivent littéralement leur effet. | Décision de l'utilisateur (2026-09-24). |
| D4 | **Un jeu pour les amoureux du xianxia : une infinité de possibilités.** La profondeur vient de la **combinaison** des systèmes du lore, de **règles symétriques** pour tous les personnages du monde et de **choix coûteux** fondés sur le lore (§11). | Demande explicite de l'utilisateur. |

### Règle de renommage (appliquée dans tout ce document)
- **On garde** le vocabulaire traditionnel taoïste / médecine chinoise / genre xianxia, qui n'appartient à personne : noms des royaumes (Respiration Embryonnaire, Culture du Qi, Établissement des Fondations, Manoir Pourpre 紫府, Noyau d'Or, Embryon du Dao, Immortel Doré), Yin/Yang, les Cinq Éléments, Qi, dantian, points Qihai / Juque / Niwan, Fondation Immortelle, capacités divines, Fruition (果位), Grotte Céleste, Palais du Tonnerre, Neuf Fils du Dragon, Palais des Dragons des Quatre Mers.
- **On renomme** : personnes, familles, sectes, portes, États, lieux, techniques nommées, chakras nommés, fondations et capacités divines nommées, Fruitions à nom poétique, objets uniques.
- **Dans le code** : identifiants en anglais (règle d'or du projet) ; les noms affichés vivent **uniquement dans les données** (ScriptableObjects), jamais en dur. Les noms d'origine n'apparaissent **que** dans le lexique §13 (document interne, non livré).
- ⚖️ Ce découpage est une précaution raisonnable, pas un avis juridique : à faire vérifier avant la sortie commerciale.

### Légende
- 📚 **Complément du wiki** : information absente de `Miror.txt`, tirée du wiki du roman.
- 🔎 **Interprétation** : la source est ambiguë, abîmée ou muette ; le choix fait ici est expliqué.
- 🎮 **En jeu** : comment la règle se traduit en mécanique et en code (phase de la restructuration indiquée).
- 💡 **Proposition de design** : mécanique inventée pour servir le lore, **à valider** par l'utilisateur.
- ⚠️ **Écart avec le code actuel** (état au 2026-09-24).

---

## 1. Vue d'ensemble — comment les pièces s'emboîtent

Un cultivateur est défini par **six couches**, de la plus concrète à la plus abstraite :

```
Orifice spirituel  →  sans lui, on reste mortel (sauf Graine de Sceau)
      │
Voie de cultivation  →  la « philosophie » : Immortelle, Diable, Bouddhiste, Démoniaque, Chamanique
      │
Technique (grade 1 à 7+)  →  le manuel pratiqué ; son grade plafonne le royaume atteignable
      │
Royaume + sous-niveau  →  la puissance : 7 royaumes, chacun découpé (6 chakras, 9 niveaux, 4 stades…)
      │
Lignée du Dao (Fruition)  →  le « principe du monde » qu'on incarne : Eau Orthodoxe, Yang Lumineux…
      │
Fondation Immortelle → Capacités divines → Essence métallique → Position de Fruition
      (Fondation)           (Manoir Pourpre)      (Noyau d'Or)          (Noyau d'Or → Embryon du Dao)
```

- La **Voie** et la **Lignée** sont deux axes distincts : la Voie dit *comment* on cultive, la Lignée dit *vers quoi*. On peut passer de l'une à l'autre par des méthodes spécifiques, mais ce n'est sûr qu'aux niveaux très bas ou très hauts.
- Le **clan du joueur** suit la Voie Immortelle, sous-voie *Dao du Noyau d'Or du Manoir Pourpre* (la voie « standard » des familles et des sectes). Les autres voies existent dans le monde : factions, ennemis, événements, et possibilités de conversion (§11).

🎮 Phase 1 (royaumes), 2 (orifice), 3 (techniques), 4 (voies, lignées, fondations, capacités, Fruitions).

---

## 2. Techniques et grades

### 2.1 Principe
Toute technique (méthode de cultivation, sort, déplacement, technique d'arme, art immortel) a un **grade**. Le grade mesure la **qualité globale et le potentiel**, **pas** un royaume. Exemple de la source : une technique de grade 5 de Respiration Embryonnaire (*Intuition du Lotus Blanc*) donne une vitesse de cultivation aussi importante qu'une technique du Manoir Pourpre.

🔎 Deux effets distincts du grade, que la source mélange :
1. **Plafond** : la méthode de cultivation principale ne mène pas au-delà d'un certain royaume (tableau ci-dessous).
2. **Vitesse** : à royaume égal, un grade plus haut cultive plus vite.

### 2.2 Tableau des grades

| Grade | Nature | Royaume maximal atteignable | Qui la possède | Exemple (renommé) |
|---|---|---|---|---|
| **1-2** | Arts immortels impurs ou techniques anciennes aux graves inconvénients | Culture du Qi | Cultivateurs marginaux, familles pauvres | *Méthode du Souffle Commun* (grade 2) : permet à **n'importe qui** de devenir cultivateur de Qi, au prix d'une essence et d'une aura **impures** et d'un **potentiel inférieur** |
| **3** | Technique principale, « pierre angulaire » de la plupart des familles | Établissement des Fondations | La plupart des familles | *Sutra de la Source Claire* (technique du clan Mo) |
| **4** | Expérience **bien plus complète** de la Fondation dès la Culture du Qi ; sorts, arts immortels et techniques d'arme montent jusqu'à la Fondation | Établissement des Fondations | Réservé aux **trois sectes et huit portes** et aux familles les plus éminentes | *Méthode de l'Averse Mesurée* |
| **5** | Mène au Manoir Pourpre | Manoir Pourpre | Extrêmement bien gardée par clans, sectes et portes immortelles | *Canon du Givre Nocturne* |
| **6** | Manuel **complet** du Manoir Pourpre : sorts, préparatifs des fondements du Dao, **et** la technique de raffinement de l'essence. Aussi appelé **« Lignée Dao »**. | Permet de **viser** le Noyau d'Or | Très rares | *Canon des Sept Terrasses* (famille Gu), le plus célèbre |
| **7+** | Véritable manuel d'immortel, mène **directement** au Noyau d'Or et au-delà, quel que soit le domaine | Noyau d'Or et au-delà | Une seule connue | *Dialogue de Gongye Shu avec le Pêcheur du Saule*, censément transmise par un immortel |

### 2.3 Les trois catégories de méthodes de Qi
Pour entrer en Culture du Qi, il faut absorber **un Qi spirituel précis** et le cultiver avec **la méthode correspondante**.

| Catégorie | Définition | Conséquence |
|---|---|---|
| **Commune** | Achetable en boutique à prix fixe | Le Qi correspondant est encore récoltable |
| **Ancestrale** | Obsolète | Le Qi qu'elle demande a disparu, ou son milieu producteur a été détruit → inutilisable sans source de Qi |
| **Secrète** | Technique ancestrale modifiée | Souvent imparfaite (risques, plafonds, effets secondaires) |

- **Technique secrète de montée au Manoir Pourpre** : les techniques de grade ≤ 4 n'en contiennent pas. C'est pourquoi la plupart des cultivateurs plafonnent au stade avancé de la Fondation (ex. *Pan Yiqiu*, *Rong Shuang*). Posséder ou non cette technique secrète indique si l'on a la technique du Manoir Pourpre correspondante.

### 2.4 Méthodes de cultivation du Qi connues (renommées)

| Méthode | Élément | Grade | Famille / faction | Royaume suprême |
|---|---|---|---|---|
| *Pas du Phénix de Braise* | Feu | 4 | — | Fondation |
| *Sutra de la Source Claire* | Eau | 3 | Mo, Ruan | Fondation |
| *Canon du Givre Nocturne* | Yin | 5 | Secte du Pic des Nuées | Manoir Pourpre |
| *Sutra de la Marée Silencieuse* | Eau | 5 | Bai | Manoir Pourpre |
| *Méthode du Ruisseau Remonté* | Eau | 3 | Ruan, Gu | Fondation |
| *Sutra du Verbe Premier* | ? (📚 inconnu aussi du wiki) | 5-6 | Bai | Noyau d'Or ? |
| *Art secret de la Perle Grise* | Archaïque | 3-4 | — | Fondation |
| *Méthode de l'Averse Mesurée* | Eau | 4 | Secte du Pic des Nuées | Fondation |
| *Méthode du Rempart d'Airain* | Métal | 4 | Zang | Fondation |
| *Méthode du Tranchant Clair* | Métal | 3 | Lü, Porte du Fer Ardent | Fondation |
| *Sutra du Cœur Tissé* | Archaïque | 3 | Tao | Fondation |
| *Méthode de la Source Souterraine* | Eau | 3 | Xun, Mo | Fondation |
| *Manuel du Soleil Intérieur* | Yang | 4+ | Manoir immortel de Chenguang, Mo | Manoir Pourpre |
| *Art du Brasier Englouti* | Feu | 5+ | Atoll des Perles Noires | Manoir Pourpre |
| *Méthode du Souffle Commun* | Autre | 2 | — | Culture du Qi |
| *Canon des Sept Terrasses* | Terre | 6 | Gu | Manoir Pourpre |
| *Art secret de l'Éclair Cendré* | Tonnerre | 3 | Mo | Fondation |
| *Méthode de la Perle de Rosée* | 📚 Lueur de l'Aube (Qi Coloré de l'Aube) | 4 | Secte du Pic des Nuées | Fondation |
| *Art du Grondement Lointain* | Tonnerre | 5+ | Qiao | Manoir Pourpre |
| *Méthode de l'Écorce Scellée* | Bois | 3+ | — | Fondation |
| *Méthode des Vapeurs du Littoral* | 📚 Yin (fondation *Brume du Yin Retourné*) | 3+ | — (📚 retrouvée par le miroir) | Fondation |
| *Art du Murmure des Cèdres* | Douze Essences | 5+ | Zang | Manoir Pourpre |
| *Méthode de la Bise Hivernale* | Douze Essences | 3 | Tao, Mo | Fondation |
| *Veilleur du Sentier* | Archaïque | 2 | Fang | Fondation |
| *Méthode des Six Harmonies* | — | — | Mer Orientale | — |
| *Méthode du Qi Limpide* | Douze Essences | 2 | — | Culture du Qi |

📚 Les deux lignes qui semblaient contredire la règle des grades sont expliquées par le wiki :
- ***Canon des Sept Terrasses*** (grade 6 → « Manoir Pourpre ») : c'est un **héritage complet** du Manoir Pourpre — des Partenaires Dao jusqu'aux Partenaires de substitution (l'*Essence du Yang Mineur* peut remplacer une fois un Partenaire Dao). La colonne « royaume suprême » indique donc le royaume **couvert en entier** ; le grade 6 donne en plus de quoi **viser** le Noyau d'Or. Son Qi (*Qi Profond du Bélier Variant*) se récolte sur une pierre de montagne exposée au soleil brûlant : **un an par filament, dix ans** pour une quantité suffisante, puis **dix ans** de raffinage à l'aide d'un démon né d'une chèvre sacrificielle. La famille Gu a décliné quand une migration forcée lui a fait perdre cette source de Qi.
- ***Veilleur du Sentier*** (grade 2 → « Fondation ») : c'est une **technique secrète** tirée d'un sutra ancien de **grade 4** (*Sutra de l'Aîné qui Frappe à la Cour*), qui menait au Manoir Pourpre (fondation *Cour du Jade Général*, Jade Premier). Son Qi d'origine (*Qi de la Poussière Rouge de la Cour*) est devenu introuvable quand le Manoir de l'Aube d'Argent s'est retiré du monde ; la version secrète utilise un autre Qi (*Qi de l'Armure de Givre de la Cour*). **Défauts** : cultivation rapide en Respiration Embryonnaire mais **très lente en Culture du Qi**, **espérance de vie légèrement réduite**, et impuissance totale face à quiconque pratique le sutra d'origine, même d'un royaume inférieur.
- ***Méthode de la Perle de Rosée*** : rarement pratiquée car complexe, mais l'une des meilleures de son rang (sorts d'esquive et de déstabilisation). **Tous ses Partenaires Dao sont perdus** : la pratiquer est donc **sûr** (personne ne peut « récolter » son Dao, §5.3.3). Son Qi se récolte chaque jour en montant dans les nuages puis en descendant au fond des vallées.

### 2.5 Qi spirituel, collecte et substitution (📚)
- **Tout Qi spirituel dépend d'une lignée du Dao** : il n'existe que parce qu'une Fruition permet sa manifestation. Une région riche en Qi n'est pas « bénie » : c'est un lieu où l'autorité d'une lignée s'exprime sans obstacle ; une région pauvre est souvent une lignée **supprimée, contredite ou vacante**. La plupart des régions ont un mélange des Douze Qi et des Qi élémentaires.
- **Innombrables types de Qi**, jamais interchangeables : chaque type correspond à au moins une capacité divine d'une Fruition ; deux Qi d'Eau peuvent être incompatibles s'ils relèvent de capacités ou d'images différentes. Confondre puissance et compatibilité a éteint des traditions entières.
- **Collecte** : le Qi ambiant doit être récolté par une **méthode adaptée**, en **filaments** (*wisps*), condensés en **portions** ; il faut au moins une portion pour les usages complexes (alchimie, fondation immortelle).
- **Substitution** : les manuels orthodoxes utilisent le Qi natif de leur Fruition ; des manuels avancés ou hérétiques utilisent un **Qi de substitution** d'une autre lignée. Selon la **compatibilité relationnelle** des Fruitions : configuration favorable → fondation parfois **plus stable** qu'avec le Qi natif ; neutre → inefficace, fondation fragile ; hostile → **mortel** (même une pilule contenant une trace de Qi incompatible).
- **États d'une lignée** : **active** (Qi abondant, façonné par le détenteur) ; **neutre** quand la Fruition n'a pas de détenteur (Qi rare, diffus, certains types disparaissent, des manuels orthodoxes deviennent **inutilisables** : il faut des réserves anciennes, des milieux scellés, du Qi de substitution, ou l'aide temporaire d'un Surplus / Intercalaire) ; **néfaste** (contradiction interne, autorité forcée, images corrompues) : le Qi ne se manifeste plus ou déstabilise la cultivation — première cause de **déviations de masse**.
- **Autorité du détenteur** : il peut **supprimer** tout le Qi de sa lignée, le **restreindre** à des régions ou à des lignées de sang, ou **cacher** le Qi d'une seule capacité divine. Exemple : le détenteur de l'Eau Muable a caché le Qi d'une de ses capacités ; les manuels et fondations qui en dépendaient ne fonctionnent plus.
- **Dérive des images** : quand l'image d'une Fruition change (nouveau détenteur, empreinte), de nouveaux Qi variants apparaissent et les anciens s'éteignent ; les fondations liées à l'ancienne image deviennent difficiles, voire **opprimées** (cas du Qi Sec). S'adapter crée de nouvelles écoles ; résister mène à l'extinction.
- **Changement de convention mondiale** : quand un Vrai Monarque obtient une Réalisation, la lignée prospère et **tout le monde de la cultivation s'adapte** (ex. après la Réalisation du Jade Premier, les sorts du monde entier exigent un ingrédient semblable au jade).

🎮 Phase 3 : `TechniqueData.Grade` (1-7, 7 = « 7+ »), `TechniqueData.Category` (Commune/Ancestrale/Secrète), `TechniqueData.Kind` (Cultivation, Sort, Déplacement, Arme, ArtImmortel), `RequiredQi` pour les méthodes de cultivation, flag `HasPurpleMansionSecret`. Le moteur de déduction du miroir (fragments de qualité 1-5) produit désormais un **grade**.
⚠️ Aujourd'hui `TechniqueData` n'a ni grade ni catégorie ; `RequiredRealm` est un plancher, pas un plafond.

---

## 3. Les cinq Voies de cultivation

La **Voie** est le chemin emprunté vers l'immortalité. Il en existe cinq, chacune avec ses sous-voies et ses propres découpages de royaumes, parfois asymétriques entre eux.

### 3.1 Dao Immortel — quête orthodoxe de l'immortalité par l'harmonie du cœur, de l'essence et la recherche de la vérité

| Sous-voie | Description complète | Royaumes |
|---|---|---|
| **Nature Spirituelle, Culture Immortelle** (« Voie ancestrale de l'ingestion du Qi ») | La plus orthodoxe et originelle. Cultive **à la fois le cœur et l'essence**, mène directement à l'immortalité. Exige de **concentrer son destin** sur une Fruition et de s'y engager totalement : la conduite, les devoirs et les actes du pratiquant sont dictés par la combinaison du destin de la Fruition et du sien (ex. *Ze Wuyan* ne pouvait s'incliner devant personne, de peur d'attirer un mal karmique sur celui qu'il aurait honoré). Seul **1 enfant sur 1000** doté d'un orifice spirituel peut réussir sur cette voie. Pratiquée surtout dans l'Antiquité ; aujourd'hui ses adeptes sont **en retraite** dans des lieux sacrés ou des Grottes Célestes. | Comme le Dao du Noyau d'Or du Manoir Pourpre, avec deux différences : **la cultivation des capacités divines est continue**, et **la naissance de l'essence métallique commence dès la première capacité divine**. Ils sont donc infiniment plus puissants et forment une sorte de **pseudo-Noyau d'Or** dès la perfection de leurs capacités. Grâce à la cultivation conjointe du destin et de l'essence, leur montée au Noyau d'Or est **directe** et ne demande pas l'alignement final du destin. |
| **Dao du Noyau d'Or du Manoir Pourpre** | La voie « standard » des familles et des sectes actuelles ; celle du clan du joueur. Détaillée au §5. | 7 royaumes (§5) |

### 3.2 Dao du Diable — survie, pillage, usage des forces maléfiques

| Sous-voie | Description complète |
|---|---|
| **Dao du Démon Embryon Céleste** (« Dao Démoniaque de la Fournaise Unifiée du Manoir Divers ») | Voie ancestrale des cultivateurs démoniaques. Au lieu de passer à la Culture du Qi après la condensation des chakras, ils les **combinent en un « Manoir Divers »** qui fusionne la Cour Juque, le Manoir Shenyang et le point Qihai. Cela **réduit la double ascension à une seule** lors de la création des pouvoirs divins. Inconvénient : il faut **consommer de l'essence de sang** pour former le Manoir Divers. |
| **Dao Démoniaque du Manoir Pourpre-Noyau d'Or** (« Voie du Démon Pourpre-Or ») | Nourrissent leurs chakras et absorbent le Qi, mais **le rejettent au 9e niveau** pour bâtir directement des fondements immortels et cultiver des pouvoirs divins. **Abaisse fortement le seuil** de cultivation : progression d'une rapidité inégalée. Ils ne recherchent **pas le destin**, seulement l'essence. Au sommet de la montée au Noyau d'Or, ils prennent le **risque d'aligner leur destin** sur leur Fruition ; en cas de réussite, leur destin est aussi accompli que celui d'un pratiquant de la Culture Spirituelle. |

### 3.3 Dao Bouddhiste — soulager la souffrance, transcender la réincarnation, maîtriser destin et karma
Au lieu de rechercher destin et essence ensemble, ses adeptes se consacrent au **destin**. Ils ne cultivent pas les vertus intérieurement mais leur **manifestation**. Si l'expression ultime des Cinq Vertus est l'existence et la réalité, celle des Cinq Manifestations est **la non-existence et le vide** : nourrir le vide, le non-vide, rechercher le vide intérieur.

| Sous-voie | Description complète |
|---|---|
| **Les Sept Aspects du Dharma** (« bouddhisme moderne ») | Recourt à **tous les moyens** pour soulager la souffrance. Domine le **Nord** grâce à une essence métallique qui accroît ses pouvoirs et lui confère des capacités quasi divines. |
| **Les Cinq Dharmas, la Nature** (« bouddhisme ancien ») | Utilise techniques et incantations pour s'aligner sur le plan sacré. N'agit **pas directement sur l'essence** d'une personne et **rejette les pratiques cruelles ou sanglantes**. |

### 3.4 Dao Démoniaque — voies des êtres démoniaques (bêtes, plantes), lignée et transformation naturelle
Ils absorbent **continuellement** l'énergie spirituelle environnante et nourrissent leur corps. Grâce à leur longévité, leur corps a le temps de développer **naturellement** son orifice spirituel, sa fondation immortelle, etc., par **l'éveil de la lignée**.

| Sous-voie | Description |
|---|---|
| **Myriade de races** | Démons, plantes et animaux ordinaires ayant acquis la conscience par hasard ou par la culture du Qi pur. |
| **Véritable Dao du Serpent-Dragon** | Réservé aux bêtes démoniaques dragonnes, descendantes du **Vrai Serpent-Dragon céleste** (§12). |

### 3.5 Dao du Sceau Chamanique — invoquer des puissances supérieures par rituels et talismans
Trois méthodes : **Transformation, Dissimulation, Sacrifice**. Ce Dao vient de la vénération des **trois Fruitions chamaniques de la Fusion Ancestrale** : la *Chouette des Seuils*, le *Jade Premier* et le *Grand Chaman*, chacune liée à une méthode. Les pratiquants invoquent la puissance d'entités supérieures par rituels ou chants.

📚 Correspondance confirmée par le wiki (« respectivement ») : **Transformation ↔ *Chouette des Seuils***, **Dissimulation ↔ *Jade Premier*** (📚 qui gouverne aussi le réel et l'illusoire, le vrai et le faux), **Sacrifice ↔ *Grand Chaman*** (essence de sang, §6.7). 📚 La Fruition du Grand Chaman est la plus proche du Dao chamanique : avec la technique adéquate, elle permet de **passer au Dao chamanique en gardant une partie de sa progression** (un maître l'a utilisée pour échapper aux Émissaires de la Mort).

### 3.6 Dao Divin — le sixième Dao (📚, absent de `Miror.txt`)
Dao des **dieux, des fantômes et des esprits**, fondé sur l'acte de **saisir une Profondeur** pour transcender la mortalité. Au lieu de se raffiner soi-même, le pratiquant **se lie à ce qui possède déjà une autorité du Dao** : un grand être, un domaine sacré, une ressource à l'échelle du monde, un concept reconnu par le Ciel (gouvernance, destin, ordre céleste). « La Profondeur précède la puissance. » Autrefois égal aux Dao immortel, bouddhiste, chamanique et démoniaque, il a **presque disparu**.
| Méthode | Principe | Prix |
|---|---|---|
| **Emprunter une Profondeur extérieure** | Tirer sa puissance d'un être ou d'une autorité supérieure en lui restant subordonné | Puissance explosive bien au-delà de son niveau, mais **aucune autonomie** : on monte et chute avec sa source |
| **S'unir à une Profondeur extérieure** | Fondre son mandat de vie avec un lieu sacré, un royaume secret ou un artefact, qui devient son vrai corps | **Quasi indestructible** tant que la Profondeur est intacte, mais **immobile** et plafonné par la qualité de la Profondeur |
| **Posséder une Profondeur** (« harnacher la Profondeur intérieure ») | Garder le plein contrôle de son mandat et intégrer la Profondeur par raffinage rituel | La plus rare et la plus exigeante ; mène vers une vraie divinité |
- **Royaumes connus** : **Serviteur Divin** (dieu mineur, esprit ou fonctionnaire fantôme ; puissance allant de la Culture du Qi au Manoir Pourpre) → **Noyau Divin** (autorité divine stable, analogue au Noyau d'Or) → royaumes supérieurs inconnus.
- Les pratiquants **abandonnent ou transforment leur corps** (formes de vie spirituelles) ; ils excellent à **hanter** : imprimer leur existence si profondément que l'imitation ou la commémoration peut déclencher corruption, folie ou calamité longtemps après leur mort.
- Né d'Au-delà des Profondeurs, adopté par la **Profondeur Englobante**, il a engendré la **Voie Impériale** (gouverner pour cultiver), perfectionnée par un détenteur du Yang Lumineux.
- Fruitions prédisposées : **Sentinelle de la Cité, Régent Céleste, Magnétisme Primordial, Élixir Parfait, Tonnerre Profond, Corps de Kui**.

### 3.7 Rangs des autres Dao (📚) — des équivalences asymétriques
**Dao Bouddhiste** (le « Dao de cultivation » et la lignée sont **séparés** : un bouddhiste peut cultiver n'importe quelle Fruition ; le **Qi Brillant** favorise les bouddhistes) :
| Rang | Équivalent (voie immortelle) |
|---|---|
| **Moine** | Respiration Embryonnaire et Culture du Qi (des mortels au corps de fer) |
| **Maître Moine** | Fondation (légèrement plus fort) |
| **Miséricordieux** | Manoir Pourpre (légèrement plus faible) — n'existe que dans le bouddhisme moderne |
| **Maha** | entre Manoir Pourpre et Noyau d'Or |
| **Maître du Dharma** | Noyau d'Or (plafonné au début du Noyau d'Or pour les modernes) |
| **Vénérable** | Embryon du Dao (inaccessible aux modernes) |
- **Bouddhisme ancien** (Cinq Dharmas) : nourrit pas à pas sa nature et son mandat de vie ; extrêmement stable ; retiré dans des Terres Bénies et Grottes Célestes après les grandes guerres. Pouvoirs : barrières contre la divination, perception des présages et du karma, suppression des démons et des ressentiments, réincarnation choisie pour préserver le savoir.
- **Bouddhisme moderne** (Sept Aspects : Colère, Grand Désir, Discipline, Adoration…) : **pyramide d'emprunt** — les Maîtres du Dharma tirent leur autorité des **Terres d'Or du Vénérable Kongji**, les Mahas empruntent à leurs Maîtres, les Miséricordieux **livrent leur âme véritable** aux Mahas, Moines et Maîtres Moines empruntent à tous. Par les **Terres Pures**, ils confient leur âme : réincarnation, résurrection, fuite de la mort ; les rangs supérieurs ont **une autorité absolue** sur la vie, le destin et la réincarnation des inférieurs. Pouvoirs : pseudo-capacités divines, rupture et détournement du karma, manifestations courroucées, création de Terres Pures, résurrection et réincarnation forcée.
- Tous les bouddhistes harmonisent leur essence avec le Qi Brillant (radiance dorée laquée qui supprime démons, esprits et ressentiments). Le bouddhisme **progresse souvent par la mort** (réincarnation). **Vœux** et **débats du Dao** sont des mécanismes réels : perdre un débat peut forcer une conversion, endommager le mandat de vie ou l'âme, détruire une voie du Dharma, ou faire naître une nouvelle doctrine.
**Dao du Diable** (Démon Embryon Céleste) : Respiration Embryonnaire → **Manoir Divers** (≈ Culture du Qi) → **Fournaise Unifiée** (≈ Fondation) → rangs supérieurs non nommés (≈ Manoir Pourpre, Noyau d'Or, Embryon du Dao).

🎮 Phase 4 : enum `CultivationPath` { Immortal, Devil, Buddhist, Demonic, Shamanic, **Divine** } + `CultivationSubPath` (9 valeurs ci-dessus + les trois méthodes du Dao Divin) ; **rangs propres** à chaque voie avec leur table d'équivalence (§3.7). Clan du joueur : Immortal / Purple-Mansion-Golden-Core. Les factions portent leur voie ; la conversion (ex. vers le bouddhisme pour échapper à la mort, §5.4.3) est un événement.

---

## 4. Orifice spirituel, mortels et Graines de Sceau

- La plupart des gens ne sont **pas** destinés à cultiver : seuls **3 sur 1000** ont un **orifice spirituel**, un canal inné **détectable uniquement par un cultivateur confirmé**.
- Les rares **Graines de Sceau** contournent cette condition en greffant des voies spirituelles artificielles sur un mortel.
- 📚 Dans le roman, ces graines viennent **du miroir** (« Perles Profondes ») : c'est ainsi que la famille du miroir, sans orifice, a commencé à cultiver. Détail en §11.5.
- 🔎 Ni la source ni le wiki ne donnent de règle générale ; 📚 le wiki cite un mortel mort « à près de soixante-dix ans ». 💡 **Recommandation** : 60-80 ans (moyenne ~65-70), avec une variation individuelle ; un Qi de talisman peut l'allonger (*Prolonger la vie et accroître la longévité* : +40 ans, §11.5). À valider en phase 2.

🎮 Phase 2 : `CharacterData.HasSpiritualOrifice` tiré à la naissance selon D3 ; `OrificeKnown` passe à vrai quand un cultivateur confirmé l'examine (tâche ou événement). Un mortel ne peut pas choisir la tâche Cultivation mais travaille (mine, patrouille, commerce). Les Graines de Sceau sont un objet rare (miroir, commerce, événements).
⚠️ Aujourd'hui tout membre peut cultiver.

---

## 5. Les royaumes du Dao du Noyau d'Or du Manoir Pourpre

Sept royaumes. Les titres entre parenthèses sont ceux que les mortels et les cultivateurs donnent à qui les atteint.

| # | Royaume | Titre | Sous-niveaux | Espérance de vie | Mur de passage |
|---|---|---|---|---|---|
| 1 | Respiration Embryonnaire | Immortel (pour les mortels) / Cultivateur | 6 chakras | 120 ans | Chakras 1, 3, 5 (« trois épreuves ») |
| 2 | Culture du Qi | Immortel / Cultivateur | 9 niveaux célestes | ~200 ans | 9e niveau → Fondation (le plus dur) |
| 3 | Établissement des Fondations | Nom taoïste / Dao | 4 stades | 300 ans | Technique secrète du Manoir Pourpre |
| 4 | Manoir Pourpre | Maître taoïste / Seigneur | 4 stades (selon les capacités divines) | ~500 ans | 4e capacité divine (« Seuil d'Immortalité ») |
| 5 | Noyau d'Or | Vrai Monarque | 4 stades | 1000 ans | Pleine Réalisation de Fruition |
| 6 | Embryon du Dao (anc. Âme Naissante) | Immortel (仙人) | inconnu | jusqu'à la fin des temps | inconnu |
| 7 | Immortel Doré (« Grande Culmination ») | Seigneur Immortel | inconnu | au-delà | — |

⚠️ Code actuel : 6 royaumes (pas d'Immortel Doré), aucun sous-niveau, durées de vie 80/120/250/500/1000/3000.
🎮 Phase 1 : `GoldenImmortal` **ajouté en fin** d'enum (les sauvegardes stockent l'entier) ; `CharacterData.RealmStage` (0-5 chakras, 0-8 niveaux, 0-3 stades) ; durées de vie ci-dessus.

### 5.1 Respiration Embryonnaire (6 chakras)

> « Dans l'Abîme Clair où règne la clarté, le soleil et la lune tissent la Marée… » — épigraphe du *Sutra de la respiration du Yin Suprême* (texte à réécrire en phase 6).

Étape fondamentale : éveiller **six chakras** par alchimie interne. On gagne une longévité de **120 ans** et la capacité de canaliser le **mana** : mouvements plus légers, muscles renforcés, sens aiguisés, et **sorts** lancés par des sceaux manuels complexes.

**Les trois dantian** exploités par les cultivateurs :

| Dantian | Siège | Réservoir |
|---|---|---|
| Inférieur | bassin pelvien | *jing* (essence vitale) |
| Médian | centre du cœur | *qi* (force vitale) |
| Supérieur | palais du front | *shen* (conscience) |

**Les six chakras** (renommés) — 🔴 = l'une des **trois épreuves** (points de blocage) :

| # | Chakra | Siège | Ce qui se passe | Effet obtenu |
|---|---|---|---|---|
| 1 🔴 | **Lac Intérieur** | point Qihai (dantian inférieur) | Former **81 filaments de qi** (vitesse très variable selon le talent inné), les rassembler au Qihai, les faire **remonter par les douze étages** (anneaux trachéaux) jusqu'au **Palais Niwan** (tempe), où le qi se condense en **liquide** ; il redescend former une **réserve** au Qihai, où le chakra se matérialise. Il faut **le maintenir jusqu'à stabilisation**. | La réserve devient du **mana** : sorts de base. Le cultivateur franchit officiellement le seuil du Dao. |
| 2 | **Marée Respirante** | Qihai | Se concentrer sur le rythme respiratoire ; le chakra émerge **naturellement**. | — |
| 3 🔴 | **Roue des Méridiens** | Cour Juque (réservoir du qi caché) | Condensation du chakra. | Le mana **circule dans tout le corps** : dans les yeux → vision à longue portée ; dans les pieds → agilité extraordinaire (marcher sur les murs comme au sol). |
| 4 | **Sève d'Émeraude** | — | Le mana se **transforme en essence** (verte). | Mana raffiné. |
| 5 🔴 | **Œil du Sommet** | Manoir Shenyang (dantian supérieur) | Cœur du royaume. | Éveil de la **conscience spirituelle** : percevoir le qi spirituel, le niveau de cultivation d'autrui, perception accrue de l'environnement. |
| 6 | **Premier Souffle** | — | Après condensation, **absorber un souffle de Qi spirituel du Ciel et de la Terre**. | Passage à la **Culture du Qi**. |

🎮 Phase 1 : `RealmStage` 0-5. Les chakras 2, 4, 6 progressent avec l'XP ; les chakras 1, 3, 5 exigent un **jet de percée**. Le chakra 5 débloque la capacité « détecter un orifice spirituel » (§4). Le chakra 6 exige d'avoir **un Qi spirituel et la méthode correspondante** (§5.2).

### 5.2 Culture du Qi (9 niveaux célestes)

- Pour y entrer : **absorber un Qi spirituel de son choix** et le cultiver avec la **technique correspondante**. Il faut donc soit rassembler ce Qi par une méthode de collecte, soit l'acquérir autrement.
- Pouvoirs : **chevaucher le vent, marcher sur l'air**, manier une **Essence Véritable** propre au Qi choisi. Espérance de vie ~**200 ans**.
- Chaque Qi a ses caractéristiques et donc ses méthodes (commune / ancestrale / secrète, §2.3).
- La culture du Qi est **moins difficile** que la formation des chakras, mais atteindre le 9e niveau demande une longue accumulation : c'est là que **s'arrêtent la plupart des cultivateurs solitaires ou de petites familles**.
- 📚 Les neuf niveaux se regroupent en **début (1-3), milieu (4-6) et fin (7-9)**.
- Le passage **9e niveau → Fondation** est **extrêmement difficile** : les cultivateurs du clan Mo le décrivent comme l'accumulation de **tous les obstacles de la Respiration Embryonnaire concentrés en un seul mur**.

🎮 Phase 1 : `RealmStage` 0-8, progression par XP ; la ressource « Qi spirituel [type] » est consommée à l'entrée (phase 3). Le jet 9 → Fondation est le plus sévère du jeu (§5.3).

### 5.3 Établissement des Fondations (4 stades)

> Dans les grandes sectes (Pic des Nuées, Mille Lames), la vraie reconnaissance est réservée à ceux qui atteignent le **sommet** de la Fondation ou du Manoir Pourpre.

Les cultivateurs de ce royaume sont **une force de la nature** : ils maîtrisent l'élément de leur technique et apparaissent comme des **divinités** aux yeux des mortels. Seul un cultivateur de ce niveau a la stature pour **consolider le prestige d'un clan ou d'une secte** dans la région.

#### 5.3.1 Percée
Activer les six chakras pleinement développés, **les désintégrer** pour qu'ils se combinent en **fondation immortelle**.
Exemple (*Qiu Mingshan*, avec une méthode d'Eau de grade 3) : les six chakras deviennent six flux de lumière tourbillonnant au-dessus de l'océan de qi ; l'essence véritable de l'océan s'élève et se diffuse dans les flux ; quand l'océan est vide et les flux saturés, il active sa technique : les six flux fusionnent en une eau claire qui remplit à nouveau l'océan de qi — la fondation immortelle est formée. La source ne précise pas comment le processus varie selon la technique.
- Processus ardu, qui exige une **grande vitalité** : il est conseillé de percer **avant 60 ans**.
- Les cultivateurs peu doués qui atteignent tard le 9e niveau **échouent généralement et meurent** : c'est la **dissolution spirituelle** (la pseudo-fondation ne se forme pas et ne crée pas de phénomène).

#### 5.3.2 Progression
- 4 stades : **initial, intermédiaire, tardif, apogée**. Chaque stade demande souvent **des décennies de retraite**. 🔎 Le tableau des rangs du wiki n'en compte que trois (sans apogée) ; la page du royaume et la source en donnent quatre : on garde **quatre**.
- Espérance de vie **300 ans** ; vitalité exponentielle (survivre quelques minutes décapité).
- **Alignement du Cœur Dao** : on adopte inconsciemment un tempérament proche de sa fondation, pour cultiver plus efficacement. Cela dépend beaucoup de la personnalité d'origine : un meneur au cœur dominateur (*Hou Lie*) cultive bien mieux le Yang Lumineux, fougueux, qu'un solitaire (*Nian Suo*). Un Cœur mal aligné **entrave** la cultivation (ampleur inconnue).
- Le corps devient **inhumain à l'image du Dao** : sang doré pour une fondation de Métal (*Ban Jinhe*), sang flottant en gouttelettes électrifiées pour une fondation du Magnétisme Primordial (*Nian Suo*).

#### 5.3.3 Partenaires Dao
- Chaque fondation immortelle a **4 Partenaires Dao** (« fondations compatibles »).
- **Consommer** un de ses Partenaires Dao fait passer **instantanément au stade suivant**, mais **bloque définitivement** toute progression ultérieure, même en cultivant.
- Revers : un Dao « mûr » devient une **proie** pour les cultivateurs des royaumes supérieurs (notamment du Manoir Pourpre), qui peuvent l'exploiter. Comprendre ses Partenaires Dao est vital **avant d'entrer dans le Dao** : choisir une lignée libre sans y penser est dangereux.
- 🔎 Interprétation : les 4 Partenaires Dao d'une fondation sont **les 4 autres fondations de la même Fruition** (§6.7 : chaque Fruition a 5 fondations/capacités orthodoxes). La source présente d'ailleurs le même tableau sous le titre « Partenaires Dao » et sous le titre « Réalisation des fruits ».

#### 5.3.4 Blocage au Manoir Pourpre
La plupart s'arrêtent au **stade avancé** : les techniques de grade ≤ 4 n'ont pas la **technique secrète** de montée (§2.3).

#### 5.3.5 Phénomènes
Par leur seule présence, ils influencent subtilement l'environnement (nuages sombres et bruine autour d'un cultivateur de l'Eau qui défend un allié). Les phénomènes sont **maximaux** à la formation de la fondation, à l'apogée, et **à la mort**. Après la mort, la région subit une **météo inhabituelle** liée à la fondation, car le corps se transforme en objets associés à celle-ci.

🎮 Phase 1 (stades, dissolution spirituelle), phase 4 (fondation choisie dans une Fruition, Partenaires Dao, alignement du Cœur via un trait de personnalité, corps inhumain en effet visuel), phase 6 (phénomène régional à la mort = modificateur de cultivation de la région + objets spirituels).

### 5.4 Manoir Pourpre (4 stades)

> « Tout être humain possède de la compassion… pour celui qui manie des pouvoirs divins, les yeux, sous ses sourcils, deviennent plus vulnérables que toute autre partie de son corps. »

Royaume de la culture et du raffinement des **pouvoirs divins** (dans l'Antiquité : **Raffinement Divin**). 4 stades (débutant, intermédiaire, avancé, apogée), **déterminés par le nombre de capacités divines** — 📚 **1-2 capacités** : début (*Maître taoïste*) ; **3** : milieu ; **4** : fin (*Grand Maître taoïste*) ; **5** : **Grande Perfection**. On les affine soit en cultivant, soit en **absorbant son Partenaire Dao** — ce qui rend le chemin vers le Noyau d'Or inévitablement périlleux.

**Pouvoirs** : traverser le vide, se **téléporter instantanément sur 5 000 m**, rester **invisible** aux cultivateurs de rang inférieur, modeler son corps (jusqu'à une forme quasi indestructible avec une capacité de Corps), modifier sa mer de Qi et son Manoir Shenyang pour pratiquer **n'importe lequel des 101 Arts Immortels** (raffinement d'artefacts, pilules, etc.) — un être naturellement doué les exécute plus instinctivement. **Manipulation du destin** : tuer les Élus, résister à la manipulation mentale d'une réincarnation de Vrai Monarque. Espérance de vie ~**500 ans** : influence sur d'innombrables générations.

#### 5.4.1 Percée (quatre épreuves successives)
1. **Montée** : avec sa méthode du Manoir Pourpre, envoyer sa fondation immortelle se raffiner dans le **Manoir Shenyang** : activer d'abord le point **Juque**, puis monter vers **Shenyang**. Mana insuffisant, fondation trop faible ou maîtrise taoïste superficielle → on **s'épuise avant Shenyang et on meurt sur le coup** (phénomène céleste grandiose mais éphémère, sans trace spirituelle).
2. **Manifestation** : au point Shenyang, **manifester son pouvoir divin**. Plus la fondation est de haut degré, plus on a cultivé de techniques secrètes et approfondi le Dao, plus c'est aisé. Durée ~**6 ans**. L'échec ici est **la principale cause de chute** des cultivateurs errants et des familles établies (fondations de degré inférieur, moins de techniques secrètes, compréhension moins profonde).
3. **Le Grand Vide** : propulser le Manoir Shenyang dans le vide ; la chair mortelle devient insignifiante (« séparation du corps mortel et dispersion des apparences terrestres » ; chez les bouddhistes : entrer en Terre Pure, état de Non-Rétrogression). Il faut **traverser l'obscurité**, s'oublier soi-même et son environnement ; on peut s'y attarder sans vouloir en sortir : **quelques jours ou des décennies**, parfois **prisonnier jusqu'à la fin de sa vie** (c'est la cause des durées de percée si variables). À ce stade la moitié des phénomènes du Manoir Pourpre sont présents : mourir maintenant peut **couvrir une région d'objets spirituels** ou **frapper plusieurs préfectures** d'une catastrophe naturelle.
4. **Les illusions** : après l'obscurité, d'innombrables illusions porteuses de pouvoir divin, d'un instant chacune. **Échouer = perdre toute sa cultivation**. Réussir = sortir de retraite au Manoir Pourpre.
- Autrefois s'y ajoutaient les **trois tribulations** : le Palais du Tonnerre envoyait foudre et nuages pour terrasser le mal, effaçant méfaits et karma. Depuis le départ de l'Immortel du Tonnerre et l'avènement de deux Vrais Monarques de la Vertu Terrestre, le Palais a disparu ; après le cataclysme (§12), le principe céleste est brisé : plus d'invocation du ciel, ce qui **favorise la compréhension taoïste** et explique la disparition des tribulations.

#### 5.4.2 Voies de contournement
- **Lien à un trésor** : lier son essence et son destin à un **trésor spirituel du Manoir Pourpre**, voire à une **Grotte Céleste**, pour atteindre divers niveaux de maîtrise — en sacrifiant la progression future ou en soumettant son destin à l'objet. Exemple : *Lan Guyu*, grâce à sa fonction innée (Bénédiction Divine), à la capacité divine *Attente du Rare Présage* d'un parent et à des ressources compatibles, obtient les pouvoirs d'un Maître à **double capacité divine sans en posséder aucune**.
- **Emprunt de lumière** : emprunter la lumière d'une Fruition par l'intermédiaire d'un Vrai Monarque ou équivalent, pour devenir un cultivateur du Manoir Pourpre comparable à un « Miséricordieux ».

#### 5.4.3 Progression : cinq capacités divines
- Après la 1re capacité, recommencer avec **une autre technique alignée sur un Partenaire Dao**, la cultiver jusqu'au Manoir Pourpre et condenser la 2e capacité, et ainsi de suite **jusqu'à cinq**.
- Les techniques du Manoir Pourpre étant rares depuis la **Migration du Sud** et difficiles, les Maîtres utilisent la **Méthode de la Greffe du Dao** : trouver un **prodige** pour cultiver une fondation ; une fois cette fondation au stade de l'Établissement des Fondations, elle est **consommée** (par divers moyens), complétée par des objets spirituels du Manoir Pourpre, et guidée en capacité divine. Gain de temps considérable : c'est **la norme** chez les cultivateurs du Sud.
- La culture des capacités repose sur trois leviers :
  - **Ressources** : objets spirituels rares, pilules spécialisées ; assez puissantes, elles amènent une capacité à la perfection vite, mais laissent des **fondements superficiels** et gênent la culture des sorts.
  - **Profondeur du Dao** : la voie choisie par la plupart ; permet d'utiliser les capacités plus efficacement et finement. Certaines capacités sont faciles, d'autres peuvent **enfermer un cultivateur pour le reste de sa vie** (ex. *Shi Kongming*, incapable de parfaire sa capacité *Dard Vermillon* du Yang Lumineux, se convertit au bouddhisme pour échapper à la mort).
  - **Imagerie** : incarner la véritable essence de sa capacité par ses actes, parfois liée au **Mandat de Vie** (ex. *Kuang Yao*, pris en embuscade et opprimé, cultive *Monarque Assiégé* à une vitesse incroyable parce que cette capacité parle d'un monarque en danger qui s'en sort par sa prudence).
- Après **3 capacités**, le Manoir Shenyang est en **équilibre parfait**. La **4e**, appelée **Seuil d'Immortalité**, arrête la plupart des cultivateurs avant le Noyau d'Or.

#### 5.4.4 Types de capacités divines
Nourries par la fondation, acheminées de la mer de qi au point Shenyang, traversées par les **Douze Chonglou** et leurs visions ; Juque, mer de qi et Shenyang rayonnent à l'unisson.

| Type | Rôle |
|---|---|
| **Corps** | Renforcement, transformation du corps |
| **Vie** | Interaction avec le **destin** : troubler le cœur, interroger l'esprit, infléchir le cours des événements, lire les présages. **La plus importante** : permet d'éviter les conflits en manipulant les événements ; sa possession marque le **vrai Maître du Manoir Pourpre**. Généralement acquise **en dernier** pour incarner au mieux le destin de la Fruition et augmenter les chances de monter au Noyau d'Or. |
| **Œil** | Perception (type rare) |
| **Magie** | Sorts, effets extérieurs |

La répartition des types dépend de la Fruition (§6.7) : dans les Cinq Vertus, elle découle des **cinq manifestations** (ex. Feu Orthodoxe : 2 Vie, 1 Corps, 2 Magie ; Feu Rassemblé : 1 Vie, 1 Corps, 3 Magie).

🔎 Dans la source, le type « Épeler » est une traduction fautive de *Spell* : il signifie **Magie**. « Corps vivant » / « Corps magique » désignent des capacités à **double type** (Corps+Vie, Corps+Magie).

#### 5.4.5 Phénomène à l'échec
Quand un cultivateur du Manoir Pourpre échoue à monter au Noyau d'Or, sa mort provoque un **phénomène céleste durable** qui modifie l'atmosphère de la région et **l'efficacité de cultivation** (+ ou −). Exemple : l'échec d'un membre de la lignée dragonne en Mer Orientale a créé la *Tempête des Eaux Renversées* (nuages noirs, pluies torrentielles, éclairs noirs et violets) ; un cultivateur de la côte y a perdu **2,5 %** de vitesse de cultivation.

🎮 Phase 4 : `DivineAbility` (nom, type, Fruition, niveau de perfection) ; `CharacterData.DivineAbilities` (0-5) ; stade = fonction des capacités ; percée en 4 jets (montée, manifestation 6 ans, vide à durée aléatoire jours→décennies avec risque d'emprisonnement à vie, illusions) ; retraite = membre indisponible plusieurs années ; Greffe du Dao = consommer la fondation d'un **autre membre du clan** (dilemme moral) ; phénomènes régionaux (phase 6).

### 5.5 Noyau d'Or (Vrai Monarque, 4 stades)

> « Un véritable monarque est un être du Ciel et de la Terre, au-delà des distinctions entre l'homme et la bête. Dès l'instant où l'on devient un véritable monarque, on cesse d'être humain ! »

Royaume de la **quête de l'essence**. Les Vrais Monarques comptent parmi les êtres les plus puissants ; **chaque grande secte** (ex. Secte du Pic des Nuées) est soutenue par **au moins un** Vrai Monarque. Incarnés par leur Fruition, ils ont abandonné leur enveloppe mortelle pour devenir partie du ciel.

#### 5.5.1 Percée : demander une position au Ciel
Maîtriser **5 capacités divines** et atteindre la perfection suprême dans les **Cinq Méthodes**, puis utiliser une **technique de recherche d'or** adaptée à son Dao et à sa position pour combiner les cinq capacités en **essence métallique** : c'est le noyau d'or. On demande alors une **position** au Ciel :

| Position | Condition | Pouvoir |
|---|---|---|
| **Réalisation de la Fruition** | Les **5 capacités orthodoxes** de la Fruition, parfaites (compréhension complète du Dao). **Une seule par Fruition.** | Maîtriser et incarner la Fruition, **substituer sa volonté à celle du Ciel** ; toutes ses images lui sont révélées. |
| **Surplus** (« Ministre / Serviteur de la Fruition ») | 5 capacités dont des capacités mineures connues (créées par d'anciens Vrais Monarques), **ou** 4 capacités de la Fruition + 1 capacité de substitution d'une autre Fruition cohérente avec l'imagerie de sa méthode. Ex. *Gongye Shu* cultive *Course du Fou vers la Cime* (Terre) au lieu de *Mandat de l'Empereur* (Grand Chaman) → Surplus du Grand Chaman. | Force puissante du Noyau d'Or, influence moindre ; **ne peut pas** accéder à l'Embryon du Dao. Comparé au nombre de jours en surplus dans un mois. |
| **Intercalaire** (« année bissextile ») | Un **changement complet** de Fruition. Type **4A+1B=B** (4 capacités de A + 1 de B ; plus accessible, succès non garanti) ou **3A+2B=B** (méthode de recherche d'or spécialisée, très difficile mais plus sûre). Variantes : **4A+1B=C** (ex. *Weng Liuxian* propose à *Bai Ruozhi* d'utiliser *Bruine Hivernale* (Eau Nourricière) comme pont entre l'Eau Muable et l'Eau Cachée — possible seulement à cause de la **corruption de la vertu de l'Eau**). | Permet ensuite la **Transformation** vers la position souveraine. |

- Si la Réalisation est **occupée**, il faut demander **la permission à son maître** pour un Surplus ou un Intercalaire.
- Il n'existe **pas** de méthode unique : chaque méthode de recherche d'or est si difficile que seuls les Vrais Monarques ayant atteint la Fruition correspondante la maîtrisent ; beaucoup se consacrent donc à la Fruition liée à la méthode dont ils disposent.
- **Échec** : l'essence métallique résiduelle prend vie en **Démon d'Essence Métallique** (mélange d'essence, de destin et de mauvais présages), bien plus puissant qu'un Manoir Pourpre à son apogée, vulnérable seulement aux forces **pseudo-Noyau d'Or** (Émissaires des Enfers, Noyaux Divins, Sceaux de Jade…).

**Vocabulaire des mouvements de position** :
- Termes du pouvoir divin : monter par la Nature Orthodoxe = **« Fruition »** ; gain par surplus partiel = **« Surplus »** ; arrangement échelonné = **« Intercalaire »**.
- Termes des vénérables : changer la position orthodoxe = **« Transmutation »** ; passer du surplus à la position première = **« Transfert »** ; plan profond pour s'emparer de la position souveraine = **« Transformation »**.

#### 5.5.2 Pouvoirs
- Le **corps n'est qu'un réceptacle** : on survit à tout tant que l'essence métallique est intacte ; **réincarnation à volonté**, puis remontée rapide (Fondation en **2 ans**, Manoir Pourpre en **10 ans**) ; contrôle partiel du destin pour assurer la réincarnation.
- **Nature Dorée** : point indestructible dont la maîtrise catalyse les capacités ; « or » au sens d'**immortalité**, pas de Métal. Espérance de vie **1000 ans** ; même après la mort physique, on peut demeurer longtemps parmi les humains si l'on échappe à ceux qui gouvernent le Yin. Source des merveilles divines, peut prendre la forme d'objets divins. Antiquité : « **Quête de sa Nature** ».
- **Grotte Céleste** : espace extradimensionnel adapté à l'Éveil du cultivateur, aux phénomènes dangereux même pour un Manoir Pourpre.
- **Essence métallique** : puissance défiant les lois physiques ; tout cultivateur de la **même fondation** est totalement impuissant face à elle.
- **Lignée transformée** : tout descendant d'un détenteur de Réalisation peut atteindre **au moins le Manoir Pourpre** et acquiert des traits de l'essence.
- **Reprise de la Fruition** (« Lutte des Cinq Visages ») : la Fruition se souvient de son ancien maître et tente de **remplacer l'âme** ou de **réincarner** le cultivateur. Ex. le Yang Lumineux porte l'image de la bête démoniaque qui le détenait avant l'*Empereur Zhen Guangling*.
- 4 stades (débutant → apogée), de difficulté très variable : certains naturels, d'autres demandant des millénaires. La progression repose sur **l'affirmation de son Image de Fruition** et le lien avec ses fruits ; on peut **intégrer de nouvelles images** (ex. l'*Empereur Zhen Guangling* intègre les Autorités Impériales au Yang Lumineux).

🎮 Phase 4 : `FruitionPosition` (Realization / Surplus / Intercalary) ; **état du monde** : chaque Fruition a un détenteur ou non (§6.8) ; percée = choix de position + permission éventuelle du détenteur ; échec = événement **Démon d'Essence Métallique** (boss régional) ; réincarnation d'un ancêtre Vrai Monarque = événement majeur ; bonus de lignée pour les descendants.

### 5.6 Embryon du Dao (Immortel)

> « Le premier jour, ils discutèrent du Qi. Le bouddhiste dit : "La source du Qi est le chaos unifié." L'immortel dit : "Le commencement est orthodoxe. Je tiens le vase sacrificiel et j'enseigne les douze divisions du Qi." »

- Ancien nom : **Âme Naissante**. Ses cultivateurs sont appelés **Immortels (仙人)**, 📚 titre *Immortel Exalté* : l'apogée de la cultivation.
- 📚 Après le cataclysme, les Embryons du Dao sont partis vers le **Ciel Extérieur** (§5.8) chercher les royaumes supérieurs et ne sont jamais revenus.
- **Seuls deux** subsistent dans ce monde : l'**Immortelle Xiaoyun** et le **Monde Souterrain**. Ils ne périssent qu'à la fin des temps (soleil et lune disparus, monde éteint).
- Accessible **uniquement** depuis une **pleine Réalisation** (il faut modifier ou affirmer le Dao du monde entier). Un Surplus peut y parvenir par **Transfert** si la Réalisation est sans maître ; un Intercalaire par **Transformation** (exemple historique : le *Basculement Geng-Dui*).
- Avancement et voie à suivre : **inconnus**.

### 5.7 Immortel Doré (Seigneur Immortel)
- Aussi appelé **Grande Culmination**. Cultivateurs de premier ordre même dans l'Antiquité, au niveau d'immortels comme *Yinjun* et *Taichen*, qui ont reçu l'enseignement direct des maîtres des Trois Profondeurs.
- Invulnérables aux **Trois Calamités** du Dao Céleste. Autrefois « Seigneurs Immortels », aujourd'hui « Immortels Dorés ».

🎮 Royaumes 6-7 : états **d'aboutissement** (victoire narrative, §11.9) et figures du monde ; ni la source ni le wiki ne décrivent leur progression interne → rien d'inventé tant que l'utilisateur ne le décide pas.

### 5.8 Au-delà des royaumes (📚)
- **Grand Vide** : reflet d'ombre du Monde Manifeste, formé par l'harmonisation de toute l'énergie spirituelle ; accessible **seulement à partir du Manoir Pourpre** (ou équivalent). Noirceur presque totale, sans soleil ni lune ; il « aspire » les sens spirituels. Chaque point correspond à un lieu réel. Voyager y est **plus difficile là où le Qi est dense**, plus facile là où il est rare ; sans aucun Qi, c'est une **zone morte** à contourner. Grottes Célestes et royaumes secrets y sont **ancrés** (isolés du Vide, ils deviennent introuvables mais vivent sur leurs réserves). Peuplé d'innombrables démons ; y agir en grand est très voyant (« allumer des pétards à côté de Noyaux d'Or endormis »).
- **Qi Pur et Qi Exilé** : le Qi Pur, tiré du Grand Vide, nourrit Grottes Célestes et royaumes secrets à leur apogée ; quand un royaume tombe, son énergie se dégrade en Qi Exilé et il peut **sombrer dans le Monde Souterrain**.
- **Atmosphères** : l'abondance d'un Qi crée une **atmosphère régionale** qui favorise certaines voies et lignées (cultivation plus efficace, percées plus faciles). Seuls les Manoirs Pourpres et plus peuvent les influencer. Les grandes sectes **se les disputent** : l'*Atmosphère de l'Équilibre Profond*, créée pour aider une percée au Noyau d'Or du Jade Premier, exige un climat doux **et la paix et la prospérité de millions de gens** ; une secte rivale a déclenché en Mer Orientale l'atmosphère *Eaux Tombantes, Tempête Montante* (pseudo-Eau Yin qui ruine les récoltes, noie les villages et engendre du ressentiment) pour la briser. Le sud de Linxi est aujourd'hui un *Grand Entrepôt des Esprits Funestes*, favorable à la Vertu de la Terre, au Dao du Diable, à l'Eau Nourricière et au sang-qi. Prévoir l'atmosphère rend une percée plus sûre.
- **Ciel Extérieur** : au-delà de la Mer Orientale, l'**Abîme du Vide sans Fin** où l'eau de mer tombe si lentement qu'elle s'évapore et remonte. Au-delà : ni air, ni Qi, ni Grand Vide. Avant la guerre des Immortels et des Diables, une barrière céleste liait les quatre mers ; depuis le cataclysme, on peut partir — les Embryons du Dao l'ont fait.
- **Élus du Destin** : quand un Vrai Monarque se réincarne, la région produit de nombreux **prodiges portant un fil de destin** ; des sectes les repèrent en secret et les « récoltent » (complot du **Temple des Pins Verts**). Les Manoirs Pourpres peuvent tuer ces Élus.

🎮 Phase 6 : `Atmosphere` par région (modificateurs par lignée, maintien par les factions), Élus du Destin générés à la réincarnation d'un Vrai Monarque, Grand Vide comme couche de déplacement des Manoirs Pourpres et du miroir.

---

## 6. Lignées du Dao (Fruitions)

### 6.1 Principe
Une **Lignée Dao** (Fruition, 果位) est la réalisation sur laquelle un cultivateur fonde son enseignement. Chaque Fruition **régit des aspects du monde** et recèle de profonds mystères.

Chaque Fruition porte (§6.7) :
- une **essence métallique** (ce que devient le noyau d'or de son détenteur),
- **5 fondations / capacités divines orthodoxes** (chacune d'un type : Corps, Vie, Œil, Magie),
- éventuellement des **capacités de substitution** (utilisées pour Surplus / Intercalaire),
- un **statut** : occupée (un Vrai Monarque en détient la Réalisation) ou libre.

### 6.2 Origine : trois générations de Fruitions
1. **Fruitions fondatrices** — apportées par les **Premiers Seigneurs Immortels des Trois Profondeurs**, venus des Cieux Extérieurs Suprêmes, avec leurs pairs venus « d'Au-delà des Profondeurs » (factions et voies extérieures à la Voie Immortelle) :
   - **Yin Suprême** et **Yang Suprême** (Deux Dualités) ;
   - **Qi Pur** (Douze Qi) ;
   - le groupe de la **Fusion Ancienne**, surtout détenu par ceux d'Au-delà des Profondeurs. On ne sait pas quelles lignées de ce groupe sont vraiment fondatrices et lesquelles sont nées d'interactions ultérieures.
2. **Fruitions d'interaction** — nées des interactions des fondatrices, elles ont **stabilisé** le monde et **perfectionné** ses lois :
   - les Deux Suprêmes engendrent **Yin Voilé, Yin Mineur, Yang Lumineux, Yang Mineur** ;
   - le Qi Pur engendre les **onze autres Qi** ;
   - l'interaction des trois Yin et des trois Yang produit les **Cinq Vertus**.
   - 🔎 Les Yin/Yang subordonnés ayant servi, avec les Suprêmes, à manifester les Cinq Vertus, ils **chevauchent** la frontière fondatrice/interaction et sont **exaltés au-dessus** des lignées ordinaires.
3. **Fruitions d'Attestation du Vide** — naissent quand une grande figure (**au moins Embryon du Dao** ou équivalent) atteste une Fruition par un exploit spirituel unique :
   - **Voie de l'Épée** : quand le Premier Immortel de l'Épée surgit du néant, exploit jamais vu ;
   - **Manifestation Céladon** : empruntée à une lignée de la Vertu Terrestre ; classée **à la fois** Fusion Ancienne et Vertu Terrestre (la « Sixième Terre ») ;
   - **Lueur de l'Aube** (aussi Lumière Wu, Lumière du Ciel, Lueur Arc-en-ciel) : quand l'Immortelle Xiaoyun l'atteste.
4. 📚 **Fusion Ancienne** (classement du wiki) : lignées associées aux entités d'**Au-delà des Profondeurs**, dont les **Trois Chamans** et les **Deux Rites**.

📚 **Les Trois Profondeurs d'origine.** Le wiki rattache chaque Fruition à l'une des trois traditions fondées par les Seigneurs Immortels des Trois Profondeurs — un axe qui traverse les groupes :
| Profondeur (jeu) | Fruitions rattachées (connues) |
|---|---|
| **Profondeur Céladon** | les cinq Eaux (Vertu de l'Eau), Jade Premier, Culture de Linxi, Voie de l'Épée |
| **Profondeur Englobante** | Yang Lumineux, Sentinelle de la Cité, Régent Céleste, Qi Exilé, Feu Nourricier, Métal Caché |
| **Profondeur Pénétrante** | Qi Sec ; tradition du Trésor Numineux |
🔎 Le miroir porte, dans le roman, le titre de **« Maître de la Profondeur Céladon »** : indice majeur pour le mystère de son origine (§11.5).

### 6.3 Les Cinq Manifestations (Cinq Vertus)
Chacun des Cinq Éléments a **cinq manifestations** : **orthodoxe** (正), **rassemblée** (收), **nourricière** (蕴), **muable** (变), **cachée** (藏). Chaque croisement élément × manifestation est une Fruition : **25 Fruitions**.

Renommage : on nomme ces Fruitions **« Élément + manifestation »** (descriptif, neutre), au lieu des noms du roman (§13.6).

📚 **Axiome des positions relationnelles** (fruit de siècles d'échecs) : **« Une position orthodoxe ne connaît pas d'Intercalaire ; une position rassemblée ne connaît pas de Surplus. »** Ces relations gouvernent la résonance, la sécurité et la viabilité de la quête d'une position d'or (§5.5.1) ; beaucoup de catastrophes historiques viennent de leur violation. 🔎 Le wiki cite pourtant un Intercalaire sur l'Eau Orthodoxe et des Surplus sur l'Eau Rassemblée : l'axiome est une **loi de danger**, pas une impossibilité — la tenter expose à un échec catastrophique (Démon d'Essence Métallique).
📚 Le wiki nomme aussi les **« Cinq Positions Orthodoxes »** : la manifestation orthodoxe de chaque élément (ex. le Bois Orthodoxe en est une).

| | Orthodoxe | Rassemblée | Nourricière | Muable | Cachée |
|---|---|---|---|---|---|
| **Bois** | Bois Orthodoxe | Bois Rassemblé | Bois Nourricier | Bois Muable | Bois Caché |
| **Feu** | Feu Orthodoxe | Feu Rassemblé | Feu Nourricier | Feu Muable | Feu Caché |
| **Terre** | Terre Orthodoxe | Terre Rassemblée | Terre Nourricière | Terre Muable | Terre Cachée |
| **Métal** | Métal Orthodoxe | Métal Rassemblé | Métal Nourricier | Métal Muable | Métal Caché |
| **Eau** | Eau Orthodoxe | Eau Rassemblée | Eau Nourricière | Eau Muable | Eau Cachée |

### 6.4 Groupes de Fruitions (vue complète, complétée par le wiki)

| Groupe | Fruitions | Génération |
|---|---|---|
| **Deux Dualités** | Yang : Yang Suprême, Yang Lumineux, Yang Mineur · Yin : Yin Suprême, Yin Voilé, Yin Mineur | Fondatrice (Suprêmes) / interaction |
| **Cinq Vertus** | 25 Fruitions (§6.3) ; 📚 le Métal en compte au moins **six** nommées (§6.7) | Interaction |
| **Douze Qi** 📚 (ordre du **Cycle d'Inversion du Qi**) | 1 Qi Pur · 2 Qi Véritable · 3 Qi Brillant · 4 Qi Violet · 5 Qi Sec · 6 Rite Supérieur · 7 Rite Inférieur · 8 Qi Froid · 9 Qi Faste · 10 Qi Funeste · 11 Qi Profond · 12 Qi Exilé | Fondatrice (Qi Pur) / interaction |
| **Trois Tonnerres** | Tonnerre Céleste ; 🔎 probablement **Tonnerre Profond** et **Magnétisme Primordial** (📚 lignées de tonnerre citées par le wiki, prédisposées au Dao Divin) | 🔎 non précisée |
| **Fusion Ancienne** 📚 (10 lignées) | Régent Céleste, Culture de Linxi, Sentinelle de la Cité, Élixir Parfait, **Corps de Kui**, Grand Chaman, Chouette des Seuils, Jade Premier, **Rite Équilibré**, **Proclamation Céladon** | Fusion Ancienne (Au-delà des Profondeurs) |
| **Trois Chamans** | Chouette des Seuils, Jade Premier, Grand Chaman (inclus dans la Fusion Ancienne) | Fusion Ancienne |
| **Deux Rites** | Rite Supérieur, Rite Inférieur 📚 (comptés parmi les Douze Qi, d'origine Fusion Ancienne) | Fusion Ancienne |
| **Attestation du Vide** | Voie de l'Épée (📚 alias **Étoile du Soir**), Manifestation Céladon, Lueur de l'Aube | Attestation |

🔎 Corrections apportées par le wiki à `Miror.txt` :
- « Culture de la Transcendance » et « Culture de Linxi » sont **la même Fruition** (修越) : le nom de la Fruition, pas une capacité.
- **Étoile du Soir** (Changgeng) est un autre nom de la **Voie de l'Épée**, attestée par l'**Ancêtre de l'Épée** ; ce n'est pas une Fruition distincte.
- Les **Deux Rites** (Supérieur, Inférieur) sont **dans** les Douze Qi.
- « Qi Violet » et « Qi Pourpre » désignent la même Fruition (紫气) ; « Essence Radieuse » est un alias du **Qi Sec**. « Essence illimitée » (source) : 🔎 non identifiée (peut-être le Qi Profond).
- **Proclamation Céladon** (wiki) et **Manifestation Céladon** (source) : 🔎 probablement la même Fruition (traductions différentes), Fusion Ancienne et « Sixième Terre ».

🔎 Rattachement des Cinq Vertus à leurs manifestations : Eau de fosse → Eau Orthodoxe ; Eau convergente → Eau Rassemblée ; Eau du Manoir → Eau Nourricière ; Eau pure → Eau Muable (📚 « réside en position de changement ») ; Eau de la vallée → Eau Cachée (📚 « ce qui est caché, pas encore libéré ») ; Feu Li → Feu Orthodoxe ; Feu fusionnant → Feu Rassemblé ; Feu véritable → Feu Nourricier ; Feu sec → Feu Muable ; Feu viril → Feu Caché ; Bois vertical (📚 *Bois Droit*, « une des Cinq Positions Orthodoxes ») → Bois Orthodoxe ; Bois rassemblé → Bois Rassemblé ; Bois de corne → Bois Caché ; Terre de Wu → Terre Nourricière ; Terre stable (Gen) → Terre Orthodoxe ; Terre au trésor → Terre Cachée ; Métal Geng → Métal Muable ; Or unifié → Métal Rassemblé ; Métal de la voûte → Métal Caché. Non placés au départ : Métal Dui, Métal Errant, Métal Harmonieux.

📚 **Liste complète du wiki** (page *Cultivation System*), qui tranche ces rattachements :
| Manifestation | Bois | Feu | Terre | Métal | Eau |
|---|---|---|---|---|---|
| **Orthodoxe** | Bois Droit | Feu Radieux (Feu Li) | Terre Stable (Gen) | **Métal Dui** | Eau de Fosse |
| **Rassemblée** | Bois Rassemblé | Feu Fusionnant | Terre du Retour | Métal Unifié | Eau Convergente |
| **Nourricière** | Bois Préservé | Feu Véritable | Terre Majestueuse (Wu) | **Métal Libéré** | Eau du Manoir |
| **Muable** | Bois Transformé | Feu Sec | Terre Manifestée | Métal Geng | Eau Pure |
| **Cachée** | Bois de Corne | Feu Viril | Terre au Trésor | Métal de la Voûte | Eau de la Vallée |
→ **Métal Dui = Métal Orthodoxe**. 🔎 « Métal Libéré » (nourricier) est probablement le **Métal Errant** (même idée de liberté). Le **Métal Harmonieux** et l'« Or de Joie » restent hors tableau.
📚 **Lignées au-delà des 25** : **Terre de la Proclamation** (= Proclamation / Manifestation Céladon, la « Sixième Terre »), **Feu en Fusion** (distinct du Feu Fusionnant), **Feu Ardent** (📚 « réside au nord »), **Métal Harmonieux**. 📚 La Vertu du Métal est dite **incomplète** : le Métal Unifié et le Métal Harmonieux sont **occupés par des héritiers partis au-delà des Cieux**, statut inconnu.

### 6.5 Fondations immortelles isolées
Fondations nommées sans Fruition certaine : *Brume de l'Aube Universelle* ; *Ouragan du Yin Spectral* ; *Brume du Yin Retourné* ; *Épine de la Porte des Tombes* ; *Stèle Gravée* et *Heaume de l'Aurore* (📚 **Partenaires Dao l'une de l'autre**, Métal Muable) ; *Sommeil de la Pierre Étreinte* (Qi Véritable).

📚 Exemples détaillés par le wiki (effets à la Fondation) :
| Fondation | Effets |
|---|---|
| *Stèle Gravée* | Qi capable de briser les formations, fendre les montagnes, détruire les artefacts ; précision, se renforce à chaque ennemi vaincu (trempée dans leur sang-qi) ; détecter l'or dans les veines de la terre, se soigner en consommant or et jade, repousser le qi funeste, corps dur comme métal et pierre, forte résistance aux sorts et malédictions. Partenaire Dao : *Heaume de l'Aurore*. |
| *Forge Souterraine* (Métal Caché) | Forge et perception des armes ; **internaliser** des artefacts dans son corps ; perturber les artefacts adverses. Culture extrêmement douloureuse (vingt ans de chair arrachée pour le prodige qui l'a réussie). |

### 6.6 Deux Dualités — détail

| Fruition | Essence métallique | Capacités / fondations (type) | Substitutions | Statut |
|---|---|---|---|---|
| **Yang Suprême** | *Essence du Zénith Sextuple* | *Gardien du Canon Solaire*, *Lance du Zénith* | — | 📚 **détenteur inconnu** · symbole : le soleil ; « rouge feu comme l'or, fulgurant comme la lumière » |
| **Yang Lumineux** | *Essence du Sceptre Lumineux* | *Arche du Salut au Ciel* (Magie), *Dard Vermillon* (Magie), *Regard du Trône* (Magie), *Monarque Assiégé* (Corps), *Clarté sur le Monde* (Vie) | *Passe de l'Aube Première*, *Cœur sans Ombre*, 📚 *Estrade Toujours Claire*, 📚 *Office du Siècle*, 📚 *Corps du Serment* | **Occupée** : Empereur Zhen Guangling · 📚 ancien détenteur : la bête démoniaque **Hongyao** · 📚 Surplus : **Roi Lumineux Xiaoming**, **Cigale Blanche**, **Vrai Monarque Chenguang**, **Vrai Monarque Gaoyao** · 📚 étoiles : les Étoiles Souveraines du Soleil Jaillissant |
| **Yin Suprême** | — | fondation 📚 *Lune Noyée* ; *Accomplissement Originel*, *Liturgie Obscure* (Corps), *Soie de Lune* (Vie) | — | 🔎 non donné · 📚 symboles : lapins blancs, crapauds, lune, orchidées blanches ; « extrêmement Yin, pur et lumineux, sans force mais inflexible, comme une pierre de glace » |
| Yin (2e lignée, non nommée) | — | 3 emplacements : Magie, Vie, Œil | — | — |
| Yin (3e lignée, non nommée) | — | 1 emplacement : Magie | — | — |
| Yang Mineur | — | non donné (« Éveil du Yang Mineur » rend **indestructible** : cf. le Monarque Démon, §12) | — | — |
| **Yin Mineur** 📚 | — | *Feu Monarque du Froid et de la Sécheresse*, *Parfum Englouti* (Magie ; aussi le nom d'une fondation) | — | 📚 base des **Immortels de Jade Tressé** (Voie de la Main Gauche) ; un manuel de grade 4 a été compilé par une servante immortelle du Yin Mineur |
| **Yin Voilé** 📚 | — | non donné | — | 📚 s'oppose au Yang Lumineux (suppression mutuelle) ; l'État de Pei (« Royaume des Filles ») cultive le Yin Voilé |

📚 **Le Yang Lumineux et la filiation.** Il chérit le lien père-fils (l'aîné est le *Qilin Blanc*, le cadet la *Cigale Blanche*), mais il est aussi le siège de « le fils tue le père », « le père tue le fils », « le père vole l'amour du fils » et « le fils s'empare du pouvoir du père » : c'est seulement quand ces cinq aspects sont réunis qu'il est le vrai Yang Lumineux. **Il abhorre l'inversion** : quand le fils est fort et le père faible, le fils prend le trône. 💡 Excellent moteur de drames dynastiques pour un clan de la lignée.

### 6.7 Cinq Vertus, Douze Qi, Tonnerres, Fusion Ancienne, Chamans — détail

**Eau** (📚 Profondeur Céladon ; la Vertu de l'Eau est dite **corrompue**)

| Fruition | Essence métallique | Capacités orthodoxes (type) | Substitution | Statut et histoire |
|---|---|---|---|---|
| **Eau Orthodoxe** | *Essence des Mille Gouttes Patientes* | *Mer sans Rivage* (📚 Magie), *Veilleur du Gué* (Vie), *Ciel d'Orage*, *Garde de la Digue*, *Adieu au Fleuve*, *Refuge du Péril* (📚 Vie) ; capacité de Magie connue : *Roi-Serpent des Eaux Noires* | — | **Libre** · 📚 nom ancien : « Eau de Fosse du Chemin Englouti, qui pratique le péril » ; nom actuel : « Eau de Fosse aux Mille Réticences » · anciens détenteurs : **Yu** 🔎, **Vrai Monarque Cehe** (mort en tentant l'Embryon du Dao) · Surplus : **Vrai Monarque de l'Eau du Royaume de Luoling** · Intercalaire : **Vrai Monarque Shencang** · étoile : Mercure |
| **Eau Rassemblée** | — | *Confluence Ultime* (📚 Magie : une eau cristalline sans fin qui voile le ciel, l'océan entier comme arme), *Présage de la Crue* (📚 le corps est remplacé par l'Eau Muable), *Retour à la Mer* (📚 fissures dans le ciel et le corps, maintient la stabilité), *Sage des Flots Égaux* (📚 Vie : dissout et tue tout dans son domaine), *Fleuve Corrompu* (📚 armée de créatures aquatiques démoniaques) | — | **Occupée** : **Ao Ming** · 📚 anciens : le Vrai Serpent-Dragon céleste, **Ao Ri** · Surplus : **Ao Yue**, **Ao Xi**, **Ao Zai**, **Ao Lin** · 📚 « aime rassembler » : puissance sans rivale |
| **Eau Nourricière** | — | *Bruine Hivernale*, 📚 *Abysse de l'Aurore*, 📚 *Retraite Hivernale* | — | **Libre** (vacante depuis ~600 ans : naissance du Lac Jingshui) |
| **Eau Muable** | *Essence du Sceau Limpide* (📚 « de l'Origine de Midi ») | *Chant de la Source Enfouie* (Corps+Vie : 📚 dispersion, un autre corps de Dharma peut apparaître), *Voile de Brouillard* (Vie : 📚 ensorcelle les cœurs, brumes grises), *Averse du Soir* (Magie : 📚 déluge qui aveugle même les sens), *Silhouette sous l'Onde* (Vie), *Rosée Lustrale* (Magie) | *Manifestation de la Crête Céladon* / *Lune Noyée* ; 📚 *Renversement du Monde* (Vie) | **Occupée** : **Tan Qing** (📚 profondeur du Dao immense mais **non aimé** de sa Fruition) · anciens : le **Serpent à Plumes**, le Vrai Serpent-Dragon céleste · Intercalaires : **Ao Feng**, les 8e et 9e fils du Serpent-Dragon · 📚 Désignation de Rang : *Épée de l'Éveil du Cœur* · « son règne est fugace » |
| **Eau Cachée** (📚 « Eau Yin ») | 📚 *Essence du Grand Dépôt de la Vallée Profonde* | *Aurore Sincère*, *Source Renaissante*, *Rencontre des Vallons*, *Porte du Trésor Profond*, 📚 *Veille sur l'Embryon* | — | **Occupée** : 📚 **Dame Yinlan** · ancien : **Vrai Monarque Baiyi** |

**Feu**

| Fruition | Capacités (type) | Statut et histoire |
|---|---|---|
| **Feu Orthodoxe** (Feu Li) | répartition : 2 Vie, 1 Corps, 2 Magie | 🔎 non donné |
| **Feu Rassemblé** (Feu Fusionnant) | *Soupir du Cœur Ardent* ; répartition : 1 Vie, 1 Corps, 3 Magie | 🔎 non donné · 📚 profil : insidieux, épuisant, nocif ; brûle la vitalité, douleur atroce surtout pour les démons ; flammes gris-noir |
| **Feu en Fusion** 📚 (lignée distincte) | *Flamme Démone* (fondation et capacité) | 🔎 non donné · 📚 profil : funeste, éclatant ; incline l'équilibre vers le Yang ; **fait fondre artefacts et formations** (contre des fondations du Métal) ; cœur brillant, flammes pâles |
| **Feu Nourricier** (Feu Véritable, 📚 Profondeur Englobante) | essence 📚 *Essence du Feu Véritable Nourricier d'avant le Ciel* ; *Faisan de Braise* (📚 manipulation extrême, destructrice et transformatrice), *Incendie Céleste* (📚 annule les attaques), *Dieu du Foyer* (Vie) | 📚 **Brisée** : aucun détenteur ; ancien : le **Maître de la Profondeur Englobante** · Surplus : **Danmei**, le **Vrai Monarque du Cycle Inné de la Vertu du Feu** |
| Feu (lignées non nommées) | emplacements : Vie ; Magie, Magie, Vie, Corps, Vie ; Magie ; Magie | — |
| Feu Muable (Feu Sec), Feu Caché (Feu Viril) | non donnés | — |

**Métal** 📚 (le wiki nomme : Métal Geng, Métal Dui, **Métal Errant**, Métal Rassemblé, **Métal Harmonieux**, Métal Caché)

| Fruition | Capacités / détails | Statut |
|---|---|---|
| **Métal Muable** (Geng) | *Stèle Gravée*, *Heaume de l'Aurore* (§6.5) | 🔎 non donné |
| **Métal Caché** (📚 Profondeur Englobante) | essence 📚 *Essence du Trésor Dao Sans Forme*, *Colonne d'Argent*, *Forge Souterraine*, 📚 *Fils d'Argent du Trésor* (perçoit les trésors cachés). 📚 Exige un Qi spécifique, le **Qi Métallique du Verrou Profond** ; manuel de rang 6 connu ; maîtrise des formations (le symbole du « verrou » fonde la plupart des demeures-grottes et royaumes secrets), perception des trésors, dissimulation. 📚 Un complot millénaire de deux Vrais Monarques, le **Verrou du Trésor Unifié**, limite cette voie ; le **Trésor Profond**, trésorerie de la Profondeur Englobante, existe entre le Métal Rassemblé et le Métal Caché. | 📚 **Brisée** (ancien : le Vrai Monarque du Métal Caché) |
| **Métal Errant** 📚 | — | contrôlé par la **famille Lian** |
| **Métal Harmonieux** 📚 | métal de préservation et de collecte ; extrêmement rare | — |
| **Or Vert d'Origine** (essence, source) | *Cœur d'Or Éclos* | **Occupée** |
| Métal Dui, Métal Rassemblé | non détaillés | — |
| lignées non nommées (source) | Vie, Magie, Vie ; Corps, Corps, Vie, Magie | — |

**Bois** — 📚 seuls le **Bois Orthodoxe** et le **Bois Caché** (Bois de Corne) sont encore largement cultivés ; la Vertu du Bois est **en déclin**.

| Fruition | Capacités (type) | Statut et histoire |
|---|---|---|
| **Bois Orthodoxe** (📚 « Bois Droit ») | *Marche contre le Vent du Nord* (Corps ; 📚 mobilité — les objets du Feu Ardent, qui réside au nord, aident à la cultiver ; faille : une fois immobilisé, inutile), *Ancrage du Regard* (Vie), *Parole Voilée* (Magie), *Loi de la Sève* (Vie), 📚 *Refuge de l'Unicité* (Vie : **clé de la position de Surplus**), 📚 *Dos au Midi*, 📚 *Le Bois Fait l'Équerre* | 📚 Lignée héréditaire de la **famille Huyan** ; le **Maître taoïste Yuanshu** a échoué une tentative d'Intercalaire sur le Bois Rassemblé faute de *Refuge de l'Unicité* · étoile : Jupiter · caractère : **inflexible comme le métal ou la pierre**, « incorruptible » même après la mort · compatible avec le Yang Lumineux et le Qi Sec pour les transformations supérieures · manuel : *Classique des Monts qui Regardent le Sud* |
| **Bois Rassemblé** | *Longévité par le Péril* (📚 « impossible à éliminer, impossible à tuer » ; mauvais présage : les sauterelles), *Aigle sans Perchoir* (📚 mouvement, ralentit les autres), *Cueillette Amère* (Magie : 📚 agit sur le Grand Vide, emprisonne et scelle), *Forêt du Délire* (Vie : 📚 auto-préservation selon la gravité des blessures) | 📚 **Libre** (vacante depuis le déclin des chamans) · anciens pratiquants : deux **Monarques Diables** du Bois Rassemblé (le cadavre de l'un a formé une montagne de jade) |
| autre lignée (source) | Magie | — |
| fondations isolées | *Bois des Nuées Hautes*, *Droiture de l'Écorce* | — |

**Terre** — lignées non nommées : (Vie, Magie, Corps), (Magie), (Corps), (Magie, Magie), (Magie) · *Terre Orthodoxe* : *Course du Fou vers la Cime* · *Terre Cachée* (📚 « Terre au Trésor », sol qui recèle) : 📚 son ancien détenteur **Wen Xiang** a forgé une **Désignation de Rang** qui garde encore la Grotte Céleste du **Ciel des Mille Prospérités** · *Terre Nourricière* : non détaillée.

**Douze Qi** (📚 ordre du Cycle d'Inversion du Qi)

| # | Fruition | Capacités (type) | Statut et histoire |
|---|---|---|---|
| 1 | **Qi Pur** (fondateur) | 📚 *Corps de Nuée Flottante* (Corps), 📚 *Vent Primordial Pur* (Magie) | 📚 nourrit une Grotte Céleste à son apogée |
| 2 | **Qi Véritable** | essence *Essence du Démon Divin des Cinq Cieux* ; fondation *Sommeil de la Pierre Étreinte* | — |
| 3 | **Qi Brillant** 📚 | — (📚 les maîtres du Dharma bouddhistes modernes lui empruntent une autorité limitée) | — |
| 4 | **Qi Violet** | essence 📚 *Essence d'Origine Immortelle du Qi Violet de la Culture Céleste* ; 📚 *Présage de l'Origine du Dao*, *Détour par le Mont d'Orient*, *Maître Nourricier de Vie*, *Culture de l'Étoile Terrestre*, *Ordonnance des Rouleaux Pourpres* ; aussi (source) *Vase Tiède et Précieux* | 📚 **Cachée** |
| 5 | **Qi Sec** (📚 alias Essence Radieuse, Profondeur Pénétrante) | *Splendeur sans Fin* (Magie), *Veillée de Relève* (Magie), *Chaleur Amère et Étouffante*, *Pavillon des Grâces*, *Débat des Huit Exemptions* (📚 laisse passer sans dommage les techniques mortelles — mais ne défend pas contre la lumière du Yang Lumineux en opposition) | 📚 **Libre** · le **Mont Qushan** (en déclin) et le **clan Hui** (fragmenté) l'ont porté · plus grand maître connu : **Maître Diyun** (Manoir Pourpre, apogée) · 📚 caractère : chaleur persistante qui épuise au lieu de nourrir ; pression contre les forces du Yang ; domine les combats prolongés · symboles : soleil déclinant, désert, terre craquelée, sécheresse |
| 6-7 | **Rite Supérieur**, **Rite Inférieur** | — | — |
| 8 | **Qi Froid** | *Givre sur les Cèdres*, *Ouïe Claire*, *Eau Froide du Nord* | — |
| 9 | **Qi Faste** | *Nuées Fastes*, *Rouleau des Heureux Accomplissements* | — |
| 10 | **Qi Funeste** 📚 | — | — |
| 11 | **Qi Profond** 📚 | — | — |
| 12 | **Qi Exilé** (📚 Profondeur Englobante) | *Approche du Gouffre du Couchant*, *Barque du Ravin* (📚 tombées dans l'oubli), 📚 *Chant du Fleuve d'Oubli* (achevé, il peut refléter le monde souterrain) ; (source) *Vaisseau Abyssal Caché* | 📚 détenteur soupçonné : le **Marquis de la Nuit** · 📚 dévore les signatures d'énergie, **brouille la divination**, efface le souvenir d'un lieu ; « une Grotte Céleste est nourrie par le Qi Pur dans sa jeunesse ; quand elle meurt, le Qi Exilé la réclame » |
| — | lignées non nommées (source) | (Corps, Magie), (Vie), (Vie, Magie), (Vie, Magie, Corps), (Magie) | — |

**Trois Tonnerres**
| Fruition | Capacités / fondations | Détails |
|---|---|---|
| **Tonnerre Céleste** | *Bassin d'Orage* (📚 variante : *Coup de Tonnerre Hivernal*), *Foudre Descendante*, *Premier Grondement du Printemps* ; autre lignée : 1 emplacement Magie | 📚 la fondation *Coup de Tonnerre Hivernal* exigeait un **Liquide de Tonnerre Or-Pourpre** disparu avec la destruction du **Temple des Nuées d'Orage** ; la technique a été modifiée pour un autre liquide de tonnerre |
| **Tonnerre Profond** 🔎 | — | 📚 commandé par la *Séquence Divine* (Régent Céleste) ; prédisposé au Dao Divin |
| **Magnétisme Primordial** 🔎 | fondation *Magnétisme Primordial* (sang flottant en gouttelettes électrifiées, §5.3.2) | 📚 prédisposé au Dao Divin |
🔎 Le wiki ne nomme jamais « les Trois Tonnerres » ; leur composition ci-dessus est une **déduction**.

**Fusion Ancienne** (📚 10 lignées)

| Fruition | Capacités / fondations (type) | Statut et histoire |
|---|---|---|
| **Régent Céleste** (📚 Profondeur Englobante) | *Séquence Divine* (📚 art du calcul des ordres profonds, fondé sur les astres — **affaibli à notre époque car les étoiles sont en désordre** ; commande le Tonnerre Profond ; prévient son porteur si son mandat de vie est défié ; protège de la divination), *Équilibre Profond de la Grande Ourse*, *Dérivations sans Fin* (📚), *Surveillance de la Loi Divine*, *Écoute des Étoiles qui s'Éveillent* (Vie) ; aussi (source) *Arrangement Divin*, *Expansion Continue* | 📚 **Libre** · manuels : *Rouleau Divin de la Séquence du Régent Céleste* (rang 6), *Art de la Révolution du Grand Vide*, *Art de la Tablette d'Or*, *Art de l'Étoile Vigilante*, *Rouleau Sacré de la Cour des Étoiles* · artefact : un manoir illusoire de tonnerre raffiné depuis la Séquence Divine dès la Fondation |
| **Culture de Linxi** (📚 alias Zhibei ; la Secte de la Lune Pâle en tire son nom dans la source) | *Forme en Transition* (📚 corps illusoire au niveau de cultivation fixe), *Usurpation Chaotique* (📚 « Assistance à l'Usurpation » : radiance qui ébranle le monde, presque impossible à cacher), *Rupture des Liens* (📚 gouverne la séparation du Yin et du Yang et le péril mortel d'un souverain), 📚 *Comète Funeste* (sent l'intention de tuer) | **Occupée** : Vrai Monarque Qinghe · 📚 ancien : **Zhiyuan** · étoile : l'étoile Xingbo |
| **Sentinelle de la Cité** (📚 Profondeur Englobante) | *Plume d'Orient* (Magie ; 📚 **ancre le champ de bataille** : fantôme de montagne pâle, alourdit le vide, bloque la traversée du vide et les fuites), *Âmes Descendantes* (Vie), *Eaux Douloureuses du Sud* (Corps+Magie ; 📚 courant noir d'encre chargé de yin et de chagrin), *Plateau du Ciel d'Occident* (Magie), *Cour du Désert du Nord* (Magie) | 📚 **Libre** · la **lignée Hei** (fragmentaire) · anciennes **Cours des Esprits** régionales (disparues) · 📚 symboles : fengshui, divinités, désignations sacrées, fantômes, lignes telluriques, terres ancestrales, domaines hantés · 📚 les maîtres du Dharma bouddhistes modernes en empruntent des fragments |
| **Élixir Parfait** (📚 aussi rattaché à la Vertu Primordiale) | *Attente du Divin Suprême* (📚 longévité, éviter la mort, commander oiseaux et bêtes ; transformer apparence, artefacts et **tradition Dao** ; aide aux conversions entre Eau, Feu et Métal), *Harmonie Parfaite* (📚 Magie : **reproduit un artefact tenu en main** à 60-70 % de sa puissance), *Humectation du Plomb Radieux* (Vie), *Séquence du Livre d'Or* ; substitution *Perle Grise secrète* | **Libre** · 📚 ancien détenteur : **Suhe** (tué par Ao Ming et Ao Xi) · ressource : le *Mercure Pur de la Grande Unité* |
| **Corps de Kui** 📚 | — | — |
| **Grand Chaman** (Sceau chamanique) | essence *Essence de Sang des Trois Neuf* ; *Ombre de la Sauterelle* (📚 Vie), *Présence Indécelable* (📚 Magie ; bloque les arts de calcul), *Bénédiction du Chaman de la Terre* (📚 Magie), *Mandat de l'Empereur* (📚 Vie), *Buveur de Sang* (📚 Vie) ; substitution *Course du Fou vers la Cime* | **Libre** · 📚 classé Fruition **fondatrice** par le wiki |
| **Chouette des Seuils** | (Vie), (Magie), (Magie), (Vie), (Vie) | 🔎 non donné |
| **Jade Premier** (📚 Profondeur Céladon) | essence 📚 *Essence du Vide Unifié Six-Neuf du Jade Premier* ; *Unification du Vrai Dao* (📚 un point de brillance blanche dans la mer de Qi), *Brocart Figuré* (Corps ; 📚 l'aspect préféré de la Vertu Primordiale), *Né du Jade* (Corps+Vie), *Falaise de Jade Vert* (Magie ; 📚 fait surgir une montagne de jade blanc), *Disque de Jade Blanc* ; substitution *Cour du Jade Général* | **Occupée** : 📚 **Vénérable Lingxu** · Surplus : 📚 **Fu Qing** (« Vrai Monarque du Jade Premier du Yang Clair ») · 🔎 la source dit que « la figure du Jade Premier » a renoncé à sa Réalisation : il s'agit d'un détenteur antérieur |
| **Rite Équilibré** 📚 | — | — |
| **Proclamation Céladon** / **Manifestation Céladon** | essence *Essence des Monts Enchaînés* ; *Bélier des Profondeurs*, *Emblème Céladon*, *Chaîne des Monts*, *Roc Souverain*, *Guetteur de l'Abîme* ; substitution *Essence du Yang Mineur* | Libre |

**Attestation du Vide**

| Fruition | Capacités | Statut et histoire |
|---|---|---|
| **Voie de l'Épée** (📚 alias Étoile du Soir, Manifestation de l'Épée ; Profondeur Céladon) | 📚 *Ciel Céladon Nourri*, 📚 *Corps Porteur d'Intention* | 📚 **Libre** · ancien : l'**Ancêtre de l'Épée**, Seigneur Immortel, qui l'a attestée du néant · Surplus : **Xuanfeng** |
| **Lueur de l'Aube** | 📚 il existe **72 types** de Lueur de l'Aube, dont **48** créés par le Vrai Monarque du Mont Xiaoyun, qui forge son Dao avec elle : toute Lueur de l'Aube doit se soumettre à lui ; « le premier rayon de l'aube ou du crépuscule vient du Mont Xiaoyun » | Immortelle Xiaoyun |

### 6.8 Statuts de Fruition (état du monde au début du jeu)

📚 Le wiki distingue **quatre** statuts, pas deux :

| Statut | Sens | Fruitions |
|---|---|---|
| **Occupée** | Un Vrai Monarque détient la Réalisation ; la volonté du Ciel y est remplacée par la sienne | Eau Muable (Tan Qing), Eau Rassemblée (Ao Ming), Eau Cachée (Dame Yinlan), Yang Lumineux (Zhen Guangling), Jade Premier (Lingxu), Culture de Linxi (Qinghe), Or Vert d'Origine |
| **Libre** (vacante) | La position est gouvernée par la volonté du Ciel ; on peut la réclamer | Eau Orthodoxe, Eau Nourricière, Bois Rassemblé, Qi Sec, Régent Céleste, Sentinelle de la Cité, Élixir Parfait, Grand Chaman, Manifestation Céladon, Voie de l'Épée |
| **Brisée** 📚 | Plus de détenteur ; 🔎 la Fruition elle-même est endommagée : on ne peut pas y monter tant qu'elle n'est pas restaurée | Feu Nourricier, Métal Caché |
| **Cachée** 📚 | 🔎 son état réel est inconnu du monde | Qi Violet |
| Soupçonnée | détenteur supposé, non confirmé | Qi Exilé (Marquis de la Nuit) |
| Non précisé | tiré au début de chaque partie (§11.7) | toutes les autres |

### 6.9 Profils des systèmes de Fruition (📚)
| Système | Profil |
|---|---|
| **Eau Orthodoxe** | Insaisissable, manipulateur, mobile ; peu de puissance brute mais excelle en **esquive et contre-attaque** (ex. *Veilleur du Gué* pousse artificiellement autrui à faire ce qu'il désire) ; essence gris-vert, pluie |
| **Eau Muable** | Volumineuse, purifiante, étouffante ; entre trouble et pur ; **immobilise, étouffe et dissout** comme une mer ; pluie et eau turquoise |
| **Eau Rassemblée** | Érosive : **empoisonne** et entrave le flux de mana ; les serpents aiment jouer avec leur proie |
| **Eau Nourricière** | Lacs, marais, vent et serpents crochus ; **perturbe le vol** et ronge les corps ; gris-blanc ; rare dans le sud de Linxi, abondante en Mer Orientale |
| **Feu Nourricier** | Flammes rouge-or, dominantes et royales ; chaleur écrasante dès l'aura, l'air sec scelle la bouche |
| **Feu Rassemblé**, **Feu en Fusion** | voir §6.7 |
| **Yang Lumineux** | Autorité : croissance, union, harmonie du masculin et du féminin, lumière céleste, feu clair. Contre naturel des **Diables** et du mal ; tempérament vif, exige un **esprit impérieux** |
| **Yin Suprême** | Extrêmement Yin, pur et lumineux, sans force mais inflexible |
| **Grand Chaman** | Passerelle vers le Dao chamanique (§3.5) |
- 📚 Grandes catégories : **Pur, Radieux, Convergent, Fusionnant** ; les **bêtes à plumes** suivent surtout le Pur et le Radieux, les **bêtes à écailles** la Convergence et la Fusion.

### 6.9 Autorité des positions (📚 compléments du wiki)

- **Les trois positions d'or** : **Fruition** (果, la Racine / le Dragon : l'Essence) ; **Surplus** (余, le Feuillage / le Tigre : la Manifestation — autorité asymétrique, dépendante de la structure posée par le détenteur) ; **Intercalaire** (闰, l'Exception / le Phénix : occuper les failles et incohérences des lois du Ciel, comme le mois intercalaire du calendrier lunaire — « ils ne forcent pas le fleuve, ils déplacent son lit »).
- **Deux étapes du Noyau d'Or** : forger l'essence métallique, puis monter à une position ; **la plupart n'achèvent jamais la seconde**.
- **Empreinte** : un détenteur qui dure imprime son être dans la Fruition (l'image de la bête Hongyao apparaît dans toute manifestation du Yang Lumineux).
- **Trésors de Dharma** : au Noyau d'Or, on condense des trésors liés à sa fondation et à son essence. Pour un détenteur de position, ils peuvent devenir une **Désignation de Rang** : un trésor **hypothéqué sur la Fruition elle-même**, qui retourne à la Fruition s'il est descellé. Seul un détenteur à la profondeur exceptionnelle **et aimé de sa Fruition** peut vraiment le manier (ex. la Désignation de Wen Xiang garde encore une Grotte Céleste après sa mort ; Tan Qing, non aimé de l'Eau Muable, ne peut manier la sienne que brièvement). Une Désignation sans maître est dangereuse : le *Livre Céleste de la Grande Divination* a dû être exilé dans le Qi Exilé.
- **Démons d'Essence Métallique** (hiérarchie) : issus d'un **détenteur de Réalisation** → catastrophes rivalisant avec un Vrai Monarque ; issus d'un **échec de montée** → pas forcément faibles (le Ciel avait déjà reconnu l'essence) ; issus de voies anciennes sans position → bien inférieurs.
- **Voies de la Main Gauche** : routes vers une puissance équivalente au Noyau d'Or **sans** position. **Vraies** : autonomes (Immortels au Visage Radieux du Yang Suprême, Immortels de Jade Tressé du Yin Mineur). **Fausses** : dépendantes d'un supérieur (le **Faux Noyau Divin**, dans les Dao Immortel, du Diable et Divin ; les maîtres du Dharma bouddhistes modernes, plafonnés au début du Noyau d'Or, qui empruntent au Qi Brillant, au Yang Lumineux et à la Sentinelle de la Cité les mécanismes laissés par le **Vénérable Kongji**).
- **Étoiles** : certaines Fruitions ont une association céleste (Mercure pour l'Eau Orthodoxe, Jupiter pour le Bois Orthodoxe, l'étoile Xingbo pour la Culture de Linxi) ; les étoiles étant « en désordre » à notre époque, les arts de calcul qui en dépendent sont affaiblis.

🎮 Phase 4 : `DaoLineage` (ScriptableObject) : groupe, génération, **Profondeur d'origine**, élément/manifestation, essence métallique, capacités (nom, types), substitutions, **statut (Occupée / Libre / Brisée / Cachée)**, détenteur, Surplus et Intercalaires connus, étoile ; `RankDesignation` ; `LeftHandPath`. Les emplacements non nommés sont créés avec leurs types et un nom provisoire. `Element` actuel (Fire, Water, Wood, Metal, Earth, Lightning, Darkness, Light) est conservé pour le **combat** et relié : Lightning ↔ Trois Tonnerres, Darkness ↔ Yin, Light ↔ Yang.

---

## 7. Carte du monde (renommée)

La carte du jeu reprend la **géographie relative** de la carte source (positions, voisinages, tailles relatives) avec des noms originaux. Le clan du joueur vit au **centre-ouest du Royaume de Linxi**.

### 7.1 États et grandes régions

| Position | Nom (jeu) | Nature |
|---|---|---|
| Centre, vert (la plus grande) | **Royaume de Linxi** | État du joueur ; « trois sectes et huit portes » |
| Nord-ouest | **Empire de Kun** | Grand État du Nord (successeur des dynasties anciennes, §12) |
| Nord-ouest, jaune | **Principauté de Tai** (Mont Jianfeng) | |
| Ouest | **Royaume de Hanshan** | |
| Sud-ouest (petit) | **Duché de Pei** | |
| Sud / sud-ouest | **Terres chamaniques de Nanwu** | Voie chamanique |
| Est | **Mer Orientale** (îles) | Lignée dragonne, Atoll des Perles Noires |
| Nord (au-delà) | **Mer du Nord**, **Glaces du Nord** | Perdue par les dragons ; Impératrice Hanlu |
| Ouest lointain | **Steppes de l'Ouest** | Seigneur Démon Gu'e |

### 7.2 Les trois sectes et les portes

| Position | Faction (jeu) | Type | Remarque |
|---|---|---|---|
| Centre-est, autour du Mont Yunfeng | **Secte du Pic des Nuées** | Secte | Dominante ; soutenue par au moins un Vrai Monarque |
| Ouest (grande zone) | **Secte des Mille Lames** | Secte | Désert des Sables Chantants, Trois Commanderies de Liuhe |
| Nord (fleuve) | **Secte de la Lune Pâle** | Secte | |
| Nord-ouest | **Porte du Fer Ardent** | Porte | Méthode du Tranchant Clair |
| Nord-est | **Porte du Roc Obscur** | Porte | Préfecture de Yunmen |
| Nord-est | **Porte du Givre Blanc** | Porte | |
| Est | **Porte de l'Épée Stellaire** | Porte | Préfecture de Zhanghe |
| Est (violet) | **Porte des Brumes d'Ambre** | Porte | Préfecture de Qingyuan |
| Sud-est | **Porte du Chrysanthème Noir** | Porte | Préfecture de Pingjiang |
| Sud | **Porte des Saules Pleureurs** | Porte | Préfectures de Luoping, Baishi |
| Sud (rouge) | **Porte du Tambour Sacré** | Porte | Préfecture de Nanling |
| Îles de l'est | **Atoll des Perles Noires** | Île immortelle | Art du Brasier Englouti |

✅ D5 : **huit portes**, comme sur la carte. `Miror.txt` disait « sept portes » ; le lore du jeu est aligné sur la carte (le wiki liste aussi huit portes).

### 7.3 Autour du clan

📚 **Le lac et ses clans.** Le Lac Jingshui, plus tard appelé **Lac Immortel**, fait face au nord et au sud ; les Ruan vivent dans les terres sauvages au nord-est. Il relevait d'abord de la Secte du Pic des Nuées avant d'être confié aux Mo. **Cinq clans** vivent autour du lac : **Lou, Fang, Mo, Lü et Tao** ; sous la domination des Mo, le lac sera unifié autour de son **île centrale, Cuidao**. Les **Monts Qingyan** forment une longue chaîne : le **Pic de Jingxi** domine le **village de Jingxi** (village natal du clan) ; plus au sud, le **Pic Meiling**, puis le **village de Qingyang**.


| Position | Lieu (jeu) | Rôle |
|---|---|---|
| Centre-ouest | **Lac Jingshui** (« Lac de l'Eau-Miroir »), ancien **Marais Jingshui** | Domaine du clan Mo |
| Sud du lac | **Monts Qingyan** | Protégés par la **Renarde des Monts Qingyan** (pacte ancien) |
| Sud-ouest du lac | **Mont Fengxi** | |
| Ouest du lac | **Préfecture de Wuyang** | |
| Est du lac | **Préfecture de Heshan**, territoire de la **famille Ruan** | Voisins directs |
| Est | **Préfecture de Qianshi** | |
| Nord | **Terres Désolées du Nord**, **Plaine de Guling** | Sauvage |
| Centre | **Plaine des Roseaux Gris** | Famille Gu |
| Centre-ouest | **Gouffre des Bêtes** | Voie démoniaque (bêtes) |
| Ouest | **Préfecture de Xiliu** | |
| Centre | **Mont Songhe** | Ancienne Secte Songhe |
| Centre-est | **Mont Tingsong**, **Préfectures de Siwei, Haiyan** | Terres du Pic des Nuées |

### 7.4 Autres puissances et lieux (📚 carte traduite du wiki)
- **Puissances bouddhistes** : Temple du Lotus (Empire de Kun), Temple de la Merveille Profonde (Principauté de Tai), Grand Monastère de Huiqiu (Linxi).
- **Puissance taoïste** : Voie des Immortels de la Capitale (Tai) ; **Porte de la Clarté Révérée** (Tai).
- **Lieux** : Lac Salé, Marais de Qingshui, Mont Baiye, Pic de l'Épée Stellaire (Tai) ; Grand Désert de Kuyan, Mont Kuiyun, **Porte du Carnage** (sud de Hanshan) ; Frontière du Tigre (Linxi) ; archipel de **Shiyuan**, **Mer du Ciel Confluent**, **Mer Vermillon**, **Marches du Sud**.
- 📚 La liste des huit portes du wiki diffère un peu de la carte : elle cite la **Porte du Carnage** et la **Porte de l'Écrit Sacré** au lieu du Chrysanthème Noir et des Saules Pleureurs. D5 s'appuie sur la carte fournie.
- 🔎 Sur la carte du wiki (état au chapitre 850), le lac, les Monts Qingyan et la Plaine des Roseaux Gris sont dans l'**ouest de Hanshan**, pas dans Linxi : les frontières bougent au fil de l'histoire. Le jeu suit la carte fournie (le lac dans Linxi) ; 💡 un déplacement de frontière pourrait devenir un événement.

🎮 Phase 5 : `RegionDefinition` (nom, position, voisins, faction dominante, modificateur de cultivation, phénomènes) ; `FactionTemplate` étendu (voie, royaume le plus haut, région, techniques possédées) ; nouvelle carte dessinée (style Shuimo) ; `WorldMapUI` par régions.
⚠️ Aujourd'hui 8 factions inventées (Wang Family, Golden Sun Empire…) disposées en cercle.

---

## 8. Familles nommées (renommées)

| Famille | Où / rôle | Méthodes |
|---|---|---|
| **Mo** (clan du joueur) | Lac Jingshui ; lignée issue de l'Empereur Martial Zhen (légende, §12) | Source Claire, Source Souterraine, Éclair Cendré, Bise Hivernale, Soleil Intérieur |
| **Ruan** | Préfecture de Heshan, voisins | Source Claire, Ruisseau Remonté |
| **Gu** | Plaine des Roseaux Gris (venue du Temple Lianhuo) | Ruisseau Remonté, **Sept Terrasses (grade 6)** |
| **Bai** | — ; Bai Chengyu devient Manoir Pourpre il y a ~200 ans | Marée Silencieuse, Verbe Premier |
| **Zang** | — | Rempart d'Airain, Murmure des Cèdres |
| **Lü** | liée à la Porte du Fer Ardent | Tranchant Clair |
| **Tao** | alliée défendue contre les Lou | Cœur Tissé, Bise Hivernale |
| **Lou** | ennemie des Tao | — |
| **Xun** | — | Source Souterraine |
| **Qiao** | — | Grondement Lointain |
| **Fang** | — | Veilleur du Sentier |
| **Qu** | Mer Orientale ; branche directe de la dynastie Zhen | — |

---

## 9. Personnages historiques et légendaires (renommés)

Les personnages cités en exemple par la source deviennent des **figures du monde** (légendes, maîtres, ennemis), jamais des membres prédéterminés du clan du joueur, dont les descendants restent **émergents**.

| Personnage | Rôle |
|---|---|
| **Mo Jian** | Fils aîné du patriarche ; **trouve le miroir** (an 0) |
| **Mo Wei** | Patriarche ; s'est engagé dans l'armée il y a ~50 ans |
| **Mo Qianshui** | Ancêtre tué dans une embuscade des Douze Portes (~400 ans) |
| **Empereur Zhen Guangling** | Fondateur de l'Empire Zhen ; détenteur de la Réalisation du Yang Lumineux |
| **Empereur Martial Zhen** | Campagne vers le sud ; ancêtre légendaire des Mo |
| **Zhen Xunshi** | Rebelle (avec Vrais Monarques et un Maître taoïste) ; échec |
| **Immortelle Xiaoyun** | Embryon du Dao ; atteste la Lueur de l'Aube ; le Mont Xiaoyun convoite le Yang Lumineux |
| **Immortel Shuangji** | Du Manoir de l'Aube d'Argent ; scinde le Monarque Démon |
| **Impératrice Hanlu**, **Seigneur Démon Gu'e**, **Roi-Dragon Changyong** | Les trois fragments du Monarque Démon |
| **Yuwen Duo** | Empereur fondateur (« Taiwu ») de l'État de Rui ; tue Ao Lin |
| **Ao Lin** | 7e fils du Vrai Serpent-Dragon céleste |
| **Ao Ming**, **Ao Xi** | Fils dragons ; tuent le maître de l'Élixir Parfait |
| **Duan Qishan** (Empereur Shengwu) | Empereur de Kun ; équilibre des trois Dao ; meurt à Hongcheng |
| **Tuoxi Hong** | Roi de Yan ; co-destructeur de Rui |
| **Duan Lihua** | Prince héritier de Kun ; accueille les sept statues (Sept Aspects du Dharma) |
| **Vrai Monarque Qinghe** | Saisit l'héritage de la Culture de Linxi |
| **Bai Chengyu** | Devient Manoir Pourpre (~200 ans) ; tue Ruan Xianzhou avec les sectes et portes |
| **Ruan Xianzhou** | Piégé et tué (~200 ans) |
| **Gongye Shu** | Vrai Monarque en Surplus du Grand Chaman ; auteur de la technique de grade 7 |
| **Weng Liuxian**, **Bai Ruozhi** | Exemple d'Intercalaire 4A+1B=C |
| **Ze Wuyan** | Exemple du Dao de la Nature Spirituelle |
| **Qiu Mingshan** | Exemple de percée de Fondation (Eau) |
| **Hou Lie**, **Nian Suo**, **Ban Jinhe** | Exemples d'alignement du Cœur Dao et de corps inhumain |
| **Pan Yiqiu**, **Rong Shuang** | Bloqués à la Fondation avancée |
| **Lan Guyu** | Lien à un trésor spirituel |
| **Shi Kongming** | Converti au bouddhisme après l'échec du Dard Vermillon |
| **Kuang Yao** | Culture par l'Imagerie (Monarque Assiégé) |
| **Moine Wuyi** | Commentateur de l'essence métallique de Xiaoyun |
| **Yinjun**, **Taichen** | Immortels Dorés de l'Antiquité |

---

## 10. Ordres et lieux sacrés (renommés)

| Nom (jeu) | Nature |
|---|---|
| **Manoir de l'Aube d'Argent** | Grand ordre ancien, fondé sous l'Empire Zhen, détruit il y a ~600 ans |
| **Manoir immortel de Chenguang** | Détient le Manuel du Soleil Intérieur |
| **Secte Songhe** | Fondée il y a ~1000 ans |
| **Temple Lianhuo** | Origine de la famille Gu |
| **Jardin des Mille Délices** | Retraite de la famille Yuwen |
| **Île aux Hérons Morts** | Lieu de la capture du Monarque Démon |
| **Gué de Lingjin** | Grande bataille du cataclysme |
| **Préfecture de Hongcheng** | Mort de Duan Qishan |
| **Douze Portes** | Coalition de Linxi (~500 ans) |
| **Trois sectes et huit portes** | Ce qui reste des Douze Portes (~400 ans) |

---

## 11. L'espace des possibles — pourquoi chaque partie est unique (D4)

Le jeu s'adresse aux **amoureux du xianxia**. Sa profondeur ne vient pas de règles ajoutées mais de la **combinaison** des systèmes du lore. Trois principes guident toutes les phases :

- **P1 — Règles symétriques.** Chaque personnage du monde (familles rivales, anciens des sectes, cultivateurs errants, démons, dragons) obéit **aux mêmes règles** que le clan du joueur : orifice, grades, royaumes, Partenaires Dao, positions de Fruition. Le monde avance **seul** ; les histoires émergent au lieu d'être écrites.
- **P2 — Tout raccourci a un prix, et ce prix vient du lore** (§11.2). Aucun chemin n'est « le bon » : chaque génération arbitre entre vitesse, sécurité, puissance et morale.
- **P3 — Le savoir est une ressource.** « Comprendre ses Partenaires Dao est crucial **avant** d'entrer dans le Dao » : les Partenaires Dao, les statuts de Fruition, les techniques secrètes, les défauts des techniques, l'histoire du monde se **découvrent** (miroir, espionnage, étude, échanges), ils ne sont pas donnés d'emblée.

### 11.1 Combinatoire d'un seul cultivateur
| Choix | Étendue (d'après le lore) |
|---|---|
| Voie et sous-voie | 5 voies, 9 sous-voies (§3) |
| Méthode de Qi | 26 méthodes connues + celles déduites par le miroir ; grade 1-7+, catégorie commune / ancestrale / secrète, Qi requis (§2) |
| Fruition | ≈ 60 : 6 Dualités + 25 Cinq Vertus + 12 Qi + 3 Tonnerres + ≥ 6 Fusion Ancienne + 3 Chamans + 2 Rites + ≥ 4 Attestation (§6) |
| Fondation | 5 par Fruition (§5.3.3) |
| Capacités divines | 5, dans un **ordre** choisi (5! = 120 ordres), chacune d'un type ; conseil du lore : la Vie en dernier (§5.4.4) |
| Moyen de cultiver chaque capacité | Ressources / Profondeur du Dao / Imagerie / Greffe du Dao / Partenaire consommé (§5.4.3) |
| Position visée | Réalisation, Surplus (2 formes), Intercalaire (4A+1B=B, 3A+2B=B, 4A+1B=C) (§5.5.1) |

→ Pour **une seule** Fruition, 5 fondations × 120 ordres de capacités = **600 parcours de Manoir Pourpre**, avant les substitutions, les moyens de culture et les positions ; sur ≈ 60 Fruitions : **plus de 36 000** parcours. Sur des dizaines de générations, chacune héritant des choix des précédentes, l'espace est **pratiquement infini**.

### 11.2 Raccourcis et leur prix (tous tirés du lore)

| Raccourci | Gain | Prix |
|---|---|---|
| Technique de grade 1-2 (*Souffle Commun*) | N'importe qui cultive | Essence et aura impures, potentiel inférieur, plafond à la Culture du Qi |
| Graine de Sceau | Cultiver sans orifice | Don **du miroir** : consomme du Clair de Lune du Yin Suprême et une place limitée par le niveau de restauration du miroir (§11.5) |
| Qi de talisman | Trait unique + bond de cultivation | Rituel : **sacrifier** un être d'au moins la Culture du Qi (encens, âme, essence de sang, force spirituelle) + **dix mille prières** (§11.5) |
| Technique secrète | Accès à une méthode dont le Qi a disparu | Souvent imparfaite |
| Consommer un Partenaire Dao | Stade suivant instantané | Progression **définitivement bloquée** ; Dao exploitable par les plus forts |
| Ressources pour une capacité | Perfection rapide | Fondements superficiels, sorts plus faibles |
| Greffe du Dao | Capacité sans recommencer toute une technique | **Consommer la fondation d'un prodige** (un autre membre du clan ?) |
| Lien à un trésor / Grotte Céleste | Pouvoirs de Maître du Manoir Pourpre | Fin de la progression, ou destin soumis à l'objet |
| Emprunt de lumière d'une Fruition | Manoir Pourpre « Miséricordieux » | Dépendance envers le Vrai Monarque prêteur |
| Voie du Démon Pourpre-Or | Progression inégalée | Pas de destin ; pari final au Noyau d'Or |
| Démon Embryon Céleste | Une seule ascension au lieu de deux | Consommer de l'**essence de sang** |
| Nature Spirituelle | Pseudo-Noyau d'Or, montée directe | 1 sur 1000 ; la Fruition **dicte la conduite** (interdits de comportement) |
| Conversion bouddhiste | Échapper à la mort | Abandon de la voie |
| Surplus au lieu de Réalisation | Noyau d'Or même si la Fruition est occupée | Jamais d'Embryon du Dao ; permission du détenteur |
| Intercalaire | Changer de Fruition | Méthodes rarissimes ; échec = **Démon d'Essence Métallique** |
| Percer la Fondation après 60 ans | Dernière chance d'un talent tardif | Risque élevé de **dissolution spirituelle** |

### 11.3 Les dilemmes du clan (reviennent à chaque génération)
- À qui confier **l'unique** technique de grade 4 ou 5 ? Au génie, au futur patriarche, ou la garder secrète ?
- Quelle **fondation** pour quel enfant, selon son tempérament (alignement du Cœur Dao, §5.3.2) et selon les Fruitions libres ?
- **Mariages** : épouser une lignée de cultivateurs pour l'orifice héréditaire (D3), une faction pour une alliance, ou par amour ?
- Envoyer un membre en **retraite** de manifestation (~6 ans) ou dans le **Grand Vide** (jours ou décennies, parfois à vie) alors que le clan a besoin de lui ?
- **Sacrifier** la fondation d'un prodige (Greffe du Dao) pour la capacité divine du patriarche ?
- **Consommer** un Partenaire Dao pour sauver le clan maintenant, au prix de l'avenir de ce cultivateur ?
- Demander la **protection** (et payer tribut) à une secte, ou rester indépendant ?
- **Révéler ou cacher** le miroir ?
- Viser une Fruition **libre** avant les rivaux, ou demander au **détenteur** une place de Surplus (et devenir son vassal) ?
- Convertir un membre condamné au **bouddhisme** pour qu'il survive, au risque de perdre sa loyauté ?

### 11.4 Un monde vivant
- Les **cultivateurs des factions** progressent selon les mêmes règles ; certains percent, d'autres meurent en dissolution spirituelle, s'emprisonnent dans le Vide ou deviennent des Démons d'Essence Métallique.
- Les **Fruitions changent de détenteur** : un Vrai Monarque meurt, une position se libère, une Reprise de la Fruition possède un héritier.
- Les **morts de cultivateurs** (Fondation et au-delà) laissent des **phénomènes** qui modifient les régions : météo, efficacité de cultivation (±), objets spirituels, catastrophes (§5.3.5, §5.4.5).
- Les **pressions extérieures** varient d'une partie à l'autre : dragons de la Mer Orientale, bêtes du Gouffre, chamans de Nanwu, bouddhisme du Nord, fragments du Monarque Démon.
- Les **héritages perdus** deviennent des quêtes : une technique ancestrale dont le Qi a disparu peut redevenir utilisable si l'on retrouve ou recrée son milieu ; une technique secrète peut cacher un défaut.
- Les **Grottes Célestes** et les **trésors du Manoir Pourpre** sont des lieux et des objets convoités.

### 11.5 Le miroir — le rôle du joueur
📚 Dans le roman, le miroir **est un personnage** : l'esprit d'un homme mort dans notre monde, piégé dans un miroir de bronze fêlé, qui guide en secret la famille qui l'a trouvé. C'est exactement le rôle du joueur (pitch de `MEMORY.md`). Le wiki décrit ses capacités ; on les reprend fidèlement (noms descriptifs conservés, D6).

**Ce qu'est le miroir**
- Un miroir de bronze **bleu-gris, fêlé en plusieurs éclats** tenus par un cadre de fer ; au dos, des symboles indéchiffrables ; douze runes autour du cadre.
- Son rang inné est **au moins celui de l'Embryon du Dao** : endormi, il est **indétectable** par toute perception inférieure à l'Embryon du Dao ; toute divination, malédiction ou lien de destin dirigé vers lui est **bloqué ou renvoyé**, avec un violent contrecoup (les puissants en concluent qu'un immortel antique protège ses porteurs).
- Il **absorbe passivement l'essence de la lune** et la condense en **Clair de Lune du Yin Suprême**, un Qi spirituel du Ciel et de la Terre extrêmement précieux, que le clan peut sceller et stocker.
- L'esprit du miroir **reste silencieux** : s'il se montrait, le clan poserait trop de questions et découvrirait combien il peut peu. Il agit par des graines, des talismans, des visions.

**Ses pouvoirs**
| Pouvoir | Ce qu'il fait (📚) |
|---|---|
| **Graines de Sceau** (« Perles Profondes ») | Une « pilule de lumière blanche » déposée dans le dantian. Elle porte un savoir (ex. le *Sutra de la respiration du Yin Suprême*) et permet **même à un mortel** de commencer à cultiver. C'est aussi un **conduit indétectable** : le miroir peut transmettre à travers elle de la puissance, des messages ou une intention (et plus tard frapper au loin). |
| **Rituel de sacrifice → Qi de talisman** | Sacrifier un être d'**au moins la Culture du Qi** (encens, âme, essence de sang, force spirituelle offerts au miroir) et réunir **dix mille prières** : le miroir raffine un Qi de talisman du rang du sacrifice (**gris** = Culture du Qi, **blanc** = Fondation). Le miroir propose **1 à 3 choix** adaptés au **talent et à la personnalité** du bénéficiaire. Des éclats du miroir ou des objets liés peuvent aussi servir. |
| **Lumière Profonde du Yin Suprême** | La frappe du miroir, pure éradication. Sa puissance suit la restauration : d'abord l'équivalent d'un coup au sommet du chakra de l'Œil du Sommet ; après un éclat, elle tue la Culture du Qi et blesse un royaume au-dessus ; plus tard, elle tue un Manoir Pourpre ou une Fondation à l'apogée (sauf fondation exceptionnelle). |
| **Perception** | Sens visuel large et sens divin précis ; portée croissante : ~66 m au début, puis le village, puis tout le lac (plus de 1000 *li*), et à terme la moitié du royaume. |
| **Âme et illusions** | Implanter des verrous cachés dans l'âme d'autrui (surveillance, vol de souvenirs), indétectables même au Manoir Pourpre ; projeter des illusions grandioses (une salle de jade blanc d'immortel) pour interroger ou dissuader. |
| **Réécriture de techniques** | Grâce à sa profondeur du Dao, « nettoyer » une technique de secte de ses marques personnelles pour que le clan l'utilise sans trahir son origine. |
| **Traverser le Grand Vide** | Après un éclat majeur, le miroir réside dans le Vide, relié à l'autel du clan, et se déplace librement. |
| **Essence métallique** | En récupérant l'essence d'une Fruition, il peut infléchir le destin, améliorer les chances d'une percée de Fondation, et **percer les secrets** de cette Fruition pour rédiger des manuels de sa lignée. |

**Qi de talisman connus** (📚, noms descriptifs conservés, D6) — chacun donne un trait **et** un bond de cultivation :
| Rang | Talisman | Effet |
|---|---|---|
| Gris | *Échapper à la mort, prolonger la vie* | Prémonition des dangers à venir |
| Gris | *Force à déplacer les montagnes* | Force sans égale, même sans mana |
| Gris | *Prolonger la vie et accroître la longévité* | +40 ans de vie |
| Gris | *Poursuivre nuages et lune* | Vitesse de vol multipliée |
| Gris | *Moineau féroce du vaste ciel* | Agilité, bravoure décuplée à la vue du sang, maîtrise du vent |
| Gris | *Garder profit et prospérité* | Améliore les cent arts (alchimie, talismans…), apaise les tourmentes de fortune |
| Blanc | *Longue baleine de l'océan profond* | Qihai ×5-6, régénération du mana ×10 |
| Blanc | *Épuiser la vie pour réussir* | 🔎 non détaillé (prix en vie ?) |
| Blanc | *Qi fluide avalant l'esprit* | Absorption rapide du Qi, commander nuages et brumes |
| Blanc | *Os de glace, chair de givre* | Absorption accrue, maîtrise de la glace et de la neige |
| Blanc | *Crête de pins sous la neige radieuse* | Cœur calme comme l'eau, dissipe les illusions, cultivation deux fois plus efficace |
| Blanc | *Vent de vallée guidant le feu* | Le feu attire le Qi et raffine l'essence |
| Blanc | *Arc-en-ciel perçant la voie céleste* | Esprit fortifié, lecture des sorts adverses, légère prolongation de la vie |
| Blanc | *Arc-en-ciel vermillon fluide* | Descendance plus douée, fondation renforcée, vol plus rapide |

**Sa restauration.** Le miroir est incomplet : chaque **éclat** retrouvé (le premier est repêché dans le lac) lui rend de la puissance et des **souvenirs** (le premier contenait le *Sutra de la respiration du Yin Suprême*, technique de la Respiration Embryonnaire du Manoir de l'Aube d'Argent). Pendant l'intégration d'un éclat, l'esprit **s'endort** par intermittence ; son âme se renforce au fil des éclats.

💡 **Recommandation (avis, à valider)** : faire du miroir **le second axe de progression**, à côté du clan.
- **Niveau de restauration** (nombre d'éclats) = « royaume » du joueur : il fixe la portée de la perception, la puissance de la Lumière, le nombre de Graines de Sceau actives, l'accès au Grand Vide.
- **Clair de Lune du Yin Suprême** remplace la jauge `MirrorPower` : produit chaque nuit, dépensé pour les graines et les interventions, ou **donné au clan** comme Qi spirituel rare.
- **Éclats** = objectifs de long terme (lac, ruines, trésors d'ennemis, événements) ; chacun révèle un souvenir : technique, fait historique, **indice sur l'origine du miroir** — le grand mystère du jeu, révélé peu à peu.
- **Sommeil d'intégration** : quelques années où le clan agit sans le joueur (l'IA gère) — tension dramatique.
- **Secret** : le miroir doit rester caché ; s'il est découvert par plus fort que lui, il peut être saisi (voir §11.9).
- Les pouvoirs actuels du code trouvent leur place : *Pulsation de Qi* → graine-conduit en combat ; *Bouclier ancestral* → essence métallique qui infléchit une percée ; *Jugement du Miroir* → Lumière Profonde ; *DeductionEngine* → souvenirs d'éclats et réécriture de techniques.

### 11.6 Générations et héritage
- **Lignée transformée** : un ancêtre à la Réalisation garantit le Manoir Pourpre à ses descendants (§5.5.2).
- **Ancêtre réincarné** : un Vrai Monarque du clan peut revenir et remonter en quelques décennies.
- **Reprise de la Fruition** : la Fruition peut tenter de posséder l'héritier : menace autant que puissance.
- **Corps inhumains** : les traits de la fondation (sang doré, électrifié…) se voient et se transmettent.
- **Techniques** transmises, perdues, volées ou déduites ; **karma** du clan (système existant).

### 11.7 Rejouabilité
💡 Proposition : chaque partie tire une **graine de monde** :
- statuts des Fruitions non fixés par le lore (§6.8) et identité de leurs détenteurs ;
- forces et ambitions des factions, techniques disponibles dans les boutiques ;
- prodiges et menaces du voisinage ;
- descendants générés procéduralement (génétique existante) ;
- événements tirés de la chronologie (§12) déclenchés selon l'état du monde.
Les faits datés du lore (§12) restent **identiques** d'une partie à l'autre : ils sont l'histoire commune.

### 11.8 Ce que l'on s'interdit
- Contredire la source. Tout ajout est marqué 💡 et validé par l'utilisateur.
- Décrire la progression interne de l'Embryon du Dao et de l'Immortel Doré tant que l'utilisateur n'en décide pas (la source les dit « inconnus »).
- Imposer une seule stratégie optimale : si un chemin domine, il faut renforcer son prix (P2).

### 11.9 Conditions de victoire et de défaite
Aujourd'hui : 10 générations + 1 ascension (DaoEmbryo).

💡 **Recommandation (avis, à valider)** : un **bac à sable dynastique sans fin imposée**, comme les grands jeux de gestion de lignée, avec :
- des **Annales du clan** : jalons enregistrés et datés (première Fondation, premier Manoir Pourpre, premier Vrai Monarque, première Réalisation, éclats du miroir retrouvés, secte fondée, Fruition conquise…) qui racontent la partie ;
- une **grande fin optionnelle**, fidèle au lore : le **miroir entièrement restauré** (son origine révélée) **et** un membre du clan à la **Réalisation d'une Fruition** ou à l'**Embryon du Dao** ; le joueur peut continuer après ;
- deux **défaites** :
  1. la **lignée s'éteint** (plus aucun membre vivant) ;
  2. le **miroir est découvert et saisi** par plus puissant — la peur qui justifie tout le secret du roman ;
- les autres pistes (durée, domination de Linxi, voie alternative) deviennent des **jalons des Annales** plutôt que des fins.
**Décision à prendre par l'utilisateur.**

---

## 12. Chronologie (renommée)

| Époque | Événements |
|---|---|
| **Antiquité** | Les trois Seigneurs Immortels des Trois Profondeurs descendent et répandent leurs enseignements. · Le **Vrai Serpent-Dragon céleste**, au sommet de l'Embryon du Dao, tente de combiner les Réalisations de l'**Eau Rassemblée** et de l'**Eau Muable** pour devenir un véritable dragon. · Il divise son noyau d'or en **neuf parties** : chacune devient un Vrai Monarque, les **Neuf Fils**. · Il périt ; naissent serpents et dragons à plumes. · Création du **Palais des Dragons des Quatre Mers**. |
| **Ère inconnue** | Des neuf fils, **seuls deux subsistent** après les complots conjoints des Embryons du Dao immortels et démons ; la **Mer du Nord** est perdue au profit d'autres factions. · L'**Empereur Zhen Guangling** fonde l'**Empire Zhen** au nord. · L'**Empereur Martial Zhen** mène campagne vers le sud, traverse le **Marais Jingshui** et y laisse peut-être une lignée : la future **famille Mo du Lac Jingshui**. · La **famille Qu** s'établit en Mer Orientale (branche directe des Zhen). · Création du **Manoir de l'Aube d'Argent**. |
| **~1600 ans** | **Le Grand Renversement** / la grande bataille du **Gué de Lingjin**. La Voie Céleste rencontre des difficultés ; les **Trois Désastres et les Neuf Tribulations** se dissipent. La mer baisse de trois *cun*, la terre s'élève de neuf *cun*. Le monde est **réduit au dixième** de sa taille ; de nombreuses lignées Dao disparaissent ou s'affaiblissent. Les Embryons du Dao et les vénérables partent au Ciel Extérieur et disparaissent. · **Effondrement de l'Empire Zhen.** |
| (suite) | La **Dynastie Xuan du Nord** remplace l'Empire Zhen. · **Zhen Xunshi** lève une armée rebelle (avec des Vrais Monarques et un Maître taoïste) : échec. · L'**État de Rui** remplace la Dynastie Xuan. · La **famille Gu** quitte le Temple Lianhuo et s'installe dans la Plaine des Roseaux Gris. |
| **~1000 ans** | **Mort du Monarque Démon** : l'**Immortel Shuangji** (Manoir de l'Aube d'Argent) le capture sur l'**Île aux Hérons Morts**. Le Monarque cultivant l'**Éveil du Yang Mineur**, il est indestructible : Shuangji le **scinde en trois**, qui deviennent l'**Impératrice Hanlu** (Glaces du Nord), le **Seigneur Démon Gu'e** (Steppes de l'Ouest) et le **Roi-Dragon Changyong** (Mer Orientale). · Fondation de la **Secte Songhe**. · Effondrement de l'État de Rui : **Yuwen Duo** tue **Ao Lin**, 7e fils du Serpent-Dragon, pour forger la **Perle Abyssale des Marées** et la **Pilule-Trésor du Rempart d'Eau** ; mort de Yuwen Duo ; Rui détruit par le père de l'Empereur de Kun (**Duan Qishan**) et le roi de Yan (**Tuoxi Hong**) ; **l'Empire de Kun** remplace Rui ; la famille Yuwen se retire au **Jardin des Mille Délices**. |
| (Fruitions) | L'héritage de la **Culture de Linxi** est saisi par le **Vrai Monarque Qinghe** à la chute de Rui. · La figure du **Jade Premier** renonce à sa Réalisation et s'en va. · Le maître de l'**Élixir Parfait** est grièvement blessé et tué par **Ao Ming** et **Ao Xi**. |
| **~700 ans** | **Duan Qishan** règne sur Kun et fait coexister en équilibre les Dao taoïste, démoniaque et bouddhiste. · Catastrophe : le ciel s'assombrit et se teinte de rouge sang. · Duan Qishan périt dans la **préfecture de Hongcheng**. · Le prince **Duan Lihua** monte sur le trône et accueille **sept statues sacrées** : les **Sept Aspects du Dharma**. |
| **~600 ans** | La Réalisation de l'**Eau Nourricière** devient **vacante** ; le **Marais Jingshui devient le Lac Jingshui**. · **Trois Vrais Monarques** périssent. · Le **Manoir de l'Aube d'Argent** est détruit. · Le **Mont Xiaoyun** commence à convoiter la Réalisation du **Yang Lumineux**. |
| **~500 ans** | Formation des **Douze Portes** dans le Royaume de Linxi. |
| **~400 ans** | **Mo Qianshui** est pris en embuscade et tué par les Douze Portes. · On demande à la **Renarde des Monts Qingyan** de protéger les **Mo** au Lac Jingshui. · Il reste **trois sectes et huit portes**. |
| **~200 ans** | **Bai Chengyu** devient cultivateur du Manoir Pourpre. · **Ruan Xianzhou** est piégé et tué par Bai Chengyu avec les sectes et les portes. |
| **~50 ans** | **Mo Wei** s'engage dans l'armée. |
| **An 0** | **Mo Jian trouve le miroir immortel.** → début du jeu. |

🔎 La source place l'Empire Zhen et la famille Qu dans une « Ère » non datée **avant** le cataclysme de ~1600 ans ; on garde cet ordre.
🔎 Le cataclysme : la chronologie dit « la mer baisse de trois *cun*, la terre s'élève de neuf *cun* » ; la page du Ciel Extérieur dit « le ciel s'abaisse de trois *zhang*, la terre s'élève de neuf *chi* ». On garde la chronologie.

### 12.1 Compléments historiques (📚)
- **Chaînes dynastiques** — Nord : Xia → Zhou → **Zhen** → **Xuan du Nord** → **Rui** → **Kun** / Yan. Sud : Xia → Zhou → Chu → Chu / Ning → **Hanshan / Linxi / Tai** → Song / Shu. (Xia, Zhou, Chu, Ning, Song, Shu, Yan : noms dynastiques historiques génériques, conservés.)
- **L'Empire Zhen et le Yang Lumineux** : l'empire était une extension de la **Profondeur Englobante** ; son fondateur, l'Empereur Zhen Guangling (Noyau d'Or à l'apogée, Réalisation du Yang Lumineux), a fondé le Manoir immortel de Chenguang. Après sa mort soudaine, lui et sa Fruition ont été **scellés dans les Neuf Enfers** par l'Embryon du Dao du Mont Xiaoyun. Ses parents occupaient des **Surplus** du Yang Lumineux (dont le Vrai Monarque Gaoyao, de la famille Qu) ; des empereurs tardifs n'étaient que de **Faux Noyaux d'Or** ; six rois fondateurs au Manoir Pourpre sont tombés l'un après l'autre. Le rebelle Zhen Xunshi, tué par l'empereur des Xuan, a eu l'âme emprisonnée dans l'**Abîme des Tombes**. Un prince a perfectionné un sutra permettant de cultiver *Clarté sur le Monde* **sans position correspondante** (Voie Impériale). Après la chute des Zhen, un de leurs Manoirs Pourpres a fondé la **Secte Songhe** à Linxi.
- **Le clan des dragons** : à son apogée, **plus de dix Rois-Dragons** ; les Neuf Fils assiégeaient des Vrais Monarques et harcelaient des héritiers impériaux. Luttes internes et manque d'Embryons du Dao, puis la répression des immortels et des démons, les ont réduits : seuls deux fils restent, dont un disparu ; le **Roi-Dragon Beixun** s'est réfugié à l'est pour cultiver le tonnerre ; lui et Changyong ne survivent que parce qu'on ne peut pas les tuer.
- **Le Monarque Diable Kuaishan** : Noyau d'Or qui détenait la Réalisation du **Yang Mineur**, donc **immortel et indestructible**. L'Immortel Shuangji l'a forcé à se réincarner et l'a scindé en trois avec une **épingle de jade blanc** ; l'Impératrice Hanlu et le Seigneur Démon Gu'e sont des Noyaux d'Or ; le troisième fragment, tombé au fond de la Mer Orientale, a été repris par les dragons (Roi-Dragon Changyong). **Premier cas connu d'une Fruition scindée en plusieurs Noyaux d'Or.**
- **Les Trois Profondeurs** viennent toutes du **Temple du Commencement Droit** :
  - **Profondeur Céladon** : le **Dao humain** ; autorité sur le soleil, la lune, le Yin et le Yang ; renommée pour **éviter les calamités** et défier le Dao Céleste ; théorie des positions cardinales Yin-Yang (la quête de l'or révère soit le Yin Suprême par la profondeur divine, soit le Yang Suprême par des actes grandioses). Traditions : Splendeur d'Orient, Splendeur du Palais, Splendeur de la Flamme. **Déclin** : ses disciples se sont entre-déchirés sur la question « les Immortels ont-ils le droit de tuer ou d'asservir les humains ? ». Son Maître avait prédit la stagnation du Ciel et de la Terre.
  - **Profondeur Englobante** : **gouverner le Ciel**, commander les dieux, juger les démons ; Grande Divination, Tonnerre du Dao Céleste, Encens du Dao Divin, Autorité Impériale ; a fondé le **Palais du Tonnerre** ; « trois Monarques et cinq Immortels » à son zénith. Déclin : « le Sheji a trahi le Dao ».
  - **Profondeur Pénétrante** : révère le soi. Sa **Tradition du Trésor Numineux** (fondée par Wen Xiang, lignées de la Terre et Feu Ardent) garde, scellée, la Grotte Céleste du Ciel des Mille Prospérités ; l'un de ses membres a **renversé le Palais du Tonnerre**.
- **Le Monde Souterrain** (*Tradition de l'Attente de la Clarté*, Profondeur Englobante) : fondé par le **Marquis de la Nuit**, « Gouverneur des Âmes » ; il réside dans le **Qi Exilé**, tient les **registres des vivants**, gère la réincarnation et **réclame tous les Démons d'Essence Métallique** et les morts. Lignées : Qi Exilé, Grand Chaman, Qi Véritable, Rite Inférieur, Chouette des Seuils (soupçonnée), Régent Céleste (« en garde »). Il contrôle en sous-main la dynastie mortelle du **Grand Song**, et contrôlait autrefois Linxi. On ignore si son Embryon du Dao existe encore.
- 📚 L'histoire du roman en était à **l'an 136** du miroir en novembre 2025.
🎮 Phase 6 : introduction (an 0), faits historiques exposés par le miroir et les événements scénarisés, état initial des Fruitions (§6.8), phénomènes régionaux hérités (le Lac Jingshui est né quand la Réalisation de l'Eau Nourricière est devenue vacante → 💡 bonus de cultivation d'Eau au lac ?).
⚠️ `ClanManager` : patriarche de 45 ans ; s'il s'est engagé dans l'armée il y a ~50 ans, il doit avoir ~65-70 ans (à ajuster en phase 6).

---

## 13. Lexique de renommage (⚠️ document interne — seul endroit où figurent les noms d'origine)

> **À valider par l'utilisateur.** Colonne de gauche : nom de la source. Colonne de droite : nom proposé pour le jeu.

### 13.1 Clan du joueur et personnages du départ
| Source | Jeu |
|---|---|
| Famille Li (du Lac du Regard Lunaire) | **Clan Mo** (墨, « Encre ») |
| Li Xiangping (trouve le miroir, an 0) | **Mo Jian** |
| Li Mutian (engagé dans l'armée) | **Mo Wei** |
| Li Jiangqian (tué par les Douze Portes) | **Mo Qianshui** |
| Renard du Mont Dali | **Renarde des Monts Qingyan** |

### 13.2 États et régions
| Source | Jeu |
|---|---|
| État de Yue (越国) | Royaume de Linxi |
| État de Zhao (赵国) | Empire de Kun |
| État de Xu (徐国) · Mont Tangdao | Principauté de Tai · Mont Jianfeng |
| État de Wu (吴国) | Royaume de Hanshan |
| État de Chen (陈) | Duché de Pei |
| États chamaniques du Jiangnan | Terres chamaniques de Nanwu |
| État de Wei (Nord, ancien) | Empire Zhen |
| État de Qi du Nord | Dynastie Xuan du Nord |
| État de Liang | État de Rui |
| Roi Yan (Murong Dechang) | Roi de Yan (État historique générique, conservé) → **Tuoxi Hong** |
| Lac / Marais du Regard Lunaire | Lac / Marais Jingshui |
| Mont Dali / Grande Montagne Mi (大黎山) | Monts Qingyan |
| Mont Xiping | Mont Fengxi |
| Préfecture de Milin | Préfecture de Wuyang |
| Préfecture de Lixia | Préfecture de Heshan |
| Préfecture de Libu | Préfecture de Qianshi |
| Wilderness (荒野) | Terres Désolées du Nord |
| Plaine de la Montagne Qiong | Plaine de Guling |
| Préfecture de Shanji | Préfecture de Yunmen |
| Préfecture de Jiachuan | Préfecture de Zhanghe |
| Préfecture de Yufu | Préfecture de Qingyuan |
| Préfecture de Lin'an | Préfecture de Pingjiang |
| Mont Azure Pond · Mont Quanwu | Mont Yunfeng · Mont Tingsong |
| Préfecture de Simon · de Linghai | Préfecture de Siwei · de Haiyan |
| Mont Dongli · Secte Dongli | Mont Songhe · Secte Songhe |
| Plaine de la Forêt des Champignons | Plaine des Roseaux Gris |
| Demon Den (妖洞) | Gouffre des Bêtes |
| Préfecture de Helin · de Cangwu | Préfecture de Luoping · de Baishi |
| Préfecture de Hengdong | Préfecture de Nanling |
| Préfecture de Tongmo | Préfecture de Xiliu |
| Hua Kuang Sanlang | Trois Commanderies de Liuhe |
| Désert de Smoke Valley | Désert des Sables Chantants |
| Beiming · Grandes Plaines de l'Ouest | Glaces du Nord · Steppes de l'Ouest |
| Île du Roseau Fendu | Île aux Hérons Morts |
| Gué de Pingming | Gué de Lingjin |
| Préfecture de Yincheng | Préfecture de Hongcheng |
| Paradis de Shengle | Jardin des Mille Délices |
| Temple de Yanyang | Temple Lianhuo |
| Mont Luoxia | Mont Xiaoyun |

### 13.3 Sectes, portes, ordres
| Source | Jeu |
|---|---|
| Secte de l'Étang d'Azur (青池) | Secte du Pic des Nuées |
| Secte de la Plume d'Or (金羽) | Secte des Mille Lames |
| Secte Yue Cultivating (修越) | Secte de la Lune Pâle |
| Porte Tang Dorée (镗金) | Porte du Fer Ardent |
| Profound Peak Gate (玄岳) | Porte du Roc Obscur |
| Snow Ji Gate (雪冀) | Porte du Givre Blanc |
| Wanyu Sword Gate (万昱剑门) | Porte de l'Épée Stellaire |
| Purple Smoke Gate (紫烟) | Porte des Brumes d'Ambre |
| Xiukui Dao Gate (鸺葵) | Porte du Chrysanthème Noir |
| Changxiao Gate (长宵) | Porte des Saules Pleureurs |
| Hengzhu Dao Gate (衡祝) | Porte du Tambour Sacré |
| Île immortelle du Récif Cramoisi (赤礁) | Atoll des Perles Noires |
| Manoir immortel de Zhaoyuan | Manoir immortel de Chenguang |
| Manoir de l'Origine du Clair de Lune | Manoir de l'Aube d'Argent |

### 13.4 Familles
| Source | Jeu |
|---|---|
| Xiao | Ruan |
| Yuan | Gu |
| Chi | Bai |
| Ning | Zang |
| Ji | Lü |
| Fei | Tao |
| Yu (ennemie des Fei) | Lou |
| Lu | Xun |
| Shen | Qiao |
| An | Fang |
| Cui | Qu |
| Tuoba | Yuwen |

### 13.5 Personnages
| Source | Jeu |
|---|---|
| Li Qianyuan | Empereur Zhen Guangling |
| Empereur Wu (campagne du sud) | Empereur Martial Zhen |
| Li Xunquan | Zhen Xunshi |
| Luoxia | Immortelle Xiaoyun |
| Yingze | Immortel Shuangji |
| Impératrice Beiyao · Seigneur Démon Xiyan · Monarque Dragon Xiyang | Impératrice Hanlu · Seigneur Démon Gu'e · Roi-Dragon Changyong |
| Tuoba Xuantan (Empereur Taiwu de Liang) | Yuwen Duo |
| Dongfang Tianye (7e fils) | Ao Lin |
| Dongfang Weiming · Dongfang Weixi | Ao Ming · Ao Xi |
| Fu Qiyan (Empereur Zhaowu) | Duan Qishan (Empereur Shengwu) |
| Murong Dechang | Tuoxi Hong |
| Fu Qidangle | Duan Lihua |
| Taiyue (Vrai Monarque) | Vrai Monarque Qinghe |
| Chi Wei | Bai Chengyu |
| Xiao Xianyou | Ruan Xianzhou |
| Duanmu Kui | Gongye Shu |
| Lu Jiangxian · Chi Buzi | Weng Liuxian · Bai Ruozhi (📚 dans le roman, Lu Jiangxian est l'esprit du miroir, donc le **rôle du joueur** : l'exemple d'Intercalaire est attribué à une figure légendaire pour ne pas dicter les actes du joueur) |
| Wang Xun | Ze Wuyan |
| Li Tongya | Qiu Mingshan |
| Li Chengliao · Li Qinghong · Li Xuanfeng | Hou Lie · Nian Suo · Ban Jinhe |
| Tang Shedu · Yu Yufeng | Pan Yiqiu · Rong Shuang |
| Li Wushao · Li Quewan (capacité) | Lan Guyu · (capacité *Attente du Rare Présage*) |
| Guangchan | Shi Kongming |
| Li Zhouwei | Kuang Yao |
| Kongheng | Moine Wuyi |
| Li Yuanjiao | un cultivateur de la côte orientale |
| Dongjun · Qingeng | Yinjun · Taichen |
| Vrai Dragon sans cornes | Vrai Serpent-Dragon céleste |

### 13.6 Chakras, techniques, objets, termes propres au roman
| Source | Jeu |
|---|---|
| Chakras : Paysage Profond · Attrait Radieux · Tourbillon Céleste · Essence Azur · Capitale de Jade · Esprit Naissant | Lac Intérieur · Marée Respirante · Roue des Méridiens · Sève d'Émeraude · Œil du Sommet · Premier Souffle |
| Graines de Talisman · Perle Profonde (graine du miroir) | Graines de Sceau · Perle Profonde (conservé) |
| Lu Jiangxian (esprit du miroir, « Immortal Mirror ») | **l'Esprit du Miroir** (le joueur ; pas de nom imposé) |
| Supreme Yin Moonlight · Supreme Yin Profound Light | Clair de Lune du Yin Suprême · Lumière Profonde du Yin Suprême (descriptifs, conservés) |
| Talisman Qi (Evade Death Prolong Life, Strength to Move Mountains…) | Qi de talisman, noms traduits et conservés (§11.5, D6) |
| Méthode Profonde de Continuation | Méthode de la Greffe du Dao |
| Lutte pour les Cinq Formes | Reprise de la Fruition (« Lutte des Cinq Visages ») |
| Jade Tissé / Port Sacré (forces pseudo-Noyau d'Or) | Sceau de Jade |
| Changement Geng-Dui | Basculement Geng-Dui (termes trigrammes, conservés) |
| Talisman chamanique (Dao) | Sceau chamanique |
| Hibou Fusionnant · Jade Véritable · Chaman Suprême | Chouette des Seuils · Jade Premier · Grand Chaman |
| Manifestation Azur · Lueur Céleste · Changgeng | Manifestation Céladon · Lueur de l'Aube · Étoile du Soir |
| Garde de la capitale · Pilule Entière · Gouvernant du Ciel · Cultiver Yue | Sentinelle de la Cité · Élixir Parfait · Régent Céleste · Culture de Linxi |
| Essence Verte d'Origine Suprême Dorée | Or Vert d'Origine |
| Mercure blanc secret (fondation et technique) | Perle Grise secrète · Art secret de la Perle Grise |
| Perle Profonde de Hehou · Pilule au Trésor du Mur d'Eau | Perle Abyssale des Marées · Pilule-Trésor du Rempart d'Eau |
| Tempête Ascendante des Eaux Tombantes | Tempête des Eaux Renversées |
| Roi Dragon Jing | Roi-Serpent des Eaux Noires |
| Cinq Vertus : Eau de fosse, convergente, du Manoir, pure, de la vallée ; Feu Li, fusionnant, véritable, sec, viril ; Bois vertical/orthodoxe, convergent, préservé, transformé, de corne ; Terre stable (Gen), revenante, majestueuse (Wu), manifestée, au trésor ; Métal (Geng), unifié, libéré, de la voûte ; Dui ; Joie | Nommage systématique « Élément + manifestation » (§6.3) |

Techniques (§2.4) :
| Source | Jeu |
|---|---|
| Intuition de Taihua | Intuition du Lotus Blanc |
| Qi spirituel mineur / Technique mineure d'essence spirituelle | Méthode du Souffle Commun |
| Technique des Six Barrages du Nuage Azur (Yuan) | Canon des Sept Terrasses (Gu) |
| Réponses de Duanmu Kui aux questions d'un mendiant sous le mûrier | Dialogue de Gongye Shu avec le Pêcheur du Saule |
| Sutra de nutrition du méridien de respiration du Yin Suprême | Sutra de la respiration du Yin Suprême |
| La longue marche de Pheasant Flame | Pas du Phénix de Braise |
| Technique Qi de la rivière One | Sutra de la Source Claire |
| Réflexion automnale du lac de la Lune | Canon du Givre Nocturne |
| Sutra du nuage sombre flottant et docile | Sutra de la Marée Silencieuse |
| Recherche de la source de la rivière Azure | Méthode du Ruisseau Remonté |
| Sutra de la Prononciation Céleste | Sutra du Verbe Premier |
| Technique de contrôle de la pluie Clear Essence | Méthode de l'Averse Mesurée |
| Technique des cieux élevés de la pierre dorée | Méthode du Rempart d'Airain |
| Technique du bord lumineux doré | Méthode du Tranchant Clair |
| Sutra du Cœur en Brocart Long | Sutra du Cœur Tissé |
| Technique de l'esprit clair de Cave Spring | Méthode de la Source Souterraine |
| Manuel d'origine radieuse du Palais d'Or | Manuel du Soleil Intérieur |
| Technique de fusion profonde des flammes | Art du Brasier Englouti |
| Technique secrète d'origine du Tonnerre Pourpre | Art secret de l'Éclair Cendré |
| Technique de collecte de la rosée de l'aube | Méthode de la Perle de Rosée |
| Technique du Tonnerre des Nuages Célestes | Art du Grondement Lointain |
| Technique du talisman en bois Yi | Méthode de l'Écorce Scellée |
| Technique de brume de montagne et de mer | Méthode des Vapeurs du Littoral |
| Technique du vent en forêt de pins | Art du Murmure des Cèdres |
| Technique de neige de rosée de pin froide | Méthode de la Bise Hivernale |
| Gardien du Chemin Courtois | Veilleur du Sentier |
| Technique tout-en-un des six Yi | Méthode des Six Harmonies |
| Technique de l'Essence Spirituelle Pure | Méthode du Qi Limpide |

Fondations et capacités divines (§6.5-6.7) :
| Source | Jeu |
|---|---|
| Porter des Écritures sacrées · Épingle à cheveux fendant le soleil / Épingle Yang divisée | Gardien du Canon Solaire · Lance du Zénith |
| Six Essences Suprêmes du Soleil Yang | Essence du Zénith Sextuple |
| Porte de vénération du Ciel · Pointe de flèche cramoisie / Flèche Pourpre Tranchée · Origine de l'observation impériale · Souverain en danger / Péril des Marches Souveraines · Illumination sous le ciel | Arche du Salut au Ciel · Dard Vermillon · Regard du Trône · Monarque Assiégé · Clarté sur le Monde |
| Essence de l'Union Impériale Yang Lumineuse · Passe d'origine radieuse · Cœur brillant et clair | Essence du Sceptre Lumineux · Passe de l'Aube Première · Cœur sans Ombre |
| Lac Moon Automne / L'automne du lac Moon · Grande Réalisation Primordiale · Rite et Ombre · L'éclat tissé du jade | Lune Noyée · Accomplissement Originel · Liturgie Obscure · Soie de Lune |
| Faisan Li · Feu qui embrasse le ciel · Dieu qui gouverne la vie | Faisan de Braise · Incendie Céleste · Dieu du Foyer |
| Feu démoniaque en fusion · Aspiration de la flamme du cœur | Flamme Démone · Soupir du Cœur Ardent |
| Eau de fosse sexagénaire Mille essences lentes | Essence des Mille Gouttes Patientes |
| Océan sans limites · Homme sur le ruisseau · Nuages qui s'assombrissent · Tenir la crête · Départ du fleuve en deuil · Position découlant du péril | Mer sans Rivage · Veilleur du Gué · Ciel d'Orage · Garde de la Digue · Adieu au Fleuve · Refuge du Péril |
| Là où les courants reviennent · Prophétie imminente · Tous retournent ensemble · Sage de la Règle Universelle · Rivière démoniaque souillée | Confluence Ultime · Présage de la Crue · Retour à la Mer · Sage des Flots Égaux · Fleuve Corrompu |
| Pluie froide du matin | Bruine Hivernale |
| Essence talismanique d'origine eau pure | Essence du Sceau Limpide |
| Écho printanier / Écho de Cave Spring · Épaisse brume / Obscurité épaisse · Pluie claire au crépuscule · Forme cachée de Chougui · Rosée purificatrice | Chant de la Source Enfouie · Voile de Brouillard · Averse du Soir · Silhouette sous l'Onde · Rosée Lustrale |
| Manifestation de la Crête d'Azur | Manifestation de la Crête Céladon |
| L'aube sans flatterie · Printemps de la renaissance · Convergence des vallées · Accéder au dépôt Profond | Aurore Sincère · Source Renaissante · Rencontre des Vallons · Porte du Trésor Profond |
| Pilier d'argent du Trésor · Grotte de métal en fusion | Colonne d'Argent · Forge Souterraine |
| Pierre gravée · Casque doré céleste | Stèle Gravée · Heaume de l'Aurore |
| Cœur à ouverture dorée | Cœur d'Or Éclos |
| Voyage rebelle vers le nord · Position découlant de la focalisation · Les mots et la perception cachée · Principe de transformation du bois | Marche contre le Vent du Nord · Ancrage du Regard · Parole Voilée · Loi de la Sève |
| Péril Vie prolongée · Faucon cherche un perchoir · Cueillette d'herbes amères · Forêt de l'Absurdité Délirante | Longévité par le Péril · Aigle sans Perchoir · Cueillette Amère · Forêt du Délire |
| Bois de Lingyun · Intégrité du bois Yi | Bois des Nuées Hautes · Droiture de l'Écorce |
| Essence tentaculaire de la montagne divine du barrage d'Azur · Chèvre profonde · Manifestation de l'emblème d'Azur · Montagne d'Azur tentaculaire · Rocher tout-puissant · Observateur des Abysses · Essence du Yang inférieur | Essence des Monts Enchaînés · Bélier des Profondeurs · Emblème Céladon · Chaîne des Monts · Roc Souverain · Guetteur de l'Abîme · Essence du Yang Mineur |
| La poursuite de la montagne du fou | Course du Fou vers la Cime |
| Disposition des rouleaux violets · Maître nourricier de vie · Culture de l'étoile terrestre · Vase précieux et chaleureux · Contourner la montagne Est | Ordonnance des Rouleaux Pourpres · Maître Nourricier de Vie · Culture de l'Étoile Terrestre · Vase Tiède et Précieux · Détour par le Mont d'Orient |
| Splendeur sans fin · Soirée de suppléance · Chaleur étouffante et amère · Pavillon des Bénédictions Accordées · Discussion de huit exemptions | Splendeur sans Fin · Veillée de Relève · Chaleur Amère et Étouffante · Pavillon des Grâces · Débat des Huit Exemptions |
| Approchez-vous de l'Abîme de Yu · Vaisseau abyssal caché | Approche du Gouffre du Couchant · Vaisseau Abyssal Caché |
| Nuages de Qi de bon augure · Rouleau des réalisations heureuses | Nuées Fastes · Rouleau des Heureux Accomplissements |
| Neige sur les pins · Audition claire · Eau froide Ni | Givre sur les Cèdres · Ouïe Claire · Eau Froide du Nord |
| Essence de démon divin Tianwu True Qi · Sommeil de l'Étreinte de Pierre | Essence du Démon Divin des Cinq Cieux · Sommeil de la Pierre Étreinte |
| Bassin de tonnerre profond · Tonnerre de Lightfall · Réveil du printemps | Bassin d'Orage · Foudre Descendante · Premier Grondement du Printemps |
| Culture de la transcendance · Forme en transition · Usurpation chaotique · Rupture des liens | Culture de la Transcendance · Forme en Transition · Usurpation Chaotique · Rupture des Liens (descriptifs, conservés) |
| Montagne de la Plume Orientale · Âmes descendantes · Eaux douloureuses du Sud · Plateau du Ciel Occidental · Cour du désert du Nord | Plume d'Orient · Âmes Descendantes · Eaux Douloureuses du Sud · Plateau du Ciel d'Occident · Cour du Désert du Nord |
| Cultiver une harmonie parfaite · Humidification du plomb radiant · En attente du Divin Suprême · Séquence du Livre d'Or | Harmonie Parfaite · Humectation du Plomb Radieux · Attente du Divin Suprême · Séquence du Livre d'Or |
| Équilibre profond du Dipper · Écoutez les étoiles qui s'éveillent · Arrangement divin · Expansion continue · Supervision de la Loi Divine | Équilibre Profond de la Grande Ourse · Écoute des Étoiles qui s'Éveillent · Arrangement Divin · Expansion Continue · Surveillance de la Loi Divine |
| Essence du Vide Unifiée Véritable Jade · Cour de Jade Général / Jadeborn One · Brocart à motifs · Falaise de Jade Azur/vert · Unification du vrai Dao · Disque de jade blanc | Essence du Vide Unifié du Jade Premier · Cour du Jade Général / Né du Jade · Brocart Figuré · Falaise de Jade Vert · Unification du Vrai Dao · Disque de Jade Blanc |
| Essence de sang des Trois Neuf du Chaman Suprême · Fantôme de l'ombre du criquet · Présence indétectable · Bénédiction du chaman de la Terre · Mandat de l'Empereur · Buveur de sang | Essence de Sang des Trois Neuf · Ombre de la Sauterelle · Présence Indécelable · Bénédiction du Chaman de la Terre · Mandat de l'Empereur · Buveur de Sang |
| Brume de l'Aube Universelle · Ouragan Ghost Yin · Retournement du Yin de la Brume · Épine de la Porte des Tombes | Brume de l'Aube Universelle · Ouragan du Yin Spectral · Brume du Yin Retourné · Épine de la Porte des Tombes (descriptifs, conservés) |

### 13.7 Ajouts du 2026-09-24 tirés du wiki (✏️ à valider)
| Source | Jeu |
|---|---|
| Azure Profundity · Encompassing Profundity · Comprehending Profundity | Profondeur Céladon · Profondeur Englobante · Profondeur Pénétrante |
| Azure Profundity Master (titre du miroir) | Maître de la Profondeur Céladon |
| Cultivation Transcendence (修越) = Cultiver Yue | Culture de Linxi (même Fruition) |
| Changgeng (Fruition) · Changgeng (Sword Ancestor) | Voie de l'Épée, alias Étoile du Soir · l'Ancêtre de l'Épée |
| Body of Kui · Balanced Ritual · Azure Proclamation | Corps de Kui (Kui : bête mythique traditionnelle) · Rite Équilibré · Proclamation Céladon |
| Brilliant Qi · Baleful Qi · Profound Qi · Upper Rite · Lower Rite | Qi Brillant · Qi Funeste · Qi Profond · Rite Supérieur · Rite Inférieur (descriptifs) |
| Wandering Metal · Harmonizing Metal | Métal Errant · Métal Harmonieux (descriptifs) |
| Shuyang (bête, ancien détenteur du Yang Lumineux) | Hongyao |
| Bright King Shengming · White Cicada · True Monarch Zhaoyuan · True Monarch Shangyao · White Qilin | Roi Lumineux Xiaoming · Cigale Blanche · Vrai Monarque Chenguang · Vrai Monarque Gaoyao · Qilin Blanc |
| Everbright Platform · Attending Worldly Post · Oath Xiao Body | Estrade Toujours Claire · Office du Siècle · Corps du Serment |
| Du Qing · Feathered Serpent · Dongfang Fengchi | Tan Qing · Serpent à Plumes · Ao Feng |
| Dongfang Rijiu · Dongfang Yuezhu · Dongfang Zaishi | Ao Ri · Ao Yue · Ao Zai |
| Overturning of All Under Heaven · Pristine Terrace Heart-Awakening Sword | Renversement du Monde · Épée de l'Éveil du Cœur |
| Xuan'nu · True Monarch Tianyi · Illuminating Immature Embryo | Dame Yinlan · Vrai Monarque Baiyi · Veille sur l'Embryon |
| Yu (Eau de fosse) · True Monarch Cejin · Daling Kingdom · True Monarch Xuancang | Yu 🔎 (peut-être Yu le Grand, mythologique) · Vrai Monarque Cehe · Royaume de Luoling · Vrai Monarque Shencang |
| Merging Dawn Abyss · Dwelling in Winter's Depth | Abysse de l'Aurore · Retraite Hivernale |
| Qingmei · Connate Profound Cycle Fire Virtue True Monarch | Danmei · Vrai Monarque du Cycle Inné de la Vertu du Feu |
| Vault-Beam Silver · Profound-Lock Daoist Metal Qi · Unified-Vault Lock · Profound Vault | Fils d'Argent du Trésor · Qi Métallique du Verrou Profond · Verrou du Trésor Unifié · Trésor Profond |
| Wang Family (Wandering Metal) | famille Lian |
| Sima Family · Yuanxiu · Mountains Gaze South Classic | famille Huyan · Maître taoïste Yuanshu · Classique des Monts qui Regardent le Sud |
| Position Follows Exclusivity · Back Facing South · Wood Forms Square · Journeying North | Refuge de l'Unicité · Dos au Midi · Le Bois Fait l'Équerre · (= Marche contre le Vent du Nord) |
| Xu Xiang · Myriad-Prosperity Heaven | Wen Xiang · Ciel des Mille Prospérités |
| Floating Cloud Body · Pure Primordial Wind | Corps de Nuée Flottante · Vent Primordial Pur (descriptifs) |
| Omen of the Dao's Origin | Présage de l'Origine du Dao (descriptif) |
| Mount Qusi · Yin Clan (Dry Qi) · Diyan | Mont Qushan · clan Hui · Maître Diyun |
| Fading Sundown Abyss · Gully-Hiding Skiff · Song of the River of Forgetfulness · Night Marquis | Approche du Gouffre du Couchant (remplace « Gouffre de Ning ») · Barque du Ravin · Chant du Fleuve d'Oubli · Marquis de la Nuit |
| Zhidu · Baleful Comet Star · Xingbo star | Zhiyuan · Comète Funeste · étoile Xingbo (nom d'étoile traditionnel) |
| Pushing the Endless Derivations · Divine Sequence | Dérivations sans Fin · Séquence Divine |
| Ye Lineage (Capital Guard) · Regional Spirit Courts | lignée Hei · Cours des Esprits régionales |
| Sude · Grand Unity Pure Mercury | Suhe · Mercure Pur de la Grande Unité |
| Shangyuan · Jiang Qing | Vénérable Lingxu · Fu Qing |
| Nurturing Azure Heaven · Intent-Bearing Body · Xuanyang | Ciel Céladon Nourri · Corps Porteur d'Intention · Xuanfeng |
| Grand Divination Heavenly Element Book | Livre Céleste de la Grande Divination |
| Radiant Visage Immortals · Woven Jade Immortals · False Divine Core | Immortels au Visage Radieux · Immortels de Jade Tressé · Faux Noyau Divin |
| Revered One Suxikong | Vénérable Kongji |
| Numinous Treasure Dao Tradition | Tradition du Trésor Numineux |

### 13.8 Ajouts de la passe complète du wiki (✏️ à valider)
| Source | Jeu |
|---|---|
| Divine Dao · Divine Attendant · Divine Core · Imperial Path | Dao Divin · Serviteur Divin · Noyau Divin · Voie Impériale (descriptifs) |
| Monk · Master Monk · Merciful One · Maha · Dharma Master · Revered One | Moine · Maître Moine · Miséricordieux · Maha · Maître du Dharma · Vénérable (termes bouddhistes génériques) |
| Golden Lands of Suxikong · Seven Dharma Aspects (Wrath, Great Desire, Discipline, Adoration) | Terres d'Or du Vénérable Kongji · Sept Aspects (Colère, Grand Désir, Discipline, Adoration) |
| Diverse Mansion · Unified Furnace | Manoir Divers · Fournaise Unifiée |
| Upright Commencement Temple · Donghua · Gonghua · Yanhua | Temple du Commencement Droit · Splendeur d'Orient · Splendeur du Palais · Splendeur de la Flamme |
| Awaiting Clarity · Governor of Souls · Nether Land · Great Song | Tradition de l'Attente de la Clarté · Gouverneur des Âmes · Terre des Ombres · Grand Song |
| Tongxuan Palace | Palais de la Pénétration |
| Kuaili (Devil Monarch) · Beijia (Dragon Monarch) | Monarque Diable Kuaishan · Roi-Dragon Beixun |
| Tomb Abyss · Refining Mountain · Nine Netherworlds | Abîme des Tombes · Mont du Raffinage · Neuf Enfers |
| Fated Ones · Green Pine Temple | Élus du Destin · Temple des Pins Verts (descriptifs) |
| Profound Balance Atmosphere · Falling Water Rising Storm · Upper Evil Spirit Storage | Atmosphère de l'Équilibre Profond · Eaux Tombantes, Tempête Montante · Grand Entrepôt des Esprits Funestes |
| Endless Void Abyss · Outer Heaven | Abîme du Vide sans Fin · Ciel Extérieur (descriptifs) |
| Lijing Village · Lijing Peak · Meiche Peak · Jingyang Village · Qingdu (island) · Immortal Lake | village de Jingxi · Pic de Jingxi · Pic Meiling · village de Qingyang · île de Cuidao · Lac Immortel |
| Capital Immortals Dao · Revering Radiance Gate · Profound Covenant Gate | Voie des Immortels de la Capitale · Porte de la Clarté Révérée · (= Porte du Roc Obscur) |
| Lotus Temple · Profound Wonder Temple · Great Xiukiu Monastery | Temple du Lotus · Temple de la Merveille Profonde · Grand Monastère de Huiqiu |
| Slaughter Jun Gate · Sacred Writ Gate | Porte du Carnage · Porte de l'Écrit Sacré |
| Salt Lake · Chengshui Marsh · Mount Yebai · Yuchuan Sword Peak | Lac Salé · Marais de Qingshui · Mont Baiye · Pic de l'Épée Stellaire |
| Guyan Great Desert · Mount Kuijun · Tiger's Frontier | Grand Désert de Kuyan · Mont Kuiyun · Frontière du Tigre |
| Shitang · Converging Heaven Sea · Vermillion Sea · Southern Borderlands | archipel de Shiyuan · Mer du Ciel Confluent · Mer Vermillon · Marches du Sud |
| Aged Courtly Path Sutra · Courtly Red Dust Qi · Courtly Frost Armor Qi | Sutra de l'Aîné qui Frappe à la Cour · Qi de la Poussière Rouge de la Cour · Qi de l'Armure de Givre de la Cour |
| Variant Goat Profound Qi · Dawn Colored Qi | Qi Profond du Bélier Variant · Qi Coloré de l'Aube |
| Monarch Fire of Cold and Dryness · Sunken Fragrance | Feu Monarque du Froid et de la Sécheresse · Parfum Englouti |
| Winter Thunderclap · Purple Gold Thunder Liquid · Thundercloud Temple | Coup de Tonnerre Hivernal · Liquide de Tonnerre Or-Pourpre · Temple des Nuées d'Orage |
| Profound Thunder · Primordial Magnetism | Tonnerre Profond · Magnétisme Primordial (descriptifs) |
| Radiant Fire · Merging Fire · Molten Fire · Blazing Fire · Virile Fire | Feu Radieux · Feu Fusionnant · Feu en Fusion · Feu Ardent · Feu Viril (descriptifs) |
| Preserving Wood · Transforming Wood · Returning Earth · Majestic Earth · Manifest Earth · Liberated Metal · Proclamation Earth | Bois Préservé · Bois Transformé · Terre du Retour · Terre Majestueuse · Terre Manifestée · Métal Libéré · Terre de la Proclamation (descriptifs) |

✅ D6 : les noms descriptifs (marqués « conservés ») restent tels quels : ils décrivent littéralement leur effet et relèvent du vocabulaire courant.

---

*Voir `NOT_DONE.md` (section « Restructuration lore ») pour les phases 1-6.*
