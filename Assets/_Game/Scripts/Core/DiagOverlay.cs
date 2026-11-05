using System.Text;
using TrenoBambini.Player;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TrenoBambini.Core
{
    /// <summary>
    /// Pannello di diagnosi (solo testo piccolo, nascosto di default): si apre/chiude con F3 (o F2).
    /// Serve a capire perché Amerigo non si muove: mostra timeScale, blocchi input e chi li ha chiesti, input letti,
    /// velocità, stato dei pannelli, FPS e scena. Pensato per uno screenshot da mandare allo sviluppatore.
    /// </summary>
    public class DiagOverlay : MonoBehaviour
    {
        bool visible;
        float fpsAcc; int fpsFrames; float fps;
        float nextFind;
        ThirdPersonController tpc;
        GUIStyle style;
        string text = "";
        float nextText;
        int keyEvents; string lastKey = "-";

        void Update()
        {
            fpsAcc += Time.unscaledDeltaTime; fpsFrames++;
            if (fpsAcc >= 0.5f) { fps = fpsFrames / fpsAcc; fpsAcc = 0f; fpsFrames = 0; }

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f3Key.wasPressedThisFrame || kb.f2Key.wasPressedThisFrame) visible = !visible;
                if (kb.anyKey.wasPressedThisFrame)
                {
                    keyEvents++;
                    foreach (var k in kb.allKeys)
                        if (k.wasPressedThisFrame) { lastKey = k.displayName; break; }
                }
            }
            if (!visible) return;

            if (tpc == null && Time.unscaledTime >= nextFind)
            {
                nextFind = Time.unscaledTime + 0.5f;
                tpc = FindAnyObjectByType<ThirdPersonController>();
            }
            if (Time.unscaledTime >= nextText) { nextText = Time.unscaledTime + 0.2f; text = Build(kb); }
        }

        string Build(Keyboard kb)
        {
            var gm = GameManager.I;
            var ui = UIRoot.I;
            var sb = new StringBuilder(512);
            sb.Append("DIAGNOSI (F3)  scena ").Append(SceneManager.GetActiveScene().name)
              .Append("  FPS ").Append(fps.ToString("0.0")).Append("  timeScale ").Append(Time.timeScale.ToString("0.00")).Append('\n');
            sb.Append("Blocchi ").Append(gm != null ? gm.InputBlockers : -1).Append(" [").Append(gm != null ? gm.BlockReasons : "").Append("]\n");
            if (ui != null)
            {
                sb.Append("Modali ").Append(ui.ModalOpen ? "SI" : "no")
                  .Append("  Pausa ").Append(ui.Pause != null && ui.Pause.IsOpen ? "SI" : "no")
                  .Append("  Inventario ").Append(ui.Inventory != null && ui.Inventory.IsOpen ? "SI" : "no")
                  .Append("  Cutscene ").Append(ui.Cutscene != null && ui.Cutscene.IsPlaying ? "SI" : "no")
                  .Append("  Fader ").Append(ui.Fader != null && ui.Fader.Busy ? "SI" : "no")
                  .Append("  Caricamento ").Append(SceneLoader.I != null && SceneLoader.I.IsLoading ? "SI" : "no")
                  .Append("  Touch ").Append(UIRoot.TouchMode ? "SI" : "no").Append('\n');
            }
            sb.Append("Tastiera ").Append(kb != null ? "ok" : "ASSENTE");
            if (kb != null)
            {
                sb.Append("  W").Append(kb.wKey.isPressed ? 1 : 0).Append(" A").Append(kb.aKey.isPressed ? 1 : 0)
                  .Append(" S").Append(kb.sKey.isPressed ? 1 : 0).Append(" D").Append(kb.dKey.isPressed ? 1 : 0)
                  .Append("  su").Append(kb.upArrowKey.isPressed ? 1 : 0).Append(" giu").Append(kb.downArrowKey.isPressed ? 1 : 0)
                  .Append(" sx").Append(kb.leftArrowKey.isPressed ? 1 : 0).Append(" dx").Append(kb.rightArrowKey.isPressed ? 1 : 0);
            }
            sb.Append("  tasti premuti ").Append(keyEvents).Append(" ultimo ").Append(lastKey).Append('\n');
            Vector2 j = VirtualJoystick.Value;
            var gp = Gamepad.current;
            sb.Append("Joystick ").Append(j.x.ToString("0.00")).Append(',').Append(j.y.ToString("0.00"));
            if (gp != null)
            {
                Vector2 g = gp.leftStick.ReadValue();
                sb.Append("  Gamepad ").Append(gp.displayName).Append(' ').Append(g.x.ToString("0.00")).Append(',').Append(g.y.ToString("0.00"));
            }
            else sb.Append("  Gamepad assente");
            sb.Append("  Focus ").Append(Application.isFocused ? "si" : "NO").Append('\n');
            if (tpc != null)
            {
                Vector3 v = tpc.DebugVelocity;
                sb.Append("Player input ").Append(tpc.DebugInput.x.ToString("0.00")).Append(',').Append(tpc.DebugInput.y.ToString("0.00"))
                  .Append("  bloccato ").Append(tpc.DebugBlocked ? "SI" : "no")
                  .Append("  cc ").Append(tpc.DebugControllerEnabled ? "on" : "OFF")
                  .Append("  vel ").Append(new Vector2(v.x, v.z).magnitude.ToString("0.00"))
                  .Append("  a terra ").Append(tpc.Grounded ? "si" : "no")
                  .Append("  pos ").Append(tpc.transform.position.ToString("0.0")).Append('\n');
            }
            else sb.Append("Player: non trovato\n");
            return sb.ToString();
        }

        void OnGUI()
        {
            if (!visible) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = false, richText = false };
                style.normal.textColor = Color.white;
            }
            var content = new GUIContent(text);
            Vector2 size = style.CalcSize(content);
            var r = new Rect(6f, Screen.height - size.y - 10f, size.x + 12f, size.y + 6f);
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(r.x + 6f, r.y + 3f, size.x, size.y), content, style);
        }
    }
}
