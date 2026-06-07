# 🎮 Collectible Rush — Unity Casual Game

> A top-down collectible game built with Unity as part of the Unity Game Dev Internship Technical Test.

---

## 📖 About the Game

**Collectible Rush** is a top-down casual game where the player moves around an arena collecting items before the timer runs out. Different item types give different point values:

| Item | Shape | Points |
|------|-------|--------|
| 🟡 Common | Yellow Sphere | +1 |
| 🔵 Rare | Blue Cube | +3 |
| 🔴 Epic | Red Gem | +5 |

- Click / tap to move the player
- Collect as many items as possible within 60 seconds
- High score is saved automatically

---

## 🛠️ How to Open the Project

### Requirements
- **Unity 6** (6000.x) or **Unity 2022 LTS**
- **Android Build Support** module installed (via Unity Hub)
- **TextMeshPro** package (auto-imported by Unity)

### Steps
1. Clone or download this repository:
   ```
   git clone https://github.com/khiranay/CollectibleRush.git
   ```
2. Open **Unity Hub** → click **Open** → select the project folder
3. Wait for Unity to import all assets
4. If prompted, click **Import TMP Essentials**
5. Open scene: `Assets/Scenes/MenuScene` or `GameScene`

---

## 📱 How to Build APK

1. Go to **File → Build Settings**
2. Select **Android** → click **Switch Platform**
3. Make sure both scenes are added in **Scenes In Build**:
   - Index 0: `MenuScene`
   - Index 1: `GameScene`
4. Click **Player Settings** and configure:
   - **Company Name**: your name
   - **Product Name**: Collectible Rush
   - **Minimum API Level**: Android 7.0 (API 24)
   - **Scripting Backend**: IL2CPP
   - **Target Architecture**: ARM64
5. Click **Build** → choose output folder → save as `CollectibleRush.apk`

> 💡 Make sure Android SDK & NDK are installed via Unity Hub → Installs → Add Modules

---

## 🧪 How to Test

### In Unity Editor (PC)
1. Open `GameScene`
2. Press **Play** (▶)
3. **Click anywhere** on the arena floor to move the player
4. Collect items before the timer hits 0

### On Android Device
1. Enable **Developer Options** on your Android device
2. Enable **USB Debugging**
3. Connect device via USB
4. In Unity: **File → Build Settings → Run Device** → select your device → **Build and Run**

### APK Install
1. Transfer `CollectibleRush.apk` to your Android device
2. Open the file → tap **Install**
3. Allow installation from unknown sources if prompted

---

## 📁 Project Structure

```
Assets/
├── Scenes/
│   ├── MenuScene.unity
│   └── GameScene.unity
├── Scripts/
│   ├── GameManager.cs
│   ├── PlayerController.cs
│   ├── Collectible.cs
│   ├── ItemSpawner.cs
│   ├── UIManager.cs
│   ├── CameraFollow.cs
│   ├── DecoAnimator.cs
│   ├── AudioManager.cs
│   ├── SceneBuilder.cs
│   └── MenuSceneBuilder.cs
└── Audio/
    └── BGM
    └── CollectSFX
    └── GameOverSFX
```

---

## 🎵 Features

- ✅ Click-to-move player controller
- ✅ 3 collectible item types with weighted random spawning
- ✅ 60-second countdown timer
- ✅ Score system with persistent high score (PlayerPrefs)
- ✅ Particle effects on item collection
- ✅ Background music (looped)
- ✅ Game Over screen with Play Again & Main Menu
- ✅ Procedurally built scene (no manual prefab setup needed)

---

## 👤 Author

**Fakhirah Inayah**  
Unity Game Dev Internship — Technical Test