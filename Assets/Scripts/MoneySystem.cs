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

        public double Coins { get { return coins; } }
        public double CoinsPerKey { get { return coinsPerKey; } }
        public double PassiveCoinsPerSecond { get { return passiveCoinsPerSecond; } }

        public Text MoneyText
        {
            get { return moneyText; }
            set { moneyText = value; }
        }

        private void Start()
        {
            codeTypingGenerator = FindFirstObjectByType<CodeTypingGenerator>();
            if (GetComponent<TestCommandBar>() == null)
            {
                gameObject.AddComponent<TestCommandBar>();
            }

            CodeClickerFont.Apply(moneyText, 30);

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
                AddCoins(passiveCoinsPerSecond * Time.deltaTime);
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
            AddCoins(coinsPerKey);
        }

        private void EarnLineBonus()
        {
            AddCoins(passiveCoinsPerSecond * 2);
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

        public void AddCoins(double amount)
        {
            coins += amount;
            UpdateMoneyText();
        }

        public void ResetCoins()
        {
            coins = 0;
            UpdateMoneyText();
        }

        public void RestoreState(
            double savedCoins,
            double savedCoinsPerKey,
            double savedPassiveCoinsPerSecond)
        {
            coins = System.Math.Max(0d, savedCoins);
            coinsPerKey = System.Math.Max(0.01d, savedCoinsPerKey);
            passiveCoinsPerSecond = System.Math.Max(0d, savedPassiveCoinsPerSecond);
            UpdateMoneyText();
        }

        public void ApplyUpgrade(UpgradeEffect effect, double amount)
        {
            if (effect == UpgradeEffect.CoinsPerKey)
            {
                coinsPerKey += amount;
            }

            else if (effect == UpgradeEffect.PassiveIncome)
            {
                passiveCoinsPerSecond += amount;
            }

            else if (effect == UpgradeEffect.KeysPerLine && codeTypingGenerator != null)
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
