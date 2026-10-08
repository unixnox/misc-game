using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CatRoom
{
    /// <summary>Creates the game at startup, so no scene setup is required: just press Play.</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (GameManager.I != null) return;
            new GameObject("CatRoomGame").AddComponent<GameManager>();
        }
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public SaveData Data { get; private set; }
        public bool Ready { get; private set; }

        public PlayablesBridge Platform { get; private set; }
        public SfxManager Sfx { get; private set; }
        public RoomManager Room { get; private set; }
        public CatManager Cats { get; private set; }
        public PlacementController Placement { get; private set; }
        public IconRenderer Icons { get; private set; }
        public GameUI UI { get; private set; }
        public FloatingUI Floating { get; private set; }
        public Camera MainCamera { get; private set; }
        public CameraRig CameraRig { get; private set; }

        public CatController Selected { get; private set; }
        public event Action SelectionChanged;
        public event Action InventoryChanged;

        GameObject selectionRing;
        bool saveDirty;
        float saveTimer;
        float autosaveTimer;
        Vector2 pressPos;
        bool pressed;
        string loadedJson;
        bool loadDone, langDone;
        string platformLang;
        long lastScoreSent = -1;

        const float MinSaveInterval = 10f;
        const float AutosaveInterval = 30f;
        const float MaxOfflineSeconds = 3f * 3600f;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;

            Application.targetFrameRate = 60;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowDistance = 30f;
            QualitySettings.shadowCascades = 1;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.antiAliasing = 2;

            Platform = new GameObject(PlayablesBridge.ObjectName).AddComponent<PlayablesBridge>();
            Platform.transform.SetParent(transform, false);
            Platform.Init();
            Platform.PauseChanged += OnPlatformPause;
            Platform.AudioEnabledChanged += on => Sfx.SetPlatformAudio(on);

            Sfx = gameObject.AddComponent<SfxManager>();
            Sfx.Init();
            Sfx.SetPlatformAudio(Platform.PlatformAudioEnabled);
            UIKit.ClickSound = () => Sfx.Play(Sound.Click, 0.5f);

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            SetupCameraAndLight();
            StartCoroutine(Boot());
        }

        IEnumerator Boot()
        {
            Platform.RequestLanguage(lang => { platformLang = lang; langDone = true; });
            Platform.LoadData(json => { loadedJson = json; loadDone = true; });

            float wait = 0f;
            while ((!loadDone || !langDone) && wait < 6f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            Data = ParseSave(loadedJson) ?? SaveData.CreateNew();
            Loc.Current = Data.language >= 0 ? (Lang)Data.language : LangFromTag(platformLang);
            Sfx.SetUserSound(Data.soundOn);

            Icons = gameObject.AddComponent<IconRenderer>();
            Icons.Init();

            Room = new GameObject("Room").AddComponent<RoomManager>();
            Room.Build(Data);
            Room.Changed += RequestSave;

            Cats = new GameObject("Cats").AddComponent<CatManager>();
            Cats.SpawnOwned(Data.cats);

            Placement = gameObject.AddComponent<PlacementController>();

            Floating = new GameObject("FloatingUI").AddComponent<FloatingUI>();
            Floating.Build();
            UI = new GameObject("GameUI").AddComponent<GameUI>();
            UI.Build();

            yield return null; // let one frame render before reporting ready
            ApplyOfflineProgress();
            Ready = true;
            Platform.GameReady();

            if (!Data.tutorialDone) UI.ShowTutorial();
            RequestSave();
        }

        static Lang LangFromTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return Application.systemLanguage == SystemLanguage.Thai ? Lang.TH : Lang.EN;
            return tag.StartsWith("th", StringComparison.OrdinalIgnoreCase) ? Lang.TH : Lang.EN;
        }

        SaveData ParseSave(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d == null || d.cats == null || d.cats.Count == 0) return null;
                if (d.placed == null) d.placed = new System.Collections.Generic.List<PlacedItemData>();
                if (d.inventory == null) d.inventory = new System.Collections.Generic.List<InventoryEntry>();
                if (d.ownedThemes == null) d.ownedThemes = new System.Collections.Generic.List<string>();
                if (!d.ownedThemes.Contains(ItemDatabase.DefaultWallpaper)) d.ownedThemes.Add(ItemDatabase.DefaultWallpaper);
                if (!d.ownedThemes.Contains(ItemDatabase.DefaultFlooring)) d.ownedThemes.Add(ItemDatabase.DefaultFlooring);
                return d;
            }
            catch (Exception e)
            {
                Platform.LogWarning("Corrupt save ignored: " + e.Message);
                return null;
            }
        }

        void ApplyOfflineProgress()
        {
            if (Data.lastSaveUnix <= 0) return;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            float elapsed = Mathf.Clamp(now - Data.lastSaveUnix, 0, MaxOfflineSeconds);
            if (elapsed < 120f) return;

            long earned = 0;
            int comfort = Room.Comfort;
            foreach (var c in Cats.Owned)
            {
                float h = CatController.HappinessOf(c.Data, comfort);
                float perTick = (2f + 8f * Mathf.Pow(h / 100f, 1.5f)) * Room.ComfortMultiplier;
                earned += Mathf.RoundToInt(perTick * elapsed / 10f * 0.25f); // offline earns at 25%
                c.Data.hunger = Mathf.Max(15f, c.Data.hunger - 0.045f * elapsed);
                c.Data.fun = Mathf.Max(20f, c.Data.fun - 0.05f * elapsed);
                c.Data.energy = Mathf.Max(c.Data.energy, 75f);
            }
            if (earned > 0)
            {
                AddCoins(earned);
                UI.ShowWelcomeBack(earned, elapsed);
            }
        }

        // ---------------- Camera & light ----------------

        void SetupCameraAndLight()
        {
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            MainCamera = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            CameraRig = camGo.AddComponent<CameraRig>();
            CameraRig.Init(MainCamera);

            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            light.shadowBias = 0.03f;
            light.shadowNormalBias = 0.3f;
            lightGo.transform.rotation = Quaternion.Euler(52f, -22f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.6f, 0.7f);
        }

        // ---------------- Economy & inventory ----------------

        public long Coins => Data != null ? Data.coins : 0;

        public void AddCoins(long amount)
        {
            if (amount <= 0) return;
            Data.coins += amount;
            Data.lifetimeCoins += amount;
            RequestSave();
        }

        public bool Spend(int amount)
        {
            if (Data.coins < amount) return false;
            Data.coins -= amount;
            RequestSave();
            return true;
        }

        public int Count(string id)
        {
            foreach (var e in Data.inventory) if (e.id == id) return e.count;
            return 0;
        }

        public void AddItem(string id, int n = 1, bool notify = true)
        {
            var entry = Data.inventory.Find(e => e.id == id);
            if (entry == null) Data.inventory.Add(entry = new InventoryEntry { id = id, count = 0 });
            entry.count += n;
            if (notify) { InventoryChanged?.Invoke(); RequestSave(); }
        }

        public bool TakeItem(string id)
        {
            var entry = Data.inventory.Find(e => e.id == id);
            if (entry == null || entry.count <= 0) return false;
            entry.count--;
            if (entry.count == 0) Data.inventory.Remove(entry);
            InventoryChanged?.Invoke();
            RequestSave();
            return true;
        }

        public bool OwnsTheme(string id) => Data.ownedThemes.Contains(id);

        public enum BuyResult { Ok, NotEnough, AlreadyOwned }

        public BuyResult Buy(ItemDef def)
        {
            if (def.IsTheme && OwnsTheme(def.id)) return BuyResult.AlreadyOwned;
            if (!Spend(def.price)) { Sfx.Play(Sound.Error, 0.6f); return BuyResult.NotEnough; }
            if (def.IsTheme)
            {
                Data.ownedThemes.Add(def.id);
                ApplyTheme(def);
            }
            else AddItem(def.id);
            Sfx.Play(Sound.Buy, 0.7f);
            return BuyResult.Ok;
        }

        public void ApplyTheme(ItemDef def)
        {
            if (!OwnsTheme(def.id)) return;
            if (def.category == ItemCategory.Wallpaper) { Data.wallpaper = def.id; Room.ApplyWallpaper(def); }
            else if (def.category == ItemCategory.Flooring) { Data.flooring = def.id; Room.ApplyFlooring(def); }
            RequestSave();
        }

        // ---------------- Selection & world input ----------------

        public void Select(CatController cat)
        {
            if (ReferenceEquals(Selected, cat)) return;
            Selected = cat;
            if (selectionRing == null)
            {
                selectionRing = Toon.MeshObj(ProcGen.Torus, null, Vector3.zero, new Vector3(1f, 0.12f, 1f), UIKit.Yellow);
                selectionRing.GetComponent<Renderer>().sharedMaterial = Toon.Get(UIKit.Yellow, false, 0.015f, 0.6f);
                selectionRing.name = "SelectionRing";
                selectionRing.AddComponent<SimpleAnim>().spin = new Vector3(0, 40f, 0);
            }
            if (cat != null)
            {
                selectionRing.SetActive(true);
                selectionRing.transform.SetParent(cat.transform, false);
                selectionRing.transform.localPosition = new Vector3(0, 0.03f, 0);
                selectionRing.transform.localScale = new Vector3(1f, 0.12f, 1f) * cat.Data.size;
                if (cat.State != CatState.Sleeping) Sfx.PlayMeow(cat.Data.size);
            }
            else
            {
                selectionRing.transform.SetParent(null, false);
                selectionRing.SetActive(false);
            }
            SelectionChanged?.Invoke();
        }

        void Update()
        {
            if (!Ready) return;

            if (!ReferenceEquals(Selected, null) && Selected == null) Select(null); // selected stray left

            if (!Placement.EditMode) HandleWorldInput();

            // saving
            saveTimer += Time.unscaledDeltaTime;
            autosaveTimer += Time.unscaledDeltaTime;
            if ((saveDirty && saveTimer >= MinSaveInterval) || autosaveTimer >= AutosaveInterval) SaveNow();
        }

        void HandleWorldInput()
        {
            if (InputUtil.PointerDown && !InputUtil.PointerOverUI())
            {
                pressed = true;
                pressPos = InputUtil.PointerPosition;
            }
            if (pressed && InputUtil.PointerUp)
            {
                pressed = false;
                float slop = Mathf.Max(14f, Mathf.Min(Screen.width, Screen.height) * 0.03f);
                if ((InputUtil.PointerPosition - pressPos).magnitude < slop) Tap(InputUtil.PointerPosition);
            }
        }

        void Tap(Vector2 screen)
        {
            var ray = MainCamera.ScreenPointToRay(screen);
            var hits = Physics.RaycastAll(ray, 100f);
            CatController bestCat = null;
            float bestDist = float.MaxValue;
            foreach (var h in hits)
            {
                var cat = h.collider.GetComponentInParent<CatController>();
                if (cat != null && h.distance < bestDist) { bestDist = h.distance; bestCat = cat; }
            }
            Select(bestCat);
        }

        // ---------------- Save / pause ----------------

        public void RequestSave() => saveDirty = true;

        public void SaveNow()
        {
            if (Data == null || !Ready) return;
            saveDirty = false;
            saveTimer = 0f;
            autosaveTimer = 0f;
            Data.placed = Room.ToSaveData();
            Cats.SyncSave();
            Data.lastSaveUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                Platform.SaveData(JsonUtility.ToJson(Data));
                if (Data.lifetimeCoins != lastScoreSent)
                {
                    lastScoreSent = Data.lifetimeCoins;
                    Platform.SendScore(Data.lifetimeCoins);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save failed: " + e.Message);
            }
        }

        public void ResetGame()
        {
            PlayablesBridge.DeleteLocalSave();
            var fresh = SaveData.CreateNew();
            Platform.SaveData(JsonUtility.ToJson(fresh));
            Ready = false;
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t != null && t.parent == null) Destroy(t.gameObject);
            new GameObject("Restarter").AddComponent<Restarter>();
        }

        /// <summary>Recreates the game one frame after everything was destroyed.</summary>
        class Restarter : MonoBehaviour
        {
            IEnumerator Start()
            {
                yield return null;
                I = null;
                new GameObject("CatRoomGame").AddComponent<GameManager>();
                Destroy(gameObject);
            }
        }

        void OnPlatformPause(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            if (paused) SaveNow();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus) SaveNow();
        }

        void OnApplicationQuit()
        {
            SaveNow();
        }

        public void SetLanguage(Lang lang)
        {
            Data.language = (int)lang;
            Loc.Current = lang;
            UI.Rebuild();
            RequestSave();
        }

        public void SetSound(bool on)
        {
            Data.soundOn = on;
            Sfx.SetUserSound(on);
            RequestSave();
        }

        public void CompleteTutorial()
        {
            Data.tutorialDone = true;
            RequestSave();
        }
    }
}
