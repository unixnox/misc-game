using UnityEngine;

namespace CatRoom
{
    /// <summary>
    /// Fixed isometric (2.5D) orthographic camera that keeps the whole room in view for any aspect
    /// ratio, plus optional zoom with the mouse wheel / pinch.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        static readonly Vector3 Focus = new Vector3(3.6f, 1.2f, 4.4f);
        const float Distance = 30f;

        Camera cam;
        float zoom = 1f;
        int lastW, lastH;

        public static readonly Color Background = new Color(0.78f, 0.86f, 0.98f);

        public void Init(Camera camera)
        {
            cam = camera;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
            cam.cullingMask = ~(1 << IconRenderer.IconLayer);
            transform.rotation = Quaternion.Euler(30f, -45f, 0f);
            Apply();
        }

        void LateUpdate()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && !InputUtil.PointerOverUI()) zoom = Mathf.Clamp(zoom - scroll * 0.08f, 0.65f, 1.15f);
            if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0); var b = Input.GetTouch(1);
                float prev = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
                float now = (a.position - b.position).magnitude;
                if (prev > 1f) zoom = Mathf.Clamp(zoom * prev / now, 0.65f, 1.15f);
            }
            Apply();
        }

        public bool Portrait => Screen.height > Screen.width;

        void Apply()
        {
            if (cam == null) return;
            float aspect = Mathf.Max(0.3f, (float)Screen.width / Mathf.Max(1, Screen.height));
            // The room is ~12 units wide on screen in this projection; keep a little margin.
            float size = Mathf.Max(5.6f, 6.6f / aspect);
            cam.orthographicSize = size * zoom;
            // Push the room up a bit in portrait so the bottom UI does not cover it.
            var focus = Focus + (Portrait ? new Vector3(0, -1.6f, 0) : new Vector3(0, -0.3f, 0));
            transform.position = focus - transform.forward * Distance;
            lastW = Screen.width; lastH = Screen.height;
        }

        public bool ScreenChanged => Screen.width != lastW || Screen.height != lastH;
    }
}
