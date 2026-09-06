using CrazyDriver.Actors;
using CrazyDriver.Events;
using CrazyDriver.Logic;
using UnityEngine;

namespace CrazyDriver
{
    public class AddMoneyOnEnemyKilled : MonoBehaviour
    {
        [SerializeField] private CoinWallet _wallet;
        
        void Start()
        {
            GameEvents.OnEnemyKilled += OnEnemyKilled;
        }

        private void OnDestroy()
        {
            GameEvents.OnEnemyKilled -= OnEnemyKilled;
        }

        private void OnEnemyKilled(Enemy enemy)
        {
            if(enemy == null) return;
            _wallet.Add(enemy.CoinsBonusPerKill);
        }
    }
}
