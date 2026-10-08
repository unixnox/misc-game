using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatRoom
{
    public enum CatState { Idle, Moving, Using, Sleeping, Eating, Petted, Playing, Hissing, Entering, Waiting, Leaving }

    public enum CatThought { None, Hungry, Bored, Sleepy, Love }

    /// <summary>Behaviour, needs and interactions of a single cat (owned or stray).</summary>
    public class CatController : MonoBehaviour
    {
        // Need decay per second
        const float HungerDecay = 0.09f;
        const float FunDecay = 0.11f;
        const float EnergyDecay = 0.07f;
        const float FloorSleepRate = 2.2f;
        const float IncomeInterval = 10f;

        public CatData Data;
        public bool IsStray;
        public CatVisual Visual;

        // Stray only
        public float Trust;
        public float StayTimeLeft;
        public bool FavoriteRevealed;
        public bool CanAdopt => IsStray && Trust >= 100f;
        public event Action<CatController> LeftRoom;

        public CatState State { get; private set; }
        public CatThought Thought { get; private set; }
        public float PetCooldown => petCooldown;
        public float PlayCooldown => playCooldown;

        struct Leg { public Vector3 pos; public bool hop; }
        readonly Queue<Leg> path = new Queue<Leg>();
        Action onArrive;
        Leg currentLeg;
        bool hasLeg;
        Vector3 hopFrom;
        float hopT;

        PlacedItem usingItem;
        float stateTimer;
        float petCooldown, playCooldown;
        float incomeTimer;
        float meowTimer;
        GameObject dish;
        float spawnScale = 1f;

        GameManager GM => GameManager.I;
        bool Perched => transform.position.y > 0.05f;

        // ---------------- Setup ----------------

        public static CatController Spawn(Transform parent, CatData data, bool stray, Vector3 position)
        {
            var go = new GameObject(stray ? "StrayCat" : "Cat_" + data.uid);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            var c = go.AddComponent<CatController>();
            c.Data = data;
            c.IsStray = stray;
            c.Visual = CatVisual.Build(go.transform, data.preset, data.size);
            var col = go.AddComponent<CapsuleCollider>();
            col.direction = 2;
            col.center = new Vector3(0, 0.38f, 0.05f) * data.size;
            col.radius = 0.32f * data.size;
            col.height = 1.0f * data.size;
            c.incomeTimer = UnityEngine.Random.Range(0f, IncomeInterval);
            c.meowTimer = UnityEngine.Random.Range(8f, 20f);
            c.SetIdle(UnityEngine.Random.Range(0.5f, 2f));
            return c;
        }

        // ---------------- Derived values ----------------

        public static float HappinessOf(CatData d, int comfort)
        {
            float h = d.hunger * 0.3f + d.fun * 0.3f + d.energy * 0.2f + d.affection * 0.2f;
            h += Mathf.Min(15f, comfort * 0.3f);
            return Mathf.Clamp(h, 0f, 100f);
        }

        public float Happiness => HappinessOf(Data, GM.Room.Comfort);

        public string StatusText
        {
            get
            {
                switch (State)
                {
                    case CatState.Sleeping: return Loc.T("st_sleep");
                    case CatState.Eating: return Loc.T("st_eat");
                    case CatState.Petted: return Loc.T("st_purr");
                    case CatState.Playing: return Loc.T("st_play");
                    case CatState.Hissing: return Loc.T("st_hiss");
                    case CatState.Using:
                        if (usingItem != null) return Loc.F("st_using", usingItem.def.Name);
                        break;
                    case CatState.Moving: return Loc.T("st_walk");
                }
                if (IsStray) return CanAdopt ? Loc.T("st_wants_home") : Loc.T("st_shy");
                return Loc.T("st_relax");
            }
        }

        // ---------------- Update ----------------

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            petCooldown -= dt;
            playCooldown -= dt;

            if (spawnScale < 1f)
            {
                spawnScale = Mathf.MoveTowards(spawnScale, 1f, dt * 2f);
                transform.localScale = Vector3.one * spawnScale;
            }

            UpdateNeeds(dt);
            UpdateMovement(dt);
            UpdateState(dt);
            UpdateThought();

            if (!IsStray) UpdateIncome(dt);
            else UpdateStray(dt);

            meowTimer -= dt;
            if (meowTimer <= 0f)
            {
                meowTimer = UnityEngine.Random.Range(18f, 40f);
                if (State != CatState.Sleeping && (Thought == CatThought.Hungry || IsStray || UnityEngine.Random.value < 0.3f))
                    GM.Sfx.PlayMeow(Data.size);
            }
        }

        void UpdateNeeds(float dt)
        {
            var d = Data;
            d.hunger -= HungerDecay * dt * (IsStray ? 0.5f : 1f);
            if (State != CatState.Sleeping) d.energy -= EnergyDecay * dt;
            d.fun -= FunDecay * dt * (State == CatState.Sleeping ? 0.3f : 1f);
            d.affection = Mathf.MoveTowards(d.affection, 30f, 0.02f * dt);

            if (State == CatState.Using && usingItem != null)
            {
                d.fun += usingItem.def.funRate * dt;
                d.energy += usingItem.def.energyRate * dt;
            }
            if (State == CatState.Sleeping)
            {
                float rate = FloorSleepRate;
                if (usingItem != null) rate = Mathf.Max(rate, usingItem.def.energyRate + 1.5f);
                d.energy += rate * dt;
            }
            d.hunger = Mathf.Clamp(d.hunger, 0f, 100f);
            d.fun = Mathf.Clamp(d.fun, 0f, 100f);
            d.energy = Mathf.Clamp(d.energy, 0f, 100f);
            d.affection = Mathf.Clamp(d.affection, 0f, 100f);
        }

        void UpdateThought()
        {
            if (State == CatState.Sleeping) Thought = CatThought.Sleepy;
            else if (State == CatState.Petted || (IsStray && CanAdopt)) Thought = CatThought.Love;
            else if (Data.hunger < 25f) Thought = CatThought.Hungry;
            else if (!IsStray && Data.fun < 25f) Thought = CatThought.Bored;
            else Thought = CatThought.None;
        }

        void UpdateIncome(float dt)
        {
            incomeTimer += dt;
            if (incomeTimer < IncomeInterval) return;
            incomeTimer = 0f;
            float h = Happiness;
            if (h < 20f) return;
            float k = Mathf.Pow(h / 100f, 1.5f);
            int amount = Mathf.Max(1, Mathf.RoundToInt((2f + 8f * k) * GM.Room.ComfortMultiplier));
            GM.Floating.AddCoinBubble(this, amount);
        }

        void UpdateStray(float dt)
        {
            Trust = Mathf.Min(100f, Trust + GM.Room.Comfort * 0.004f * dt);
            if (CanAdopt || State == CatState.Leaving || State == CatState.Entering) return;
            StayTimeLeft -= dt;
            if (StayTimeLeft <= 0f) Leave();
        }

        // ---------------- Movement ----------------

        void MoveTo(Vector3 groundGoal, Vector3? perchGoal, Action arrive)
        {
            path.Clear();
            hasLeg = false;
            if (Perched)
            {
                var down = transform.position;
                down.y = 0f;
                var toCenter = new Vector3(RoomManager.Size / 2f, 0, RoomManager.Size / 2f) - down;
                down += toCenter.normalized * 0.7f;
                path.Enqueue(new Leg { pos = RoomManager.ClampToFloor(down), hop = true });
            }
            groundGoal.y = 0f;
            path.Enqueue(new Leg { pos = RoomManager.ClampToFloor(groundGoal), hop = false });
            if (perchGoal.HasValue) path.Enqueue(new Leg { pos = perchGoal.Value, hop = true });
            onArrive = arrive;
            State = CatState.Moving;
            Visual.Pose = CatPose.Walk;
        }

        void UpdateMovement(float dt)
        {
            if (State != CatState.Moving && State != CatState.Entering && State != CatState.Leaving) return;

            if (!hasLeg)
            {
                if (path.Count == 0)
                {
                    var cb = onArrive;
                    onArrive = null;
                    if (cb != null) cb();
                    else SetIdle(UnityEngine.Random.Range(1f, 3f));
                    return;
                }
                currentLeg = path.Dequeue();
                hasLeg = true;
                hopFrom = transform.position;
                hopT = 0f;
            }

            if (currentLeg.hop)
            {
                hopT += dt / 0.45f;
                var p = Vector3.Lerp(hopFrom, currentLeg.pos, Mathf.SmoothStep(0, 1, hopT));
                p.y += Mathf.Sin(Mathf.Clamp01(hopT) * Mathf.PI) * 0.45f;
                Face(currentLeg.pos - hopFrom, dt);
                Visual.Pose = CatPose.Play;
                transform.position = p;
                if (hopT >= 1f)
                {
                    transform.position = currentLeg.pos;
                    hasLeg = false;
                    Visual.Pose = CatPose.Walk;
                }
                return;
            }

            float speed = (IsStray ? 1.0f : 1.3f) * (Data.energy < 20f ? 0.7f : 1f);
            var pos = transform.position;
            var target = currentLeg.pos;
            target.y = pos.y;
            var delta = target - pos;
            Visual.Pose = CatPose.Walk;
            if (delta.magnitude <= speed * dt || delta.magnitude < 0.02f)
            {
                transform.position = target;
                hasLeg = false;
                return;
            }
            Face(delta, dt);
            transform.position = pos + delta.normalized * speed * dt;
        }

        void Face(Vector3 dir, float dt)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            var rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, dt * 8f);
        }

        void FaceInstant(Vector3 worldPoint)
        {
            var dir = worldPoint - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        // ---------------- State machine ----------------

        void SetIdle(float seconds, CatPose pose = CatPose.Stand)
        {
            State = CatState.Idle;
            stateTimer = seconds;
            Visual.Pose = pose;
        }

        void UpdateState(float dt)
        {
            stateTimer -= dt;
            switch (State)
            {
                case CatState.Idle:
                    if (Visual.Pose == CatPose.Stand || Visual.Pose == CatPose.Loaf)
                        Visual.LookYaw = Mathf.Sin(Time.time * 0.7f + Data.size * 10f) * 20f;
                    if (stateTimer <= 0f)
                    {
                        if (IsStray) StrayThink();
                        else Think();
                    }
                    break;
                case CatState.Waiting:
                    if (stateTimer <= 0f) StrayThink();
                    break;
                case CatState.Using:
                    if (usingItem == null) { SetIdle(1f); break; }
                    if (usingItem.def.use == UseKind.Sleep && Data.energy < 60f)
                    {
                        State = CatState.Sleeping;
                        Visual.Pose = CatPose.Sleep;
                        break;
                    }
                    if (stateTimer <= 0f) FinishUsing();
                    break;
                case CatState.Sleeping:
                    if (Data.energy >= 98f || (stateTimer <= -60f && Data.energy > 60f))
                    {
                        FinishUsing();
                        SetIdle(2f, CatPose.Stand);
                    }
                    break;
                case CatState.Eating:
                case CatState.Petted:
                case CatState.Playing:
                case CatState.Hissing:
                    if (stateTimer <= 0f)
                    {
                        if (dish != null) { Destroy(dish); dish = null; }
                        if (usingItem != null) ResumeUsing();
                        else SetIdle(UnityEngine.Random.Range(1f, 3f), CatPose.Loaf);
                    }
                    break;
            }
        }

        void Think()
        {
            var room = GM.Room;
            var pos = transform.position;
            Visual.LookYaw = 0f;

            if (Data.energy < 30f)
            {
                var bed = room.FindFree(i => i.def.use == UseKind.Sleep || i.def.use == UseKind.Sit || i.def.use == UseKind.Climb, pos);
                if (bed != null) { GoUse(bed); return; }
                SleepHere();
                return;
            }
            if (Data.hunger < 25f)
            {
                var bowl = room.NearestBowl(pos);
                if (bowl != null && Vector3.Distance(pos, bowl.ApproachPoint) > 0.6f)
                    MoveTo(bowl.ApproachPoint, null, () => { FaceInstant(bowl.transform.position); SetIdle(6f, CatPose.Loaf); });
                else SetIdle(5f, CatPose.Loaf);
                return;
            }
            if (Data.fun < 55f && UnityEngine.Random.value < 0.75f)
            {
                var toy = room.FindFree(i => i.def.funRate > 0f && i.def.use != UseKind.Sleep, pos);
                if (toy != null) { GoUse(toy); return; }
            }

            float r = UnityEngine.Random.value;
            if (r < 0.35f) MoveTo(RoomManager.RandomFloorPoint(), null, null);
            else if (r < 0.6f) SetIdle(UnityEngine.Random.Range(3f, 7f), UnityEngine.Random.value < 0.5f ? CatPose.Loaf : CatPose.Stand);
            else
            {
                var any = room.FindFree(i => i.def.use != UseKind.None && i.def.use != UseKind.Bowl, pos);
                if (any != null) GoUse(any);
                else MoveTo(RoomManager.RandomFloorPoint(), null, null);
            }
        }

        void SleepHere()
        {
            ReleaseItem();
            State = CatState.Sleeping;
            stateTimer = 0f;
            Visual.Pose = CatPose.Sleep;
        }

        void GoUse(PlacedItem item)
        {
            ReleaseItem();
            usingItem = item;
            item.occupant = this;
            MoveTo(item.ApproachPoint, item.Perch ? item.UsePoint : (Vector3?)null, StartUsing);
        }

        void StartUsing()
        {
            if (usingItem == null) { SetIdle(1f); return; }
            if (!usingItem.Perch) FaceInstant(usingItem.transform.position);
            else transform.rotation = Quaternion.Euler(0, usingItem.transform.eulerAngles.y + 180f + UnityEngine.Random.Range(-30f, 30f), 0);
            stateTimer = usingItem.def.useTime * UnityEngine.Random.Range(0.8f, 1.3f);
            ResumeUsing();
        }

        void ResumeUsing()
        {
            if (usingItem == null) { SetIdle(1f); return; }
            State = CatState.Using;
            if (stateTimer <= 0f) stateTimer = 3f;
            switch (usingItem.def.use)
            {
                case UseKind.Play: Visual.Pose = CatPose.Play; break;
                case UseKind.Scratch: Visual.Pose = CatPose.Scratch; break;
                case UseKind.Sleep:
                    if (Data.energy < 60f) { State = CatState.Sleeping; Visual.Pose = CatPose.Sleep; }
                    else Visual.Pose = CatPose.Loaf;
                    break;
                default: Visual.Pose = CatPose.Loaf; break;
            }
            if (usingItem.def.id == "box") Visual.Pose = CatPose.Loaf;
        }

        void FinishUsing()
        {
            ReleaseItem();
            SetIdle(UnityEngine.Random.Range(1f, 3f), CatPose.Stand);
        }

        /// <summary>Stop using the current item. If perched and the item vanished, drop to the floor.</summary>
        public void ReleaseItem()
        {
            if (usingItem != null)
            {
                if (usingItem.occupant == this) usingItem.occupant = null;
                usingItem = null;
            }
            if (State == CatState.Using || State == CatState.Sleeping) SetIdle(0.5f);
        }

        /// <summary>Called when the item under a perched cat is moved or stored.</summary>
        public void DropToFloor()
        {
            ReleaseItem();
            var p = transform.position;
            p.y = 0f;
            transform.position = RoomManager.ClampToFloor(p);
            path.Clear();
            hasLeg = false;
            SetIdle(1f);
        }

        // ---------------- Stray behaviour ----------------

        public void BeginStrayVisit(float stayTime)
        {
            IsStray = true;
            StayTimeLeft = stayTime;
            Trust = 0f;
            spawnScale = 0.4f;
            transform.localScale = Vector3.one * spawnScale;
            var room = GM.Room;
            transform.position = room.DoorThreshold;
            transform.rotation = Quaternion.Euler(0, 90, 0);
            room.OpenDoor(1.6f);
            var spot = room.DoorInside + new Vector3(UnityEngine.Random.Range(0.6f, 1.4f), 0, UnityEngine.Random.Range(-0.4f, 0.8f));
            MoveTo(spot, null, () => { FaceCamera(); SetIdle(2f, CatPose.Stand); });
            State = CatState.Entering;
        }

        void StrayThink()
        {
            var room = GM.Room;
            float r = UnityEngine.Random.value;
            if (r < 0.3f)
            {
                // stay near the door while shy, wander further as trust grows
                float radius = Mathf.Lerp(1.0f, 3.5f, Trust / 100f);
                var p = room.DoorInside + new Vector3(UnityEngine.Random.Range(0.3f, radius), 0, UnityEngine.Random.Range(-radius * 0.6f, radius));
                MoveTo(p, null, () => { FaceCamera(); SetIdle(UnityEngine.Random.Range(3f, 6f), CatPose.Stand); });
            }
            else
            {
                FaceCamera();
                SetIdle(UnityEngine.Random.Range(3f, 6f), UnityEngine.Random.value < 0.5f ? CatPose.Loaf : CatPose.Stand);
            }
        }

        void FaceCamera()
        {
            var cam = GM.MainCamera;
            if (cam == null) return;
            var dir = -cam.transform.forward;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0, UnityEngine.Random.Range(-35f, 35f), 0);
        }

        public void Leave()
        {
            if (State == CatState.Leaving) return;
            ReleaseItem();
            var room = GM.Room;
            MoveTo(room.DoorInside, null, null);
            path.Enqueue(new Leg { pos = room.DoorThreshold, hop = false });
            onArrive = () =>
            {
                LeftRoom?.Invoke(this);
                Destroy(gameObject);
            };
            State = CatState.Leaving;
            room.OpenDoor(4f);
        }

        /// <summary>Turns this stray into an owned cat.</summary>
        public void BecomeOwned()
        {
            IsStray = false;
            Trust = 100f;
            Data.affection = Mathf.Max(Data.affection, 70f);
            name = "Cat_" + Data.uid;
            stateTimer = 3f;
            State = CatState.Petted;
            Visual.Pose = CatPose.Happy;
        }

        // ---------------- Player interactions ----------------

        bool BusyMoving => State == CatState.Entering || State == CatState.Leaving;

        public enum Result { Ok, Cooldown, Full, Tired, Busy, Hiss, NoFood }

        public Result Pet()
        {
            if (BusyMoving) return Result.Busy;
            if (petCooldown > 0f) return Result.Cooldown;
            petCooldown = 2.5f;
            StopMovingKeepItem();

            if (IsStray && Trust < 25f && UnityEngine.Random.value < 0.6f)
            {
                Trust += 2f;
                State = CatState.Hissing;
                stateTimer = 1.2f;
                Visual.Pose = CatPose.Hiss;
                GM.Sfx.Play(Sound.Hiss, 0.6f);
                return Result.Hiss;
            }

            if (IsStray) Trust = Mathf.Min(100f, Trust + 6f);
            Data.affection = Mathf.Min(100f, Data.affection + 7f);
            Data.fun = Mathf.Min(100f, Data.fun + 3f);
            State = CatState.Petted;
            stateTimer = 2.2f;
            Visual.Pose = CatPose.Happy;
            GM.Sfx.Play(Sound.Purr, 0.7f);
            GM.Floating.SpawnHearts(Visual.HeadTop, 3);
            return Result.Ok;
        }

        public Result Play()
        {
            if (BusyMoving) return Result.Busy;
            if (playCooldown > 0f) return Result.Cooldown;
            if (Data.energy < 12f) return Result.Tired;
            playCooldown = 6f;
            StopMovingKeepItem();
            if (State == CatState.Sleeping) { ReleaseItem(); }
            if (IsStray) Trust = Mathf.Min(100f, Trust + (Trust < 20f ? 4f : 8f));
            Data.fun = Mathf.Min(100f, Data.fun + 18f);
            Data.energy = Mathf.Max(0f, Data.energy - 4f);
            Data.affection = Mathf.Min(100f, Data.affection + 3f);
            State = CatState.Playing;
            stateTimer = 2.8f;
            Visual.Pose = CatPose.Play;
            GM.Sfx.PlayMeow(Data.size);
            return Result.Ok;
        }

        public Result Feed(ItemDef food)
        {
            if (BusyMoving) return Result.Busy;
            if (Data.hunger > 92f) return Result.Full;
            if (!GM.TakeItem(food.id)) return Result.NoFood;

            bool favorite = food.id == Data.favoriteFood;
            Data.hunger = Mathf.Min(100f, Data.hunger + food.hungerGain);
            Data.fun = Mathf.Min(100f, Data.fun + food.funGain + (favorite ? 8f : 0f));
            Data.affection = Mathf.Min(100f, Data.affection + (favorite ? 6f : 3f));
            if (IsStray)
            {
                Trust = Mathf.Min(100f, Trust + food.trustGain * (favorite ? 2f : 1f));
                StayTimeLeft += 20f;
            }
            if (favorite) FavoriteRevealed = true;

            StopMovingKeepItem();
            if (State == CatState.Sleeping) ReleaseItem();
            State = CatState.Eating;
            stateTimer = 3f;
            Visual.Pose = CatPose.Eat;
            SpawnDish(food);
            GM.Sfx.Play(Sound.Eat, 0.7f);
            if (favorite) GM.Floating.SpawnHearts(Visual.HeadTop, 5);
            return Result.Ok;
        }

        void StopMovingKeepItem()
        {
            if (State != CatState.Moving) return;
            bool headingToPerch = usingItem != null && usingItem.Perch;
            path.Clear();
            hasLeg = false;
            onArrive = null;
            if (Perched)
            {
                // interrupted mid-hop: land on the perch if close, otherwise on the floor
                var flat = transform.position; flat.y = 0;
                if (headingToPerch && Vector3.Distance(flat, usingItem.ApproachPoint) < 1.2f)
                {
                    transform.position = usingItem.UsePoint;
                    return;
                }
                transform.position = RoomManager.ClampToFloor(flat);
            }
            if (usingItem != null)
            {
                usingItem.occupant = null;
                usingItem = null;
            }
        }

        void SpawnDish(ItemDef food)
        {
            if (dish != null) Destroy(dish);
            dish = new GameObject("Dish");
            dish.transform.SetParent(transform, false);
            dish.transform.localPosition = new Vector3(0, 0.0f, 0.55f * Data.size);
            Toon.Prim(PrimitiveType.Cylinder, dish.transform, new Vector3(0, 0.03f, 0), new Vector3(0.3f, 0.03f, 0.3f), new Color(1f, 0.98f, 0.95f), true, default, 0.01f);
            Color foodColor = food.id == "food_fish" ? new Color(0.62f, 0.72f, 0.85f)
                : food.id == "food_treat" ? new Color(1f, 0.75f, 0.8f)
                : food.id == "food_wet" ? new Color(0.85f, 0.55f, 0.45f) : new Color(0.75f, 0.5f, 0.3f);
            Toon.Prim(PrimitiveType.Sphere, dish.transform, new Vector3(0, 0.06f, 0), new Vector3(0.22f, 0.06f, 0.22f), foodColor);
        }

        void OnDestroy()
        {
            if (usingItem != null && usingItem.occupant == this) usingItem.occupant = null;
        }
    }
}
