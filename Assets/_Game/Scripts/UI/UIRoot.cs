using System;
using System.Collections.Generic;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Dialogue;
using TrenoBambini.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>Canvas globale costruito da codice: HUD, prompt, toast, dialoghi modali, fader, cutscene, inventario, pausa.</summary>
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot I { get; private set; }

        public Canvas Canvas { get; private set; }
        public RectTransform HudLayer { get; private set; }
        public RectTransform ScreenLayer { get; private set; }
        public RectTransform CutsceneLayer { get; private set; }
        public RectTransform ModalLayer { get; private set; }
        public RectTransform DialogLayer { get; private set; }
        public RectTransform ToastLayer { get; private set; }
        public Fader Fader { get; private set; }
        public CutscenePlayer Cutscene { get; private set; }
        public InventoryPanel Inventory { get; private set; }
        public PausePanel Pause { get; private set; }

        EventSystem eventSystem;
        bool menuScene = true;
        bool touchBuilt;

        // HUD
        GameObject promptRoot; TextMeshProUGUI promptText; string lastPrompt;
        GameObject counterRoot; TextMeshProUGUI counterText;
        GameObject toastRoot; TextMeshProUGUI toastText; float toastTimer;

        // dialogo modale
        class DialogRequest { public string titolo, testo, okLabel, noLabel; public Action onOk; public bool confirm; }
        readonly Queue<DialogRequest> dialogQueue = new Queue<DialogRequest>();
        DialogRequest dialogCurrent;
        GameObject dlgRoot; TextMeshProUGUI dlgTitle, dlgBody; Button dlgOk, dlgNo;
        float dialogOpenedAt;

        int modalCount;
        int lastModalPopFrame = -1;

        public bool ModalOpen { get { return modalCount > 0; } }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Build();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (I == this) { SceneManager.sceneLoaded -= OnSceneLoaded; I = null; }
        }

        // ---------------------------------------------------------------- costruzione
        void Build()
        {
            gameObject.layer = 5;
            Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            HudLayer = NewLayer("Hud");
            ScreenLayer = NewLayer("Schermate");
            CutsceneLayer = NewLayer("Cutscene");
            ModalLayer = NewLayer("Modali");
            DialogLayer = NewLayer("Dialoghi");
            ToastLayer = NewLayer("Toast");

            var cgo = UIFactory.NewRect("CutscenePlayer", CutsceneLayer);
            Cutscene = cgo.gameObject.AddComponent<CutscenePlayer>();

            BuildHud();
            BuildToast();
            BuildDialog();

            var igo = UIFactory.NewRect("InventoryPanel", ModalLayer);
            Inventory = igo.gameObject.AddComponent<InventoryPanel>();
            var pgo = UIFactory.NewRect("PausePanel", ModalLayer);
            Pause = pgo.gameObject.AddComponent<PausePanel>();

            // il fader sta sopra a tutto
            var fgo = UIFactory.NewRect("Fader", transform);
            Fader = fgo.gameObject.AddComponent<Fader>();
            fgo.SetAsLastSibling();
        }

        RectTransform NewLayer(string name)
        {
            var rt = UIFactory.NewRect(name, transform);
            UIFactory.Stretch(rt);
            return rt;
        }

        void EnsureEventSystem()
        {
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                go.transform.SetParent(transform, false);
                eventSystem = go.AddComponent<EventSystem>();
                var module = go.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }
            // eventuali EventSystem creati dalle scene vanno rimossi (uno solo, con il nuovo Input System)
            var all = FindObjectsByType<EventSystem>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == eventSystem) continue;
                var modules = all[i].GetComponents<BaseInputModule>();
                for (int m = 0; m < modules.Length; m++) Destroy(modules[m]);
                Destroy(all[i]);
            }
        }

        void BuildHud()
        {
            // prompt in basso al centro
            var pimg = UIFactory.MakePanel(HudLayer, "Prompt", new Color(0.1f, 0.07f, 0.04f, 0.8f), false);
            UIFactory.Place(pimg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(640f, 60f));
            promptText = UIFactory.MakeText(pimg.rectTransform, "", 28, UIFactory.Carta);
            UIFactory.Stretch(promptText.rectTransform, 10f, 0f, 10f, 0f);
            promptText.enableAutoSizing = true; promptText.fontSizeMin = 16; promptText.fontSizeMax = 28;
            promptRoot = pimg.gameObject;
            promptRoot.SetActive(false);

            // contatore in alto al centro
            var cimg = UIFactory.MakePanel(HudLayer, "Contatore", new Color(0.1f, 0.07f, 0.04f, 0.8f), false);
            UIFactory.Place(cimg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(480f, 52f));
            counterText = UIFactory.MakeText(cimg.rectTransform, "", 26, UIFactory.Carta);
            UIFactory.Stretch(counterText.rectTransform, 10f, 0f, 10f, 0f);
            counterText.enableAutoSizing = true; counterText.fontSizeMin = 14; counterText.fontSizeMax = 26;
            counterRoot = cimg.gameObject;
            counterRoot.SetActive(false);

            // pulsanti Inventario / Pausa in alto a destra
            var pausa = UIFactory.MakeButton(HudLayer, "Pausa", TogglePause, new Vector2(140f, 56f), UIFactory.Marrone, UIFactory.Carta, 24);
            UIFactory.Place((RectTransform)pausa.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(140f, 56f));
            var inv = UIFactory.MakeButton(HudLayer, "Inventario", ToggleInventory, new Vector2(180f, 56f), UIFactory.Marrone, UIFactory.Carta, 24);
            UIFactory.Place((RectTransform)inv.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-166f, -16f), new Vector2(180f, 56f));
        }

        void BuildTouchControls()
        {
            touchBuilt = true;

            var back = UIFactory.MakePanel(HudLayer, "Joystick", new Color(0.1f, 0.07f, 0.04f, 0.35f), true);
            back.sprite = UIFactory.Circle();
            UIFactory.Place(back.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(50f, 50f), new Vector2(220f, 220f));
            var knob = UIFactory.MakePanel(back.rectTransform, "Pomello", new Color(0.91f, 0.85f, 0.71f, 0.8f), false);
            knob.sprite = UIFactory.Circle();
            UIFactory.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
            var joy = back.gameObject.AddComponent<VirtualJoystick>();
            joy.background = back.rectTransform;
            joy.knob = knob.rectTransform;
            joy.radius = 65f;

            var act = UIFactory.MakeButton(HudLayer, "Interagisci", OnInteractButton, new Vector2(240f, 90f), UIFactory.Accento, UIFactory.Carta, 30);
            UIFactory.Place((RectTransform)act.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 70f), new Vector2(240f, 90f));
        }

        static void OnInteractButton()
        {
            if (PlayerInteractor.Active != null) PlayerInteractor.Active.TriggerInteract();
        }

        void BuildToast()
        {
            var img = UIFactory.MakePanel(ToastLayer, "Toast", new Color(0.1f, 0.07f, 0.04f, 0.9f), false);
            UIFactory.Place(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(860f, 84f));
            toastText = UIFactory.MakeText(img.rectTransform, "", 24, UIFactory.Carta);
            UIFactory.Stretch(toastText.rectTransform, 16f, 4f, 16f, 4f);
            toastText.enableAutoSizing = true; toastText.fontSizeMin = 14; toastText.fontSizeMax = 24;
            toastRoot = img.gameObject;
            toastRoot.SetActive(false);
        }

        void BuildDialog()
        {
            var veil = UIFactory.MakePanel(DialogLayer, "Dialogo", UIFactory.Velo, true);
            UIFactory.Stretch(veil.rectTransform);
            dlgRoot = veil.gameObject;

            var card = UIFactory.MakePanel(veil.rectTransform, "Carta", UIFactory.Carta, true);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 480f));

            dlgTitle = UIFactory.MakeText(card.rectTransform, "", 36, UIFactory.Accento, TextAlignmentOptions.Center, FontStyles.Bold);
            dlgTitle.rectTransform.anchorMin = new Vector2(0f, 1f); dlgTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            dlgTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            dlgTitle.rectTransform.offsetMin = new Vector2(30f, -80f); dlgTitle.rectTransform.offsetMax = new Vector2(-30f, -20f);

            dlgBody = UIFactory.MakeText(card.rectTransform, "", 26, UIFactory.Inchiostro, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(dlgBody.rectTransform, 40f, 110f, 40f, 90f);
            dlgBody.enableAutoSizing = true; dlgBody.fontSizeMin = 16; dlgBody.fontSizeMax = 28;

            dlgOk = UIFactory.MakeButton(card.rectTransform, "Continua", () => CloseDialog(true), new Vector2(280f, 64f));
            UIFactory.Place((RectTransform)dlgOk.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(280f, 64f));
            dlgNo = UIFactory.MakeButton(card.rectTransform, "Annulla", () => CloseDialog(false), new Vector2(280f, 64f), UIFactory.Marrone);
            UIFactory.Place((RectTransform)dlgNo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 24f), new Vector2(280f, 64f));

            dlgRoot.SetActive(false);
        }

        // ---------------------------------------------------------------- API pubblica
        public void ShowPrompt(string text)
        {
            if (promptRoot == null || string.IsNullOrEmpty(text)) { HidePrompt(); return; }
            if (text != lastPrompt) { lastPrompt = text; promptText.text = text; }
            if (!promptRoot.activeSelf) promptRoot.SetActive(true);
        }

        public void HidePrompt()
        {
            lastPrompt = null;
            if (promptRoot != null && promptRoot.activeSelf) promptRoot.SetActive(false);
        }

        public void Toast(string text, float secs = 3f)
        {
            if (toastRoot == null || string.IsNullOrEmpty(text)) return;
            toastText.text = text;
            toastTimer = Mathf.Max(0.5f, secs);
            toastRoot.SetActive(true);
        }

        public void SetHudCounter(string text)
        {
            if (counterRoot == null) return;
            bool has = !string.IsNullOrEmpty(text);
            if (has) counterText.text = text;
            if (counterRoot.activeSelf != has) counterRoot.SetActive(has);
        }

        public void ShowMessage(string titolo, string testo, Action onClose)
        {
            Enqueue(new DialogRequest { titolo = titolo, testo = testo, okLabel = "Continua", onOk = onClose, confirm = false });
        }

        public void ShowConfirm(string titolo, string testo, string okLabel, string noLabel, Action onYes)
        {
            Enqueue(new DialogRequest { titolo = titolo, testo = testo, okLabel = okLabel, noLabel = noLabel, onOk = onYes, confirm = true });
        }

        public void PushModal() { modalCount++; }
        public void PopModal()
        {
            if (modalCount > 0) modalCount--;
            lastModalPopFrame = Time.frameCount;
        }

        // ---------------------------------------------------------------- dialogo
        void Enqueue(DialogRequest r)
        {
            dialogQueue.Enqueue(r);
            if (dialogCurrent == null) ShowNextDialog();
        }

        void ShowNextDialog()
        {
            if (dialogQueue.Count == 0) return;
            dialogCurrent = dialogQueue.Dequeue();
            var r = dialogCurrent;
            dlgTitle.text = r.titolo ?? "";
            dlgBody.text = r.testo ?? "";
            UIFactory.SetLabel(dlgOk, r.okLabel ?? "Continua");
            var okRt = (RectTransform)dlgOk.transform;
            if (r.confirm)
            {
                UIFactory.SetLabel(dlgNo, r.noLabel ?? "Annulla");
                dlgNo.gameObject.SetActive(true);
                okRt.anchoredPosition = new Vector2(160f, 24f);
            }
            else
            {
                dlgNo.gameObject.SetActive(false);
                okRt.anchoredPosition = new Vector2(0f, 24f);
            }
            dlgRoot.SetActive(true);
            dlgRoot.transform.SetAsLastSibling();
            dialogOpenedAt = Time.unscaledTime;
            if (GameManager.I != null) GameManager.I.PushBlock();
            PushModal();
        }

        void CloseDialog(bool ok)
        {
            if (dialogCurrent == null) return;
            var r = dialogCurrent;
            dialogCurrent = null;
            dlgRoot.SetActive(false);
            if (GameManager.I != null) GameManager.I.PopBlock();
            PopModal();
            if (ok && r.onOk != null) r.onOk();
            if (dialogCurrent == null && dialogQueue.Count > 0) ShowNextDialog();
        }

        // ---------------------------------------------------------------- menu / hotkey
        bool CanOpenMenus
        {
            get
            {
                return !menuScene && modalCount == 0 && !Cutscene.IsPlaying && !Fader.Busy
                    && !(SceneLoader.I != null && SceneLoader.I.IsLoading);
            }
        }

        public void ToggleInventory()
        {
            if (Inventory.IsOpen) { Inventory.Hide(); return; }
            if (Pause.IsOpen || CanOpenMenus) Inventory.Show();
        }

        public void TogglePause()
        {
            if (Inventory.IsOpen) { Inventory.Hide(); return; }
            if (Pause.IsOpen) { Pause.Hide(); return; }
            if (CanOpenMenus) Pause.Show();
        }

        void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            menuScene = GameManager.IsMenuScene(s.name);
            EnsureEventSystem();
        }

        void Update()
        {
            if (!touchBuilt && Touchscreen.current != null) BuildTouchControls();

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toastRoot.SetActive(false);
            }

            bool showHud = !menuScene && !Cutscene.IsPlaying;
            if (HudLayer.gameObject.activeSelf != showHud) HudLayer.gameObject.SetActive(showHud);

            var kb = Keyboard.current;
            if (kb == null) return;

            // dialogo semplice: E/Spazio/Invio chiude (con piccola pausa per non riusare lo stesso tasto)
            if (dialogCurrent != null)
            {
                if (!dialogCurrent.confirm && Time.unscaledTime - dialogOpenedAt > 0.3f && InputHelper.ConfirmPressed(false))
                    CloseDialog(true);
                return;
            }

            if (menuScene || Cutscene.IsPlaying || Fader.Busy || lastModalPopFrame == Time.frameCount) return;

            if (kb.iKey.wasPressedThisFrame) ToggleInventory();
            else if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) TogglePause();
        }
    }
}
