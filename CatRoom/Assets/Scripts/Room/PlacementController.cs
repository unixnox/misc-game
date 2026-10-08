using System;
using UnityEngine;

namespace CatRoom
{
    /// <summary>
    /// Decorate mode: place new items from the bag, or pick up existing ones to move / rotate / store.
    /// A "ghost" preview follows the pointer and is committed when the pointer is released on the floor.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        public bool EditMode { get; private set; }
        public bool HasGhost => ghost != null;
        public bool MovingExisting => moving != null;
        public ItemDef GhostDef => ghostDef;
        public event Action Changed;

        ItemDef ghostDef;
        PlacedItem moving;
        GameObject ghost;
        int rot;
        Vector2Int cell;
        bool valid;
        bool pressed;
        Vector2 pressPos;
        Vector3 dragOffset;
        Material okMat, badMat;
        Renderer[] ghostRenderers;

        GameManager GM => GameManager.I;
        RoomManager Room => GM.Room;

        void Awake()
        {
            okMat = Toon.Ghost(new Color(0.45f, 1f, 0.55f, 0.55f));
            badMat = Toon.Ghost(new Color(1f, 0.35f, 0.35f, 0.55f));
        }

        public void EnterEdit()
        {
            if (EditMode) return;
            EditMode = true;
            GM.Select(null);
            Room.ShowGrid(true);
            Changed?.Invoke();
        }

        public void ExitEdit()
        {
            if (!EditMode) return;
            if (moving != null) CommitOrRestore();
            CancelGhost();
            EditMode = false;
            Room.ShowGrid(false);
            Changed?.Invoke();
        }

        public void BeginPlaceNew(ItemDef def)
        {
            if (!EditMode) EnterEdit();
            if (moving != null) CommitOrRestore();
            CancelGhost();
            ghostDef = def;
            rot = 0;
            dragOffset = Vector3.zero;
            CreateGhost();
            cell = FindFreeCell(def, rot);
            UpdateGhost();
            Changed?.Invoke();
        }

        public void BeginMove(PlacedItem item)
        {
            CancelGhost();
            moving = item;
            dragOffset = Vector3.zero;
            ghostDef = item.def;
            rot = item.rot;
            cell = new Vector2Int(item.x, item.z);
            if (item.occupant != null) item.occupant.DropToFloor();
            item.gameObject.SetActive(false);
            CreateGhost();
            UpdateGhost();
            GM.Sfx.Play(Sound.Pop, 0.6f);
            Changed?.Invoke();
        }

        public void Rotate()
        {
            if (ghost == null) return;
            rot = (rot + 1) % 4;
            var center = RoomManager.FootprintCenter(ghostDef, cell.x, cell.y, (rot + 3) % 4);
            cell = RoomManager.CellFor(ghostDef, rot, center);
            UpdateGhost();
            GM.Sfx.Play(Sound.Click, 0.5f);
        }

        /// <summary>Put the item being moved back into the bag.</summary>
        public void StoreMoving()
        {
            if (moving == null) return;
            var def = moving.def;
            moving.gameObject.SetActive(true);
            Room.Remove(moving);
            moving = null;
            GM.AddItem(def.id);
            DestroyGhost();
            GM.Sfx.Play(Sound.Pop, 0.6f);
            Changed?.Invoke();
        }

        public void ConfirmHere()
        {
            if (ghost == null) return;
            if (!TryCommit()) GM.Sfx.Play(Sound.Error, 0.5f);
        }

        public void CancelGhost()
        {
            if (moving != null)
            {
                moving.gameObject.SetActive(true);
                moving = null;
            }
            DestroyGhost();
            Changed?.Invoke();
        }

        void CommitOrRestore()
        {
            if (moving == null) return;
            if (valid) TryCommit();
            else CancelGhost();
        }

        bool TryCommit()
        {
            if (ghost == null || !valid) return false;
            if (moving != null)
            {
                moving.gameObject.SetActive(true);
                Room.Move(moving, cell.x, cell.y, rot);
                moving = null;
            }
            else
            {
                if (!GM.TakeItem(ghostDef.id)) { CancelGhost(); return false; }
                Room.Place(ghostDef, cell.x, cell.y, rot);
            }
            GM.Sfx.Play(Sound.Place, 0.8f);
            DestroyGhost();
            GM.RequestSave();
            Changed?.Invoke();
            return true;
        }

