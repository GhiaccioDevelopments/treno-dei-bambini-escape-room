using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Dialogue;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// R028: il pulsante "altoparlante" sotto le battute del Narratore (cutscene, introduzioni e conclusioni dei ricordi). Icona d'epoca disegnata da codice
    /// (un altoparlante a tromba da grammofono, inchiostro su carta), non un'emoji. Clic, tasto V o gamepad Nord = riproduce o ripete la voce della battuta;
    /// mentre suona compaiono le onde; secondo clic = ferma. Se il file non c'e' il pulsante resta, grigio, con il suggerimento (testo in ui_menu.json, blocco voce).
    /// La voce passa dal canale Voci di AudioManager. Lettura automatica: SaveData.narratoreManuale = false (default) = la voce parte da sola.
    /// </summary>
    public class Altoparlante : MonoBehaviour
    {
        Image icona;
        readonly Image[] onde = new Image[3];
        TextMeshProUGUI nota;
        AudioClip clip;
        string voce;
        bool mio;            // la voce che sta suonando l'ho fatta partire io

        public string Voce { get { return voce; } }
        public bool HaVoce { get { return clip != null; } }
        public bool Suona { get { return mio && AudioManager.I != null && AudioManager.I.VoicePlaying; } }

        /// <summary>true = le battute del Narratore si leggono da sole (impostazione).</summary>
        public static bool LetturaAutomatica { get { return GameManager.I == null || !GameManager.I.Save.narratoreManuale; } }

        public static Altoparlante Crea(RectTransform parent, Vector2 anchor, Vector2 pos)
        {
            var rt = UIFactory.NewRect("Altoparlante", parent);
            UIFactory.Place(rt, anchor, anchor, pos, new Vector2(64f, 52f));
            var a = rt.gameObject.AddComponent<Altoparlante>();
            var fondo = UIFactory.MakePanel(rt, "Fondo", new Color(1f, 1f, 1f, 0.001f), true);
            UIFactory.Stretch(fondo.rectTransform);
            fondo.gameObject.AddComponent<Button>().onClick.AddListener(a.Premi);
            a.icona = UIFactory.MakeImage(rt, "Icona", Icona(), UITheme.Inchiostro);
            UIFactory.Place(a.icona.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(2f, 0f), new Vector2(40f, 40f));
            a.icona.raycastTarget = false;
            for (int i = 0; i < 3; i++)
            {
                var o = UIFactory.MakePanel(rt, "Onda" + i, UITheme.Inchiostro, false);
                UIFactory.Place(o.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(44f + i * 7f, 0f), new Vector2(3f, 10f + i * 8f));
                o.raycastTarget = false;
                a.onde[i] = o;
            }
            a.nota = UIFactory.MakeText(rt, "", 13, new Color(0.23f, 0.16f, 0.1f, 0.7f), TextAlignmentOptions.TopRight, FontStyles.Italic);
            UIFactory.Place(a.nota.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(260f, 20f));
            a.nota.raycastTarget = false;
            return a;
        }

        /// <summary>Collega il pulsante alla battuta (id del file in Assets/Audio/Voci, senza estensione). Vuoto = pulsante nascosto.</summary>
        public void Imposta(string id)
        {
            Ferma();
            voce = id;
            gameObject.SetActive(!string.IsNullOrEmpty(id));
            clip = string.IsNullOrEmpty(id) ? null : VoiceLibrary.Get(id);
            var c = UITheme.Inchiostro;
            icona.color = clip != null ? c : new Color(c.r, c.g, c.b, 0.3f);
            nota.text = clip != null ? "" : GameData.UiMenu.voce.mancante;
            Onde(false, 0f);
        }

        /// <summary>Fa partire la voce (lettura automatica o pulsante). false se il file manca.</summary>
        public bool Parti()
        {
            if (clip == null || AudioManager.I == null) return false;
            AudioManager.I.PlayVoice(clip);
            mio = true;
            return true;
        }

        public void Ferma()
        {
            if (mio && AudioManager.I != null) AudioManager.I.StopVoice();
            mio = false;
        }

        public void Premi()
        {
            if (clip == null) { UISfx.Play(Sfx.Tick, 0.3f); return; }
            if (Suona) Ferma(); else Parti();
            UISfx.Play(Sfx.ClickPaper, 0.3f);
        }

        void Update()
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            if ((kb != null && kb.vKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame)) Premi();
            bool s = Suona;
            if (!s && mio && AudioManager.I != null && !AudioManager.I.VoicePlaying) mio = false;
            Onde(s, Time.unscaledTime);
        }

        void Onde(bool on, float t)
        {
            for (int i = 0; i < onde.Length; i++)
            {
                if (onde[i] == null) continue;
                var c = onde[i].color;
                c.a = on ? 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 5f - i * 0.9f)) : 0f;
                onde[i].color = c;
            }
        }

        void OnDisable() { Ferma(); }

        // ------------------------------------------------------------------ icona disegnata (tromba da grammofono)
        static Sprite cache;

        static Sprite Icona()
        {
            if (cache != null) return cache;
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)(N - 1), v = y / (float)(N - 1) - 0.5f;
                    float a = 0f;
                    // corpo: rettangolino a sinistra
                    if (u > 0.08f && u < 0.3f && Mathf.Abs(v) < 0.13f) a = 1f;
                    // tromba: si allarga verso destra
                    float semi = Mathf.Lerp(0.13f, 0.42f, Mathf.InverseLerp(0.3f, 0.82f, u));
                    if (u >= 0.3f && u < 0.82f && Mathf.Abs(v) < semi) a = 1f;
                    // bordo della campana, un po' piu' scuro e spesso
                    if (u >= 0.78f && u < 0.86f && Mathf.Abs(v) < 0.44f) a = 1f;
                    // vuoto interno (resta solo il contorno della tromba, come un disegno a china)
                    float semiIn = semi - 0.07f;
                    if (u > 0.36f && u < 0.78f && Mathf.Abs(v) < semiIn) a = 0f;
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            cache = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
            return cache;
        }
    }
}
