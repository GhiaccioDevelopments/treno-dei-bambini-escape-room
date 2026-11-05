using System;
using System.Collections.Generic;
using TMPro;
using TrenoBambini.Core;
using TrenoBambini.Data;
using TrenoBambini.Dialogue;
using TrenoBambini.Player;
using TrenoBambini.UI.Fx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrenoBambini.UI
{
    /// <summary>
    /// Canvas globale costruito da codice: HUD, prompt, toast, dialoghi modali, fader, cutscene, inventario, pausa.
    /// Ogni layer ha un Canvas annidato (le animazioni di un layer non fanno ricostruire gli altri); l'ordine dei layer è quello dei figli.
    /// </summary>
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
        public RectTransform FlightLayer { get; private set; }
        public Fader Fader { get; private set; }
        public CutscenePlayer Cutscene { get; private set; }
        public InventoryPanel Inventory { get; private set; }
        public PausePanel Pause { get; private set; }

        EventSystem eventSystem;
        bool menuScene = true;
        bool touchBuilt;

        // HUD
        InteractPrompt prompt;
        TicketHud ticket;
        ToastStack toasts;
        CanvasGroup hudGroup;
        RectTransform inventoryButton;

        // dialogo modale (lettera)
        class DialogRequest { public string titolo, testo, okLabel, noLabel; public Action onOk; public bool confirm; }
        readonly Queue<DialogRequest> dialogQueue = new Queue<DialogRequest>();
        DialogRequest dialogCurrent;
        GameObject dlgRoot; PanelFx dlgFx; TextMeshProUGUI dlgTitle, dlgBody; Button dlgOk, dlgNo;
        Typewriter dlgType;
        float dialogOpenedAt;
        bool dlgHiding;

        int modalCount;
        int lastModalPopFrame = -1;

        // nuovo oggetto ottenuto (animazione della valigia)
        string pendingNewItem;

        // effetti ridotti automatici se gli fps restano bassi
        float fpsAcc; int fpsFrames; int lowWindows; bool autoReduceDone;

        public bool ModalOpen { get { return modalCount > 0; } }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            Build();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void Start()
        {
            SyncReduceEffects();
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

            HudLayer = NewLayer("Hud", true);
            ScreenLayer = NewLayer("Schermate", true);
            CutsceneLayer = NewLayer("Cutscene", true);
            ModalLayer = NewLayer("Modali", true);
            DialogLayer = NewLayer("Dialoghi", true);
            ToastLayer = NewLayer("Toast", false);
            FlightLayer = NewLayer("Volo", false);

            var cgo = UIFactory.NewRect("CutscenePlayer", CutsceneLayer);
            Cutscene = cgo.gameObject.AddComponent<CutscenePlayer>();

            BuildHud();
            toasts = ToastStack.Create(ToastLayer);
            BuildDialog();

            var igo = UIFactory.NewRect("InventoryPanel", ModalLayer);
            Inventory = igo.gameObject.AddComponent<InventoryPanel>();
            var pgo = UIFactory.NewRect("PausePanel", ModalLayer);
            Pause = pgo.gameObject.AddComponent<PausePanel>();

            // particelle sopra tutto tranne il fader
            var partLayer = NewLayer("Particelle", false);
            UIParticles.Create(partLayer);

            // il fader sta sopra a tutto (layer con raycaster: blocca i click durante le transizioni)
            var fl = NewLayer("FaderLayer", true);
            var fgo = UIFactory.NewRect("Fader", fl);
            Fader = fgo.gameObject.AddComponent<Fader>();
        }

        RectTransform NewLayer(string name, bool interactive)
        {
            var rt = UIFactory.NewRect(name, transform);
            UIFactory.Stretch(rt);
            rt.gameObject.AddComponent<Canvas>(); // Canvas annidato: isola le ricostruzioni, l'ordine segue la gerarchia
            if (interactive) rt.gameObject.AddComponent<GraphicRaycaster>();
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

        static Button MakeIconButton(Transform parent, string label, Sprite icon, UnityEngine.Events.UnityAction onClick, Vector2 size)
        {
            var b = UIFactory.MakeButton(parent, label, onClick, size, ButtonStyle.Brass, 24f);
            var txt = b.GetComponentInChildren<TextMeshProUGUI>();
            UIFactory.Stretch(txt.rectTransform, 54f, 6f, 12f, 6f);
            var ic = UIFactory.MakeImage(b.transform, "Icona", icon, Color.white);
            UIFactory.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(40f, 40f));
            return b;
        }

        void BuildHud()
        {
            hudGroup = HudLayer.gameObject.AddComponent<CanvasGroup>();

            // contatore enigmi a biglietto (in alto a sinistra)
            ticket = TicketHud.Create(HudLayer);

            // pulsanti Inventario (valigia) / Pausa (orologio) in alto a destra, sempre cliccabili (utili su LIM)
            var pausa = MakeIconButton(HudLayer, "Pausa", UIArt.IconWatch(), TogglePause, new Vector2(170f, 60f));
            UIFactory.Place((RectTransform)pausa.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(170f, 60f));
            var inv = MakeIconButton(HudLayer, "Valigia", UIArt.IconSuitcase(), ToggleInventory, new Vector2(190f, 60f));
            UIFactory.Place((RectTransform)inv.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-196f, -16f), new Vector2(190f, 60f));
            inventoryButton = (RectTransform)inv.transform;

            // prompt "interagisci" + indicatore nel mondo (layer annidato proprio)
            var anim = UIFactory.NewRect("Animati", HudLayer);
            UIFactory.Stretch(anim);
            anim.gameObject.AddComponent<Canvas>();
            prompt = InteractPrompt.Create(anim);
        }

        void BuildTouchControls()
        {
            touchBuilt = true;

            var back = UIFactory.MakePanel(HudLayer, "Joystick", new Color(0.1f, 0.07f, 0.04f, 0.35f), true);
            back.sprite = UIFactory.Circle();
            UIFactory.Place(back.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(50f, 50f), new Vector2(220f, 220f));
            var knob = UIFactory.MakePanel(back.rectTransform, "Pomello", new Color(0.88f, 0.77f, 0.54f, 0.85f), false);
            knob.sprite = UIFactory.Circle();
            UIFactory.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
            var joy = back.gameObject.AddComponent<VirtualJoystick>();
            joy.background = back.rectTransform;
            joy.knob = knob.rectTransform;
            joy.radius = 65f;

            var act = UIFactory.MakeButton(HudLayer, "Interagisci", OnInteractButton, new Vector2(240f, 90f), ButtonStyle.Seal, 30f, true);
            UIFactory.Place((RectTransform)act.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 70f), new Vector2(240f, 90f));
        }

        static void OnInteractButton()
        {
            if (PlayerInteractor.Active != null) PlayerInteractor.Active.TriggerInteract();
        }

        void BuildDialog()
        {
            var veil = UIFactory.MakePanel(DialogLayer, "Dialogo", UIFactory.Velo, true);
            UIFactory.Stretch(veil.rectTransform);
            dlgRoot = veil.gameObject;
            dlgFx = dlgRoot.AddComponent<PanelFx>();
            dlgFx.entry = PanelEntry.Letter;

            // lettera: carta con bordo strappato; un clic sulla lettera completa la scrittura
            var card = UIFactory.MakePaper(veil.rectTransform, "Lettera", Color.white, true);
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 500f));
            dlgFx.card = card.rectTransform;
            var skipBtn = card.gameObject.AddComponent<Button>();
            skipBtn.transition = Selectable.Transition.None;
            skipBtn.targetGraphic = card;
            skipBtn.onClick.AddListener(() => { if (dlgType != null) dlgType.Complete(); });

            dlgTitle = UIFactory.MakeText(card.rectTransform, "", 38, UITheme.Ceralacca, TextAlignmentOptions.Center, FontStyles.Bold);
            dlgTitle.rectTransform.anchorMin = new Vector2(0f, 1f); dlgTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            dlgTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            dlgTitle.rectTransform.offsetMin = new Vector2(40f, -84f); dlgTitle.rectTransform.offsetMax = new Vector2(-40f, -24f);
            var rule = UIFactory.MakePanel(card.rectTransform, "Filetto", new Color(UITheme.Ottone.r, UITheme.Ottone.g, UITheme.Ottone.b, 0.8f), false);
            rule.sprite = UIArt.White();
            UIFactory.Place(rule.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(560f, 3f));

            dlgBody = UIFactory.MakeText(card.rectTransform, "", 26, UITheme.Inchiostro, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(dlgBody.rectTransform, 56f, 112f, 56f, 104f);
            dlgBody.enableAutoSizing = true; dlgBody.fontSizeMin = 16; dlgBody.fontSizeMax = 28;
            dlgType = dlgBody.gameObject.AddComponent<Typewriter>();

            dlgOk = UIFactory.MakeButton(card.rectTransform, "Continua", () => CloseDialog(true), new Vector2(280f, 64f), ButtonStyle.Seal, 28f, true);
            UIFactory.Place((RectTransform)dlgOk.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(280f, 64f));
            dlgNo = UIFactory.MakeButton(card.rectTransform, "Annulla", () => CloseDialog(false), new Vector2(280f, 64f), ButtonStyle.Tag, 26f);
            UIFactory.Place((RectTransform)dlgNo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 30f), new Vector2(280f, 64f));

            dlgRoot.SetActive(false);
        }

        // ---------------------------------------------------------------- API pubblica
        public void ShowPrompt(string text) { ShowPrompt(text, false); }

        public void ShowPrompt(string text, bool special)
        {
            if (prompt == null || string.IsNullOrEmpty(text)) { HidePrompt(); return; }
            prompt.Show(text, special);
        }

        public void HidePrompt()
        {
            if (prompt == null) return;
            prompt.Hide();
            prompt.SetMarker(false, Vector3.zero);
        }

        /// <summary>Rombo dorato nel mondo 3D sopra l'oggetto interagibile (chiamato ogni frame mentre c'è un bersaglio).</summary>
        public void ShowMarker(Vector3 worldPoint)
        {
            if (prompt != null) prompt.SetMarker(true, worldPoint);
        }

        /// <summary>E premuto: il tasto del cartellino si abbassa.</summary>
        public void PromptPressed()
        {
            if (prompt != null) prompt.Press();
        }

        public void Toast(string text, float secs = 3f)
        {
            if (toasts == null || string.IsNullOrEmpty(text)) return;
            toasts.Push(text, secs);
        }

        /// <summary>Vecchia API testuale: una riga di testo nel biglietto (vuoto = nasconde).</summary>
        public void SetHudCounter(string text)
        {
            if (ticket == null) return;
            if (string.IsNullOrEmpty(text)) { ticket.Hide(); return; }
            ticket.SetText(text);
        }

        /// <summary>Biglietto del treno: una casella per enigma (total &lt;= 0 lo nasconde), riga extra sotto.</summary>
        public void SetHudProgress(int solved, int total, string extra)
        {
            if (ticket != null) ticket.Set(solved, total, extra);
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

        /// <summary>Segnala un oggetto appena ottenuto: l'animazione della valigia parte quando il gioco è di nuovo libero.</summary>
        public void QueueNewItem(string oggettoId)
        {
            pendingNewItem = oggettoId;
        }

        /// <summary>Effetti ridotti: salvati nel SaveData e applicati subito.</summary>
        public void SetReduceEffects(bool value, bool save = true)
        {
            UITheme.SetReduceEffects(value);
            if (save && GameManager.I != null)
            {
                GameManager.I.Save.effettiRidotti = value;
                GameManager.I.SaveGame();
            }
        }

        void SyncReduceEffects()
        {
            if (GameManager.I != null) UITheme.SetReduceEffects(GameManager.I.Save.effettiRidotti);
        }

        // ---------------------------------------------------------------- dialogo
        void Enqueue(DialogRequest r)
        {
            dialogQueue.Enqueue(r);
            if (dialogCurrent == null && !dlgHiding) ShowNextDialog();
        }

        void ShowNextDialog()
        {
            if (dialogQueue.Count == 0) return;
            dialogCurrent = dialogQueue.Dequeue();
            var r = dialogCurrent;
            dlgTitle.text = r.titolo ?? "";
            UIFactory.SetLabel(dlgOk, r.okLabel ?? "Continua");
            var okRt = (RectTransform)dlgOk.transform;
            if (r.confirm)
            {
                UIFactory.SetLabel(dlgNo, r.noLabel ?? "Annulla");
                dlgNo.gameObject.SetActive(true);
                okRt.anchoredPosition = new Vector2(160f, 30f);
            }
            else
            {
                dlgNo.gameObject.SetActive(false);
                okRt.anchoredPosition = new Vector2(0f, 30f);
            }
            dlgRoot.transform.SetAsLastSibling();
            dlgFx.Show();
            Typewriter.Play(dlgBody, r.testo ?? "", 70f, true);
            dialogOpenedAt = Time.unscaledTime;
            if (GameManager.I != null) GameManager.I.PushBlock();
            PushModal();
        }

        void CloseDialog(bool ok)
        {
            if (dialogCurrent == null) return;
            var r = dialogCurrent;
            dialogCurrent = null;
            dlgHiding = true;
            dlgFx.Hide(() =>
            {
                dlgHiding = false;
                if (dialogCurrent == null && dialogQueue.Count > 0) ShowNextDialog();
            });
            if (GameManager.I != null) GameManager.I.PopBlock();
            PopModal();
            if (ok && r.onOk != null) r.onOk();
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
            SyncReduceEffects();
            if (Fader != null) Fader.SetFlashbackLook(s.name.Contains("_FB_"));
        }

        void Update()
        {
            if (!touchBuilt && Touchscreen.current != null) BuildTouchControls();

            bool showHud = !menuScene && !Cutscene.IsPlaying;
            if (HudLayer.gameObject.activeSelf != showHud)
            {
                HudLayer.gameObject.SetActive(showHud);
                if (showHud)
                {
                    hudGroup.alpha = 0f;
                    UITween.Cancel(hudGroup);
                    UITween.Alpha(hudGroup, 1f, 0.5f, Ease.OutCubic, null, 0.15f);
                }
            }

            MonitorFps();

            if (pendingNewItem != null && CanOpenMenus && !dlgHiding && dialogCurrent == null && dialogQueue.Count == 0)
            {
                string id = pendingNewItem;
                pendingNewItem = null;
                PlayNewItem(id);
            }

            var kb = Keyboard.current;
            if (kb == null) return;

            // dialogo semplice: E/Spazio/Invio completa la scrittura, poi chiude (con piccola pausa per non riusare lo stesso tasto)
            if (dialogCurrent != null)
            {
                if (Time.unscaledTime - dialogOpenedAt > 0.3f && InputHelper.ConfirmPressed(false))
                {
                    if (dlgType != null && dlgType.IsTyping) dlgType.Complete();
                    else if (!dialogCurrent.confirm) CloseDialog(true);
                }
                return;
            }

            if (menuScene || Cutscene.IsPlaying || Fader.Busy || lastModalPopFrame == Time.frameCount) return;

            if (kb.iKey.wasPressedThisFrame) ToggleInventory();
            else if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame) TogglePause();
        }

        // ---------------------------------------------------------------- fps bassi => effetti ridotti
        void MonitorFps()
        {
            if (autoReduceDone || UITheme.ReduceEffects || menuScene || TrenoBambini.Audio.AudioLibrary.IsBuilding || (SceneLoader.I != null && SceneLoader.I.IsLoading) || Fader.Busy)
            {
                fpsAcc = 0f; fpsFrames = 0;
                return;
            }
            fpsAcc += Time.unscaledDeltaTime;
            fpsFrames++;
            if (fpsAcc < 2f) return;
            float fps = fpsFrames / fpsAcc;
            fpsAcc = 0f; fpsFrames = 0;
            lowWindows = fps < 25f ? lowWindows + 1 : 0;
            if (lowWindows >= 3)
            {
                autoReduceDone = true;
                SetReduceEffects(true, false); // automatico: non salvato, alla prossima partenza si riprova
                Toast("Il computer fatica: ho attivato gli effetti ridotti. Puoi cambiarli dalla Pausa.", 5f);
            }
        }

        // ---------------------------------------------------------------- nuovo oggetto: vola nella valigia
        void PlayNewItem(string id)
        {
            var o = GameData.Oggetto(id);
            Sprite spr = o != null ? UIFactory.LoadSprite("oggetti", o.icona) : null;
            if (spr == null) spr = UIArt.IconSuitcase();

            var card = UIFactory.MakePanel(FlightLayer, "NuovoOggetto", Color.white, false);
            card.sprite = UIArt.Postcard();
            card.type = Image.Type.Sliced;
            var cr = card.rectTransform;
            cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0.5f);
            cr.sizeDelta = new Vector2(190f, 190f);
            var pic = UIFactory.MakeImage(cr, "Icona", spr, Color.white);
            UIFactory.Stretch(pic.rectTransform, 18f, 18f, 18f, 18f);
            var label = UIFactory.MakeText(FlightLayer, o != null ? o.nome : id, 34, UITheme.OttoneLuce, TextAlignmentOptions.Center, FontStyles.Bold);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(700f, 50f);
            label.rectTransform.anchoredPosition = new Vector2(0f, -140f);
            var lg = label.gameObject.AddComponent<CanvasGroup>();

            cr.localScale = Vector3.zero;
            cr.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-4f, 4f));
            UISfx.Play(Sfx.Chime, 0.8f);
            AudioManager.Fire("oggetto_ritrovato");
            UITween.Scale(cr, Vector3.one, 0.5f, Ease.OutBack);
            lg.alpha = 0f;
            UITween.Alpha(lg, 1f, 0.3f);
            UIParticles.BurstAtScreen(ParticleKind.Spark, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), 14);

            // dopo una pausa vola verso la valigia dell'HUD lasciando una scia di scintille
            UITween.After(cr, 1.3f, () =>
            {
                if (cr == null) return;
                UITween.Alpha(lg, 0f, 0.25f);
                Vector2 from = cr.anchoredPosition;
                Vector2 to = TargetInInventoryButton();
                Vector2 mid = new Vector2((from.x + to.x) * 0.5f - 120f, Mathf.Max(from.y, to.y) + 100f);
                float nextSpark = 0f;
                UITween.Run(cr, 0.85f, Ease.InOutCubic, v =>
                {
                    if (cr == null) return;
                    Vector2 a = Vector2.Lerp(from, mid, v), b = Vector2.Lerp(mid, to, v);
                    Vector2 p = Vector2.Lerp(a, b, v);
                    cr.anchoredPosition = p;
                    float s = Mathf.Lerp(1f, 0.28f, v);
                    cr.localScale = new Vector3(s, s, 1f);
                    nextSpark -= Time.unscaledDeltaTime;
                    if (nextSpark <= 0f && UIParticles.I != null) { nextSpark = 0.03f; UIParticles.I.Burst(ParticleKind.Spark, p, 1); }
                }, () =>
                {
                    if (cr != null) Destroy(cr.gameObject);
                    if (label != null) Destroy(label.gameObject);
                    ArrivedInSuitcase();
                });
            });
        }

        Vector2 TargetInInventoryButton()
        {
            if (inventoryButton == null || UIParticles.I == null) return new Vector2(500f, 300f);
            Vector3 w = inventoryButton.TransformPoint(inventoryButton.rect.center);
            return UIParticles.I.ScreenToLocal(RectTransformUtility.WorldToScreenPoint(null, w));
        }

        void ArrivedInSuitcase()
        {
            if (inventoryButton == null) return;
            UISfx.Play(Sfx.SuitcaseClose, 0.6f);
            UISfx.Play(Sfx.Sparkle, 0.7f);
            UIFx.Pop(inventoryButton, 1.2f, 0.5f);
            UIParticles.BurstAtRect(ParticleKind.Spark, inventoryButton, 10);
            var plus = UIFactory.MakeText(inventoryButton, "+1", 34, UITheme.Oro, TextAlignmentOptions.Center, FontStyles.Bold);
            plus.rectTransform.anchorMin = plus.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            plus.rectTransform.sizeDelta = new Vector2(80f, 40f);
            plus.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            var pg = plus.gameObject.AddComponent<CanvasGroup>();
            UITween.Move(plus.rectTransform, new Vector2(0f, -54f), 1.1f, Ease.OutCubic);
            UITween.Alpha(pg, 0f, 0.5f, Ease.InCubic, () => { if (plus != null) Destroy(plus.gameObject); }, 0.7f);
        }
    }
}
