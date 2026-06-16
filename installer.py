#!/usr/bin/env python3
"""
RecBack Installer v1.0.0
Brings Rec Room back to life with your own private server.
"""

import os
import sys
import json
import time
import shutil
import zipfile
import tarfile
import tempfile
import subprocess
import urllib.request
import urllib.error
import textwrap
import platform
import hashlib
import io
import struct
import re
from pathlib import Path
from typing import Optional, Callable

# ─── ANSI Colors & Styles ─────────────────────────────────────────────

class Style:
    RESET = "\033[0m"
    BOLD = "\033[1m"
    DIM = "\033[2m"
    ITALIC = "\033[3m"
    UNDERLINE = "\033[4m"
    BLINK = "\033[5m"
    REVERSE = "\033[7m"
    HIDDEN = "\033[8m"
    STRIKE = "\033[9m"

    # Foreground
    BLACK = "\033[30m"
    RED = "\033[31m"
    GREEN = "\033[32m"
    YELLOW = "\033[33m"
    BLUE = "\033[34m"
    MAGENTA = "\033[35m"
    CYAN = "\033[36m"
    WHITE = "\033[37m"
    GRAY = "\033[90m"

    # Bright
    BRIGHT_RED = "\033[91m"
    BRIGHT_GREEN = "\033[92m"
    BRIGHT_YELLOW = "\033[93m"
    BRIGHT_BLUE = "\033[94m"
    BRIGHT_MAGENTA = "\033[95m"
    BRIGHT_CYAN = "\033[96m"
    BRIGHT_WHITE = "\033[97m"

    # Background
    BG_BLACK = "\033[40m"
    BG_RED = "\033[41m"
    BG_GREEN = "\033[42m"
    BG_YELLOW = "\033[43m"
    BG_BLUE = "\033[44m"
    BG_MAGENTA = "\033[45m"
    BG_CYAN = "\033[46m"
    BG_WHITE = "\033[47m"

    # Cursor
    CURSOR_UP = "\033[A"
    CLEAR_LINE = "\033[2K"
    CLEAR_SCREEN = "\033[2J\033[H"
    HIDE_CURSOR = "\033[?25l"
    SHOW_CURSOR = "\033[?25h"

# ─── Animations ──────────────────────────────────────────────────────

LOGO = f"""
{Style.BRIGHT_CYAN}██████╗ ███████╗ ██████╗ ██████╗  █████╗  ██████╗██╗  ██╗
{Style.BRIGHT_BLUE}██╔══██╗██╔════╝██╔════╝ ██╔══██╗██╔══██╗██╔════╝██║ ██╔╝
{Style.CYAN}██████╔╝█████╗  ██║      ██████╔╝███████║██║     █████╔╝
{Style.BLUE}██╔══██╗██╔══╝  ██║      ██╔══██╗██╔══██║██║     ██╔═██╗
{Style.BRIGHT_CYAN}██║  ██║███████╗╚██████╗ ██████╔╝██║  ██║╚██████╗██║  ██╗
{Style.GRAY}╚═╝  ╚═╝╚══════╝ ╚═════╝ ╚═════╝ ╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝{Style.RESET}
{Style.DIM}━━━ Rec Room is Back ─━━━{Style.RESET}
"""

def animate_spinner(duration: float = 2.0, message: str = "Loading", color: str = Style.CYAN):
    frames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"]
    end_time = time.time() + duration
    i = 0
    while time.time() < end_time:
        sys.stdout.write(f"\r{color}{frames[i % len(frames)]} {message}{Style.RESET}")
        sys.stdout.flush()
        time.sleep(0.08)
        i += 1
    sys.stdout.write(f"\r{' ' * 40}\r")
    sys.stdout.flush()

def animate_progress(current: int, total: int, prefix: str = "",
                     bar_length: int = 40, color: str = Style.GREEN):
    percent = current / total
    filled = int(bar_length * percent)
    bar = f"{color}{'█' * filled}{Style.GRAY}{'░' * (bar_length - filled)}{Style.RESET}"
    sys.stdout.write(f"\r{prefix} {bar} {percent:6.1%}")
    sys.stdout.flush()

def animate_logo():
    lines = LOGO.strip().split("\n")
    for line in lines:
        sys.stdout.write(line + "\n")
        sys.stdout.flush()
        time.sleep(0.04)
    time.sleep(0.3)

