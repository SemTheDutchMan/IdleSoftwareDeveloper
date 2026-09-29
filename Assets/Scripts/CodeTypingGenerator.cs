using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CodeClicker
{
    public sealed class CodeTypingGenerator : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Text codeDisplay;
        [SerializeField, Min(4)] private int maximumVisibleLines = 16;
        [SerializeField] private bool showCursor = true;

        [Header("Typing")]
        [SerializeField, Min(1)] private int keysPerLine = 5;

        private static readonly string[] Keywords =
        {
            "using", "public", "private", "protected", "sealed", "class", "void",
            "float", "int", "string", "bool", "return", "if", "else", "new",
            "true", "false", "null", "this", "static"
        };

        private readonly StringBuilder visibleCode = new StringBuilder();
        private System.Random random;
        private string[] currentLines;
        private int lineIndex;
        private int pressedKeysForCurrentLine;
        private bool hasStartedTyping;

        public Text CodeDisplay
        {
            get { return codeDisplay; }
            set { codeDisplay = value; }
        }

        public int KeysPerLine { get { return keysPerLine; } }

        public int TotalKeyPresses { get; private set; }
        public string VisibleCode { get { return visibleCode.ToString(); } }
        public int PressedKeysForCurrentLine { get { return pressedKeysForCurrentLine; } }
        public bool HasStartedTyping { get { return hasStartedTyping; } }
        public event Action KeyPressed;
        public event Action LineCompleted;

        private void Awake()
        {
            random = new System.Random(Environment.TickCount);
            CreateNewCodeFile();

            if (codeDisplay == null)
            {
                codeDisplay = GetComponentInChildren<Text>(true);
            }

            ConfigureDisplay();
            RefreshDisplay();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null ||
                !Application.isFocused ||
                TestCommandBar.IsTypingOrOpening ||
                SaveSystem.IsModalOpen)
            {
                return;
            }

            if (keyboard.anyKey != null && keyboard.anyKey.wasPressedThisFrame)
            {
                RegisterKeyPress();
            }
        }

        public void RegisterKeyPress()
        {
            hasStartedTyping = true;
            TotalKeyPresses++;
            pressedKeysForCurrentLine++;
            if (KeyPressed != null)
            {
                KeyPressed();
            }

            if (pressedKeysForCurrentLine >= keysPerLine)
            {
                CompleteLine();
            }

            RefreshDisplay();
        }

        public void SetKeysPerLine(int amount)
        {
            keysPerLine = Mathf.Min(keysPerLine, Mathf.Max(1, amount));

            if (pressedKeysForCurrentLine >= keysPerLine)
            {
                CompleteLine();
                RefreshDisplay();
            }
        }

        public void ClearScreen()
        {
            visibleCode.Clear();
            CreateNewCodeFile();
            pressedKeysForCurrentLine = 0;
            hasStartedTyping = false;
            TotalKeyPresses = 0;
            RefreshDisplay();
        }

        public void StartNewGame()
        {
            keysPerLine = 5;
            ClearScreen();
        }

        public void RestoreState(
            int savedTotalKeyPresses,
            int savedKeysPerLine,
            string savedVisibleCode,
            int savedPressedKeysForCurrentLine,
            bool savedHasStartedTyping)
        {
            TotalKeyPresses = Mathf.Max(0, savedTotalKeyPresses);
            keysPerLine = Mathf.Max(1, savedKeysPerLine);
            pressedKeysForCurrentLine = Mathf.Clamp(
                savedPressedKeysForCurrentLine,
                0,
                KeysPerLine - 1);
            hasStartedTyping = savedHasStartedTyping;
            if (TotalKeyPresses > 0 || !string.IsNullOrEmpty(savedVisibleCode))
            {
                hasStartedTyping = true;
            }

            visibleCode.Clear();
            if (!string.IsNullOrEmpty(savedVisibleCode))
            {
                visibleCode.Append(savedVisibleCode);
                TrimOldLines();
            }

            RefreshDisplay();
        }

        private void CompleteLine()
        {
            if (lineIndex >= currentLines.Length)
            {
                visibleCode.Append('\n');
                CreateNewCodeFile();
            }

            visibleCode.Append(currentLines[lineIndex]);
            visibleCode.Append('\n');
            lineIndex++;
            pressedKeysForCurrentLine = 0;
            TrimOldLines();
            if (LineCompleted != null)
            {
                LineCompleted();
            }
        }

        private void CreateNewCodeFile()
        {
            currentLines = CreateSnippet().Split(
                new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            lineIndex = 0;
        }

        private void ConfigureDisplay()
        {
            if (codeDisplay == null)
            {
                Debug.LogError("CodeTypingGenerator needs a UI Text reference.", this);
                enabled = false;
                return;
            }

            CodeClickerFont.Apply(codeDisplay, 18, true);
            codeDisplay.supportRichText = true;
        }

        private void TrimOldLines()
        {
            int lineCount = 1;
            for (int i = 0; i < visibleCode.Length; i++)
            {
                if (visibleCode[i] == '\n')
                {
                    lineCount++;
                }
            }

            while (lineCount > maximumVisibleLines)
            {
                int firstNewLine = IndexOf(visibleCode, '\n');
                if (firstNewLine < 0)
                {
                    break;
                }

                visibleCode.Remove(0, firstNewLine + 1);
                lineCount--;
            }
        }

        private static int IndexOf(StringBuilder builder, char character)
        {
            for (int i = 0; i < builder.Length; i++)
            {
                if (builder[i] == character)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RefreshDisplay()
        {
            if (codeDisplay == null)
            {
                return;
            }

            if (!hasStartedTyping)
            {
                codeDisplay.text = "<color=#A9D989>Type to code</color>\n<color=#4FC1FF>|</color>";
                return;
            }

            string highlightedCode = HighlightSyntax(visibleCode.ToString());
            string cursor = string.Empty;
            if (showCursor)
            {
                cursor = "<color=#4FC1FF>|</color>";
            }
            codeDisplay.text = highlightedCode + cursor;
        }

        private string CreateSnippet()
        {
            string[] classNames =
            {
                "FeatureCompiler", "BuildPipeline", "BugTracker", "CodeBot",
                "TaskScheduler", "ReleaseManager", "CoffeeService", "PixelRenderer"
            };
            string[] methodNames =
            {
                "CompileFeature", "ProcessTask", "FixBug", "ShipBuild",
                "WriteModule", "RunTests", "ReviewCode", "DeployUpdate"
            };
            string[] resourceNames =
            {
                "code", "experience", "focus", "progress", "quality", "velocity"
            };

            string className = classNames[random.Next(classNames.Length)];
            string methodName = methodNames[random.Next(methodNames.Length)];
            string resourceName = resourceNames[random.Next(resourceNames.Length)];
            int target = random.Next(20, 101);
            float speed = random.Next(10, 36) / 10f;

            return
                "using System;\n" +
                "using UnityEngine;\n\n" +
                $"public sealed class {className} : MonoBehaviour\n" +
                "{\n" +
                $"    [SerializeField] private float speed = {speed:0.0}f;\n" +
                $"    private int {resourceName};\n\n" +
                "    private void Update()\n" +
                "    {\n" +
                $"        {methodName}(Time.deltaTime);\n" +
                "    }\n\n" +
                $"    private void {methodName}(float deltaTime)\n" +
                "    {\n" +
                $"        {resourceName} += Mathf.CeilToInt(speed * deltaTime);\n" +
                $"        if ({resourceName} >= {target})\n" +
                "        {\n" +
                $"            Debug.Log(\"{className} complete!\");\n" +
                $"            {resourceName} = 0;\n" +
                "        }\n" +
                "    }\n" +
                "}";
        }

        private static string HighlightSyntax(string source)
        {
            StringBuilder result = new StringBuilder(source.Length * 2);
            int index = 0;

            while (index < source.Length)
            {
                if (source[index] == '"')
                {
                    int end = index + 1;
                    while (end < source.Length)
                    {
                        if (source[end] == '"' && source[end - 1] != '\\')
                        {
                            end++;
                            break;
                        }

                        end++;
                    }

                    AppendColored(result, source.Substring(index, end - index), "CE9178");
                    index = end;
                    continue;
                }

                if (char.IsLetter(source[index]) || source[index] == '_')
                {
                    int end = index + 1;
                    while (end < source.Length &&
                           (char.IsLetterOrDigit(source[end]) || source[end] == '_'))
                    {
                        end++;
                    }

                    string word = source.Substring(index, end - index);
                    if (Array.IndexOf(Keywords, word) >= 0)
                    {
                        AppendColored(result, word, "4FC1FF");
                    }
                    else
                    {
                        AppendEscaped(result, word);
                    }

                    index = end;
                    continue;
                }

                if (char.IsDigit(source[index]))
                {
                    int end = index + 1;
                    while (end < source.Length &&
                           (char.IsDigit(source[end]) || source[end] == '.' || source[end] == 'f'))
                    {
                        end++;
                    }

                    AppendColored(result, source.Substring(index, end - index), "B5CEA8");
                    index = end;
                    continue;
                }

                AppendEscaped(result, source[index].ToString());
                index++;
            }

            return result.ToString();
        }

        private static void AppendColored(StringBuilder builder, string text, string color)
        {
            builder.Append("<color=#");
            builder.Append(color);
            builder.Append('>');
            AppendEscaped(builder, text);
            builder.Append("</color>");
        }

        private static void AppendEscaped(StringBuilder builder, string text)
        {
            foreach (char character in text)
            {
                switch (character)
                {
                    case '<':
                        builder.Append("&lt;");
                        break;
                    case '>':
                        builder.Append("&gt;");
                        break;
                    case '&':
                        builder.Append("&amp;");
                        break;
                    default:
                        builder.Append(character);
                        break;
                }
            }
        }
    }
}
