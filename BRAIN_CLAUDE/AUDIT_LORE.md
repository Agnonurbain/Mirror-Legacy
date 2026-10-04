# Audit lore ↔ gameplay (2026-10-03)

Demande de l'utilisateur (2026-10-03) : passer en revue tout le lore (`LORE.md`, `Miror.txt`) **et le wiki** pour trouver les incohérences avec le gameplay actuel — comme la forge ouverte à tout cultivateur du Qi alors que les arts immortels demandent un don sous le Manoir Pourpre.

Méthode : sept audits en lecture seule (LORE §0-2, §3-5.3, §5.4-5.9, §6, §7-10/12, §11, passe complète du wiki — 551 pages), chaque constat vérifié dans le code. Doublons fusionnés ici. Légende : **C** contradiction · **P** prérequis manquant · **A** absent du jeu · **D** à décider avec l'utilisateur. Les références `fichier:ligne` sont celles du 2026-10-03 (commit 3a39894).

---

## 1. L'écart entre les royaumes n'existe pas (le plus grave)

Dans le lore, un royaume supérieur est d'une autre nature ; dans le jeu, la puissance est une somme linéaire.

| # | Constat | Jeu | Correction proposée |
|---|---|---|---|
| 1.1 | ✅ 2026-10-03 (`RealmGap`, balance « realmGap ») **C** Une puissance inférieure capture un Manoir Pourpre (30 % pour une Fondation) ou un Noyau d'Or (plancher 5 %). Le Manoir Pourpre est invisible aux royaumes inférieurs et se téléporte à 5 000 m (LORE:358). | `World/SchemeRules.cs:12,26-27` | Capture, sonde et vol visant un membre impossibles si le plus fort de la puissance est d'un royaume inférieur ; plancher seulement à royaume égal. |
| 1.2 | ✅ 2026-10-03 (refus motivé, sans dépenser l'opération) **C** Le sauvetage additionne la puissance : trois Fondations battent un Noyau d'Or. | `SchemeRules.cs:38-42`, `Mirror/HuntRules.cs:13` | Ne compter que les membres du royaume du ravisseur ou plus. |
| 1.3 | ✅ 2026-10-03 (un coup hors de portée ne porte pas ; un camp hors de portée ne gagne pas de bataille ; les éclats, sabotages et chasses ne comptent que ceux qui atteignent l'adversaire) **C** Le combat et la guerre croissent linéairement avec le royaume ; un Noyau d'Or « dont le corps n'est qu'un réceptacle » (LORE:427) meurt sous le nombre. | `Combat/CombatUnit.cs:45-50`, `WarRules.cs:12-20` | Échelle exponentielle, ou immunité aux unités deux royaumes en dessous. |
| 1.4 | ✅ 2026-10-03 (sondes à la main impossibles hors de portée, refus motivé ; un Manoir Pourpre qui sonde plus faible n'est pas vu ; corruption et miroir inchangés ; embuscades : §1.1) **A** Invisibilité et téléportation du Manoir Pourpre ignorées par l'espionnage, les chasses, les embuscades. | — | Invisible aux royaumes inférieurs ; échappe toujours à une embuscade inférieure. |
| 1.5 | ✅ 2026-10-03 pour le clan (`TravelRules`, balance « travel » 🔎 : Respiration au domaine, Culture du Qi 2 régions, Fondation tout l'État, Manoir Pourpre partout ; sondes à la main, chasses, sauvetages, sabotages, vols d'éclats ; la vue du miroir sans route) et les puissances (§6.3) **A** Portée des déplacements par royaume (vol dès la Culture du Qi, ~200 lieues en quelques heures ; le Grand Vide au-delà) : chasses et sondes portent n'importe où. | `Mirror/HuntOperations.cs:61-84` | Rayon d'action en voisinages qui croît avec le royaume. |
| 1.6 | ✅ 2026-10-03 (décision de l'utilisateur : `rebirthChance` 1 pour le clan et les puissances ; restent les risques du lore ; le Qi exilé est la lignée du Monde Souterrain, qui tient les registres des vivants : c'est la rancune des Enfers, déjà en jeu) **C / D** La renaissance d'un Noyau d'Or est un tirage à 30 % ; le lore : « autant de fois qu'il le souhaite » tant que l'essence est intacte (LORE:427). | `Characters/AncestorReturn.cs:51`, `balance.json ancestors.rebirthChance` | Certaine sauf rancune des Enfers, récolte des Élus, Qi exilé — **à décider** (choix validé plus tôt : « renaissance rapide avec risque de récolte »). |
| 1.7 | ✅ 2026-10-03 en combat (`RealmGap.Suppressed`) et dans les complots (`RealmGap.SuppressedBy` : la capture par une puissance dont un Noyau d'Or tient la lignée de la fondation du membre est presque sûre, et ce membre ne compte pas pour un sauvetage contre elle) **A** L'essence d'un Noyau d'Or réduit à l'impuissance les cultivateurs de même fondation (LORE:430). | — | Défaite automatique en combat / complot. |
| 1.8 | ✅ 2026-10-03 en partie (décision de l'utilisateur : seule une puissance ayant un Manoir Pourpre à portée récolte un Élu, du clan comme du monde ; le clan récolte l'Élu d'une puissance par son Manoir Pourpre — onglet « Élus » des Opérations ; envoûtement (`Enthrallment`, balance « enthrallment ») : un Vrai Monarque réincarné et revenu plie l'esprit d'un membre sous le Manoir Pourpre, qui devient son espion sans le savoir — « regard absent » parmi les suspects, le miroir le sonde et brise le sort, le Manoir Pourpre y résiste et s'en libère ; l'ancêtre revenu du clan plie de même un ancien moindre d'une puissance, les yeux du clan chez elle) **A** Pouvoirs du destin du Manoir Pourpre : tuer les Élus, résister à la manipulation d'un Vrai Monarque réincarné (LORE:358). | `AncestorReturn`, `WorldRebirths` | La récolte d'un Élu demande un agent du Manoir Pourpre ; le clan peut aussi la faire (parité). |
| 1.9 | ✅ 2026-10-03 (`AscentWatch`, balance « ascentWatch » : méfiance des puissances du Manoir Pourpre à portée, épreuve possible, le perdant blessé — clan et monde) **A** Un nouveau Manoir Pourpre attire l'attention des vieilles puissances, qui « testent » (wiki *Chi_Wei*). | — | Méfiance et épreuve possible à l'ascension. |

## 2. Arts immortels, pilules et ressources de percée

Décisions de l'utilisateur du 2026-10-03 (mémoire `immortal-arts`) : quatre arts ; don requis sous le Manoir Pourpre ; héritage + maître ; génie ≠ talent ordinaire.

| # | Constat | Jeu | Correction proposée |
|---|---|---|---|
| 2.1 | **P** Forge : seul le royaume du forgeron est vérifié (même chose pour élever un artefact). | `Characters/ArtifactForge.cs:35-43,59-66` | Don + héritage + maître ; *Forge Souterraine* (Métal Caché) donne le don et un bonus. |
| 2.2 | **P** Pilules : condenser une capacité « par pilules » et purifier un démon du cœur dépensent herbes et pierres, personne ne fabrique les pilules. | `DivineAbilitySystem.cs:82-99`, `OathSystem.cs:115-124` | Étape d'alchimiste (doué ou Manoir Pourpre), ou achat. |
| 2.3 | **P** Formation protectrice : il suffit d'une Fondation vivante. | `Data/BuildingData.cs:48` | Maître des formations doué, ou loué à une puissance. |
| 2.4 | **A** Dessin de talismans (« un des rares moyens de gagner des pierres », wiki *Li_Xuanxuan*). | `Data/Enums.cs` (TaskType) | Tâche gated par le don. |
| 2.5 | **A** Aucun art immortel dans les données (`TechniqueKind.ImmortalArt` existe, `techniques.json` n'en a aucun ; le miroir les exclut). | `Data/TechniqueData.cs:14`, `DeductionEngine.cs:24-26` | Héritages des quatre arts comme techniques. |
| 2.6 | **A** Pilule de Rassemblement d'Essence pour la Fondation : quotas, usage rapporté aux anciens, percée « sans pilule » = pari (wiki *Li_Chenghui*, *Li_Jiangxia*). | `CultivationSystem.PayTrialQi` (seul le Qi) ; `ItemData.cs` | Ressource de percée ; sans elle, chances en chute ; son attribution est un dilemme du clan. |
| 2.7 | **A** Une pilule doit être de l'élément du cultivateur ; une essence opposée fait échouer, un Qi incompatible empoisonne (wiki *Li_Chengliao*, *Spiritual_Energy*). | — | Élément / lignée par pilule ; ouvre un sabotage. |
| 2.8 | **A** Poudre d'Esprit Lumineux pour le 5e chakra (wiki). | `BreakthroughRules` | Ressource ou forte pénalité. |
| 2.9 | **A** Pilules de puissance : coût en longévité, dose maximale, risque pour la fondation (wiki *Autumn_Convergence_Pill*). | `Combat/CombatActions.cs:164-186` | Coût et plafond. |
| 2.10 | **A** Seconde fondation par pilule (Li Xuanfeng, pilule humaine) ; prolongation de vie (seize méthodes, pilules humaines) — pour le clan **et** les anciens du monde. | `PatronDesignEffects.cs:96` seul | Rare, avec prix (karma, voie scellée). |

## 3. Le miroir est trop puissant trop tôt

Le §11.5 validé dit : la restauration fixe la portée de perception, la puissance de la Lumière, le nombre de graines, l'accès au Vide. Seuls graines, déduction d'ascension et Vide lisent les éclats.

| # | Constat | Jeu | Correction proposée |
|---|---|---|---|
| 3.1 | **C** Le Jugement tue n'importe quel royaume, Noyau d'Or compris, dès l'an 1. Wiki : frappe de l'Œil du Sommet au départ, puis la Culture du Qi, bien plus tard le Manoir Pourpre. | `Mirror/MirrorSystem.cs:107-113` | Royaume tué / blessé plafonné par les éclats. |
| 3.2 | **C** Le Jugement ne vise que les membres du clan ; dans le wiki il frappe les ennemis (le démon loup, les intrus). | `Presentation/MirrorView.cs:76-81` | Viser intrus, agents, ravisseurs à portée, avec coût de secret. |
| 3.3 | **P** La déduction ordinaire produit une méthode du Manoir Pourpre sans aucun éclat (4 fragments de qualité 4 → grade 5). | `DeductionEngine.cs:49-65,133-171`, `TechniqueRules.cs:83-84,138-145` | Plafond grade 4 / sans secret tant que < 3 éclats. |
| 3.4 | **C** Le Bouclier ancestral (+30 % à toute percée) ne demande ni essence métallique ni restauration (LORE §11.5 le lie à l'essence métallique). | `MirrorSystem.cs:98-104` | Essence détenue ou seuil d'éclats ; limité à sa Fruition. |
| 3.5 | **C** Brouiller les souvenirs efface les indices de n'importe quelle puissance, partout, dès l'an 1. | `World/SecretSystem.cs:56-67` | Portée (graine, membre proche), cible individuelle. |
| 3.6 | **P** Sonde du miroir sans portée ; aides de chasse (illusion, vol de mémoire) sans éclats. | `World/ProbeSystem.cs:116-121`, `HuntRules.cs:30,53` | Rayon de perception ; paliers d'éclats. |
| 3.7 | **A** Le Clair de Lune du Yin Suprême n'existe pas ; l'énergie du miroir se recharge gratuitement (+1/an, +5 par percée — sans base dans le lore). La graine coûte de l'énergie générique au lieu du Clair de Lune. | `MirrorSystem.cs:51-52,129` | La ressource validée ; retirer la recharge par percée. |
| 3.8 | **C** Graines réservées aux mortels ; le wiki en donne aussi à des membres nés avec l'orifice (canal caché). Compatibilité (auréole d'un *chi* = parfaite, d'un *cun* ≈ 10 %) absente. | `SpiritualOrificeRules.cs:63-66` | Graine-canal pour les cultivateurs ; compatibilité tirée par mortel. |
| 3.9 | **A** Le miroir ne purifie que les manuels de parrains ; le wiki : n'importe quel art de secte. | `Sponsorships.Cleanse` | `Cleanse` pour tout art de faction appris. |
| 3.10 | **D** LORE vs wiki : capacité de départ des graines 2 (wiki : six) ; sommeil après le premier éclat 1 an (wiki : trois). | `balance.json`, `shards.json` | 🔎 justifié ou aligner. |

## 4. Le monde de départ ne correspond pas au roman

| # | Constat | Jeu | Correction proposée |
|---|---|---|---|
| 4.1 | **C** Le clan démarre libre ; dans le wiki, les familles du lac sont gouvernées par la secte du Pic des Nuées, paient tribut, et l'impayé = anéantissement silencieux. | `PowerPoliticsSystem.cs:27` | Vassalité initiale au Pic des Nuées. |
| 4.2 | **C** Le Pic des Nuées n'a aucun voisin (`mount-yunfeng`) : il n'allie, ne guerroie, ne soumet jamais. | `regions.json`, `FactionManager.cs:104` | Portée sur sa juridiction (lac, Heshan, Siwei…). |
| 4.3 | **C / D** Le clan de départ cultive (patriarche Culture du Qi 3, deux techniques, Qi) ; le roman commence par des paysans mortels, les fils reçoivent graines et techniques du miroir. Mo Jian : 3e de quatre fils, 13 ans (jeu : 20 ans ; LORE §9 dit « aîné »). | `game/data/clan.json:6-12`, `FoundingClan.cs:77` | **Décision majeure** : départ fidèle au roman ou départ « accéléré ». |
| 4.4 | **C** Les Ruan sont hostiles (agressifs, −10) ; le wiki en fait les protecteurs manipulateurs du clan. Les Lü sont marchands amicaux ; le wiki en fait le premier ennemi. | `factions.json` | Personnalités et relations du wiki. |
| 4.5 | **C** Le pacte du Renard est à gagner (Manoir Pourpre + tribut) ; il est en vigueur depuis ~400 ans dans le lore. | `patrons.json:2` | Pacte actif au départ, termes hérités. |
| 4.6 | **C** Les figures démarrent à leur royaume de pointe : Zang Wanru Manoir Pourpre à 12 ans, Guan Yunhe à 20. | `ElderSystem.cs:42-43` | `realmAtStart` ou royaume selon l'âge. |
| 4.7 | **C** Bai et Zang sont des puissances indépendantes ; dans le wiki, des familles **de** la secte. Les puissances placées à `linxi` deviennent voisines de Kun, qui peut les soumettre. | `factions.json`, `regions.json` | Familles internes ou alliées permanentes ; vraies sous-régions. |
| 4.8 | **C** Porte du Fer Ardent : Fondation au plus (wiki : jusqu'à trois Manoirs Pourpres, Situ Huo vivant). Tan Qing (Eau Muable, fondateur du Pic des Nuées) n'est rattaché à aucune puissance. Xun : au lac selon le wiki. Familles du lac trop fortes (Fondation pour toutes ; wiki : Culture du Qi, sauf les Tao). Famille Wan absente. | `factions.json`, `figures.json`, `fruitions.json:778` | Corriger les données. |
| 4.9 | **A** Aucune inimitié de départ (Pic des Nuées ↔ Lune Pâle / Fer Ardent, Bai ↔ Ruan, Lou ↔ Tao, Douze Portes ↔ clan). | `SuspicionLedger` vide | `startingDistrust` dans les données. |
| 4.10 | **C** Calendrier décalé d'un an (naissances comptées depuis l'an 0, horloge à l'an 1). | `Session/GameClock.cs:10` | Horloge à 0 ou décaler. |

## 5. La secte suzeraine, le territoire, les champs

| # | Constat | Correction proposée |
|---|---|---|
| 5.1 | **C** La secte ne veut pas de pierres mais des **prodiges** (sélection annuelle des plus prometteurs comme disciples), de la main-d'œuvre, des levées pour la frontière ; elle encourage les annexions entre ses familles (« des récoltes qu'on moissonne le moment venu »). Jeu : tribut de 10 % des pierres (`TreatySystem.cs:248-254`). | Sélection annuelle, levées, anéantissement en cas de défaut. |
| 5.2 | **A** La secte défend ses familles clientes (les Maîtres taoïstes interviennent). | Protection patron → clients dans les querelles et guerres. |
| 5.3 | **A** Champs spirituels : ils nourrissent le clan, sont gardés par des formations et sont le premier butin des annexions. | Champs par région, travaillés par les mortels. |
| 5.4 | **A** À la mort du pilier d'une famille, son territoire tombe dans la journée (les Lu). | Régions ouvertes à l'annexion l'année de la mort (parité : le clan aussi). Lié à L5b. |

## 6. Parité du monde (le monde ne subit pas les règles du clan)

| # | Constat | Jeu | Correction proposée |
|---|---|---|---|
| 6.1 | **P** Les anciens montent de royaume sans méthode (Lou et Kang n'ont aucune technique mais des Fondations ; `RaiseACadet` crée des Fondations). | `ElderSystem.cs:119-130,180-187` | Plafond par grade et secret du Manoir Pourpre des techniques de la puissance. |
| 6.2 | **A** Pas de mur de la Fondation pour les anciens : ni pénalité d'âge, ni dissolution. | `ElderSystem.Rise` | Mêmes règles que le clan (`BreakthroughRules`). |
| 6.3 | ✅ 2026-10-03 (une puissance doit atteindre le domaine — `TravelRules.PowerReaches` — pour l'embusquer, le sonder à la main, le voler ou lui déclarer la guerre ; le chantage passe par des messages) **P** N'importe quelle puissance déclare la guerre à n'importe qui, à toute distance. | `WarSystem.cs:288-293` | Contiguïté ou portée selon royaume et type. |
| 6.4 | **P** Le type de puissance n'a aucun effet : une famille peut prendre une porte en vassale. | `PowerPoliticsSystem.cs:116,179` | Hiérarchie par type. |

## 7. Cultivation de base

| # | Constat | Jeu | Correction proposée |
|---|---|---|---|
| 7.1 | **P** Un orifice jamais examiné cultive quand même (et la liste des tâches le trahit). | `SpiritualOrificeRules.cs:44-47`, `TaskRules.cs:50` | Exiger `OrificeKnown` (ou graine, ou non humain). |
| 7.2 | **P** L'examinateur captif ou absent examine quand même. Les fondateurs naissent tous avec un orifice connu. | `SpiritualOrificeRules.cs:50-54`, `FoundingClan.cs:77-78` | Examinateur présent ; tirage. |
| 7.3 | **P** Sorts et duels dès le chakra 0, même pour les mortels (le mana vient au 1er chakra). | `CombatUnit.cs:47`, `CombatActions.cs:213`, `ChallengeSystem.cs:67` | 0 Qi et aucun art sous le chakra 1. |
| 7.4 | **C** La Respiration Embryonnaire a moins de tâches que les mortels (« trop fragile », sans base). | `TaskRules.cs:26-34,51-52` | Au moins les tâches des mortels ; patrouille / espionnage dès le chakra 3. |
| 7.5 | **C** Conjoints mariés : Culture du Qi sans Qi ni méthode → vitesse 0 à vie, sauf après un rechargement (`NormalizeMember`). | `MarriageSystem.cs:90-95,150-155` | Méthode et Qi de leur puissance au mariage. |
| 7.6 | **C** Membre absorbé : fondation du premier Qi de `qi.json`, sans rapport avec son Qi. | `ClanAbsorption.cs:107-111` | Fondation tirée de son Qi. |
| 7.7 | **C** Partenaires Dao : 5 à 9 au lieu de 4, substitutions comprises ; `IsPrey` les exclut, `DaoPartners` non. | `FoundationRules.cs:29-30,53` | Exclure les substitutions ; trancher les lignées à 6-7 capacités. |
| 7.8 | **P** Le clan récolte un Qi absent de sa région ; rendement fixe ; pas de méthode de collecte propre. | `TaskAssignmentSystem.cs:142-170` | Seulement si `Abundance > 0`, rendement proportionnel (B4). |
| 7.9 | **C** L'état d'une lignée pèse peu (libre = pleine vitesse ; brisée = −40 %) ; le lore : libre = Qi rare, brisée = plus de Qi ou déviations. Le détenteur ne peut pas supprimer ou cacher le Qi de sa lignée. | `RegionalQi.cs:72-77` | Effets réels ; levier du détenteur (complots). |
| 7.10 | **P** Manuels de grade 4 et techniques secrètes vendus contre des pierres par toute puissance amie. | `Diplomacy/KnowledgeExchange.cs:64-72` | Pierres seulement pour les communes de grade ≤ 3 ; le reste en nature ou par complot. |
| 7.11 | **C** Les arts déduits (sorts, déplacements, armes) ne sont jamais utilisables : seuls les méthodes de cultivation entrent dans `KnownTechniqueIDs`. Le miroir associe un Qi par élément seul. | `CombatActions.cs:212`, `DeductionEngine.cs:161,175-184` | Apprentissage des arts (G6) ; Qi récoltable et lié. |
| 7.12 | **C** Lignée transformée : orifice non garanti ; Manoir Pourpre « garanti » (LORE:507) vs bonus de +20 % (code). | `ClanManager.cs:109-113`, `PurpleMansionSystem.cs:40` | **D** garantie ou bonus. |
| 7.13 | **C** Montée au Manoir Pourpre : le lore cite mana, grade de fondation, maîtrise taoïste ; le code : racine et stabilité. | `PurpleMansionSystem.cs:15-20` | Ajouter grade et maîtrise. |
| 7.14 | **A** Démons du cœur par traumatisme (deuil, captivité), pas seulement par serment brisé. | `OathSystem` | Source supplémentaire. |

## 8. Fruitions et capacités divines

| # | Constat | Jeu | Correction proposée |
|---|---|---|---|
| 8.1 | **A** **Aucune capacité n'a d'effet propre** : *Forge Souterraine*, *Fils d'Argent* (trésors), *Séquence Divine* / *Présence Indécelable* (anti-divination), *Harmonie Parfaite* (copie d'artefact), *Plume d'Orient* (bloque la fuite par le Vide), Feu en Fusion (fond artefacts et formations)… | aucun id lu dans `src/Core` | Champ `effects` par capacité, lu par les systèmes existants. |
| 8.2 | **A** Types de capacité sans effet (Corps, Vie, Œil, Sort) ; capacités superficielles sans effet sur les sorts. | `GoldenCoreRules.cs:121-124` | Effets en combat / complots. |
| 8.3 | **A** Pas de contre-relations entre lignées (Yang Lumineux contre les diables, Yin Voilé ↔ Yang Lumineux…). | `CombatActions.cs:236` (élément seul) | Table de contres. |
| 8.4 | **A** Le détenteur d'une position n'a aucune autorité sur les cultivateurs de sa lignée, dans un sens comme dans l'autre. | — | Pression, exigences, vassalité. |
| 8.5 | **C** Surplus ouvert dans une lignée libre (wiki : seulement si le centre est occupé) → Surplus bon marché puis Transfert. | `GoldenCoreSystem.cs:102,439` | Refuser hors lignée occupée. |
| 8.6 | **C** Désignation de Rang ouverte au Surplus et à l'Intercalaire (wiki : Réalisation seule) ; « aimé » réduit au tempérament, l'inaimé manie toujours à 50 %. | `DharmaTreasures.cs:20,96-106` | Réalisation seule ; maniement bref sans amour. |
| 8.7 | **C** La substitution d'une autre lignée doit être une copie : *Course du Fou vers la Cime* existe en double ; le vrai Terre ne donne pas le Surplus Chamane. Les capacités de substitution ou mineures se cultivent mal (3 sur 69 ont un Qi). | `GoldenCoreRules.cs:33-41`, `DivineAbilitySystem.cs:72-73,168-172` | Référencer l'original ; cultiver par la technique alignée ou la profondeur du Dao. |
| 8.8 | **A** Pas d'état « perfectionné » d'une capacité (requis pour la Réalisation) ; pas de Cinq Méthodes pour forger le Noyau. | `GoldenCoreSystem.cs:417-429` | Niveau de perfection ; Cinq Méthodes en données ou lacune déclarée. |
| 8.9 | **C** Données : *Refuge de l'Unicité* clé du Surplus du Bois Orthodoxe ; un Qi unique pour le Métal Caché ; Métal Rassemblé / Harmonieux tirés « libres » ; détenteurs (Lian, Huyan, Hui, Hei) aléatoires ; la Reprise peut rappeler un groupe ou un Seigneur Immortel. | `fruitions.json`, `qi.json`, `FruitionRegistry.cs:55`, `GoldenCoreSystem.cs:467` | Corriger les données. |
| 8.10 | **P** Cibles de forge cachées ou soupçonnées listées sans avertissement (membre bloqué sans position) ; lignée brisée condensée comme libre ; permission du détenteur indépendante de la relation. | `GoldenCoreSystem.cs:140-147,190-205`, `DivineAbilitySystem.cs:62-78` | Griser, malus, pondérer. |
| 8.11 | **A** Une Réalisation ne change pas le monde (Jade Premier : tout sort demande du jade) ; pas d'empreinte d'un long règne ; descendants sans affinité de lignée ; Vertu corrompue sans autre effet ; métadonnées (Profondeur, Étoile) inutilisées. | — | Modificateurs du monde en données. |
| 8.12 | **D** Embryon du Dao inaccessible (fin atteignable depuis une Réalisation pleine ?). | `PowerLadder.Next` | À décider. |

## 9. Divers (poids plus faible)

- Fenêtre de l'Intention d'Épée : seulement à la Fondation (wiki *Way_of_the_Sword*).
- Une bête capturée peut aussi être mangée ou raffinée, en concurrence avec le rituel.
- Le phénomène de mort est toujours placé au domaine, même pour un membre mort en captivité ou en expédition ; une mort dans le Grand Vide laisse un phénomène de Fondation.
- Les phénomènes de formation et d'apogée d'une Fondation manquent (L4c).
- Les trois autres Voies n'ont pas d'échelle propre (déjà L7).

## 10. Corrections de LORE.md (documentation seule)

- §5.4 l.358 : rétablir « sans être limité par ses forces naturelles » et la règle des arts.
- l.253 : avertissement d'espérances de vie périmé.
- l.281 : la porte du Qi est le passage du 6e chakra à la Culture du Qi.
- 🔎 manquants : seuil de détection (Œil du Sommet), « 1-2 Respiration » des arts, récolte dès le 5e chakra.
- l.66 vs l.150 : grade ≠ royaume, mais l'art est lié au royaume par son grade.
- §5.5 l.431 vs l.507 : lignée transformée, « peut atteindre » vs « garantit ».
- Titres : Maître taoïste couvre les stades 1 à 3 (wiki).
- §6.1 « 5 capacités orthodoxes » vs 6-7 listées ; §6.2 générations vs wiki ; §6.8 statuts incomplets ; numéros §6.8/6.8b et deux §6.9 ; Céladon.
- §6.9 Désignation : « détenteur de position » vs Réalisation seule.
- §7.2 / §7.3 / §8 / §9 / §10 : sièges du Fer Ardent et de la Lune Pâle, juridiction du lac, Lü et Lou, Mo Jian (3e fils), « huit portes » ✅ D5.