def animate_text(text: str, delay: float = 0.02, color: str = Style.WHITE):
    for char in text:
        sys.stdout.write(f"{color}{char}{Style.RESET}")
        sys.stdout.flush()
        time.sleep(delay)
    print()

def type_writer(text: str, color: str = Style.CYAN):
    animate_text(text, 0.02, color)

# ─── Utilities ───────────────────────────────────────────────────────

def clear_screen():
    sys.stdout.write(Style.CLEAR_SCREEN)
    sys.stdout.flush()

def print_header():
    clear_screen()
    print(LOGO)

def print_step(num: int, text: str, completed: bool = False):
    if completed:
        print(f"  {Style.GREEN}✓{Style.RESET} Step {num}: {text}")
    else:
        print(f"  {Style.CYAN}→{Style.RESET} Step {num}: {text}")

def print_info(text: str):
    print(f"  {Style.BLUE}i{Style.RESET} {text}")

def print_success(text: str):
    print(f"  {Style.GREEN}✓{Style.RESET} {text}")

def print_warn(text: str):
    print(f"  {Style.YELLOW}⚠{Style.RESET} {text}")

def print_error(text: str):
    print(f"  {Style.RED}✗{Style.RESET} {text}")

def prompt_input(text: str, default: str = "", color: str = Style.CYAN) -> str:
    if default:
        val = input(f"  {color}?{Style.RESET} {text} [{Style.DIM}{default}{Style.RESET}]: ").strip()
        return val if val else default
    return input(f"  {color}?{Style.RESET} {text}: ").strip()

def prompt_yes_no(text: str, default: bool = True) -> bool:
    hint = "Y/n" if default else "y/N"
    val = input(f"  {Style.CYAN}?{Style.RESET} {text} [{hint}]: ").strip().lower()
    if not val:
        return default
    return val.startswith("y")

def download_file(url: str, dest: str, progress_callback: Optional[Callable] = None):
    try:
        req = urllib.request.Request(url, headers={
            "User-Agent": "RecBack-Installer/1.0"
        })
        with urllib.request.urlopen(req, timeout=30) as resp:
            total = int(resp.headers.get("Content-Length", 0))
            downloaded = 0
            chunk_size = 8192

            with open(dest, "wb") as f:
                while True:
                    chunk = resp.read(chunk_size)
                    if not chunk:
                        break
                    f.write(chunk)
                    downloaded += len(chunk)
                    if progress_callback and total > 0:
                        progress_callback(downloaded, total)

        return True
    except Exception as e:
        print_error(f"Download failed: {e}")
        return False

def get_github_releases(owner: str, repo: str) -> list:
    """Fetch releases from GitHub API."""
    url = f"https://api.github.com/repos/{owner}/{repo}/releases"
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "RecBack-Installer/1.0", "Accept": "application/vnd.github.v3+json"})
        with urllib.request.urlopen(req, timeout=15) as resp:
            return json.loads(resp.read())
    except:
        return []

def get_latest_release(owner: str, repo: str) -> Optional[dict]:
    """Get the latest release for the current version of RecBack."""
    url = f"https://api.github.com/repos/{owner}/{repo}/releases/latest"
    try:
        req = urllib.request.Request(url, headers={"User-Agent": "RecBack-Installer/1.0", "Accept": "application/vnd.github.v3+json"})
        with urllib.request.urlopen(req, timeout=15) as resp:
            return json.loads(resp.read())
    except:
        return None

def get_latest_bepinex_url() -> str:
    """Get the latest BepInEx IL2CPP download URL for Windows x64."""
    releases = get_github_releases("BepInEx", "BepInEx")
    for release in releases:
        for asset in release.get("assets", []):
            name = asset.get("name", "")
            if "win" in name.lower() and "x64" in name.lower() and "il2cpp" in name.lower():
                return asset["browser_download_url"]
    return "https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.1/BepInEx_Unity_IL2CPP_x64_6.0.0-pre.1.zip"

# ─── Installer Core ──────────────────────────────────────────────────

RECBACK_VERSION = "1.0.0"
RECBACK_REPO = "RecBack"
RECBACK_OWNER = "RecBack"

