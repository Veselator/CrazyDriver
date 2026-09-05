using System;

namespace CrazyDriver.Core.Economy
{
    /// <summary>Coins earned during a run.</summary>
    public sealed class Wallet
    {
        private int _coins;

        public event Action<int> CoinsChanged;

        public int Coins => _coins;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _coins += amount;
            CoinsChanged?.Invoke(_coins);
        }

        public void Reset()
        {
            _coins = 0;
            CoinsChanged?.Invoke(_coins);
        }
    }
}
