using TMPro;
using TrenoBambini.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrenoBambini.Puzzles
{
    /// <summary>Lucchetto a combinazione: ruote cifre con +/-, selezione e tastiera numerica.</summary>
    public class LucchettoPanel : PuzzlePanel
    {
        string code;
        int n;
        int[] digits;
        Image[] wheelBg;
        TextMeshProUGUI[] wheelText;
        int sel;

        protected override void BuildContent(RectTransform area)
        {
            code = (def.codice ?? "").Trim();
            if (code.Length == 0)
            {
                Debug.LogWarning("Lucchetto senza codice: " + def.id);
                Solved();
                return;
            }
            n = Mathf.Min(code.Length, 8);
            digits = new int[n];
            wheelBg = new Image[n];
            wheelText = new TextMeshProUGUI[n];

            var row = UIFactory.NewRect("Ruote", area);
            UIFactory.Stretch(row);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 22f;
            h.childControlWidth = false; h.childControlHeight = false;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;

            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var col = UIFactory.NewRect("Ruota" + i, row);
                col.sizeDelta = new Vector2(110f, 290f);
                UIFactory.AddVLayout(col.gameObject, 8f);

                var up = UIFactory.MakeButton(col, "+", () => Change(idx, 1), new Vector2(110f, 66f), UIFactory.Marrone, null, 40);
                var wheel = UIFactory.MakeButton(col, "0", () => Select(idx), new Vector2(110f, 120f), UIFactory.CartaScura, UIFactory.Inchiostro, 72);
                var down = UIFactory.MakeButton(col, "-", () => Change(idx, -1), new Vector2(110f, 66f), UIFactory.Marrone, null, 40);
                wheelBg[i] = (Image)wheel.targetGraphic;
                wheelText[i] = wheel.GetComponentInChildren<TextMeshProUGUI>();
                wheelText[i].enableAutoSizing = false;
                wheelText[i].fontSize = 72;
            }
            Select(0);
            ShowAction("Apri", TryOpen);
            Info("Gira le ruote con + e - (oppure scrivi le cifre con la tastiera), poi premi Apri.");
        }

        void Select(int i)
        {
            sel = Mathf.Clamp(i, 0, n - 1);
            for (int k = 0; k < n; k++)
                wheelBg[k].color = k == sel ? new Color(0.98f, 0.9f, 0.62f) : UIFactory.CartaScura;
        }

        void Change(int i, int delta)
        {
            if (solved) return;
            digits[i] = (digits[i] + delta + 10) % 10;
            wheelText[i].text = digits[i].ToString();
            Select(i);
        }

        void SetDigit(int i, int v)
        {
            digits[i] = v;
            wheelText[i].text = v.ToString();
        }

        void TryOpen()
        {
            if (solved) return;
            var sb = new System.Text.StringBuilder(n);
            for (int i = 0; i < n; i++) sb.Append(digits[i]);
            if (sb.ToString() == code.Substring(0, n))
                Solved("Clic! Il lucchetto si apre.");
            else
                Wrong(def.erroreMsg);
        }

        protected override void OnUpdate()
        {
            if (solved || digits == null) return;
            var kb = Keyboard.current;
            if (kb == null) return;

            for (int k = 0; k < 10; k++)
            {
                var d = kb[k == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + k - 1)]; // enum: Digit1..Digit9, Digit0
                var p = kb[(Key)((int)Key.Numpad0 + k)];
                if (d.wasPressedThisFrame || p.wasPressedThisFrame)
                {
                    SetDigit(sel, k);
                    Select(sel + 1);
                }
            }
            if (kb.backspaceKey.wasPressedThisFrame) Select(sel - 1);
            if (kb.leftArrowKey.wasPressedThisFrame) Select(sel - 1);
            if (kb.rightArrowKey.wasPressedThisFrame) Select(sel + 1);
            if (kb.upArrowKey.wasPressedThisFrame) Change(sel, 1);
            if (kb.downArrowKey.wasPressedThisFrame) Change(sel, -1);
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) TryOpen();
        }
    }
}
