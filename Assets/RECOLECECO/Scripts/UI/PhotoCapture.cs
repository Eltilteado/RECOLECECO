using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RecolecEco
{
    /// <summary>Guarda una imagen limpia de la vista de juego en el equipo.</summary>
    public sealed class PhotoCapture : MonoBehaviour
    {
        Canvas _hud;
        bool _capturing, _restoreHud;

        public void SetHud(Canvas hud) => _hud = hud;

        void Update()
        {
            var game = GameManager.I;
            var keys = Keyboard.current;
            if (_capturing || !_hud || !game || !game.State.introShown || game.ControlsLocked
                || (keys != null && (keys.escapeKey.wasPressedThisFrame || keys.mKey.wasPressedThisFrame))) return;
            if (keys != null && keys.f8Key.wasPressedThisFrame) StartCoroutine(Capture());
        }

        IEnumerator Capture()
        {
            _capturing = true;
            _restoreHud = _hud.enabled;
            _hud.enabled = false;
            yield return new WaitForEndOfFrame();
            Texture2D image = null;
            string message;
            try
            {
                image = ScreenCapture.CaptureScreenshotAsTexture();
                if (!image) throw new IOException("La vista del juego no produjo una imagen.");
                string folder = Path.Combine(Application.persistentDataPath, "Fotos");
                Directory.CreateDirectory(folder);
                string name = $"Pisco_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                File.WriteAllBytes(Path.Combine(folder, name), image.EncodeToPNG());
                message = "Foto guardada · Fotos/" + name;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("No se pudo guardar la foto: " + exception.Message);
                message = "No se pudo guardar la foto";
            }
            finally
            {
                if (image) Destroy(image);
                RestoreHud();
            }
            GameEvents.RaiseToast(message);
        }

        void RestoreHud()
        {
            if (_capturing && _hud) _hud.enabled = _restoreHud;
            _capturing = false;
        }

        void OnDisable() => RestoreHud();
    }
}
