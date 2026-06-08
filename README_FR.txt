# Centauri Carbon Downloader v0.5.4

**Créé par ashemka.**

Application Windows portable pour l’Elegoo Centauri Carbon. Elle permet de lister les fichiers locaux de l’imprimante, de télécharger ou supprimer des G-code, et de créer des vidéos timelapse sur le PC à partir des images stockées dans `/local/aic_tlp/`.

## Objectif

Le but est de rester KISS côté utilisateur final :

1. lancer l’application ;
2. entrer l’IP de l’imprimante ;
3. cliquer sur **Connexion** ;
4. cocher les fichiers ou timelapses ;
5. cliquer sur **Télécharger sélection** ou **Créer vidéos sur PC**.

Pas de PowerShell, pas de CMD, pas d’onglets avec du G-code brut, pas d’export manuel depuis le slicer.

## Fonctions principales

- connexion directe à l’imprimante par WebSocket ;
- listing des fichiers locaux via `Cmd 258` ;
- suppression des fichiers sélectionnés via `Cmd 259` ;
- téléchargement séquentiel des G-code dans `Téléchargements\Centauri_Downloads\GCode` ;
- scan rapide de la racine `/local/aic_tlp/` ;
- détection des frames timelapse sans extension, notamment `tlp_layer_*` ;
- génération locale de MP4 avec FFmpeg ;
- téléchargement parallèle des images sources ;
- fallback **Export imprimante** via `Cmd 323` ;
- logs dans `Téléchargements\Centauri_Downloads` ;
- thème clair / sombre ;
- interface multilingue : français, anglais, italien, espagnol, allemand, japonais, chinois, coréen.

## Nouveautés v0.5.4

- Correction de l'échec de compilation provoqué par `ToggleAllVisible` déclaré en `static` alors qu'il utilisait la méthode de traduction d'instance `Tr(...)`.
- Métadonnées de version mises à jour en 0.5.2.
- Déclaration du SDK projet ajustée pour éviter l'avertissement NETSDK1137 avec les SDK .NET récents.

## Nouveautés v0.5.0

- ajout du branding **Créé par ashemka** dans l’UI, le code, les métadonnées projet et la documentation ;
- ajout d’un sélecteur de langue ;
- ajout d’un mode sombre ;
- adaptation de l’interface pour supporter les libellés plus longs ;
- paramètres UI persistés dans `%LOCALAPPDATA%\CentauriCarbonDownloader\settings.json` ;
- timeout de handshake WebSocket cadré à 8 secondes ;
- timeout de réponse courte WebSocket cadré à 6 secondes ;
- timeout de probe HTTP cadré à 15 secondes ;
- documentation GitHub renforcée.

## Timelapses : fonctionnement

La Centauri Carbon expose souvent les images de timelapse dans `/local/aic_tlp/`, mais pas forcément une vidéo MP4 déjà prête. L’application propose donc deux modes :

### Créer vidéos sur PC

Mode recommandé. L’application télécharge les images sources puis fabrique le MP4 localement avec FFmpeg. C’est généralement plus rapide que d’attendre l’export vidéo côté imprimante.

### Export imprimante

Mode fallback. L’application demande à l’imprimante de générer le MP4, puis le télécharge quand il devient disponible. Ce mode peut être beaucoup plus lent.

## Réglages

- **FPS** : images par seconde de la vidéo finale. Valeur conseillée : `30`.
- **Flux** : nombre de téléchargements parallèles d’images. Valeur conseillée : `6`.
- **Garder images** : conserve les images sources après encodage, utile pour diagnostic.

## FFmpeg

Le mode rapide PC nécessite FFmpeg. Le script `build_release_win64.bat` tente de récupérer automatiquement `ffmpeg.exe` dans `tools\ffmpeg.exe`.

Si `tools\ffmpeg.exe` est présent au moment de la compilation, il est embarqué dans l’EXE final. Au lancement, l’application l’extrait dans `%LOCALAPPDATA%\CentauriCarbonDownloader\tools\ffmpeg.exe`.

L’utilisateur final n’a normalement rien à faire.

## Compilation Windows

Prérequis : SDK .NET 8 pour Windows.

1. Télécharger ou cloner le projet.
2. Double-cliquer sur `build_release_win64.bat`.
3. Attendre la compilation.
4. Récupérer l’application dans :

```txt
bin\Release\net8.0-windows\win-x64\publish\
```

Fichiers recommandés à distribuer :

```txt
Centauri Carbon Downloader.exe
FFMPEG_NOTICE.txt
```

## Notes réseau et timeouts

L’application ne fait pas de login au sens compte utilisateur : elle ouvre une connexion WebSocket locale vers :

```txt
ws://IP_IMPRIMANTE/websocket
```

Durées cadrées en v0.5.0 :

| Étape | Durée |
|---|---:|
| Handshake WebSocket | 8 s |
| Réponse courte WebSocket | 6 s |
| Probe HTTP dossier/fichier | 15 s |
| Transfert long G-code / images / vidéo | 45 min |
| Export imprimante timelapse | 10 min max |

Ces valeurs évitent de laisser l’interface bloquée inutilement tout en restant assez larges pour les gros fichiers.

## Dépannage

### Le scan timelapse trouve des MP4 mais pas les images

Vérifier dans le navigateur :

```txt
http://IP_IMPRIMANTE/local/aic_tlp/
```

Certaines versions de firmware exposent les images comme des fichiers sans extension du type `tlp_layer_1`, `tlp_layer_2`, etc. La v0.5.0 les traite comme des frames.

### FFmpeg introuvable

Relancer `build_release_win64.bat`. Le script tente de télécharger FFmpeg automatiquement. Si le réseau bloque le téléchargement, placer manuellement `ffmpeg.exe` dans :

```txt
tools\ffmpeg.exe
```

puis recompiler.

### Connexion impossible

- vérifier que le PC et l’imprimante sont sur le même réseau ;
- vérifier l’adresse IP ;
- tester l’accès navigateur à `http://IP_IMPRIMANTE/` ;
- relancer l’application.

## Crédits

Créé par ashemka.

Développement assisté par Hex Kernel / ChatGPT.

FFmpeg reste soumis à ses propres licences et notices. Voir `FFMPEG_NOTICE.txt`.