        void CreateGhost()
        {
            DestroyGhost();
            ghost = ItemModels.Build(ghostDef);
            ghost.name = "Ghost";
            foreach (var anim in ghost.GetComponentsInChildren<SimpleAnim>()) anim.enabled = false;
            ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
        }

        void DestroyGhost()
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
            ghostRenderers = null;
            ghostDef = moving != null ? ghostDef : null;
        }

        void UpdateGhost()
        {
            if (ghost == null) return;
            valid = Room.CanPlace(ghostDef, cell.x, cell.y, rot, moving);
            ghost.transform.position = RoomManager.FootprintCenter(ghostDef, cell.x, cell.y, rot) + Vector3.up * 0.02f;
            ghost.transform.rotation = Quaternion.Euler(0, rot * 90f, 0);
            var mat = valid ? okMat : badMat;
            foreach (var r in ghostRenderers) r.sharedMaterial = mat;
        }

        Vector2Int FindFreeCell(ItemDef def, int r)
        {
            int fw = RoomManager.FootW(def, r), fd = RoomManager.FootD(def, r);
            // spiral-ish search from the room center
            var best = new Vector2Int(Mathf.Clamp(4 - fw / 2, 0, RoomManager.Size - fw), Mathf.Clamp(4 - fd / 2, 0, RoomManager.Size - fd));
            float bestD = float.MaxValue;
            for (int x = 0; x <= RoomManager.Size - fw; x++)
            for (int z = 0; z <= RoomManager.Size - fd; z++)
            {
                if (!Room.CanPlace(def, x, z, r)) continue;
                float d = Vector2.Distance(new Vector2(x, z), new Vector2(3.5f, 3.5f));
                if (d < bestD) { bestD = d; best = new Vector2Int(x, z); }
            }
            return best;
        }

        void Update()
        {
            if (!EditMode || GM == null || !GM.Ready) return;

            if (Input.GetKeyDown(KeyCode.R)) Rotate();
            if (Input.GetKeyDown(KeyCode.Escape)) CancelGhost();

            bool overUI = InputUtil.PointerOverUI();

            if (InputUtil.PointerDown && !overUI)
            {
                pressed = true;
                pressPos = InputUtil.PointerPosition;
                if (ghost == null)
                {
                    var item = PickItem(InputUtil.PointerPosition);
                    if (item != null)
                    {
                        BeginMove(item);
                        // keep the grab point under the finger so a simple tap does not move the item
                        var r0 = GM.MainCamera.ScreenPointToRay(InputUtil.PointerPosition);
                        if (RoomManager.RaycastFloor(r0, out var p0)) dragOffset = item.transform.position - p0;
                    }
                }
            }

            if (ghost != null && (pressed || (!overUI && Input.touchCount == 0 && !InputUtil.PointerHeld)))
            {
                // follow the pointer (drag on touch, hover with a mouse)
                var ray = GM.MainCamera.ScreenPointToRay(InputUtil.PointerPosition);
                if (RoomManager.RaycastFloor(ray, out var p) && p.x > -1f && p.z > -1f && p.x < RoomManager.Size + 1f && p.z < RoomManager.Size + 1f)
                {
                    var c = RoomManager.CellFor(ghostDef, rot, p + dragOffset);
                    if (c != cell) { cell = c; UpdateGhost(); }
                }
            }

            if (pressed && InputUtil.PointerUp)
            {
                pressed = false;
                bool dragged = (InputUtil.PointerPosition - pressPos).magnitude > 18f;
                if (ghost != null && !overUI)
                {
                    // A simple tap on an item that was just picked up keeps it selected (so it can be rotated/stored).
                    if (moving != null && !dragged && cell.x == moving.x && cell.y == moving.z && rot == moving.rot) return;
                    if (!TryCommit()) GM.Sfx.Play(Sound.Error, 0.4f);
                }
            }
        }

        PlacedItem PickItem(Vector2 screen)
        {
            var ray = GM.MainCamera.ScreenPointToRay(screen);
            PlacedItem best = null;
            float bestDist = float.MaxValue;
            foreach (var h in Physics.RaycastAll(ray, 100f))
            {
                var it = h.collider.GetComponent<PlacedItem>();
                if (it == null) continue;
                // prefer non-flat items standing on rugs
                float d = h.distance + (it.def.flat ? 50f : 0f);
                if (d < bestDist) { bestDist = d; best = it; }
            }
            return best;
        }
    }
}
