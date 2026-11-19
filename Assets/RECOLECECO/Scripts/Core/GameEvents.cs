using System;
using UnityEngine;

namespace RecolecEco
{
    /// <summary>Bus de eventos: la lógica avisa, la interfaz escucha (separación lógica / UI, §8).</summary>
    public static class GameEvents
    {
        public static event Action StateChanged;
        public static event Action<string> Toast;
        public static event Action<CollectibleWaste> WasteCollected;
        public static event Action<int> MissionStarted;
        public static event Action<int> MissionCompleted;
        public static event Action<int> MilestoneReached;
        public static event Action Docked;
        public static event Action Undocked;

        public static void RaiseStateChanged() => StateChanged?.Invoke();
        public static void RaiseToast(string msg) { Toast?.Invoke(msg); }
        public static void RaiseWasteCollected(CollectibleWaste w) => WasteCollected?.Invoke(w);
        public static void RaiseMissionStarted(int i) => MissionStarted?.Invoke(i);
        public static void RaiseMissionCompleted(int i) => MissionCompleted?.Invoke(i);
        public static void RaiseMilestone(int pct) => MilestoneReached?.Invoke(pct);
        public static void RaiseDocked() => Docked?.Invoke();
        public static void RaiseUndocked() => Undocked?.Invoke();

        /// <summary>Limpia suscriptores al cargar escena para evitar referencias muertas.</summary>
        public static void Clear()
        {
            StateChanged = null; Toast = null; WasteCollected = null; MissionStarted = null;
            MissionCompleted = null; MilestoneReached = null; Docked = null; Undocked = null;
        }
    }

    /// <summary>Texto de interacción contextual ("E — Recoger", "E — Atracar").</summary>
    public static class InteractionPrompt
    {
        static object _owner;
        static string _text;
        static int _priority;

        public static string Text => _text;
        public static object Owner => _owner;
        /// <summary>Frame en que la interfaz usó una tecla (evita que E cierre un diálogo y recoja a la vez).</summary>
        public static int UIConsumedFrame = -1;
        public static bool KeyFree => UnityEngine.Time.frameCount > UIConsumedFrame + 1;

        public static void Set(object owner, string text, int priority = 0)
        {
            if (_owner != null && _owner != owner && priority < _priority) return;
            _owner = owner; _text = text; _priority = priority;
        }

        public static void Clear(object owner)
        {
            if (_owner == owner) { _owner = null; _text = null; _priority = 0; }
        }

        public static void Reset() { _owner = null; _text = null; _priority = 0; }
    }

    /// <summary>Olas compartidas entre C# y el shader de agua.</summary>
    public static class Waves
    {
        // dirX, dirZ, amplitud, k (2π/λ)
        public static readonly Vector4 W0 = new Vector4(1f, 0.3f, 0.16f, 2f * Mathf.PI / 15f);
        public static readonly Vector4 W1 = new Vector4(-0.45f, 1f, 0.10f, 2f * Mathf.PI / 9f);
        public static readonly Vector4 W2 = new Vector4(0.7f, -0.7f, 0.05f, 2f * Mathf.PI / 5f);
        public static readonly Vector4 Speeds = new Vector4(1.25f, 1.6f, 2.3f, 0f);

        public static float Time;

        static float One(Vector4 w, float s, float x, float z)
        {
            Vector2 d = new Vector2(w.x, w.y).normalized;
            return w.z * Mathf.Sin(w.w * (d.x * x + d.y * z) + s * Time);
        }

        public static float Height(float x, float z)
        {
            return One(W0, Speeds.x, x, z) + One(W1, Speeds.y, x, z) + One(W2, Speeds.z, x, z);
        }

        public static float Height(Vector3 p) => Height(p.x, p.z);

        /// <summary>Restablece variables globales de render (agua limpia, texto 3D con profundidad).</summary>
        public static void ResetGlobals()
        {
            for (int i = 0; i < 3; i++) Shader.SetGlobalVector("_RecSector" + i, Vector4.zero);
            Shader.SetGlobalFloat("unity_GUIZTestMode", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        }

        public static void PushToShader(float time)
        {
            Time = time;
            Shader.SetGlobalFloat("_RecTime", time);
            Shader.SetGlobalVector("_RecWave0", W0);
            Shader.SetGlobalVector("_RecWave1", W1);
            Shader.SetGlobalVector("_RecWave2", W2);
            Shader.SetGlobalVector("_RecWaveSpeed", Speeds);
        }
    }
}
