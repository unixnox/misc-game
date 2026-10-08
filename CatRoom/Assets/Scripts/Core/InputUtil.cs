using UnityEngine;
using UnityEngine.EventSystems;

namespace CatRoom
{
    /// <summary>Unified mouse/touch pointer helpers (legacy Input Manager).</summary>
    public static class InputUtil
    {
        public static bool PointerDown => Input.GetMouseButtonDown(0);
        public static bool PointerUp => Input.GetMouseButtonUp(0);
        public static bool PointerHeld => Input.GetMouseButton(0);
        public static Vector2 PointerPosition => Input.mousePosition;

        public static bool PointerOverUI()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            for (int i = 0; i < Input.touchCount; i++)
                if (es.IsPointerOverGameObject(Input.GetTouch(i).fingerId)) return true;
            return es.IsPointerOverGameObject();
        }
    }
}
