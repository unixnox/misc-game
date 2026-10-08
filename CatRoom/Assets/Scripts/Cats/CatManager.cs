using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatRoom
{
    /// <summary>Owns the list of cats and schedules stray-cat visits.</summary>
    public class CatManager : MonoBehaviour
    {
        public const int MaxCats = 6;
        const float FirstVisitDelay = 35f;

        public readonly List<CatController> Owned = new List<CatController>();
        public CatController Stray { get; private set; }

        public event Action StrayArrived;
        public event Action StrayLeft;
        public event Action CatsChanged;

        float strayTimer = FirstVisitDelay;
        readonly System.Random rng = new System.Random();

        GameManager GM => GameManager.I;
        public bool RoomFull => Owned.Count >= MaxCats;

        public void SpawnOwned(List<CatData> cats)
        {
            foreach (var d in cats)
            {
                var pos = RoomManager.ClampToFloor(new Vector3(d.posX, 0f, d.posZ));
                Owned.Add(CatController.Spawn(transform, d, false, pos));
            }
        }

        void Update()
        {
            if (GM == null || !GM.Ready) return;
            if (Stray != null || RoomFull) return;
            strayTimer -= Time.deltaTime;
            if (strayTimer <= 0f) SpawnStray();
        }

        public void SpawnStray()
        {
            if (Stray != null) return;
            var data = CatData.CreateRandomStray(rng);
            // avoid duplicate names in the room
            for (int tries = 0; tries < 10 && Owned.Exists(c => c.Data.nameIndex == data.nameIndex); tries++)
                data.nameIndex = rng.Next(1, CatAppearance.NameCount);

            Stray = CatController.Spawn(transform, data, true, GM.Room.DoorThreshold);
            Stray.BeginStrayVisit(150f + (float)rng.NextDouble() * 60f);
            Stray.LeftRoom += OnStrayLeft;
            GM.Sfx.Play(Sound.DoorBell, 0.6f);
            StrayArrived?.Invoke();
        }

        void OnStrayLeft(CatController c)
        {
            if (Stray != c) return;
            Stray = null;
            strayTimer = UnityEngine.Random.Range(50f, 110f);
            StrayLeft?.Invoke();
        }

        public bool Adopt()
        {
            if (Stray == null || !Stray.CanAdopt || RoomFull) return false;
            var c = Stray;
            Stray = null;
            c.LeftRoom -= OnStrayLeft;
            c.BecomeOwned();
            Owned.Add(c);
            GM.Data.cats.Add(c.Data);
            GM.Data.adoptedCount++;
            strayTimer = UnityEngine.Random.Range(60f, 120f);
            GM.Sfx.Play(Sound.Adopt, 0.8f);
            GM.Floating.SpawnHearts(c.Visual.HeadTop, 8);
            GM.RequestSave();
            CatsChanged?.Invoke();
            return true;
        }

        public void SyncSave()
        {
            foreach (var c in Owned)
            {
                if (c == null) continue;
                c.Data.posX = c.transform.position.x;
                c.Data.posZ = c.transform.position.z;
            }
        }
    }
}
