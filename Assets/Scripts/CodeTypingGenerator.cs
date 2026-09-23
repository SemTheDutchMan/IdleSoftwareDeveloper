using System;
using System.Collections.Generic;
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

        private readonly StringBuilder visibleCode = new();
        private System.Random random;
        private string[] currentLines;
        private int lineIndex;
        private int pressedKeysForCurrentLine;
        private bool hasStartedTyping;

        public Text CodeDisplay
        {
            get => codeDisplay;
            set => codeDisplay = value;
        }

        public int KeysPerLine
        {
            get => keysPerLine;
            private set => keysPerLine = Mathf.Max(1, value);
        }

        public int TotalKeyPresses { get; private set; }
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
            if (keyboard == null || !Application.isFocused)
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
            KeyPressed?.Invoke();

            if (pressedKeysForCurrentLine >= keysPerLine)
            {
                CompleteLine();
            }

            RefreshDisplay();
        }

        public void SetKeysPerLine(int amount)
        {
            KeysPerLine = Mathf.Min(keysPerLine, amount);

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
            LineCompleted?.Invoke();
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

            if (codeDisplay.font == null)
            {
                codeDisplay.font = CreateCodeFont();
            }

            codeDisplay.supportRichText = true;
        }

        private static Font CreateCodeFont()
        {
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Cascadia Mono", "Consolas", "Courier New", "Arial" }, 18);

            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
                codeDisplay.text = "<color=#6A9955>Press any key to start coding...</color>\n<color=#4FC1FF>|</color>";
                return;
            }

            string highlightedCode = HighlightSyntax(visibleCode.ToString());
            string cursor = showCursor ? "<color=#4FC1FF>|</color>" : string.Empty;
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
            HashSet<string> keywords = new()
            {
                "using", "public", "private", "protected", "sealed", "class", "void",
                "float", "int", "string", "bool", "return", "if", "else", "new",
                "true", "false", "null", "this", "static"
            };

            StringBuilder result = new(source.Length * 2);
            int index = 0;

            while (index < source.Length)
            {
                if (index + 1 < source.Length && source[index] == '/' && source[index + 1] == '/')
                {
                    int end = source.IndexOf('\n', index);
                    if (end < 0)
                    {
                        end = source.Length;
                    }

                    AppendColored(result, source.Substring(index, end - index), "6A9955");
                    index = end;
                    continue;
                }

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
                    if (keywords.Contains(word))
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
