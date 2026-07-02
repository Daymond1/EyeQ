<div align="center">

<img src="ico.ico" width="80" alt="EyeQ Logo" />

# EyeQ

**Instant QR & Barcode scanning — right from your system tray.**

![Windows](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)
![License](https://img.shields.io/badge/license-MIT-22c55e)
![Release](https://img.shields.io/github/v/release/Daymond1/EyeQ)

EyeQ lives quietly in your Windows system tray and lets you scan **any QR code or barcode visible on your screen** in seconds — no camera, no phone, no fuss.

[⬇️ Download Latest Release](https://github.com/Daymond1/EyeQ/releases) · [🐛 Report a Bug](https://github.com/Daymond1/EyeQ/issues) · [💡 Request a Feature](https://github.com/Daymond1/EyeQ/issues)

</div>

---

## 📋 Table of Contents

- [✨ Features](#-features)
- [🚀 Installation](#-installation)
- [🖥️ Usage](#️-usage)
  - [Screen Capture](#1️⃣-screen-capture)
  - [Clipboard Image](#2️⃣-clipboard-image)
  - [Image File](#3️⃣-image-file)
- [⚙️ Settings](#️-settings)
- [📜 Scan History](#-scan-history)
- [🛠️ Building from Source](#️-building-from-source)
- [🤝 Contributing](#-contributing)
- [📄 License](#-license)

---

## ✨ Features

### 🔍 Supported Code Formats

| Format | Type |
|---|---|
| QR Code | 2D Matrix |
| Data Matrix | 2D Matrix |
| PDF-417 | 2D Stacked |
| Aztec | 2D Matrix |
| EAN-13 / EAN-8 | Linear Barcode |
| UPC-A | Linear Barcode |
| Code-128 | Linear Barcode |
| Code-39 | Linear Barcode |

### 🎯 Core Capabilities

- **Three scan sources** — screen region, clipboard image, or image file
- **Multi-code detection** — scans all codes present in a selected area simultaneously
- **Instant clipboard copy** — results are automatically copied, ready to paste
- **Scan history** — every result is logged with timestamps; search, copy, or open URLs
- **Custom hotkey** — configure your preferred keyboard shortcut (Ctrl/Shift/Alt/Win + any key)
- **Auto-open URLs** — optionally launch detected URLs directly in your browser
- **Sound feedback** — optional audio cue on successful detection
- **Windows startup toggle** — opt into launch-on-login from the tray menu
- **Multi-monitor aware** — works across all monitors, including mixed DPI setups
- **No admin rights required** — runs entirely in user space
- **Lightweight** — lives in the system tray with no taskbar entry

---

## 🚀 Installation

### Method 1 — MSI Installer (Recommended)

1. Go to the [**Releases page**](https://github.com/Daymond1/EyeQ/releases).
2. Download the latest `EyeQ-Installer.msi`.
3. Run the installer and follow the on-screen steps.
4. EyeQ will start automatically and appear in your system tray.

> **Requirements:** Windows 10 or later · .NET Framework 4.8 (pre-installed on Windows 10/11)

### Method 2 — Build from Source

See the [Building from Source](#️-building-from-source) section below.

---

## 🖥️ Usage

EyeQ adds an icon to your **Windows system tray** (bottom-right corner of your taskbar).

- **Left-click** the tray icon — start a screen region scan
- **Right-click** the tray icon — open the context menu for all options

---

### 1️⃣ Screen Capture

Scan any code visible anywhere on your screen.

1. **Left-click** the tray icon, **or** press **`Ctrl + Shift + Q`** (default hotkey).
2. Your cursor changes to a crosshair.
3. **Click and drag** to draw a selection rectangle around the code.
   - Live corner markers, a blue border, and a **W × H px** dimension overlay guide your selection.
   - Press **`Escape`** at any time to cancel.
4. Release the mouse button — EyeQ scans the region instantly.
5. The decoded text is **copied to your clipboard** automatically.

> 💡 Works across **all monitors**, even with different DPI scaling settings.

---

### 2️⃣ Clipboard Image

Already have a screenshot or image in your clipboard?

1. Copy an image to your clipboard (e.g., `Win + Shift + S` or `PrintScreen`).
2. **Right-click** the EyeQ tray icon.
3. Select **"Scan clipboard image"**.
4. The result is decoded and copied to your clipboard.

---

### 3️⃣ Image File

Scan a code from a saved image on disk.

1. **Right-click** the EyeQ tray icon.
2. Select **"Open image file…"**.
3. Browse to and select your image (**PNG, JPG, BMP, GIF, TIFF** supported).
4. The result is decoded and copied to your clipboard.

---

## ⚙️ Settings

Open **Settings** via right-click tray → **"Settings…"**

| Option | Description | Default |
|---|---|---|
| **Hotkey** | Choose modifier keys (Ctrl / Shift / Alt / Win) + a key (A–Z, 0–9, F1–F12) | `Ctrl + Shift + Q` |
| **Sound on detection** | Play a sound when a code is successfully decoded | Off |
| **Auto-open URLs** | Automatically open detected URLs in your default browser (single-result scans only) | Off |
| **Max history entries** | Maximum number of entries kept in scan history (5–500) | 20 |

Settings are stored at `%AppData%\EyeQ\settings.json`.

---

## 📜 Scan History

EyeQ remembers every code you've scanned.

Open the history window via right-click tray → **"History…"**

**Features of the history window:**

- 🌑 Dark-themed interface
- **Copy** — copy any entry's value to clipboard (`Ctrl + C`)
- **Open URL** — open a detected link directly in your browser
- **Delete** — remove individual entries (`Delete` key)
- **Clear All** — wipe the entire history
- **Auto-deduplication** — duplicate scans are not stored twice

History is stored at `%AppData%\EyeQ\history.json`.

---

## 🛠️ Building from Source

**Prerequisites:**

- [Visual Studio 2019 or later](https://visualstudio.microsoft.com/) with the **.NET desktop development** workload
- .NET Framework 4.8 SDK

**Steps:**

```bash
# 1. Clone the repository
git clone https://github.com/Daymond1/EyeQ.git
cd EyeQ
```

2. Open `EyeQ.csproj` in Visual Studio.
3. Restore NuGet packages (**right-click solution → Restore NuGet Packages**).
4. Switch the build configuration to **Release**.
5. Press **`Ctrl + Shift + B`** to build.

The compiled executable will be in `bin\Release\`.

**Key dependency:** [ZXing.Net 0.16.9](https://github.com/micjahn/ZXing.Net) — barcode/QR decoding library.

---

## 🤝 Contributing

Contributions are warmly welcome! Here's how to get involved:

1. **Fork** the repository.
2. **Create a branch** for your feature or fix:
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Commit** your changes with a clear message.
4. **Push** the branch and open a **Pull Request**.

For bugs or feature ideas, please open a [GitHub Issue](https://github.com/Daymond1/EyeQ/issues) first so we can discuss the approach.

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

<div align="center">

Made with ❤️ · [GitHub](https://github.com/Daymond1/EyeQ)

</div>
