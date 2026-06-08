# Compilation de Centauri Carbon Downloader depuis la source

## Prérequis

1. **Windows 10/11** (x64)
2. **.NET 8.0 SDK** ou plus récent
   - Télécharger depuis : https://dotnet.microsoft.com/download
   - Vérifier l'installation : `dotnet --version`

3. **FFmpeg** (optionnel - téléchargé automatiquement au premier lancement)
   - Ou placez manuellement `ffmpeg.exe` dans le dossier `tools/`

## Méthodes de compilation

### Option 1 : Build rapide (Recommandé)

**Windows :**
```batch
build_release_win64.bat
```

Ce script va :
- Nettoyer les builds précédentes
- Restaurer les paquets NuGet
- Compiler la configuration Release pour win-x64
- Résultat : `bin/Release/net8.0-windows/win-x64/Centauri Carbon Downloader.exe`

### Option 2 : Compilation manuelle avec .NET CLI

```powershell
# Restaurer les dépendances
dotnet restore

# Compiler la version release
dotnet build -c Release --self-contained -r win-x64

# L'exécutable se trouvera à :
# bin/Release/net8.0-windows/win-x64/Centauri Carbon Downloader.exe
```

### Option 3 : Build Debug (pour les développeurs)

```powershell
dotnet build -c Debug
# Exécutable : bin/Debug/net8.0-windows/Centauri Carbon Downloader.exe
```

## Après la compilation

Après la compilation :

1. **Configuration au premier lancement :**
   - L'app crée automatiquement : `%LOCALAPPDATA%\CentauriCarbonDownloader\`
   - FFmpeg sera téléchargé automatiquement si non trouvé
   - Les paramètres sont sauvegardés dans : `settings.json`

2. **Configuration FFmpeg manuelle** (optionnel) :
   - Télécharger depuis : https://ffmpeg.org/download.html
   - Extraire `ffmpeg.exe`
   - Placer dans : `tools/ffmpeg.exe` avant de compiler

## Dépannage

**Erreur : "dotnet: Le terme 'dotnet' n'est pas reconnu"**
- Installer le .NET SDK : https://dotnet.microsoft.com/download
- Redémarrer le terminal/IDE
- Vérifier : `dotnet --version`

**Erreur : "No required supplemental .NET Runtime version"**
- Mettre à jour .NET : `dotnet sdk update`

**FFmpeg ne peut pas être téléchargé**
- Télécharger manuellement depuis : https://ffmpeg.org/download.html
- Placer `ffmpeg.exe` dans le dossier `tools/`

## Informations du projet

- **Langage :** C# (.NET 8.0)
- **Framework :** WinForms
- **Target :** net8.0-windows, win-x64
- **Version :** 0.5.4

## Ressources supplémentaires

- Documentation complète : [README_FR.md](README_FR.md)
- Documentation anglaise : [README_EN.md](README_EN.md)
- Licence : Consulter le fichier LICENSE (le cas échéant)

## Développement

Pour les développeurs voulant modifier le code source :

```powershell
# Ouvrir dans Visual Studio ou VS Code
code .

# Ou avec Visual Studio IDE
start CentauriCarbonDownloader.csproj
```

---

**Créé par ashemka** - Signaler les problèmes sur GitHub
