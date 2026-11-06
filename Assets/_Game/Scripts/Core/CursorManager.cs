using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace TrenoBambini.Core
{
    /// <summary>
    /// R005: unico punto che decide se il cursore è bloccato (modalità "esplorazione") o libero.
    /// - Modalità "muovendo il mouse" (predefinita, SaveData.visualeTastoDestro = false): su PC, quando si gioca liberamente
    ///   (scena di gioco, nessun blocco dell'input, nessun menu/dialogo/enigma/cutscene/caricamento) il cursore è nascosto e
    ///   bloccato e la camera segue il mouse senza clic. Touch: mai bloccato.
    /// - Modalità "tasto destro": il cursore si blocca solo mentre si tiene premuto il tasto destro (fuori dal browser), come prima.
    /// - WebGL: il blocco (pointer lock) lo concede il browser solo dopo un clic: finché manca compare "Clicca per giocare" e
    ///   quel clic non attiva oggetti. Se il browser toglie il blocco mentre si esplora (Esc, Alt+Tab) si apre la pausa.
    /// </summary>
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager I { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void TrenoInputInit();
        [DllImport("__Internal")] static extern void TrenoSetWantLock(int want);
        [DllImport("__Internal")] static extern void TrenoRequestPointerLock();
        [DllImport("__Internal")] static extern void TrenoExitPointerLock();
        [DllImport("__Internal")] static extern int TrenoPointerLocked();
        [DllImport("__Internal")] static extern int TrenoConsumeLockLost();
        [DllImport("__Internal")] static extern int TrenoLockFailures();
#endif

        /// <summary>True se il gioco vuole il cursore bloccato in questo momento.</summary>
        public bool WantLock { get; private set; }
        /// <summary>True se il cursore è davvero bloccato (nel browser: pointer lock attivo sul canvas).</summary>
        public bool Locked { get; private set; }
        /// <summary>Visuale col mouse senza clic (impostazione salvata).</summary>
        public static bool MouseLookMode
        {
            get { var gm = GameManager.I; return gm == null || !gm.Save.visualeTastoDestro; }
        }
        /// <summary>Esplorazione libera: si può muovere Amerigo e girare la visuale.</summary>
        public bool Exploring { get; private set; }
        /// <summary>Il mouse gira la visuale senza tasti (esplorazione + cursore bloccato).</summary>
        public static bool MouseLookActive { get { return I != null && I.Exploring && I.Locked && MouseLookMode; } }
        /// <summary>Cursore da "riprendere" con un clic (WebGL): il cartellino "Clicca per giocare" è visibile.</summary>
        public bool NeedsClick { get; private set; }
        /// <summary>Il browser ha rifiutato il blocco più volte: il cartellino suggerisce la visuale col tasto destro.</summary>
        public bool LockRefused { get; private set; }

        bool wasLocked;
        float lockedSince = -10f;
        float wantSince;
#if UNITY_WEBGL && !UNITY_EDITOR
        float lastRequest = -10f;
#endif
        int swallowFrame = -1;
        EventSystem disabledFor;

        public static bool IsWebGL
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        void Awake()
        {
            I = this;
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = true; // la tastiera arriva al gioco anche se il focus è sulla pagina
            TrenoInputInit();
#endif
        }

        void OnDestroy() { if (I == this) I = null; }

        /// <summary>
        /// Il clic sinistro di questo frame va ignorato dal gioco (PlayerInteractor, conferme): serve a prendere il blocco
        /// del cursore oppure arriva subito dopo averlo preso.
        /// </summary>
        public static bool SwallowPointer
        {
            get
            {
                var m = I;
                if (m == null) return false;
                if (m.swallowFrame == Time.frameCount) return true;
                if (m.WantLock && MouseLookMode && !m.Locked) return true;
                return m.Locked && Time.unscaledTime - m.lockedSince < 0.25f;
            }
        }

        void Update()
        {
            var gm = GameManager.I;
            var ui = UIRoot.I;
            bool touch = UIRoot.TouchMode;
            bool loading = SceneLoader.I != null && SceneLoader.I.IsLoading;
            bool menus = ui == null || ui.MenuScene || ui.ModalOpen
                || (ui.Pause != null && ui.Pause.IsOpen)
                || (ui.Inventory != null && ui.Inventory.IsOpen)
                || (ui.Cutscene != null && ui.Cutscene.IsPlaying)
                || (ui.Fader != null && ui.Fader.Busy);
            Exploring = gm != null && !gm.InputBlocked && !loading && !menus;

            bool mouseLook = MouseLookMode;
            var mouse = Mouse.current;
            bool want;
            if (touch || !Exploring) want = false;
            else if (mouseLook) want = true;
            else want = !IsWebGL && mouse != null && mouse.rightButton.isPressed; // vecchio comportamento col tasto destro

            if (want && !WantLock) wantSince = Time.unscaledTime;
            WantLock = want;

#if UNITY_WEBGL && !UNITY_EDITOR
            TrenoSetWantLock(want && mouseLook ? 1 : 0); // la richiesta vera parte dal mousedown del canvas (gesto utente)
            LockRefused = TrenoLockFailures() >= 2;
            bool lostByBrowser = TrenoConsumeLockLost() != 0; // un'uscita chiesta dal gioco non conta
#else
            bool lostByBrowser = false;
#endif
            bool locked = ReadLocked();
#if !(UNITY_WEBGL && !UNITY_EDITOR)
            lostByBrowser = wasLocked && !locked; // editor: Esc toglie il blocco
#endif

            // il browser (o l'editor con Esc) ha tolto il blocco mentre si esplorava: si apre la pausa
            if (lostByBrowser && want && mouseLook && ui != null)
            {
                ui.OpenPauseFromLockLoss();
                want = false; WantLock = false; // niente nuovo blocco in questo frame: la pausa è aperta
            }

            ApplyLock(want, locked, mouse);

            locked = ReadLocked();
            if (locked && !wasLocked) lockedSince = Time.unscaledTime;
            Locked = locked;
            wasLocked = locked;

            NeedsClick = want && mouseLook && !locked && Time.unscaledTime - wantSince > 0.3f;

            // con il cursore bloccato la UI non deve ricevere clic "alla cieca" nel punto in cui il cursore è sparito
            var es = EventSystem.current;
            bool uiOff = locked && mouseLook;
            if (uiOff && es != null && es.enabled) { es.enabled = false; disabledFor = es; }
            else if (!uiOff && disabledFor != null) { disabledFor.enabled = true; disabledFor = null; }
        }

        bool ReadLocked()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return TrenoPointerLocked() != 0;
#else
            return Cursor.lockState == CursorLockMode.Locked && Application.isFocused;
#endif
        }

        void ApplyLock(bool want, bool locked, Mouse mouse)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!want)
            {
                if (locked) TrenoExitPointerLock();
                return;
            }
            if (locked) return;
            // il clic che prende il blocco lo gestisce il listener mousedown del canvas (TrenoInput.jslib); qui si segna solo
            // che quel clic non deve attivare oggetti. Ripiego: richiesta appena il blocco diventa necessario
            // (es. "Riprendi" in pausa), senza raffiche
            bool click = mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame);
            if (click) swallowFrame = Time.frameCount;
            if (!click && Time.unscaledTime - wantSince < 0.05f && Time.unscaledTime - lastRequest > 1f)
            {
                lastRequest = Time.unscaledTime;
                TrenoRequestPointerLock();
            }
#else
            if (want)
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    if (mouse != null && mouse.leftButton.wasPressedThisFrame) swallowFrame = Time.frameCount;
                    Cursor.lockState = CursorLockMode.Locked;
                }
                Cursor.visible = false;
            }
            else
            {
                if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
#endif
        }

        void OnDisable()
        {
            if (disabledFor != null) { disabledFor.enabled = true; disabledFor = null; }
#if !(UNITY_WEBGL && !UNITY_EDITOR)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
#endif
        }
    }
}
