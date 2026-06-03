# Scene Setup Guide

## One-Time Scene Setup (5 minutes)

This project uses a **runtime scene builder** — the entire game world
(ground, walls, obstacles, items, player, camera, UI) is created in C# code.
You only need to do this once.

### Steps

1. Open Unity 6.4.x LTS

2. Create a new **3D (Built-In Render Pipeline)** project
   - Name: `TopDownCollector`
   - Location: anywhere on your computer

3. Copy the `Assets/Scripts/` folder from this repo into your project's Assets folder

4. Open the default **SampleScene** (or create a new empty scene)

5. Delete everything in the scene **except** the Main Camera
   - In the Hierarchy: right-click → Delete on any unwanted objects

6. Create a new empty GameObject:
   - **GameObject → Create Empty**
   - Name it: `SceneBuilder`

7. With `SceneBuilder` selected, click **Add Component** in the Inspector
   - Search for `SceneBuilder` and add it

8. Press **Play** ▶ — the script builds everything automatically!

9. **Save the scene**: File → Save As → `Assets/Scenes/MainScene.unity`

---

## Build Settings

1. File → Build Settings
2. Click **Add Open Scenes** (adds MainScene)
3. Platform: **Android**
4. Click **Switch Platform**
5. Player Settings → Other Settings:
   - Package Name: `com.yourname.topdowncollector`
   - Minimum API: 24
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64 ✅
6. Build → choose output folder

---

## TextMeshPro Setup

On first build, Unity may ask to import TMP Essentials.
- Click **Import TMP Essentials** when prompted
- Re-run the scene after import

---

## Troubleshooting

**"PlayerController" not found error**: Make sure all scripts from `Assets/Scripts/` are copied correctly.

**Items not collecting**: Check that the Player GameObject has the `Player` tag (set automatically by SceneBuilder).

**Camera not following**: The CameraFollow script auto-finds the object tagged "Player" at Start.

**Gray screen on Android**: Make sure Camera's Clear Flags is "Solid Color" and Background is not black — SceneBuilder sets this automatically.
