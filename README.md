# 🎮 Top-Down Collector — FXMedia Technical Test

A simple Android top-down collecting game built with **Unity 6.4.x LTS** using **only primitive shapes** (no external assets).

---

## 📋 Requirements Checklist

### Core (Must Have)
| # | Requirement | Status |
|---|-------------|--------|
| 1 | Player moves by tapping ground | ✅ |
| 2 | Top-down camera follows player | ✅ |
| 3 | At least 5 collectible items | ✅ (10 total) |
| 4 | Player collects items by touching them | ✅ |
| 5 | Score increases on collection | ✅ |
| 6 | Collected items disappear | ✅ |
| 7 | Score displayed on screen (UI Text) | ✅ |
| 8 | At least 1 obstacle blocking movement | ✅ (8 obstacles) |
| 9 | Builds to Android APK | ✅ |

### Bonus (Nice to Have)
| # | Requirement | Status |
|---|-------------|--------|
| 1 | Sound effect on collection | ✅ (runtime AudioSource) |
| 2 | 60-second timer + game over screen | ✅ |
| 3 | Different item types (different point values) | ✅ (+1 / +3 / +5) |
| 4 | Particle effect on collection | ✅ (runtime ParticleSystem) |
| 5 | High score saved (PlayerPrefs) | ✅ |

---

## 🛠️ How to Open the Project

1. Install **Unity Hub** from [unity.com](https://unity.com/download)
2. Install **Unity 6.4.x LTS** via Unity Hub
   - Make sure to include **Android Build Support** + **Android SDK & NDK Tools**
3. Clone this repository:
   ```bash
   git clone https://github.com/<your-username>/topdown-collector.git
   ```
4. In Unity Hub → **Open** → select the project folder
5. Allow Unity to import and compile (first time may take 2–3 minutes)
6. Open **Assets/Scenes/MainScene.unity**
7. Press **Play** to test in the editor

---

## 📱 How to Build the APK

### Prerequisites
- Unity 6.4.x LTS with Android Build Support
- Android SDK / NDK (installed via Unity Hub)
- Java JDK (bundled with Unity)

### Build Steps

1. Open **File → Build Settings**
2. Select **Android** platform → click **Switch Platform**
3. Click **Player Settings** and configure:
   - **Company Name**: Your name
   - **Product Name**: TopDownCollector
   - **Package Name**: `com.yourname.topdowncollector`
   - **Minimum API Level**: Android 7.0 (API 24) or higher
   - **Target API Level**: Automatic (highest installed)
   - **Scripting Backend**: IL2CPP
   - **Target Architecture**: ARM64 ✅, ARMv7 ✅
4. Back in Build Settings → **Build**
5. Choose output folder → Unity builds `TopDownCollector.apk`

### Install on Device
```bash
# Enable USB debugging on your Android phone first
adb install TopDownCollector.apk
```
Or transfer the APK to your phone and install directly (allow "Unknown Sources").

---

## 🎮 How to Play / Test

### Controls
- **Tap anywhere on the ground** → player walks to that position
- Collect glowing items to earn points
- Avoid obstacles (brown boxes) — they block your path

### Item Types
| Visual | Type | Points |
|--------|------|--------|
| 🟡 Yellow Sphere | Common | +1 |
| 🔵 Blue Cube | Rare | +3 |
| 🔴 Red Gem | Epic | +5 |

### Goal
- Collect as many items as possible before the **60-second timer** runs out
- Your **best score is saved** automatically
- Tap **PLAY AGAIN** on the game over screen to restart

---

## 📁 Project Structure

```
Assets/
├── Scenes/
│   └── MainScene.unity        ← Only scene needed
├── Scripts/
│   ├── SceneBuilder.cs        ← Builds entire scene at runtime
│   ├── PlayerController.cs    ← Tap-to-move with Rigidbody
│   ├── Collectible.cs         ← Item types, particles, collection logic
│   ├── GameManager.cs         ← Score, timer, high score, game state
│   ├── UIManager.cs           ← HUD, popups, game over screen
│   └── CameraFollow.cs        ← Smooth top-down camera
```

> 💡 **No external assets.** All materials, shapes, particles, and UI are created in code at runtime.

---

## 🔧 Scene Setup (for fresh project)

If you want to recreate the scene from scratch:

1. Create a **new empty scene**
2. Create an empty GameObject named `SceneBuilder`
3. Attach `SceneBuilder.cs` to it
4. Press Play — the script builds everything automatically

That's it! No other manual setup needed.

---

## 📦 Technical Details

- **Engine**: Unity 6.4.x LTS
- **Language**: C#
- **Platform**: Android (API 24+)
- **Architecture**: ARM64 + ARMv7 (IL2CPP)
- **Assets**: Primitive shapes only (Cube, Sphere, Capsule)
- **UI**: TextMeshPro (included with Unity)
- **Physics**: Rigidbody + Trigger Colliders

---

## 👤 Author

**[Your Name]**  
Unity Developer Intern Candidate  
Technical Test Submission — FXMedia

---

*Submitted to: maynard@fxmweb.com | cc: sheila@fxmweb.com, anna@fxmweb.com, meuti@fxmweb.com*  
*Subject: [Technical Test] Unity Game Dev Internship - [Your Name]*
