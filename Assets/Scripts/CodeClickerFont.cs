using UnityEngine;
using UnityEngine.UI;

namespace CodeClicker
{
    public static class CodeClickerFont
    {
        private static Font font;
        private static Font codeFont;

        public static void Apply(Text text, int size, bool monospace = false)
        {
            if (text == null)
            {
                return;
            }

            if (monospace && codeFont == null)
            {
                codeFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Cascadia Mono", "Consolas", "Courier New" }, size);
            }

            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Segoe UI", "Arial", "Liberation Sans" }, size);
            }

            Font chosenFont = monospace && codeFont != null ? codeFont : font;
            if (chosenFont != null)
            {
                text.font = chosenFont;
            }

            text.fontSize = size;
        }
    }
}