def check_for_updates():
    """Check if there's a newer version of RecBack available."""
    print_info("Checking for updates...")
    animate_spinner(1.0, "Checking GitHub", Style.CYAN)

    # Check our own repo for newer installer version
    release = get_latest_release("RecBack", "RecBack")
    if release:
        tag = release.get("tag_name", "").lstrip("v")
        if tag and tag != RECBACK_VERSION:
            print_warn(f"RecBack v{tag} is available! You have v{RECBACK_VERSION}")
            if prompt_yes_no("Update now?"):
                # Download updated installer
                for asset in release.get("assets", []):
                    if "installer" in asset.get("name", "").lower():
                        print_info("Downloading updated installer...")
                        # Would download and restart here
                        print_success("Please download the latest installer from GitHub Releases")
                        return
    else:
        print_info("Could not check for updates (offline or no release yet)")

def download_bepinex(dest_dir: str) -> bool:
    """Download and extract BepInEx for IL2CPP."""
    print_info("Downloading BepInEx (IL2CPP loader)...")

    url = get_latest_bepinex_url()
    filename = url.split("/")[-1]

    with tempfile.TemporaryDirectory() as tmp:
        zip_path = os.path.join(tmp, filename)

        def on_progress(curr, total):
            pct = curr / total
            bar_length = 30
            filled = int(bar_length * pct)
            bar = f"{Style.GREEN}{'█' * filled}{Style.GRAY}{'░' * (bar_length - filled)}{Style.RESET}"
            sys.stdout.write(f"\r  {bar} {pct:6.1%}")
            sys.stdout.flush()

        if not download_file(url, zip_path, on_progress):
            return False

        print()
        print_info("Extracting BepInEx...")

        try:
            with zipfile.ZipFile(zip_path, "r") as zf:
                zf.extractall(dest_dir)
        except:
            print_error("Failed to extract BepInEx")
            return False

    print_success("BepInEx installed")
    return True

def setup_doorstop(game_dir: str, bepinex_dir: str) -> bool:
    """Configure Doorstop to load BepInEx."""
    try:
        doorstop_config = """# RecBack Doorstop Configuration
[General]
enabled = true
target_assembly = BepInEx\\core\\BepInEx.Unity.IL2CPP.dll
redirect_output_log = false
ignore_disable_switch = false

[Il2Cpp]
coreclr_path = dotnet\\coreclr.dll
corlib_dir = dotnet
"""
        config_path = os.path.join(game_dir, "doorstop_config.ini")
        with open(config_path, "w") as f:
            f.write(doorstop_config)

        # Copy winhttp.dll for Doorstop
        winhttp_src = os.path.join(bepinex_dir, "winhttp.dll")
        winhttp_dst = os.path.join(game_dir, "winhttp.dll")
        if os.path.exists(winhttp_src):
            shutil.copy2(winhttp_src, winhttp_dst)

        # Copy dotnet runtime
        dotnet_src = os.path.join(bepinex_dir, "dotnet")
        dotnet_dst = os.path.join(game_dir, "dotnet")
        if os.path.exists(dotnet_src) and not os.path.exists(dotnet_dst):
            shutil.copytree(dotnet_src, dotnet_dst)

        # Copy BepInEx folder
        bepinex_src = os.path.join(bepinex_dir, "BepInEx")
        bepinex_dst = os.path.join(game_dir, "BepInEx")
        if os.path.exists(bepinex_src) and not os.path.exists(bepinex_dst):
            shutil.copytree(bepinex_src, bepinex_dst)

        return True
    except Exception as e:
        print_error(f"Doorstop setup failed: {e}")
        return False

def install_patcher_plugin(game_dir: str, plugin_path: str, server_ip: str) -> bool:
    """Install the RecBack.Patcher.dll into BepInEx plugins."""
    try:
        plugins_dir = os.path.join(game_dir, "BepInEx", "plugins")
        os.makedirs(plugins_dir, exist_ok=True)

        if os.path.exists(plugin_path):
            shutil.copy2(plugin_path, os.path.join(plugins_dir, "RecBack.Patcher.dll"))
        else:
            print_warn("Patcher DLL not found, will be downloaded from GitHub")

        # Create config
        config_dir = os.path.join(game_dir, "BepInEx", "config")
        os.makedirs(config_dir, exist_ok=True)

        config_content = f"""## RecBack Patcher Configuration
[Nameserver]
## Set this to your RecBack server IP:port
Target = {server_ip}
"""
        with open(os.path.join(config_dir, "recback.patches.cfg"), "w") as f:
            f.write(config_content)

        return True
    except Exception as e:
        print_error(f"Plugin installation failed: {e}")
        return False

