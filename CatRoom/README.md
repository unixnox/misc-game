# Cozy Cat Room (ห้องแมวเหมียว)

เกมเลี้ยงแมวในห้องเล็ก ๆ แบบ 2.5D (มุมมอง isometric) โทนการ์ตูน ทำด้วย **Unity** สำหรับเล่นบน **YouTube Playables** (WebGL)

- ตกแต่งห้อง: วาง ย้าย หมุน และเก็บเฟอร์นิเจอร์บนกริด 8×8 เปลี่ยนวอลเปเปอร์และพื้นห้องได้
- ร้านค้า: เฟอร์นิเจอร์, ของเล่น, อาหาร, วอลเปเปอร์ และพื้นห้อง
- แมวมีค่า **อิ่มท้อง / สนุก / พลังงาน / ความรัก** ซึ่งรวมกันเป็น **ความสุข**
- **หาเงินจากความสุขของแมว**: แมวที่มีความสุขจะปล่อยเหรียญลอยขึ้นมา แตะเพื่อเก็บได้ **x2** (ถ้าไม่แตะ ระบบจะเก็บให้เองแบบ x1)
- **ความสบายของห้อง** (มาจากของแต่งห้อง) เป็นตัวคูณเหรียญ (+2% ต่อแต้ม สูงสุด x3) และช่วยให้แมวจรไว้ใจเร็วขึ้น
- **แมวจร** จะแวะมาที่ประตูเป็นระยะ ให้อาหาร ลูบหัว เล่นด้วย จนค่า **ความไว้ใจ** เต็ม 100 แล้วกด **รับเลี้ยง** (สูงสุด 6 ตัว)
  - แมวจรแต่ละตัวมี **อาหารโปรด** ที่ซ่อนอยู่ ให้ถูกจะได้ความไว้ใจ x2
  - ถ้าไปลูบตอนยังไม่ไว้ใจ อาจโดนขู่ฟ่อ!
  - มีสายพันธุ์ไทยแบบหายาก: วิเชียรมาศ, สีสวาด (โคราช), ขาวมณี (ตาสองสี)
- แมวทำกิจกรรมเอง: นอนบนเบาะ ปีนคอนโดแมว ลับเล็บ เล่นของเล่น นั่งดูตู้ปลา ไปรอที่ชามข้าวตอนหิว
- รายได้ตอนออฟไลน์ (25% สูงสุด 3 ชั่วโมง) + popup ต้อนรับกลับ
- รองรับ **ภาษาไทย / อังกฤษ** (เลือกอัตโนมัติจาก `ytgame.system.getLanguage()`) และจอแนวตั้ง/แนวนอน (มือถือ/เดสก์ท็อป)

ไม่ต้องมีไฟล์อาร์ตหรือเสียงเลย: โมเดล 3D ประกอบจาก primitive, toon shader + เส้นขอบ (outline), พื้นผิว/ไอคอน/เสียง/เพลงสร้างด้วยโค้ดทั้งหมด
(ไอคอนในร้านค้าเรนเดอร์จากโมเดลจริงตอนเริ่มเกม) มีแค่ฟอนต์ Kanit (ภาษาไทย, ไลเซนส์ OFL)

---

## เริ่มต้นใช้งาน

1. ติดตั้ง **Unity 6 (6000.0 LTS)** ผ่าน Unity Hub พร้อมโมดูล **WebGL Build Support**
2. Unity Hub → **Add project from disk** → เลือกโฟลเดอร์ `CatRoom/`
3. เปิดโปรเจกต์ครั้งแรก สคริปต์ `Assets/Editor/CatRoomBuild.cs` จะสร้างซีน `Assets/Scenes/Main.unity` และตั้งค่า WebGL ให้อัตโนมัติ
   (หรือกดเมนู **Cat Room → 1. Setup Project** เอง)
4. กด **Play** ได้เลย (เกมสร้างตัวเองทั้งหมดตอนรันผ่าน `Bootstrap` ไม่ต้องลากอะไรใส่ซีน)

> ทดสอบโหมดมือถือแนวตั้ง: ในหน้าต่าง Game ตั้งความละเอียดเป็น 1080×1920 ได้เลย UI จะจัดเลย์เอาต์ใหม่ให้อัตโนมัติ

ปุ่มลัดตอนตกแต่ง (เดสก์ท็อป): `R` = หมุน, `Esc` = ยกเลิก, ลูกกลิ้งเมาส์ = ซูม (มือถือใช้สองนิ้วซูม)

ล้างเซฟในเอดิเตอร์: **Cat Room → Delete Local Save** หรือในเกม **ตั้งค่า → เริ่มเกมใหม่**

## Build สำหรับ YouTube Playables

1. เมนู **Cat Room → 2. Build WebGL for YouTube Playables**
   - ใช้ WebGL template `Assets/WebGLTemplates/YouTubePlayables` ซึ่งโหลด SDK `https://www.youtube.com/game_api/v1`
   - บีบอัด Gzip + decompression fallback, ปิด data caching
2. ผลลัพธ์อยู่ที่ `CatRoom/Builds/YouTubePlayables/`
3. Zip **เนื้อหาข้างใน** โฟลเดอร์ (ให้ `index.html` อยู่ที่ root ของ zip) แล้วอัปโหลดใน YouTube Playables / Developer Portal
4. ทดสอบก่อนส่งด้วย **Playables Test Suite** ของ YouTube

Build แบบ command line (CI):

