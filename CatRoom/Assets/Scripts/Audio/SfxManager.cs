using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatRoom
{
    public enum Sound { Click, Meow, Purr, Coin, Buy, Place, Pop, Hiss, Adopt, Eat, Error, DoorBell }

    /// <summary>All sounds are synthesized at startup so the project ships without audio files.</summary>
    public class SfxManager : MonoBehaviour
    {
        const int Rate = 22050;

        AudioSource sfxSource, musicSource;
        readonly Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();
        readonly Dictionary<Sound, float> lastPlayed = new Dictionary<Sound, float>();
        System.Random rng = new System.Random(7);

        bool userSoundOn = true;
        bool platformAudioOn = true;

        public void Init()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = 0.22f;

            try
            {
                clips[Sound.Click] = Make("click", 0.05f, t => Tone(t, 900f) * Env(t, 0.05f, 0.005f));
                clips[Sound.Pop] = Make("pop", 0.12f, t => Tone(t, Mathf.Lerp(900f, 400f, t / 0.12f)) * Env(t, 0.12f, 0.005f));
                clips[Sound.Coin] = Make("coin", 0.28f, t => Square(t, t < 0.07f ? 1320f : 1760f) * 0.35f * Env(t, 0.28f, 0.003f));
                clips[Sound.Buy] = Make("buy", 0.45f, t => Arp(t, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.09f) * Env(t, 0.45f, 0.005f));
                clips[Sound.Place] = Make("place", 0.16f, t => Tone(t, Mathf.Lerp(220f, 120f, t / 0.16f)) * Env(t, 0.16f, 0.002f) + Noise() * 0.15f * Env(t, 0.05f, 0.001f));
                clips[Sound.Meow] = Make("meow", 0.55f, Meow);
                clips[Sound.Purr] = Make("purr", 1.0f, t => Noise() * 0.5f * (0.55f + 0.45f * Mathf.Sin(t * Mathf.PI * 2f * 24f)) * Env(t, 1.0f, 0.15f) * 0.6f);
                clips[Sound.Hiss] = Make("hiss", 0.45f, t => Noise() * 0.45f * Env(t, 0.45f, 0.02f));
                clips[Sound.Eat] = Make("eat", 0.5f, t => (Mathf.Repeat(t, 0.125f) < 0.04f ? Noise() * 0.5f : 0f) * Env(t, 0.5f, 0.01f));
                clips[Sound.Adopt] = Make("adopt", 1.1f, t => Arp(t, new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f, 1318.5f }, 0.13f) * Env(t, 1.1f, 0.005f));
                clips[Sound.Error] = Make("error", 0.22f, t => Square(t, 180f) * 0.25f * Env(t, 0.22f, 0.005f));
                clips[Sound.DoorBell] = Make("bell", 0.9f, t => t < 0.3f
                    ? Tone(t, 1318.5f) * Env(t, 0.3f, 0.005f)
                    : Tone(t, 1046.5f) * Env(t - 0.3f, 0.6f, 0.005f));

                musicSource.clip = MakeMusic();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Audio synthesis failed: " + e.Message);
            }
            ApplyMute();
        }

        public void SetUserSound(bool on) { userSoundOn = on; ApplyMute(); }
        public void SetPlatformAudio(bool on) { platformAudioOn = on; ApplyMute(); }
        public bool UserSoundOn => userSoundOn;

        void ApplyMute()
        {
            bool on = userSoundOn && platformAudioOn;
            AudioListener.volume = on ? 1f : 0f;
            if (musicSource == null || musicSource.clip == null) return;
            if (on && !musicSource.isPlaying) musicSource.Play();
            else if (!on && musicSource.isPlaying) musicSource.Pause();
        }

        public void Play(Sound s, float volume = 0.8f, float pitch = 1f)
        {
            if (sfxSource == null || !clips.TryGetValue(s, out var clip) || clip == null) return;
            // Avoid stacking the same sound many times in one burst.
            if (lastPlayed.TryGetValue(s, out var t) && Time.unscaledTime - t < 0.05f) return;
            lastPlayed[s] = Time.unscaledTime;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume);
        }

        public void PlayMeow(float size = 1f)
        {
            Play(Sound.Meow, 0.7f, Mathf.Clamp(1.15f / Mathf.Max(0.6f, size) + (float)(rng.NextDouble() * 0.15 - 0.07), 0.8f, 1.5f));
        }

        // ---------------- synthesis helpers ----------------

        AudioClip Make(string name, float seconds, Func<float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f) * 0.8f;
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Tone(float t, float freq) => Mathf.Sin(t * freq * Mathf.PI * 2f);
        static float Square(float t, float freq) => Mathf.Repeat(t * freq, 1f) < 0.5f ? 1f : -1f;
        static float Tri(float t, float freq) => 1f - 4f * Mathf.Abs(Mathf.Repeat(t * freq + 0.25f, 1f) - 0.5f);
        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        static float Env(float t, float length, float attack)
        {
            if (t < attack) return t / attack;
            float r = 1f - (t - attack) / Mathf.Max(0.0001f, length - attack);
            return Mathf.Clamp01(r) * Mathf.Clamp01(r);
        }

        static float Arp(float t, float[] notes, float step)
        {
            int i = Mathf.Min(notes.Length - 1, (int)(t / step));
            float local = t - i * step;
            return (Tone(t, notes[i]) * 0.6f + Tri(t, notes[i] * 2f) * 0.2f) * Mathf.Clamp01(1.2f - local / (step * 2.5f));
        }

        float meowPhase;
        float Meow(float t)
        {
            const float len = 0.55f;
            float x = t / len;
            // "mi-aaa-ow": rising then falling pitch, nasal harmonics
            float f = 520f + 380f * Mathf.Sin(Mathf.Clamp01(x * 1.3f) * Mathf.PI) - 120f * x;
            f *= 1f + 0.02f * Mathf.Sin(t * 38f);
            meowPhase += f / Rate;
            float p = meowPhase * Mathf.PI * 2f;
            float s = Mathf.Sin(p) * 0.55f + Mathf.Sin(p * 2f) * 0.3f * (0.4f + x) + Mathf.Sin(p * 3f) * 0.18f;
            float env = Mathf.Clamp01(t / 0.04f) * Mathf.Clamp01((len - t) / 0.18f);
            return s * env;
        }

        AudioClip MakeMusic()
        {
            // A small looping lullaby in C major pentatonic: melody + soft bass, 8 bars at 92 bpm.
            const float bpm = 92f;
            float beat = 60f / bpm;
            float[] scale = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f };
            int[] melody = { 0, 2, 3, 2, 4, 3, 2, -1, 1, 2, 3, 1, 0, -1, 2, -1,
                             3, 4, 5, 4, 3, 2, 3, -1, 2, 1, 0, 1, 2, -1, 0, -1 };
            float[] bass = { 130.81f, 174.61f, 196f, 130.81f, 110f, 174.61f, 196f, 130.81f };
            float total = melody.Length * beat;
            int n = Mathf.CeilToInt(total * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                int step = Mathf.Min(melody.Length - 1, (int)(t / beat));
                float lt = t - step * beat;
                float v = 0f;
                if (melody[step] >= 0)
                {
                    float fr = scale[melody[step]];
                    float env = Mathf.Clamp01(lt / 0.02f) * Mathf.Exp(-lt * 2.6f);
                    v += (Tone(t, fr) * 0.5f + Tone(t, fr * 2f) * 0.12f) * env * 0.5f;
                }
                int bar = Mathf.Min(bass.Length - 1, (int)(t / (beat * 4f)));
                float bt = t - bar * beat * 4f;
                v += Tri(t, bass[bar]) * 0.22f * Mathf.Exp(-bt * 0.9f) * Mathf.Clamp01(bt / 0.03f);
                // fade loop edges to avoid clicks
                float edge = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((total - t) / 0.02f);
                data[i] = v * edge * 0.7f;
            }
            var clip = AudioClip.Create("lullaby", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
