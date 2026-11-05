using UnityEngine;
using UnityEngine.UI;

namespace TrenoBambini.UI.Fx
{
    public enum ParticleKind { Dust, Ink, Spark, Paper }

    /// <summary>
    /// Particelle UI a pool (polvere, schizzi d'inchiostro, scintille dorate, coriandoli di carta).
    /// Pool fisso di Image su un layer con Canvas annidato (non fa ricostruire il resto della UI); zero allocazioni per frame.
    /// Con "Effetti ridotti" non emette nulla.
    /// </summary>
    public class UIParticles : MonoBehaviour
    {
        struct P
        {
            public bool alive;
            public Vector2 pos, vel;
            public float life, max, size, rot, rotVel, gravity, drag;
            public Color color;
            public bool twinkle;
        }

        public static UIParticles I { get; private set; }

        const int MaxParticles = 72;
        P[] ps;
        Image[] imgs;
        RectTransform layer;
        int cursor;

        /// <summary>Crea il pool sotto "parent" (un layer a schermo intero).</summary>
        public static UIParticles Create(RectTransform parent)
        {
            var rt = UIFactory.NewRect("Particelle", parent);
            UIFactory.Stretch(rt);
            var cv = rt.gameObject.AddComponent<Canvas>(); // canvas annidato: isola le variazioni per frame
            cv.overrideSorting = false;
            var p = rt.gameObject.AddComponent<UIParticles>();
            p.layer = rt;
            p.ps = new P[MaxParticles];
            p.imgs = new Image[MaxParticles];
            for (int i = 0; i < MaxParticles; i++)
            {
                var img = UIFactory.MakePanel(rt, "p", Color.white, false);
                img.sprite = UIArt.Glow();
                img.gameObject.SetActive(false);
                p.imgs[i] = img;
            }
            I = p;
            return p;
        }

        void OnDestroy() { if (I == this) I = null; }

        /// <summary>Posizione in coordinate del layer (centro schermo = 0,0) partendo da un punto in pixel di schermo.</summary>
        public Vector2 ScreenToLocal(Vector2 screen)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out local);
            return local;
        }

        public static void BurstAtScreen(ParticleKind kind, Vector2 screen, int count)
        {
            if (I == null || UITheme.ReduceEffects) return;
            I.Burst(kind, I.ScreenToLocal(screen), count);
        }

        public static void BurstAtRect(ParticleKind kind, RectTransform rt, int count)
        {
            if (I == null || rt == null || UITheme.ReduceEffects) return;
            Vector3 w = rt.TransformPoint(rt.rect.center);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(I.layer, RectTransformUtility.WorldToScreenPoint(null, w), null, out local);
            I.Burst(kind, local, count);
        }

        public void Burst(ParticleKind kind, Vector2 local, int count)
        {
            if (UITheme.ReduceEffects) return;
            for (int i = 0; i < count; i++) Spawn(kind, local);
        }

        void Spawn(ParticleKind kind, Vector2 local)
        {
            int idx = -1;
            for (int k = 0; k < MaxParticles; k++)
            {
                int j = (cursor + k) % MaxParticles;
                if (!ps[j].alive) { idx = j; break; }
            }
            if (idx < 0) idx = cursor; // pool pieno: ricicla la più vecchia
            cursor = (idx + 1) % MaxParticles;

            P p = new P();
            p.alive = true; p.pos = local;
            float ang = Random.value * Mathf.PI * 2f;
            var img = imgs[idx];
            switch (kind)
            {
                case ParticleKind.Dust:
                    p.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(20f, 70f) + Vector2.up * 15f;
                    p.max = Random.Range(0.5f, 0.9f); p.size = Random.Range(14f, 30f); p.gravity = 0f; p.drag = 2.5f;
                    p.color = new Color(0.62f, 0.52f, 0.4f, 0.5f);
                    img.sprite = UIArt.Glow();
                    break;
                case ParticleKind.Ink:
                    p.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.8f + 0.4f) * Random.Range(80f, 220f);
                    p.max = Random.Range(0.4f, 0.8f); p.size = Random.Range(5f, 12f); p.gravity = -500f; p.drag = 1.2f;
                    p.color = new Color(0.14f, 0.17f, 0.3f, 0.85f);
                    img.sprite = UIArt.Glow();
                    break;
                case ParticleKind.Spark:
                    p.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(50f, 190f);
                    p.max = Random.Range(0.5f, 1.0f); p.size = Random.Range(12f, 26f); p.gravity = -60f; p.drag = 2f;
                    p.color = Random.value > 0.5f ? UITheme.Oro : UITheme.OttoneLuce;
                    p.twinkle = true;
                    img.sprite = UIArt.Spark();
                    break;
                default: // Paper: coriandoli che cadono
                    p.pos += new Vector2(Random.Range(-60f, 60f), 0f);
                    p.vel = new Vector2(Random.Range(-70f, 70f), Random.Range(20f, 160f));
                    p.max = Random.Range(0.9f, 1.5f); p.size = Random.Range(8f, 14f); p.gravity = -260f; p.drag = 1.4f;
                    p.color = Random.value > 0.5f ? UITheme.Carta : new Color(0.8f, 0.7f, 0.5f, 1f);
                    p.rotVel = Random.Range(-360f, 360f);
                    img.sprite = UIArt.White();
                    break;
            }
            p.life = p.max;
            p.rot = Random.value * 360f;
            ps[idx] = p;
            img.color = p.color;
            img.gameObject.SetActive(true);
            var rt = img.rectTransform;
            rt.anchoredPosition = p.pos;
            rt.sizeDelta = new Vector2(p.size, kind == ParticleKind.Paper ? p.size * 0.6f : p.size);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < MaxParticles; i++)
            {
                if (!ps[i].alive) continue;
                ps[i].life -= dt;
                if (ps[i].life <= 0f)
                {
                    ps[i].alive = false;
                    imgs[i].gameObject.SetActive(false);
                    continue;
                }
                ps[i].vel.y += ps[i].gravity * dt;
                ps[i].vel *= Mathf.Max(0f, 1f - ps[i].drag * dt);
                ps[i].pos += ps[i].vel * dt;
                ps[i].rot += ps[i].rotVel * dt;
                float k = ps[i].life / ps[i].max;
                Color c = ps[i].color;
                c.a *= Mathf.Clamp01(k * 2f) * (ps[i].twinkle ? 0.6f + 0.4f * Mathf.Sin(ps[i].life * 40f) : 1f);
                var rt = imgs[i].rectTransform;
                rt.anchoredPosition = ps[i].pos;
                rt.localRotation = Quaternion.Euler(0f, 0f, ps[i].rot);
                imgs[i].color = c;
            }
        }
    }
}
