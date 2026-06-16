# RecBack 🎮

**Rec Room is back.** RecBack lets you run your own Rec Room server so you can keep playing after the official servers shut down.

## What you need

- **A Windows PC** to run the game
- **A computer or NAS** to run the server (can be the same PC)
- **A Rec Room build** from 2023 (the installer can help you get one)
- **Steam** installed and running

## How to install (5 easy steps)

### 1. Download RecBack
Grab the latest release from [GitHub Releases](https://github.com/RecBack/RecBack/releases).

### 2. Run the installer
Double-click `installer.py` or run:
```
python installer.py
```

The installer will:
- Show a cool animated logo
- Ask where to install
- Help you get a Rec Room build (via Steam DepotDownloader)
- Download and install BepInEx (a mod loader)
- Install the RecBack patcher plugin
- Create launchers for you

### 3. Start the server
If you're running the server on the same PC:
```
cd RecBack
dotnet run --project server/RecBack.Server
```

If you have a NAS (like UGREEN):
```
docker compose up -d
```

### 4. Launch the game
Double-click `RecBack_ScreenMode.bat` (or `RecBack_VR.bat` for VR).

### 5. Play!
The game will connect to your RecBack server. You're back in Rec Room!

## First-time setup notes

- The first launch takes extra long as BepInEx generates game files
- The server must be running before you launch the game
- For NAS hosting, make sure your PC can reach the NAS on your network

## File structure

```
RecBack/
├── installer.py           # The installer (run this first)
├── server/                # Server source code
│   └── RecBack.Server/    # C# .NET 8 server project
├── patcher/               # BepInEx plugin source
│   └── RecBack.Patcher/   # C# .NET 6 plugin project
├── build/                 # Pre-compiled binaries
│   ├── server/            # Compiled server DLL
│   └── patcher/           # Compiled patcher DLL
├── bepinex/               # BepInEx core files (for development)
├── docker-compose.yml     # NAS deployment
├── Dockerfile             # ARM64 Docker image
├── README.md              # This file
└── HOW_IT_WAS_MADE.md     # How it works (simple explanation)
```

## Need help?

Check the [Discussions](https://github.com/RecBack/RecBack/discussions) tab on GitHub.
