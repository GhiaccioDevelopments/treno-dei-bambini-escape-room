using UnityEngine;

namespace TrenoBambini.World
{
    /// <summary>Emissione pulsante sui Renderer figli + leggero bob verticale.</summary>
    public class Glow : MonoBehaviour
    {
        public bool on = true;
        public Color color = new Color(1f, 0.78f, 0.35f);
        public float intensity = 1.2f;
        public float speed = 3f;
        public bool bob = true;
        public float bobAmplitude = 0.05f;

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        Renderer[] renderers;
        MaterialPropertyBlock block;
        Vector3 basePos;
        bool wasOn;
        bool hasBase;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                var mats = renderers[i].materials; // istanze: si può abilitare la keyword senza toccare l'asset
                for (int m = 0; m < mats.Length; m++) mats[m].EnableKeyword("_EMISSION");
            }
        }

        void OnEnable()
        {
            basePos = transform.localPosition;
            hasBase = true;
            wasOn = !on; // forza l'aggiornamento al primo frame
        }

        void Update()
        {
            if (on)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.time * speed);
                SetEmission(color * (intensity * (0.35f + 0.65f * k)));
                if (bob) transform.localPosition = basePos + Vector3.up * (Mathf.Sin(Time.time * speed * 0.7f) * bobAmplitude);
                wasOn = true;
            }
            else if (wasOn)
            {
                SetEmission(Color.black);
                if (hasBase) transform.localPosition = basePos;
                wasOn = false;
            }
        }

        void SetEmission(Color c)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].GetPropertyBlock(block);
                block.SetColor(EmissionId, c);
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
