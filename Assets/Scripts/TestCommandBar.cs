using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CodeClicker
{
    public sealed class TestCommandBar : MonoBehaviour
    {
        private const float SecondsToOpen = 3f;

        private MoneySystem moneySystem;
        private GameObject bar;
        private InputField input;
        private Text placeholder;
        private float heldFor;
        private static int escapeHandledFrame = -1;

        public static bool IsOpen { get; private set; }
        public static bool EscapeHandledThisFrame => escapeHandledFrame == Time.frameCount;

        public static bool IsTypingOrOpening => IsOpen || ShortcutHeld;

        private static bool ShortcutHeld
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                return keyboard != null &&
                       (keyboard.digit0Key.isPressed || keyboard.numpad0Key.isPressed) &&
                       (keyboard.digit1Key.isPressed || keyboard.numpad1Key.isPressed);
            }
        }

        private void Awake()
        {
            moneySystem = GetComponent<MoneySystem>();
            CreateBar();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !Application.isFocused || SaveSystem.IsModalOpen)
            {
                heldFor = 0f;
                return;
            }

            if (IsOpen)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    escapeHandledFrame = Time.frameCount;
                    CloseBar();
                }
                else if (keyboard.enterKey.wasPressedThisFrame ||
                         keyboard.numpadEnterKey.wasPressedThisFrame)
                {
                    RunCommand();
                }

                return;
            }

            heldFor = ShortcutHeld ? heldFor + Time.unscaledDeltaTime : 0f;
            if (heldFor >= SecondsToOpen)
            {
                OpenBar();
            }
        }

        private void OnDestroy()
        {
            IsOpen = false;
        }

        private void OpenBar()
        {
            heldFor = 0f;
            IsOpen = true;
            placeholder.text = "Type a command...";
            input.text = string.Empty;
            bar.SetActive(true);
            input.Select();
            input.ActivateInputField();
        }

        private void CloseBar()
        {
            IsOpen = false;
            input.DeactivateInputField();
            bar.SetActive(false);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void RunCommand()
        {
            string command = input.text.Trim();

            if (command.Equals("giveMoney", StringComparison.OrdinalIgnoreCase))
            {
                moneySystem.AddCoins(100_000);
            }
            else if (command.Equals("Rich", StringComparison.OrdinalIgnoreCase))
            {
                moneySystem.AddCoins(1_000_000);
            }
            else if (command.Equals("Bill", StringComparison.OrdinalIgnoreCase))
            {
                moneySystem.AddCoins(1_000_000_000);
            }
            else if (command.Equals("demote", StringComparison.OrdinalIgnoreCase))
            {
                moneySystem.ResetCoins();
            }
            else if (command.Equals("save", StringComparison.OrdinalIgnoreCase))
            {
                SaveSystem.RequestSave();
            }
            else if (command.Equals("load", StringComparison.OrdinalIgnoreCase))
            {
                SaveSystem.RequestLoad();
            }
            else
            {
                input.text = string.Empty;
                placeholder.text = "Unknown command";
                input.ActivateInputField();
                return;
            }

            CloseBar();
        }

        private void CreateBar()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("TestCommandBar needs a Canvas.", this);
                enabled = false;
                return;
            }

            bar = new GameObject("TestCommandBar", typeof(RectTransform), typeof(Image), typeof(InputField));
            bar.transform.SetParent(canvas.transform, false);
            bar.transform.SetAsLastSibling();

            RectTransform barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 1f);
            barRect.anchorMax = new Vector2(0.5f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = new Vector2(0f, -24f);
            barRect.sizeDelta = new Vector2(350f, 48f);

            Image background = bar.GetComponent<Image>();
            background.color = new Color(0.035f, 0.10f, 0.16f, 0.98f);

            Text commandText = CreateText("CommandText", Color.white);
            placeholder = CreateText("Placeholder", new Color(0.55f, 0.72f, 0.78f));
            placeholder.text = "Type a command...";

            input = bar.GetComponent<InputField>();
            input.targetGraphic = background;
            input.textComponent = commandText;
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 40;

            bar.SetActive(false);
        }

        private Text CreateText(string objectName, Color color)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(bar.transform, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(15f, 5f);
            rect.offsetMax = new Vector2(-15f, -5f);

            Text text = textObject.GetComponent<Text>();
            CodeClickerFont.Apply(text, 20);
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            return text;
        }
    }
}
