using UnityEngine;

namespace CatRoom
{
    public enum CatPose { Stand, Walk, Loaf, Sleep, Eat, Happy, Play, Hiss, Scratch }

    /// <summary>
    /// A chibi cat assembled from primitives, with simple procedural animation.
    /// The model faces +Z and stands on y = 0.
    /// </summary>
    public class CatVisual : MonoBehaviour
    {
        const float OutlineW = 0.012f;

        Transform model, body, head, tailPivot, earL, earR;
        Transform[] legs = new Transform[4];
        Transform[] eyes = new Transform[2];
        Transform[] closedEyes = new Transform[2];
        float blinkTimer = 2f, blinkT;
        float walkCycle;
        float t0;
        readonly float[] legSwing = new float[4];

        public CatPose Pose = CatPose.Stand;
        public float LookYaw;   // head yaw offset in degrees

        public static CatVisual Build(Transform parent, int presetIndex, float size)
        {
            var p = CatAppearance.Get(presetIndex);
            var root = new GameObject("CatModel");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * size;
            var v = root.AddComponent<CatVisual>();
            v.Create(p);
            return v;
        }

        void Create(CatPreset p)
        {
            t0 = Random.value * 10f;
            model = Toon.Pivot("Model", transform, Vector3.zero);
            body = Toon.Pivot("Body", model, new Vector3(0, 0.32f, 0));
            Toon.Prim(PrimitiveType.Sphere, body, Vector3.zero, new Vector3(0.48f, 0.4f, 0.66f), p.body, true, default, OutlineW);
            Toon.Prim(PrimitiveType.Sphere, body, new Vector3(0, -0.04f, 0.16f), new Vector3(0.34f, 0.3f, 0.32f), p.legs, false);

            // legs: pivots at the hip so they can swing
            Vector3[] legPos = { new Vector3(-0.13f, 0.2f, 0.19f), new Vector3(0.13f, 0.2f, 0.19f), new Vector3(-0.13f, 0.2f, -0.19f), new Vector3(0.13f, 0.2f, -0.19f) };
            for (int i = 0; i < 4; i++)
            {
                legs[i] = Toon.Pivot("Leg" + i, model, legPos[i]);
                Toon.Prim(PrimitiveType.Sphere, legs[i], new Vector3(0, -0.11f, 0), new Vector3(0.13f, 0.24f, 0.13f), p.legs, true, default, OutlineW);
            }

            // tail
            tailPivot = Toon.Pivot("Tail", model, new Vector3(0, 0.38f, -0.3f), new Vector3(-35, 0, 0));
            Toon.Prim(PrimitiveType.Capsule, tailPivot, new Vector3(0, 0.2f, 0), new Vector3(0.085f, 0.21f, 0.085f), p.tail, true, default, OutlineW);
            Toon.Prim(PrimitiveType.Sphere, tailPivot, new Vector3(0, 0.4f, 0), Vector3.one * 0.1f, p.tailTip, true, default, OutlineW);

            // head
            head = Toon.Pivot("Head", model, new Vector3(0, 0.6f, 0.28f));
            Toon.Prim(PrimitiveType.Sphere, head, Vector3.zero, new Vector3(0.5f, 0.42f, 0.44f), p.head, true, default, OutlineW);
            if (p.patch.a > 0.5f)
                Toon.Prim(PrimitiveType.Sphere, head, new Vector3(0.11f, 0.07f, 0.05f), new Vector3(0.28f, 0.26f, 0.36f), p.patch, false);
            Toon.Prim(PrimitiveType.Sphere, head, new Vector3(0, -0.07f, 0.17f), new Vector3(0.22f, 0.14f, 0.13f), p.muzzle, false);
            Toon.Prim(PrimitiveType.Sphere, head, new Vector3(0, -0.025f, 0.225f), new Vector3(0.06f, 0.04f, 0.035f), new Color(1f, 0.55f, 0.62f));
            // blush
            Toon.Prim(PrimitiveType.Sphere, head, new Vector3(-0.15f, -0.07f, 0.14f), new Vector3(0.08f, 0.04f, 0.03f), new Color(1f, 0.7f, 0.75f), false, new Vector3(0, -35, 0));
            Toon.Prim(PrimitiveType.Sphere, head, new Vector3(0.15f, -0.07f, 0.14f), new Vector3(0.08f, 0.04f, 0.03f), new Color(1f, 0.7f, 0.75f), false, new Vector3(0, 35, 0));

            // eyes: colored iris, dark pupil, white sparkle
            Color[] iris = { p.eyeL, p.eyeR };
            var dark = new Color(0.13f, 0.1f, 0.12f);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                eyes[i] = Toon.Pivot("Eye" + i, head, new Vector3(0.1f * side, 0.025f, 0.185f), new Vector3(0, 18f * side, 0));
                Toon.Prim(PrimitiveType.Sphere, eyes[i], Vector3.zero, new Vector3(0.095f, 0.115f, 0.05f), iris[i]);
                Toon.Prim(PrimitiveType.Sphere, eyes[i], new Vector3(0, 0, 0.012f), new Vector3(0.06f, 0.09f, 0.035f), dark);
                Toon.Prim(PrimitiveType.Sphere, eyes[i], new Vector3(0.015f * side, 0.025f, 0.028f), Vector3.one * 0.028f, Color.white);
                // closed eye "^" made of a flat bar
                closedEyes[i] = Toon.Pivot("Closed" + i, head, new Vector3(0.1f * side, 0.02f, 0.2f), new Vector3(0, 18f * side, 0));
                Toon.Prim(PrimitiveType.Cube, closedEyes[i], Vector3.zero, new Vector3(0.09f, 0.018f, 0.02f), dark);
                closedEyes[i].gameObject.SetActive(false);
            }

