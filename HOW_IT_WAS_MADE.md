# How RecBack Was Made

## For a 5th grader

Imagine Rec Room is a giant video game arcade. The arcade had a big computer in the back that kept everything running. One day, the arcade closed and turned off that computer. The games stopped working.

**RecBack is like building your OWN computer for the arcade.**

Here's how it works:

### 1. The game (Rec Room)
You have a copy of Rec Room on your computer. It's like having the game disc. But the disc tries to call the old arcade computer, which doesn't exist anymore.

### 2. BepInEx (the translator)
BepInEx is a special tool that loads into the game before it starts. Think of it like putting on special glasses that change what you see. When the game tries to call the old arcade, BepInEx makes it call YOUR computer instead.

### 3. The RecBack Patcher (the note)
The patcher is a tiny program (a DLL file) that goes inside BepInEx. It's like a note that says:
- "When the game asks for accounts.rec.net, go to MY server instead"
- "When the game checks for anti-cheat, say 'everything's fine!'"
- "When the game looks for special screens, show them"

### 4. The RecBack Server (your arcade computer)
This is a program that runs on your computer (or a NAS - a little box plugged into your router). It pretends to be Rec Room's old servers. When the game asks:
- "Can I log in?" → Server says "Yes, here's your account!"
- "Where's the Rec Center?" → Server says "It's right here!"
- "Show me my profile" → Server shows a pretend profile

## What's in each file

### `installer.py`
A Python program that does all the hard work for you:
- Shows cool animations
- Downloads BepInEx automatically
- Sets everything up in the right places
- Creates launcher buttons

### `server/RecBack.Server/`
The main server. Written in C# (a programming language). It has four smaller servers inside:
1. **NameServer** - Tells the game where everything else is
2. **ApiServer** - Handles accounts, rooms, profiles
3. **ImageServer** - Serves pictures (profile pics, room images)
4. **NotifyServer** - Keeps a connection open for notifications

### `patcher/RecBack.Patcher/`
The BepInEx plugin. Also C#. Uses something called Harmony to "patch" the game at runtime - like putting sticky notes over the game's instructions to change them.

### `Dockerfile` and `docker-compose.yml`
These let the server run on a NAS (Network Attached Storage - a small computer for your home network). The UGREEN DH-2300 NAS has an ARM64 processor (like a phone chip) and 4GB of RAM. The Docker setup makes the server tiny and efficient.

## What I can't do for you

1. **Get a 2023 Rec Room build** - I can't distribute Rec Room's files (that's copyright infringement). You need to get a build yourself using Steam DepotDownloader or from someone who already has one.

2. **Host the server publicly** - The server runs on YOUR network. If you want friends to connect from outside, you need to set up port forwarding on your router, which I can't do for you.

3. **Make the game 100% work** - Rec Room had hundreds of server features. The RecBack server handles basics (login, profile, rooms) but won't have every single thing the real servers had. Full multiplayer (Photon networking) is very complex and needs more development.

4. **Fix anti-cheat fully** - The BepInEx plugin tries to skip EasyAntiCheat, but different builds may need different patches.

## Why C#?

Rec Room is made with Unity, which uses C#. BepInEx also uses C#. So the patcher (which needs to work inside the game) has to be C# too. The server is also C# because:
- .NET 8 runs great on ARM64 (your NAS)
- It's fast and efficient
- It can use the same JSON formats as the game

## Why Python for the installer?

Python is:
- Already installed on most systems
- Easy to read and modify
- Has great cross-platform support
- Perfect for file management, downloads, and terminal UI

## The name "RecBack"

Simple! "Rec" from Rec Room + "Back" = "Rec Room is Back!" 🎮
