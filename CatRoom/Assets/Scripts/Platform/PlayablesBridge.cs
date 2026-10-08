using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CatRoom
{
    /// <summary>
    /// Wraps the YouTube Playables SDK (ytgame). Outside of YouTube (editor, local web server)
    /// it falls back to PlayerPrefs so the game is fully playable anywhere.
    /// The GameObject must be named <see cref="ObjectName"/> because the JS side uses SendMessage.
    /// </summary>
    public class PlayablesBridge : MonoBehaviour
    {
        public const string ObjectName = "PlayablesBridge";
        const string LocalSaveKey = "catroom_save_v1";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int YT_IsAvailable();
        [DllImport("__Internal")] static extern void YT_RegisterCallbacks(string gameObjectName);
        [DllImport("__Internal")] static extern void YT_GameReady();
        [DllImport("__Internal")] static extern void YT_LoadData(string gameObjectName);
        [DllImport("__Internal")] static extern void YT_SaveData(string data);
        [DllImport("__Internal")] static extern int YT_IsAudioEnabled();
        [DllImport("__Internal")] static extern void YT_RequestLanguage(string gameObjectName);
        [DllImport("__Internal")] static extern void YT_SendScore(double value);
        [DllImport("__Internal")] static extern void YT_LogWarning(string message);
#endif

        public bool InPlayables { get; private set; }
        public bool PlatformAudioEnabled { get; private set; } = true;
        public bool Paused { get; private set; }

        public event Action<bool> PauseChanged;
        public event Action<bool> AudioEnabledChanged;

        Action<string> pendingLoad;
        Action<string> pendingLanguage;

        public void Init()
        {
            gameObject.name = ObjectName;
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                InPlayables = YT_IsAvailable() == 1;
                if (InPlayables)
                {
                    YT_RegisterCallbacks(ObjectName);
                    PlatformAudioEnabled = YT_IsAudioEnabled() == 1;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Playables SDK unavailable: " + e.Message);
                InPlayables = false;
            }
#endif
        }

        /// <summary>Call once the game is interactive (after loading save + building the room).</summary>
        public void GameReady()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (InPlayables) YT_GameReady();
#endif
        }

        public void LoadData(Action<string> onLoaded)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (InPlayables)
            {
                pendingLoad = onLoaded;
                YT_LoadData(ObjectName);
                return;
            }
#endif
            onLoaded?.Invoke(PlayerPrefs.GetString(LocalSaveKey, ""));
        }

        public void SaveData(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (InPlayables)
            {
                YT_SaveData(json);
                return;
            }
#endif
            PlayerPrefs.SetString(LocalSaveKey, json);
            PlayerPrefs.Save();
        }

        public static void DeleteLocalSave()
        {
            PlayerPrefs.DeleteKey(LocalSaveKey);
            PlayerPrefs.Save();
        }

        /// <summary>Returns a BCP-47 language tag such as "th" or "en-US".</summary>
        public void RequestLanguage(Action<string> onLanguage)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (InPlayables)
            {
                pendingLanguage = onLanguage;
                YT_RequestLanguage(ObjectName);
                return;
            }
#endif
            onLanguage?.Invoke(Application.systemLanguage == SystemLanguage.Thai ? "th" : "en");
        }

        public void SendScore(long value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (InPlayables) YT_SendScore(value);
#endif
        }

        public void LogWarning(string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (InPlayables) { YT_LogWarning(message); return; }
#endif
            Debug.LogWarning(message);
        }

        // ---------- Called from JavaScript via SendMessage ----------

        void OnYTLoadData(string data)
        {
            var cb = pendingLoad;
            pendingLoad = null;
            cb?.Invoke(data ?? "");
        }

        void OnYTLoadDataError(string error)
        {
            Debug.LogWarning("loadData failed: " + error);
            var cb = pendingLoad;
            pendingLoad = null;
            cb?.Invoke("");
        }

        void OnYTLanguage(string lang)
        {
            var cb = pendingLanguage;
            pendingLanguage = null;
            cb?.Invoke(lang ?? "");
        }

        void OnYTPause(string _)
        {
            Paused = true;
            PauseChanged?.Invoke(true);
        }

        void OnYTResume(string _)
        {
            Paused = false;
            PauseChanged?.Invoke(false);
        }

        void OnYTAudioEnabled(string value)
        {
            PlatformAudioEnabled = value == "1";
            AudioEnabledChanged?.Invoke(PlatformAudioEnabled);
        }
    }
}