            // ears
            var inner = new Color(1f, 0.72f, 0.78f);
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var ear = Toon.Pivot("Ear" + i, head, new Vector3(0.13f * side, 0.13f, -0.02f), new Vector3(0, 0, -18f * side));
                Toon.MeshObj(ProcGen.Cone, ear, Vector3.zero, new Vector3(0.17f, 0.2f, 0.1f), p.ears, true, default, OutlineW);
                Toon.MeshObj(ProcGen.Cone, ear, new Vector3(0, 0.01f, 0.03f), new Vector3(0.1f, 0.14f, 0.05f), inner, false);
                if (i == 0) earL = ear; else earR = ear;
            }

            // whiskers
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                for (int k = 0; k < 2; k++)
                    Toon.Prim(PrimitiveType.Cube, head, new Vector3(0.17f * side, -0.06f + k * 0.03f, 0.17f), new Vector3(0.16f, 0.006f, 0.006f),
                        new Color(0.35f, 0.28f, 0.26f), false, new Vector3(0, -20f * side, (k == 0 ? 8f : -8f) * side));
            }
        }

        void Update()
        {
            float t = Time.time + t0;
            float dt = Time.deltaTime;

            // blinking
            bool forceClosed = Pose == CatPose.Sleep || Pose == CatPose.Happy;
            blinkTimer -= dt;
            if (blinkTimer <= 0f) { blinkT = 0.12f; blinkTimer = Random.Range(2f, 5f); }
            blinkT -= dt;
            bool closed = forceClosed || blinkT > 0f;
            for (int i = 0; i < 2; i++)
            {
                eyes[i].gameObject.SetActive(!closed);
                closedEyes[i].gameObject.SetActive(closed);
            }

            Vector3 modelPos = Vector3.zero;
            Quaternion modelRot = Quaternion.identity;
            Vector3 bodyScale = Vector3.one;
            float headPitch = 0f, headYaw = LookYaw, headRoll = 0f;
            float tailYaw = Mathf.Sin(t * 2f) * 15f, tailPitch = -35f;
            float earTilt = 0f;
            float legScale = 1f;
            for (int i = 0; i < 4; i++) legSwing[i] = 0f;

            switch (Pose)
            {
                case CatPose.Walk:
                    walkCycle += dt * 9f;
                    for (int i = 0; i < 4; i++)
                        legSwing[i] = Mathf.Sin(walkCycle + (i == 0 || i == 3 ? 0f : Mathf.PI)) * 28f;
                    modelPos.y = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.035f;
                    tailYaw = Mathf.Sin(t * 5f) * 12f;
                    tailPitch = -50f;
                    break;
                case CatPose.Loaf:
                    legScale = 0.35f;
                    modelPos.y = -0.12f;
                    bodyScale = new Vector3(1.05f, 1f + Mathf.Sin(t * 2f) * 0.02f, 1f);
                    tailYaw = 70f + Mathf.Sin(t * 1.5f) * 8f;
                    tailPitch = -95f;
                    headYaw += Mathf.Sin(t * 0.5f) * 25f;
                    break;
                case CatPose.Sleep:
                    legScale = 0.3f;
                    modelPos.y = -0.15f;
                    bodyScale = new Vector3(1.08f, 0.95f + Mathf.Sin(t * 1.6f) * 0.04f, 1f);
                    headPitch = 18f;
                    headRoll = 12f;
                    tailYaw = 95f;
                    tailPitch = -100f;
                    break;
                case CatPose.Eat:
                    headPitch = 30f + Mathf.Sin(t * 10f) * 6f;
                    tailYaw = Mathf.Sin(t * 3f) * 20f;
                    break;
                case CatPose.Happy:
                    headRoll = Mathf.Sin(t * 4f) * 12f;
                    bodyScale = new Vector3(1f + Mathf.Sin(t * 8f) * 0.03f, 1f - Mathf.Sin(t * 8f) * 0.03f, 1f);
                    tailYaw = Mathf.Sin(t * 6f) * 25f;
                    tailPitch = -15f;
                    break;
                case CatPose.Play:
                {
                    float j = Mathf.Abs(Mathf.Sin(t * 6f));
                    modelPos.y = j * 0.25f;
                    modelRot = Quaternion.Euler(-j * 15f, 0, 0);
                    for (int i = 0; i < 2; i++) legSwing[i] = -j * 40f;
                    tailYaw = Mathf.Sin(t * 9f) * 30f;
                    tailPitch = -10f;
                    break;
                }
                case CatPose.Scratch:
                {
                    // stand up on hind legs and paw
                    modelRot = Quaternion.Euler(-40f, 0, 0);
                    modelPos = new Vector3(0, 0.12f, -0.1f);
                    legSwing[0] = -60f + Mathf.Sin(t * 14f) * 25f;
                    legSwing[1] = -60f - Mathf.Sin(t * 14f) * 25f;
                    tailPitch = -80f;
                    break;
                }
                case CatPose.Hiss:
                    bodyScale = new Vector3(1.15f, 1.2f, 1.05f);
                    modelPos.y = 0.04f + Mathf.Sin(t * 30f) * 0.008f;
                    earTilt = 45f;
                    tailPitch = 10f;
                    tailYaw = 0f;
                    break;
            }

            model.localPosition = Vector3.Lerp(model.localPosition, modelPos, dt * 12f);
            model.localRotation = Quaternion.Slerp(model.localRotation, modelRot, dt * 10f);
            body.localScale = Vector3.Lerp(body.localScale, bodyScale, dt * 10f);
            head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(headPitch, headYaw, headRoll), dt * 8f);
            tailPivot.localRotation = Quaternion.Slerp(tailPivot.localRotation, Quaternion.Euler(tailPitch, tailYaw, 0), dt * 8f);
            earL.localRotation = Quaternion.Slerp(earL.localRotation, Quaternion.Euler(-earTilt * 0.5f, 0, 18f + earTilt), dt * 10f);
            earR.localRotation = Quaternion.Slerp(earR.localRotation, Quaternion.Euler(-earTilt * 0.5f, 0, -18f - earTilt), dt * 10f);
            for (int i = 0; i < 4; i++)
            {
                legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, Quaternion.Euler(legSwing[i], 0, 0), dt * 14f);
                legs[i].localScale = Vector3.Lerp(legs[i].localScale, new Vector3(1, legScale, 1), dt * 10f);
            }
        }

        /// <summary>World position just above the cat's head, for UI bubbles.</summary>
        public Vector3 HeadTop => head != null ? head.position + Vector3.up * 0.35f * transform.lossyScale.y : transform.position;
    }
}
