# Changelog

## v0.5.4

- Reworked dark mode colors across tabs, grids, buttons, inputs, and disabled controls.
- Added manual export folder selection from the UI.
- Persisted the selected export folder in the user settings file.
- Added an active export folder label in the main window.

## v0.5.2

- Fixed a build failure caused by `ToggleAllVisible` being declared static while using the instance translation method `Tr(...)`.
- Updated application version metadata to 0.5.2.
- Switched the project SDK declaration from `Microsoft.NET.Sdk.WindowsDesktop` to `Microsoft.NET.Sdk` to avoid the NETSDK1137 warning on recent .NET SDKs.

## v0.5.1

- Added English `README.md` for GitHub.
- Added explicit `README_EN.md`.
- Added `README_FR.md` while keeping `README_FR.txt`.


## v0.5.0

- Branding ajouté : **Créé par ashemka** dans l’interface, le code, les métadonnées et la documentation.
- Ajout du mode sombre.
- Ajout du sélecteur de langue : français, anglais, italien, espagnol, allemand, japonais, chinois, coréen.
- Interface agrandie et adaptée aux libellés plus longs.
- Paramètres UI persistés dans `%LOCALAPPDATA%\CentauriCarbonDownloader\settings.json`.
- Handshake WebSocket limité à 8 secondes.
- Réponses WebSocket courtes limitées à 6 secondes.
- Probes HTTP limités à 15 secondes.
- Documentation GitHub ajoutée / renforcée.

## v0.4.5

- Correction de la détection des fichiers timelapse sans extension `tlp_layer_*`.
- Réduction du scan récursif inutile.
- Progression visible pendant analyse, téléchargement et encodage.

## v0.4.x

- Ajout du mode rapide PC avec FFmpeg.
- Ajout du fallback export imprimante.
- Amélioration progressive du scan `/local/aic_tlp/`.

## v0.3.x

- Ajout du support timelapse via export imprimante.

## v0.1 / v0.2

- Listing, téléchargement et suppression des G-code locaux.
