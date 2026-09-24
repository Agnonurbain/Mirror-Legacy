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
| *Sutra du Verbe Premier* | ? | 5-6 | Bai | Noyau d'Or ? |
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
| *Méthode de la Perle de Rosée* | ? | 4 | Secte du Pic des Nuées | Fondation |
| *Art du Grondement Lointain* | Tonnerre | 5+ | Qiao | Manoir Pourpre |
| *Méthode de l'Écorce Scellée* | Bois | 3+ | — | Fondation |
| *Méthode des Vapeurs du Littoral* | ? | 3+ | — | Fondation |
| *Art du Murmure des Cèdres* | Douze Essences | 5+ | Zang | Manoir Pourpre |
| *Méthode de la Bise Hivernale* | Douze Essences | 3 | Tao, Mo | Fondation |
| *Veilleur du Sentier* | Archaïque | 2 | Fang | Fondation |
| *Méthode des Six Harmonies* | — | — | Mer Orientale | — |
| *Méthode du Qi Limpide* | Douze Essences | 2 | — | Culture du Qi |

🔎 Deux lignes semblent contredire la règle des grades : *Canon des Sept Terrasses* (grade 6 → « Manoir Pourpre » et non Noyau d'Or) et *Veilleur du Sentier* (grade 2 → « Fondation »). Interprétation retenue : la colonne « royaume suprême » indique le royaume **que la méthode couvre en entier**, le grade 6 donnant en plus la *possibilité de viser* le Noyau d'Or ; *Veilleur du Sentier* est une technique ancienne à grave inconvénient qui atteint la Fondation au prix d'un défaut (à définir en phase 3).

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

🔎 Correspondance méthode ↔ Fruition non donnée par la source. Proposition : Transformation ↔ *Chouette des Seuils*, Dissimulation ↔ *Jade Premier*, Sacrifice ↔ *Grand Chaman* (dont l'essence est une « essence de sang », §6.7).

🎮 Phase 4 : enum `CultivationPath` { Immortal, Devil, Buddhist, Demonic, Shamanic } + `CultivationSubPath` (9 valeurs ci-dessus). Clan du joueur : Immortal / Purple-Mansion-Golden-Core. Les factions portent leur voie ; la conversion (ex. vers le bouddhisme pour échapper à la mort, §5.4.3) est un événement.

---

## 4. Orifice spirituel, mortels et Graines de Sceau

- La plupart des gens ne sont **pas** destinés à cultiver : seuls **3 sur 1000** ont un **orifice spirituel**, un canal inné **détectable uniquement par un cultivateur confirmé**.
- Les rares **Graines de Sceau** contournent cette condition en greffant des voies spirituelles artificielles sur un mortel.
- 📚 Dans le roman, ces graines viennent **du miroir** (« Perles Profondes ») : c'est ainsi que la famille du miroir, sans orifice, a commencé à cultiver. Détail en §11.5.
- 🔎 Ni la source ni le wiki ne donnent l'espérance de vie d'un mortel. 💡 **Recommandation** : 60-80 ans (moyenne ~65), avec une variation individuelle ; un Qi de talisman peut l'allonger (*Prolonger la vie et accroître la longévité* : +40 ans, §11.5). À valider en phase 2.

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
- 4 stades : **initial, intermédiaire, tardif, apogée**. Chaque stade demande souvent **des décennies de retraite**.
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

Royaume de la culture et du raffinement des **pouvoirs divins** (dans l'Antiquité : **Raffinement Divin**). 4 stades (débutant, intermédiaire, avancé, apogée), **déterminés par le niveau des capacités divines**. On les affine soit en cultivant, soit en **absorbant son Partenaire Dao** — ce qui rend le chemin vers le Noyau d'Or inévitablement périlleux.

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

- Ancien nom : **Âme Naissante**. Ses cultivateurs sont appelés **Immortels (仙人)** : l'apogée de la cultivation.
- **Seuls deux** subsistent dans ce monde : l'**Immortelle Xiaoyun** et le **Monde Souterrain**. Ils ne périssent qu'à la fin des temps (soleil et lune disparus, monde éteint).
- Accessible **uniquement** depuis une **pleine Réalisation** (il faut modifier ou affirmer le Dao du monde entier). Un Surplus peut y parvenir par **Transfert** si la Réalisation est sans maître ; un Intercalaire par **Transformation** (exemple historique : le *Basculement Geng-Dui*).
- Avancement et voie à suivre : **inconnus**.

### 5.7 Immortel Doré (Seigneur Immortel)
- Aussi appelé **Grande Culmination**. Cultivateurs de premier ordre même dans l'Antiquité, au niveau d'immortels comme *Yinjun* et *Taichen*, qui ont reçu l'enseignement direct des maîtres des Trois Profondeurs.
- Invulnérables aux **Trois Calamités** du Dao Céleste. Autrefois « Seigneurs Immortels », aujourd'hui « Immortels Dorés ».

🎮 Royaumes 6-7 : états **d'aboutissement** (victoire narrative, §11.9) et figures du monde ; la source ne décrit pas leur progression interne → rien d'inventé tant que l'utilisateur ne le décide pas.

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

### 6.3 Les Cinq Manifestations (Cinq Vertus)
Chacun des Cinq Éléments a **cinq manifestations** : **orthodoxe** (正), **rassemblée** (收), **nourricière** (蕴), **muable** (变), **cachée** (藏). Chaque croisement élément × manifestation est une Fruition : **25 Fruitions**.

Renommage : on nomme ces Fruitions **« Élément + manifestation »** (descriptif, neutre), au lieu des noms du roman (§13.6).

| | Orthodoxe | Rassemblée | Nourricière | Muable | Cachée |
|---|---|---|---|---|---|
| **Bois** | Bois Orthodoxe | Bois Rassemblé | Bois Nourricier | Bois Muable | Bois Caché |
| **Feu** | Feu Orthodoxe | Feu Rassemblé | Feu Nourricier | Feu Muable | Feu Caché |
| **Terre** | Terre Orthodoxe | Terre Rassemblée | Terre Nourricière | Terre Muable | Terre Cachée |
| **Métal** | Métal Orthodoxe | Métal Rassemblé | Métal Nourricier | Métal Muable | Métal Caché |
| **Eau** | Eau Orthodoxe | Eau Rassemblée | Eau Nourricière | Eau Muable | Eau Cachée |

### 6.4 Groupes de Fruitions (vue complète)

| Groupe | Fruitions | Génération |
|---|---|---|
| **Deux Dualités** | Yang : Yang Suprême, Yang Lumineux, Yang Mineur · Yin : Yin Suprême, Yin Voilé, Yin Mineur | Fondatrice (Suprêmes) / interaction |
| **Cinq Vertus** | 25 Fruitions (§6.3) | Interaction |
| **Douze Qi** | Qi Pur (fondateur) + 11 : Qi Violet, Qi Sec, Qi Exilé, Qi Froid, Qi Faste, Qi Véritable, Qi Pourpre, Qi Radieux, Qi Illimité, + 2 non nommés | Fondatrice / interaction |
| **Trois Tonnerres** | Tonnerre Céleste + 2 non nommés | 🔎 non précisée |
| **Fusion Ancienne** | Fusion Ancienne, Sentinelle de la Cité, Élixir Parfait, Régent Céleste, Culture de Linxi, Manifestation Céladon (+ lignée de la Perle Grise) | Fondatrice (en partie) |
| **Trois Chamans** | Chouette des Seuils, Jade Premier, Grand Chaman | Fusion ancestrale |
| **Deux Rites** | 2 non nommées | 🔎 non précisée |
| **Attestation du Vide** | Voie de l'Épée, Manifestation Céladon, Lueur de l'Aube, Étoile du Soir | Attestation |

🔎 Le tableau source des lignées a perdu la plupart de ses noms (cellules « ↗ ») : seuls les **types** des capacités subsistent pour de nombreuses lignées. On garde ces **emplacements non nommés** avec leurs types (§6.7) ; ils seront nommés en phase 4 (création originale, à valider).

🔎 Les Fruitions des Cinq Vertus dont la source donne le nom d'origine sont rattachées à leur manifestation ainsi (d'après les caractères chinois et la liste des manifestations) : Eau de fosse → Eau Orthodoxe ; Eau convergente → Eau Rassemblée ; Eau du Manoir → Eau Nourricière ; Eau pure → Eau Muable ; Eau de la vallée → Eau Cachée ; Feu Li / rayonnant → Feu Orthodoxe ; Feu en fusion / fusionnant → Feu Rassemblé ; Feu véritable → Feu Nourricier ; Feu sec → Feu Muable ; Feu viril → Feu Caché ; Bois vertical → Bois Orthodoxe ; Ramasser du bois → Bois Rassemblé ; Terre de Wu → Terre Nourricière ; Métal Geng → Métal Muable ; Métal du coffre-fort / de la voûte → Métal Caché ; Or unifié → Métal Rassemblé ; Terre Gen → Terre Orthodoxe (« Terre stable »). Non placées : Métal Dui, Or de Joie.

### 6.5 Fondations immortelles nommées sans Fruition connue
La source cite aussi (tableau des Partenaires Dao) des fondations isolées, renommées : *Stèle Gravée*, *Heaume de l'Aurore* (Métal Muable) ; *Forge Souterraine* (Métal Caché) ; *Brume de l'Aube Universelle* ; *Cœur d'Or Éclos* ; *Nuées Fastes*, *Rouleau des Heureux Accomplissements* (Qi Faste) ; *Flamme Démone* (Feu Rassemblé) ; *Plume d'Orient* (Sentinelle de la Cité) ; *Passe de l'Aube Première*, *Cœur sans Ombre* (Yang Lumineux) ; *Givre sur les Cèdres*, *Ouïe Claire* (Qi Froid) ; *Bassin d'Orage*, *Foudre Descendante*, *Premier Grondement du Printemps* (Tonnerre Céleste) ; *Bois des Nuées Hautes*, *Droiture de l'Écorce* (Bois) ; *Ouragan du Yin Spectral* ; *Âmes Descendantes* ; *Brume du Yin Retourné* ; *Épine de la Porte des Tombes* ; *Perle Grise secrète* ; *Humectation du Plomb Radieux* (Élixir Parfait) ; *Sommeil de la Pierre Étreinte* (Qi Véritable).

### 6.6 Deux Dualités — détail

| Fruition | Essence métallique | Capacités / fondations (type) | Statut |
|---|---|---|---|
| **Yang Suprême** | *Essence du Zénith Sextuple* | *Gardien du Canon Solaire*, *Lance du Zénith* (types non donnés) | 🔎 non donné |
| **Yang Lumineux** | *Essence du Sceptre Lumineux* | *Arche du Salut au Ciel* (Magie), *Dard Vermillon* (Magie), *Regard du Trône* (Magie), *Monarque Assiégé* (Corps), *Clarté sur le Monde* (Vie) ; fondations citées ailleurs : *Passe de l'Aube Première*, *Cœur sans Ombre* | **Occupée** (Empereur Zhen Guangling ; l'Immortelle Xiaoyun la convoite) |
| **Yin Suprême** | — | *Lune Noyée*, *Accomplissement Originel*, *Liturgie Obscure* (Corps), *Soie de Lune* (Vie) | 🔎 non donné |
| Yin (2e lignée, non nommée) | — | 3 emplacements : Magie, Vie, Œil | — |
| Yin (3e lignée, non nommée) | — | 1 emplacement : Magie | — |
| Yang Mineur | — | non donné (« Éveil du Yang Mineur » rend **indestructible** : cf. le Monarque Démon, §12) | — |
| Yin Voilé, Yin Mineur | — | non donnés | — |

### 6.7 Cinq Vertus, Douze Qi, Tonnerres, Fusion Ancienne, Chamans — détail

**Eau**

| Fruition | Essence métallique | 5 capacités orthodoxes (type) | Substitution | Statut |
|---|---|---|---|---|
| **Eau Orthodoxe** | *Essence des Mille Gouttes Patientes* | *Mer sans Rivage* (fondation), *Veilleur du Gué* (Vie), *Ciel d'Orage*, *Garde de la Digue*, *Adieu au Fleuve* ; aussi *Refuge du Péril* ; capacité de Magie connue : *Roi-Serpent des Eaux Noires* | — | Libre |
| **Eau Rassemblée** | — | *Confluence Ultime*, *Présage de la Crue*, *Retour à la Mer*, *Sage des Flots Égaux*, *Fleuve Corrompu* | — | **Occupée** |
| **Eau Nourricière** | — | *Bruine Hivernale* | — | Libre |
| **Eau Muable** | *Essence du Sceau Limpide* | *Chant de la Source Enfouie* (Corps+Vie), *Voile de Brouillard* (Vie), *Averse du Soir* (Magie), *Silhouette sous l'Onde* (Vie), *Rosée Lustrale* (Magie) | *Manifestation de la Crête Céladon* / *Lune Noyée* | **Occupée** |
| **Eau Cachée** | — | *Aurore Sincère*, *Source Renaissante*, *Rencontre des Vallons*, *Porte du Trésor Profond* | — | **Occupée** |

🔎 « Corruption de la vertu de l'Eau » : la source l'évoque sans l'expliquer ; c'est ce qui rend possibles des ponts atypiques entre Fruitions d'Eau. À traiter en phase 6 comme un **état du monde** (fait historique).

**Feu**

| Fruition | Capacités (type) | Statut |
|---|---|---|
| **Feu Orthodoxe** | répartition : 2 Vie, 1 Corps, 2 Magie | 🔎 non donné |
| **Feu Rassemblé** | *Flamme Démone* (fondation), *Soupir du Cœur Ardent* ; répartition : 1 Vie, 1 Corps, 3 Magie | 🔎 non donné |
| **Feu Nourricier** | *Faisan de Braise* (Corps), *Incendie Céleste*, *Dieu du Foyer* | 🔎 non donné |
| Feu (lignées non nommées) | emplacements : Vie ; Magie, Magie, Vie, Corps, Vie ; Magie ; Magie | — |
| Feu Muable, Feu Caché | non donnés | — |

**Métal** — *Métal Caché* : *Colonne d'Argent*, *Forge Souterraine* · *Métal Muable* : *Stèle Gravée*, *Heaume de l'Aurore* · *Or Vert d'Origine* (essence) : *Cœur d'Or Éclos* — **occupée** · lignées non nommées : Vie, Magie, Vie ; Corps, Corps, Vie, Magie ; Métal Dui, Métal Rassemblé (« Or unifié »), Or de Joie : non détaillés.

**Bois** — *Bois Orthodoxe* : *Marche contre le Vent du Nord* (Corps), *Ancrage du Regard* (Vie), *Parole Voilée* (Magie), *Loi de la Sève* (Vie) · *Bois Rassemblé* : *Longévité par le Péril*, *Aigle sans Perchoir*, *Cueillette Amère* (Magie), *Forêt du Délire* (Vie) · autre lignée : Magie · fondations isolées : *Bois des Nuées Hautes*, *Droiture de l'Écorce*.

**Terre** — lignées non nommées : (Vie, Magie, Corps), (Magie), (Corps), (Magie, Magie), (Magie) · *Terre Orthodoxe* : *Course du Fou vers la Cime* · *Terre Nourricière* : non détaillée.

**Douze Qi**

| Fruition | Capacités (type) | Statut |
|---|---|---|
| **Qi Pur** (fondateur) | non donné | — |
| **Qi Violet** | *Ordonnance des Rouleaux Pourpres*, *Maître Nourricier de Vie*, *Culture de l'Étoile Terrestre*, *Vase Tiède et Précieux*, *Détour par le Mont d'Orient* | — |
| **Qi Sec** | *Splendeur sans Fin* (Magie), *Veillée de Relève* (Magie), *Chaleur Amère et Étouffante*, *Pavillon des Grâces*, *Débat des Huit Exemptions* | — |
| **Qi Exilé** | *Approche du Gouffre de Ning*, *Vaisseau Abyssal Caché* | — |
| **Qi Faste** (« Essence de bon augure ») | *Nuées Fastes*, *Rouleau des Heureux Accomplissements* | — |
| **Qi Froid** | *Givre sur les Cèdres*, *Ouïe Claire*, *Eau Froide du Nord* | — |
| **Qi Véritable** | essence *Essence du Démon Divin des Cinq Cieux* ; *Sommeil de la Pierre Étreinte* | — |
| Qi Pourpre, Qi Radieux, Qi Illimité | non détaillés | — |
| lignées non nommées | (Corps, Magie), (Vie), (Vie, Magie), (Vie, Magie, Corps), (Magie) | — |

**Trois Tonnerres** — *Tonnerre Céleste* : *Bassin d'Orage*, *Foudre Descendante*, *Premier Grondement du Printemps* · autre lignée : 1 emplacement Magie.

**Fusion Ancienne**

| Fruition | Capacités (type) | Statut |
|---|---|---|
| **Fusion Ancienne** | *Culture de la Transcendance*, *Forme en Transition* (Corps), *Usurpation Chaotique* (Magie), *Rupture des Liens* (Magie) | — |
| **Sentinelle de la Cité** | *Plume d'Orient* (Magie), *Âmes Descendantes* (Vie), *Eaux Douloureuses du Sud* (Corps+Magie), *Plateau du Ciel d'Occident* (Magie), *Cour du Désert du Nord* (Magie) | — |
| **Élixir Parfait** | *Harmonie Parfaite*, *Humectation du Plomb Radieux* (Vie), *Attente du Divin Suprême*, *Séquence du Livre d'Or* ; fondation *Perle Grise secrète* | Libre (son maître a été tué, §12) |
| **Régent Céleste** | *Équilibre Profond de la Grande Ourse*, *Écoute des Étoiles qui s'Éveillent* (Vie), *Arrangement Divin*, *Expansion Continue*, *Surveillance de la Loi Divine* | — |
| **Culture de Linxi** | non détaillée | **Occupée** (héritage saisi par le Vrai Monarque Qinghe) |
| **Manifestation Céladon** | essence *Essence des Monts Enchaînés* ; *Bélier des Profondeurs*, *Emblème Céladon*, *Chaîne des Monts*, *Roc Souverain*, *Guetteur de l'Abîme* ; substitution *Essence du Yang Mineur* | Libre |

**Trois Chamans**

| Fruition | Essence métallique | Capacités (type) | Substitution | Statut |
|---|---|---|---|---|
| **Jade Premier** | *Essence du Vide Unifié du Jade Premier* | *Cour du Jade Général* / *Né du Jade* (Corps+Vie), *Brocart Figuré* (Corps), *Falaise de Jade Vert*, *Unification du Vrai Dao*, *Disque de Jade Blanc* ; autre lignée : (Magie), (Vie) | — | **Occupée** 🔎 statut ambigu : sa figure a renoncé à la Réalisation et s'en est allée (§12) |
| **Grand Chaman** (Sceau chamanique) | *Essence de Sang des Trois Neuf* | *Ombre de la Sauterelle*, *Présence Indécelable*, *Bénédiction du Chaman de la Terre*, *Mandat de l'Empereur*, *Buveur de Sang* | *Course du Fou vers la Cime* | Libre |
| **Chouette des Seuils** | — | (Vie), (Magie), (Magie), (Vie), (Vie) | — | 🔎 non donné |

**Deux Rites** — lignées non nommées : (Corps) ; 5 emplacements sans type.
**Attestation du Vide** — *Étoile du Soir* : 2 emplacements sans type ; Voie de l'Épée, Lueur de l'Aube : non détaillées.

### 6.8 Statuts de Fruition (état du monde au début du jeu)

| Occupées | Libres | Non précisé |
|---|---|---|
| Eau Muable, Jade Premier, Yang Lumineux, Or Vert d'Origine, Eau Rassemblée, Eau Cachée, Culture de Linxi | Eau Orthodoxe, Grand Chaman, Manifestation Céladon, Eau Nourricière, Élixir Parfait | toutes les autres → tirées au début de chaque partie (§11.7) |

🎮 Phase 4 : `DaoLineage` (ScriptableObject) : groupe, génération, élément/manifestation, essence métallique, 5 `DivineAbilityDefinition` (nom, types), substitutions, statut + détenteur. Les emplacements non nommés sont créés avec leurs types et un nom provisoire. `Element` actuel (Fire, Water, Wood, Metal, Earth, Lightning, Darkness, Light) est conservé pour le **combat** et relié : Lightning ↔ Trois Tonnerres, Darkness ↔ Yin, Light ↔ Yang.

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
| Approchez-vous de l'Abîme de Yu · Vaisseau abyssal caché | Approche du Gouffre de Ning · Vaisseau Abyssal Caché |
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

✅ D6 : les noms descriptifs (marqués « conservés ») restent tels quels : ils décrivent littéralement leur effet et relèvent du vocabulaire courant.

---

*Voir `NOT_DONE.md` (section « Restructuration lore ») pour les phases 1-6.*
