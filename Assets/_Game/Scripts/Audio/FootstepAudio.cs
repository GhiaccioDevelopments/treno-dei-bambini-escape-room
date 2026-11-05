using TrenoBambini.Core;
using TrenoBambini.Player;
using UnityEngine;

namespace TrenoBambini.Audio
{
    /// <summary>
    /// Passi di Amerigo legati alla velocità reale del ThirdPersonController: più rapidi quando corre, assenti da fermo o in aria.
    /// Superficie (pietra / legno / ovattato) letta dalla scena tramite audio.json (AudioManager.Surface). Alternanza sinistro/destro
    /// con leggere differenze di tono e stereo, varianti diverse a ogni passo. Viene aggiunto da SceneAudioDirector al player.
    /// </summary>
    [RequireComponent(typeof(ThirdPersonController))]
    public class FootstepAudio : MonoBehaviour
    {
        public float strideWalk = 0.95f;   // metri per passo camminando
        public float strideRun = 1.5f;     // metri per passo correndo

        ThirdPersonController tpc;
        float travelled;
        bool left;

        void Awake() { tpc = GetComponent<ThirdPersonController>(); }

        void Update()
        {
            if (tpc == null || AudioManager.I == null) return;
            float speed = tpc.CurrentSpeed;
            if (!tpc.Grounded || speed < 0.35f) { travelled = Mathf.Min(travelled, strideWalk * 0.6f); return; }
            if (GameManager.I != null && GameManager.I.InputBlocked && speed < 0.6f) return;

            bool running = speed > (tpc.walkSpeed + tpc.runSpeed) * 0.5f;
            float stride = running ? strideRun : strideWalk;
            travelled += speed * Time.deltaTime;
            if (travelled < stride) return;
            travelled -= stride;
            left = !left;

            string surface = AudioManager.I.Surface;
            string id = "passo_" + (string.IsNullOrEmpty(surface) ? "pietra" : surface);
            float t = Mathf.InverseLerp(tpc.walkSpeed * 0.5f, tpc.runSpeed, speed);
            AudioManager.I.Sfx.Play(id, Mathf.Lerp(0.45f, 0.8f, t), 0.06f + (left ? 0f : 0.02f), left ? -0.12f : 0.12f);
        }
    }
}
