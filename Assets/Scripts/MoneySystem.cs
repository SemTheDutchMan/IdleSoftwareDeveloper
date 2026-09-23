using UnityEngine;
using UnityEngine.UI;

namespace CodeClicker
{
    public class MoneySystem : MonoBehaviour
    {
        [SerializeField] private Text moneyText;
        [SerializeField] private double coins;
        [SerializeField] private double coinsPerKey = 0.01;
        [SerializeField] private double passiveCoinsPerSecond;

        private CodeTypingGenerator codeTypingGenerator;

        public double Coins => coins;
        public double CoinsPerKey => coinsPerKey;
        public double PassiveCoinsPerSecond => passiveCoinsPerSecond;

        public Text MoneyText
        {
            get => moneyText;
            set => moneyText = value;
        }

        private void Start()
        {
            codeTypingGenerator = FindFirstObjectByType<CodeTypingGenerator>();

            if (moneyText != null && moneyText.font == null)
            {
                moneyText.font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Cascadia Mono", "Consolas", "Arial" }, 30);

                if (moneyText.font == null)
                {
                    moneyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }

            if (codeTypingGenerator != null)
            {
                codeTypingGenerator.KeyPressed += EarnKeyPressMoney;
                codeTypingGenerator.LineCompleted += EarnLineBonus;
            }

            UpdateMoneyText();
        }

        private void Update()
        {
            if (passiveCoinsPerSecond > 0)
            {
                coins += passiveCoinsPerSecond * Time.deltaTime;
                UpdateMoneyText();
            }
        }

        private void OnDestroy()
        {
            if (codeTypingGenerator != null)
            {
                codeTypingGenerator.KeyPressed -= EarnKeyPressMoney;
                codeTypingGenerator.LineCompleted -= EarnLineBonus;
            }
        }

        private void EarnKeyPressMoney()
        {
            coins += coinsPerKey;
            UpdateMoneyText();
        }

        private void EarnLineBonus()
        {
            coins += passiveCoinsPerSecond * 2;
            UpdateMoneyText();
        }

        public bool TrySpend(double price)
        {
            if (coins < price)
            {
                return false;
            }

            coins -= price;
            UpdateMoneyText();
            return true;
        }

        public void ApplyUpgrade(UpgradeEffect effect, double amount)
        {
            if (effect == UpgradeEffect.CoinsPerKey)
            {
                coinsPerKey += amount;
            }

            if (effect == UpgradeEffect.PassiveIncome)
            {
                passiveCoinsPerSecond += amount;
            }

            if (effect == UpgradeEffect.KeysPerLine && codeTypingGenerator != null)
            {
                codeTypingGenerator.SetKeysPerLine((int)amount);
            }

            UpdateMoneyText();
        }

        private void UpdateMoneyText()
        {
            if (moneyText != null)
            {
                moneyText.text = FormatCoins(coins);
            }
        }

        public static string FormatCoins(double amount)
        {
            if (amount >= 1_000_000_000)
            {
                return $"{amount / 1_000_000_000:0.##}B coins";
            }

            if (amount >= 1_000_000)
            {
                return $"{amount / 1_000_000:0.##}M coins";
            }

            if (amount >= 1_000)
            {
                return $"{amount / 1_000:0.##}K coins";
            }

            return $"{amount:0.00} coins";
        }
    }
}