def create_launchers(game_dir: str, build_exe: str):
    """Create batch files to launch the game in VR or Screen mode."""
    exe_name = os.path.basename(build_exe)
    exe_dir = os.path.dirname(build_exe)

    screen_launcher = f"""@echo off
title RecBack - Screen Mode
echo Starting RecBack...
start "" "{exe_name}" +mode:screen
exit
"""
    vr_launcher = f"""@echo off
title RecBack - VR Mode
echo Starting RecBack...
start "" "{exe_name}" +mode:vr
exit
"""

    with open(os.path.join(game_dir, "RecBack_ScreenMode.bat"), "w") as f:
        f.write(screen_launcher)
    with open(os.path.join(game_dir, "RecBack_VR.bat"), "w") as f:
        f.write(vr_launcher)

    print_success("Created launchers (Screen Mode + VR)")

def create_steam_appid(game_dir: str):
    """Create steam_appid.txt for non-Steam launch."""
    with open(os.path.join(game_dir, "steam_appid.txt"), "w") as f:
        f.write("471710")

def run_depot_downloader(dest_dir: str) -> bool:
    """Use DepotDownloader to download a 2023 Rec Room build from Steam."""
    print_info("You need a 2023 Rec Room build from Steam.")
    print_info("RecBack can use DepotDownloader to get it.")

    if not prompt_yes_no("Download Rec Room build via DepotDownloader?"):
        print_info("You can manually place a build in the game folder later.")
        return False

    print_info("Downloading DepotDownloader...")

    dd_url = "https://github.com/SteamRE/DepotDownloader/releases/latest/download/DepotDownloader-win-x64.zip"
    with tempfile.TemporaryDirectory() as tmp:
        dd_zip = os.path.join(tmp, "DepotDownloader.zip")

        def on_progress(curr, total):
            pct = curr / total
            bar_length = 30
            filled = int(bar_length * pct)
            bar = f"{Style.GREEN}{'█' * filled}{Style.GRAY}{'░' * (bar_length - filled)}{Style.RESET}"
            sys.stdout.write(f"\r  {bar} {pct:6.1%}")
            sys.stdout.flush()

        if not download_file(dd_url, dd_zip, on_progress):
            print_error("Failed to download DepotDownloader")
            return False

        print()
        print_info("Extracting DepotDownloader...")
        with zipfile.ZipFile(dd_zip, "r") as zf:
            zf.extractall(tmp)

        dd_exe = os.path.join(tmp, "DepotDownloader.exe")
        if not os.path.exists(dd_exe):
            print_error("DepotDownloader executable not found")
            return False

        print()
        print_info("Rec Room Steam App ID: 471710")
        print_info("Depot ID: 471711 (Rec Room main content)")
        print_warn("You need a Steam account that owns Rec Room.")

        username = prompt_input("Steam username")
        if not username:
            print_error("Steam username required")
            return False

        import getpass
        password = getpass.getpass(f"  {Style.CYAN}?{Style.RESET} Steam password: ")
        if not password:
            print_error("Steam password required")
            return False

        manifest_id = prompt_input(
            "2023 build manifest ID (from SteamDB)",
            "7490748483298966814"
        )

        print()
        print_info("Downloading Rec Room 2023 build...")
        print_warn("This will download ~5-10 GB. It may take a while.")

        result = subprocess.run([
            dd_exe,
            "-app", "471710",
            "-depot", "471711",
            "-manifest", manifest_id,
            "-username", username,
            "-password", password,
            "-dir", dest_dir
        ], capture_output=True, text=True)

        if result.returncode != 0:
            print_error(f"DepotDownloader failed:\n{result.stderr[:500]}")
            return False

        print_success("Build downloaded!")
        return True

    return False

def rename_build_folder(game_dir: str, original_name: str, new_name: str):
    """Rename the game executable and data folder like RecPlace does."""
    exe_src = os.path.join(game_dir, original_name + ".exe")
    exe_dst = os.path.join(game_dir, new_name + ".exe")
    data_src = os.path.join(game_dir, original_name + "_Data")
    data_dst = os.path.join(game_dir, new_name + "_Data")

    if os.path.exists(exe_src) and not os.path.exists(exe_dst):
        os.rename(exe_src, exe_dst)
    if os.path.exists(data_src) and not os.path.exists(data_dst):
        os.rename(data_src, data_dst)

