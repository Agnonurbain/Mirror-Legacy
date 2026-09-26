# 🛠️ CLI.md — Commandes de développement (Godot 4.7.2 .NET, Linux)

## Construire, tester, lancer

| Commande | Effet |
|---|---|
| `./Scripts/dev.sh build` | Compile Core, les tests et l'assemblage Godot |
| `./Scripts/dev.sh test` | Tests de la simulation (`dotnet test`, sans Godot) |
| `./Scripts/dev.sh smoke` | Jeu sans affichage (`-- --smoke`) : joue 5 ans puis quitte ; échoue sur toute erreur du moteur |
| `./Scripts/dev.sh screenshot <png>` | Idem dans une fenêtre, puis capture de l'écran |
| `./Scripts/dev.sh gaps` | Ce que le lore ne donne pas encore : interprétations, capacités non révélées, types inconnus (`ContentGaps`) — à remplacer dans les données quand une source parle |
| `./Scripts/dev.sh all` | build + test + smoke |

- Godot est attendu dans `~/Godot/Godot_v4.7.2-stable_mono_linux_x86_64/` ; sinon `GODOT_BIN=/chemin/vers/godot`.
- Ouvrir l'éditeur : `$GODOT_BIN --path game --editor`.
- Les parties de fumée et de capture ne touchent jamais la sauvegarde du joueur.

## Graphe de connaissances

| Commande | Effet |
|---|---|
| `graphify query "<question>"` | Sous-graphe ciblé |
| `graphify explain "<concept>"` | Un concept et ses liens |
| `graphify path "<A>" "<B>"` | Chemin entre deux nœuds |
| `graphify update .` | Remise à jour après modification du code (AST seul, gratuit) |

## Git

- Une branche par phase (`feature/...`), commits conventionnels, étapes TDD RED puis GREEN.
- `main` ne reçoit qu'une branche revue et verte (`dev.sh all`).