```bash
Unity -batchmode -quit -projectPath CatRoom -buildTarget WebGL \
      -executeMethod CatRoom.EditorTools.CatRoomBuild.BuildFromCommandLine
```

### การเชื่อมต่อ Playables SDK

| ความต้องการของ Playables | จัดการที่ |
|---|---|
| `firstFrameReady()` เมื่อแสดงเฟรมแรก (หน้าโหลด) | `index.html` ของ template |
| `gameReady()` เมื่อเล่นได้ | `GameManager.Boot()` → `PlayablesBridge.GameReady()` |
| `loadData()` / `saveData()` (เซฟบนคลาวด์ของ YouTube) | `PlayablesBridge` + `YouTubePlayables.jslib` (นอก YouTube ใช้ PlayerPrefs แทน) |
| `onPause` / `onResume` | หยุดเวลา (`Time.timeScale = 0`), หยุดเสียง และเซฟทันที |
| `isAudioEnabled` / `onAudioEnabledChange` | `SfxManager.SetPlatformAudio()` |
| `getLanguage()` | เลือกภาษาไทย/อังกฤษอัตโนมัติ |
| `engagement.sendScore()` | ส่งจำนวนเหรียญสะสมทั้งหมด (lifetime coins) |
| ไม่มีลิงก์ออกนอกเกม, ไม่ใช้ context menu | template ปิด context menu ไว้แล้ว |

เซฟเป็น JSON ขนาดไม่กี่ KB (ไม่เกินลิมิตของ Playables) ระบบเซฟอัตโนมัติทุก 10–30 วินาทีเมื่อมีการเปลี่ยนแปลง และตอนเกมถูก pause

---

## โครงสร้างโปรเจกต์

```
CatRoom/
├─ Assets/
│  ├─ Scripts/
│  │  ├─ Core/        GameManager (บูต/เศรษฐกิจ/เซฟ/อินพุต), SaveData, ItemDatabase, Loc (TH/EN), CameraRig, InputUtil
│  │  ├─ Room/        RoomManager (ห้อง, กริด, ธีม, ประตู), ItemModels (โมเดลของทุกชิ้น), PlacementController (โหมดตกแต่ง)
│  │  ├─ Cats/        CatController (AI + ค่าต่าง ๆ + การโต้ตอบ), CatVisual (โมเดล+แอนิเมชัน), CatAppearance (สายพันธุ์/ชื่อ), CatManager (แมวจร/รับเลี้ยง)
│  │  ├─ UI/          GameUI (HUD, ร้านค้า, ตกแต่ง, แผงแมว, popup), FloatingUI (เหรียญ/หัวใจ/ความคิดเหนือหัวแมว), UIKit
│  │  ├─ Rendering/   Toon (material cache), ProcGen (mesh/texture/sprite), IconRenderer (เรนเดอร์ไอคอนจากโมเดล 3D)
│  │  ├─ Audio/       SfxManager (สังเคราะห์เสียงเหมียว/เหรียญ/เพลงกล่อม)
│  │  └─ Platform/    PlayablesBridge (ytgame SDK)
│  ├─ Resources/Shaders/  CatToon, CatToonOutline (cel shading + inverted-hull outline), CatGhost
│  ├─ Resources/Fonts/    Kanit-Regular.ttf (+ OFL license)
│  ├─ Plugins/WebGL/      YouTubePlayables.jslib
│  ├─ WebGLTemplates/YouTubePlayables/index.html
│  └─ Editor/CatRoomBuild.cs
├─ Packages/manifest.json
└─ ProjectSettings/ProjectVersion.txt
```

## เพิ่มของใหม่ในร้าน

1. เพิ่ม `ItemDef` ใน `ItemDatabase.cs` (ราคา, ขนาดบนกริด `w`×`d`, ค่าความสบาย, การใช้งานของแมว `use`, ความสูงที่แมวนั่ง `useHeight`, อัตราเพิ่มค่า `funRate/energyRate`)
2. เพิ่มฟังก์ชันสร้างโมเดลใน `ItemModels.Build()` (ใช้ `Toon.Prim(...)` ประกอบรูปทรง) — ไอคอนร้านค้าจะถูกเรนเดอร์ให้เอง
3. ถ้ามีข้อความใหม่ เพิ่มคีย์ใน `Loc.cs` ทั้งไทยและอังกฤษ

ปรับสมดุลเกม: อัตราลดของค่าต่าง ๆ อยู่บนสุดของ `CatController.cs`, สูตรเหรียญใน `CatController.UpdateIncome()`,
ความถี่แมวจรใน `CatManager`, เงินเริ่มต้น/ของเริ่มต้นใน `SaveData.CreateNew()`

## ข้อจำกัดที่รู้อยู่

- ใช้ `UnityEngine.UI.Text` (legacy) เพื่อให้ไม่ต้อง import TMP Essentials — สระ/วรรณยุกต์ไทยบางคู่อาจซ้อนกันเล็กน้อย
  ถ้าต้องการตัวอักษรไทยสวยสมบูรณ์ ให้เปลี่ยนเป็น TextMeshPro + font asset ของ Kanit
- แมวเดินทะลุเฟอร์นิเจอร์ได้ (ไม่มี pathfinding) เพื่อความเรียบง่าย
- ยังไม่ได้ทดสอบบนอุปกรณ์จริง/Playables Test Suite — ควรทดสอบก่อนส่ง

ฟอนต์ Kanit © The Kanit Project Authors — SIL Open Font License 1.1 (`Assets/Resources/Fonts/Kanit-OFL.txt`)