def find_build_exe(directory: str) -> Optional[str]:
    """Find a Rec Room executable in the directory."""
    candidates = ["RecRoom.exe", "Recroom_Release.exe", "RecRoom_Release.exe", "RecBack.exe"]
    for c in candidates:
        path = os.path.join(directory, c)
        if os.path.exists(path):
            return path
    # Fallback: find any exe that looks like Rec Room
    for f in os.listdir(directory):
        if f.lower().endswith(".exe") and ("rec" in f.lower() or "room" in f.lower()):
            return os.path.join(directory, f)
    return None

# ─── Main Installation Flow ──────────────────────────────────────────

def main():
    # Hide cursor during animation
    sys.stdout.write(Style.HIDE_CURSOR)

    animate_logo()
    type_writer("Welcome to RecBack - The Rec Room Revival Project!\n", Style.BRIGHT_CYAN)
    time.sleep(0.3)

    sys.stdout.write(Style.SHOW_CURSOR)

    # ─── Step 1: Choose Install Directory ─────────────────────
    print()
    print_step(1, "Choose where to install RecBack")

    default_dir = os.path.join(os.path.expanduser("~"), "RecBack")
    install_dir = prompt_input("Installation directory", default_dir)
    install_dir = os.path.abspath(install_dir)
    os.makedirs(install_dir, exist_ok=True)

    game_dir = os.path.join(install_dir, "game")  # The Rec Room build goes here
    server_dir = install_dir  # Server files go here

    print_success(f"Will install to: {install_dir}")
    time.sleep(0.5)

    # ─── Step 2: Check for updates ────────────────────────────
    print()
    print_step(2, "Checking for RecBack updates")
    check_for_updates()
    time.sleep(0.3)

    # ─── Step 3: Get the game build ───────────────────────────
    print()
    print_step(3, "Get a Rec Room build")

    # Check if there's already a build in the game directory
    existing_exe = find_build_exe(game_dir)
    if existing_exe:
        print_success(f"Found existing build: {os.path.basename(existing_exe)}")
        if not prompt_yes_no("Re-download/install a different build?", False):
            build_exe = existing_exe
        else:
            build_exe = None
    else:
        build_exe = None

    if not build_exe:
        os.makedirs(game_dir, exist_ok=True)

        if prompt_yes_no("Download a 2023 Rec Room build from Steam?"):
            run_depot_downloader(game_dir)
            build_exe = find_build_exe(game_dir)

        if not build_exe:
            print_warn("No build found. You can:")
            print_info("1. Copy a 2023 build into: " + game_dir)
            print_info("2. Or copy your existing build from: " +
                       os.path.join(os.path.expanduser("~"), "Downloads", "BLANKRENAMETHIS"))
            if prompt_yes_no("Copy your existing BLANKRENAMETHIS build?"):
                src = r"C:\Users\invin\Downloads\BLANKRENAMETHIS"
                if os.path.exists(src):
                    for item in os.listdir(src):
                        s = os.path.join(src, item)
                        d = os.path.join(game_dir, item)
                        if os.path.isdir(s):
                            shutil.copytree(s, d, dirs_exist_ok=True)
                        else:
                            shutil.copy2(s, d)
                    build_exe = find_build_exe(game_dir)
                    print_success("Build copied!")
                else:
                    print_error("Source build not found")

            if not build_exe:
                print_error("No build available. Please add one manually and re-run.")
                input(f"\n  {Style.DIM}Press Enter to exit...{Style.RESET}")
                return

    # ─── Step 4: Configure Server IP ──────────────────────────
    print()
    print_step(4, "Configure your RecBack server")

    server_ip = prompt_input(
        "RecBack server IP address",
        "localhost" if prompt_yes_no("Running server on this machine?", True) else "192.168.1.100"
    )
    server_port = prompt_input("Server port (NameServer)", "9999")

    print_success(f"Server: {server_ip}:{server_port}")
    time.sleep(0.3)

    # ─── Step 5: Install BepInEx + Doorstop ──────────────────
    print()
    print_step(5, "Install BepInEx mod loader")

    bepinex_dir = os.path.join(install_dir, "bepinex")
    os.makedirs(bepinex_dir, exist_ok=True)

    if not os.path.exists(os.path.join(bepinex_dir, "BepInEx", "core", "BepInEx.Core.dll")):
        animate_spinner(0.5, "Preparing BepInEx download", Style.CYAN)

        if not download_bepinex(bepinex_dir):
            # Fallback: copy from bundled bepinex folder
            bundled = os.path.join(os.path.dirname(os.path.abspath(__file__)), "bepinex")
            if os.path.exists(bundled):
                print_info("Using bundled BepInEx...")
                shutil.copytree(bundled, bepinex_dir, dirs_exist_ok=True)
            else:
                print_error("BepInEx not available. Download manually from github.com/BepInEx/BepInEx")
                input(f"\n  {Style.DIM}Press Enter to exit...{Style.RESET}")
                return
    else:
        print_success("BepInEx already installed")

    # Set up Doorstop
    animate_spinner(0.3, "Configuring Doorstop", Style.CYAN)
    if setup_doorstop(game_dir, bepinex_dir):
        print_success("Doorstop configured")
    else:
        print_error("Failed to configure Doorstop")

    # ─── Step 6: Install RecBack Patcher Plugin ──────────────
    print()
    print_step(6, "Install RecBack patches")

    plugins_dir = os.path.join(game_dir, "BepInEx", "plugins")
    os.makedirs(plugins_dir, exist_ok=True)

    # Try finding the pre-compiled DLL
    patcher_dll = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                "build", "patcher", "RecBack.Patcher.dll")
    if not os.path.exists(patcher_dll):
        patcher_dll = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    "RecBack.Patcher.dll")

    if os.path.exists(patcher_dll):
        animate_spinner(0.3, "Installing patcher plugin", Style.CYAN)
        install_patcher_plugin(game_dir, patcher_dll, f"{server_ip}:{server_port}")
        print_success("RecBack Patcher plugin installed")
    else:
        print_warn("Patcher DLL not bundled. Install manually from GitHub releases.")
        config_dir = os.path.join(game_dir, "BepInEx", "config")
        os.makedirs(config_dir, exist_ok=True)
        with open(os.path.join(config_dir, "recback.patches.cfg"), "w") as f:
            f.write(f"[Nameserver]\nTarget = {server_ip}:{server_port}\n")
        print_info("Config file created. Place RecBack.Patcher.dll in BepInEx/plugins/")

    # ─── Step 7: Create Launchers ────────────────────────────
    print()
    print_step(7, "Create launchers")

    build_exe = find_build_exe(game_dir) or ""
    create_launchers(game_dir, build_exe)
    create_steam_appid(game_dir)

    time.sleep(0.3)

    # ─── Step 8: Summary ─────────────────────────────────────
    print()
    print("  " + Style.BRIGHT_GREEN + "═" * 50 + Style.RESET)
    print(f"  {Style.BRIGHT_GREEN}RecBack v{RECBACK_VERSION} Installation Complete!{Style.RESET}")
    print("  " + Style.BRIGHT_GREEN + "═" * 50 + Style.RESET)
    print()
    print(f"  {Style.CYAN}Game:{Style.RESET}     {game_dir}")
    print(f"  {Style.CYAN}Server:{Style.RESET}    {server_ip}:{server_port}")
    print(f"  {Style.CYAN}Launcher:{Style.RESET}  {os.path.join(game_dir, 'RecBack_ScreenMode.bat')}")
    print()
    print("  " + Style.DIM + "Quick Start:" + Style.RESET)
    print(f"  1. Start the server:  {Style.BOLD}dotnet run --project server{Style.RESET}")
    print(f"  2. Or on NAS:         {Style.BOLD}docker compose up -d{Style.RESET}")
    print(f"  3. Launch the game:   {Style.BOLD}RecBack_ScreenMode.bat{Style.RESET}")
    print()
    print(f"  {Style.YELLOW}Note: The first launch may take a while as BepInEx generates{Style.RESET}")
    print(f"  {Style.YELLOW}interop assemblies for your game build.{Style.RESET}")
    print()

    input(f"  {Style.DIM}Press Enter to exit...{Style.RESET}")

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print(f"\n  {Style.YELLOW}Installation cancelled.{Style.RESET}")
        sys.stdout.write(Style.SHOW_CURSOR)
        sys.exit(1)
    except Exception as e:
        print(f"\n  {Style.RED}Error: {e}{Style.RESET}")
        sys.stdout.write(Style.SHOW_CURSOR)
        sys.exit(1)
