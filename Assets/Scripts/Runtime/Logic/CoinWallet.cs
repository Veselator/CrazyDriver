using System;
using UnityEngine;

namespace CrazyDriver.Logic
{
    /// <summary>Coins earned during the current run. The persisted total lives in the player profile.</summary>
    [DisallowMultipleComponent]
    public sealed class CoinWallet : MonoBehaviour
    {
        private int _coins;

        public event Action<int> CoinsChanged;

        public int Coins => _coins;

        public void Add(int amount)
        {
            if (amount == 0)
            {
                return;
            }

            // Signed rather than clamped to gains: the counter animates red on a loss, and a wallet
            // that silently refused to go down would leave that half of the animation unreachable.
            _coins = Mathf.Max(0, _coins + amount);
            CoinsChanged?.Invoke(_coins);
        }

        public void ResetCoins()
        {
            _coins = 0;
            CoinsChanged?.Invoke(_coins);
        }
    }
}
