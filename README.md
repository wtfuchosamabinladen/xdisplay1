# XDisplay — Open Source Second Screen App
Use your Android phone as a wired or wireless second monitor for your Windows PC.

---

## What it does
- **Streams your Windows screen** to your Android phone in real time
- **Touch on phone = mouse on PC** — tap to click, drag to move
- Supports both **WiFi** and **USB** connection
- Works on **Android 4.0 and newer**

---

## Project Structure
```
XDisplay/
├── Windows/         ← Run this on your PC
│   ├── ScreenServer.cs
│   ├── XDisplay.csproj
│   └── build.bat
└── Android/         ← Build this as an APK
    ├── app/src/main/
    │   ├── java/com/xdisplay/
    │   │   ├── MainActivity.java
    │   │   └── StreamActivity.java
    │   ├── res/layout/activity_main.xml
    │   └── AndroidManifest.xml
    ├── build.gradle
    └── settings.gradle
```

---

## STEP 1 — Build the Windows App

### Option A: Using Visual Studio (Recommended)
1. Download & install **Visual Studio Community** (free):
   https://visualstudio.microsoft.com/vs/community/
   - During install, tick **".NET desktop development"**
2. Open the `Windows/` folder
3. Double-click `XDisplay.csproj`
4. Press **Ctrl+Shift+B** to build
5. Run `bin\Debug\net48\XDisplay.exe`

### Option B: Using .NET SDK (command line)
1. Install .NET SDK from https://dotnet.microsoft.com/download
2. Open Command Prompt in the `Windows/` folder
3. Run: `dotnet run`

### Option C: Quick build (no install needed)
1. Open the `Windows/` folder
2. Double-click `build.bat`
3. It will produce `XDisplay.exe` in the same folder

---

## STEP 2 — Build the Android App

### Using Android Studio (Recommended for beginners)
1. Download **Android Studio** (free): https://developer.android.com/studio
2. Open Android Studio → **Open an Existing Project**
3. Select the `Android/` folder
4. Wait for Gradle sync to finish
5. Connect your phone via USB (enable **USB Debugging** in Developer Options)
6. Click the green ▶ **Run** button

### Enable USB Debugging on your phone:
1. Go to **Settings → About Phone**
2. Tap **Build Number** 7 times
3. Go back to Settings → **Developer Options**
4. Enable **USB Debugging**

---

## STEP 3 — Using the App

### WiFi Connection (easiest)
1. Make sure PC and phone are on the **same WiFi network**
2. Start **XDisplay.exe** on Windows → click **Start Server**
3. Note the IP shown (e.g. `192.168.1.100`)
4. Open **XDisplay** on your phone
5. Enter the IP → tap **Connect via WiFi**

### USB Connection (lower latency)
1. Install **Android Platform Tools** on Windows:
   https://developer.android.com/studio/releases/platform-tools
   Extract the zip and note the folder path (e.g. `C:\platform-tools`)
2. Add it to your PATH:
   - Search Windows for "Environment Variables"
   - Edit PATH → add the platform-tools folder
3. Connect phone via USB
4. Start **XDisplay.exe** → click **Setup USB**
5. Open **XDisplay** on your phone → tap **Connect via USB**

---

## Adjusting Quality & FPS
In the Windows app:
- **Quality slider** — higher = sharper image but uses more bandwidth
- **FPS dropdown** — lower FPS uses less CPU; 20fps is a good balance for USB

---

## Firewall Note
Windows Firewall may block the app. When prompted, click **Allow Access**.
Or manually allow ports **5555** and **5556** (TCP, inbound) in Windows Defender Firewall.

---

## Technical Details
| Item | Value |
|------|-------|
| Video port | TCP 5555 |
| Input port | TCP 5556 |
| Frame format | JPEG (quality adjustable) |
| Protocol | Big-endian length-prefixed frames |
| Min Android | 4.0 (API 14) |
| Windows requirement | .NET Framework 4.8 (pre-installed on Windows 10/11) |

---

## Troubleshooting

**"Connection failed"**
- Check both devices are on the same network (WiFi)
- Check Windows Firewall is allowing ports 5555 & 5556
- Make sure the server is started (green status) before connecting

**Black screen on Android**
- Try lowering quality/FPS in Windows app
- Ensure screen isn't off or locked on Windows

**USB not working**
- Run `adb devices` in Command Prompt — your phone should appear
- If it says "unauthorized", check your phone screen for a prompt to allow debugging

---

## License
MIT License — free to use, modify, and distribute.
