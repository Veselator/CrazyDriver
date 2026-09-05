using System.IO;
using System.Text;
using CrazyDriver.Core.Actors;
using CrazyDriver.Core.Car;
using CrazyDriver.Core.Combat;
using CrazyDriver.Core.Economy;
using CrazyDriver.Core.Level;
using CrazyDriver.Core.Path;
using CrazyDriver.Core.Progression;
using CrazyDriver.Core.Run;
using CrazyDriver.Game.Data;
using UnityEditor;
using UnityEngine;

namespace CrazyDriver.Editor
{
    /// <summary>
    /// Runs whole levels headlessly and reports what happened.
    /// <para>
    /// This is the payoff for keeping the simulation free of MonoBehaviours: the entire game can be
    /// played to completion in a few milliseconds with no scene, no views and no play mode, so
    /// balance questions get an answer immediately instead of after a 40 second play-through.
    /// </para>
    /// </summary>
    public static class SimulationSmokeTest
    {
        private const float FixedStep = 1f / 60f;
        private const int MaxSteps = 60 * 240;

        [MenuItem("CrazyDriver/Run Simulation Smoke Test", priority = 20)]
        public static void Run()
        {
            var constants = AssetDatabase.LoadAssetAtPath<GameConstantsSO>($"{GameAssetBuilder.DataFolder}/GameConstants.asset");
            var level = AssetDatabase.LoadAssetAtPath<LevelSO>($"{GameAssetBuilder.DataFolder}/Level_Desert.asset");

            if (constants == null || level == null)
            {
                Debug.LogError("[SmokeTest] Run CrazyDriver/Rebuild Project first.");
                return;
            }

            var report = new StringBuilder("[SmokeTest]\n");

            // A sweep rather than just the two endpoints. Proving that a win and a loss are both
            // reachable is the easy part; what actually decides whether the level is fair is where
            // the threshold between them sits.
            float[] accuracies = { 0f, 0.5f, 0.7f, 0.8f, 0.9f, 1f };

            foreach (int seed in new[] { 12345, 999 })
            {
                foreach (float accuracy in accuracies)
                {
                    report.AppendLine(Simulate(constants, level, seed, accuracy));
                }
            }

            report.AppendLine(CheckProfileRoundTrip());

            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Writes a profile, reads it back and compares. Persistence is the one system whose bugs
        /// only show up on the player's second session, so it is worth checking on the first.
        /// </summary>
        private static string CheckProfileRoundTrip()
        {
            // A scratch file, not the real save. A test that leaves the player's coins and best
            // distance different from how it found them is a bug, not a test.
            const string fileName = "player-profile.smoketest.json";

            var storage = new JsonProfileStorage(fileName);

            try
            {
                var service = new PlayerProfileService(storage);
                service.RecordRun(won: true, coinsEarned: 7, kills: 3, distance: 123f);

                // A second service reads from disk rather than from the instance that just wrote,
                // which is what makes this a round trip instead of a memory check.
                PlayerProfile reloaded = new PlayerProfileService(new JsonProfileStorage(fileName)).Profile;

                bool coinsMatch = reloaded.TotalCoins == 7;
                bool runsMatch = reloaded.RunsPlayed == 1 && reloaded.RunsWon == 1;
                bool killsMatch = reloaded.TotalKills == 3;
                bool bestMatch = Mathf.Approximately(reloaded.BestDistance, 123f);

                string verdict = coinsMatch && runsMatch && killsMatch && bestMatch ? "OK" : "FAILED";

                return $"  profile round trip -> {verdict} " +
                       $"(coins {reloaded.TotalCoins}, runs {reloaded.RunsWon}/{reloaded.RunsPlayed}, " +
                       $"kills {reloaded.TotalKills}, best {reloaded.BestDistance:0} m)";
            }
            finally
            {
                if (File.Exists(storage.FilePath))
                {
                    File.Delete(storage.FilePath);
                }
            }
        }

        /// <param name="marksmanship">
        /// Fraction of woken enemies the stand-in player manages to kill. Substitutes for aiming,
        /// which is the only part of the loop this harness cannot exercise.
        /// </param>
        private static string Simulate(GameConstantsSO constants, LevelSO level, int seed, float marksmanship)
        {
            var stateMachine = new GameStateMachine();
            var progress = new PathProgress();
            var path = new ActivePath();
            var wallet = new Wallet();
            var car = new CarModel(constants.Car, path, progress);
            var turret = new TurretModel(constants.Turret);
            var weapon = new WeaponModel(constants.Weapon);
            var health = new Health(level.CarMaxHealth);
            var enemies = new EnemyDirector(constants.Enemy, car, progress, path);
            var bonuses = new BonusDirector(constants.Road, progress, wallet, path);
            var generator = new LevelGenerator(constants.Road, constants.Enemy, constants.Car);

            var run = new RunController(
                stateMachine, generator, level, progress, path, car, turret, weapon,
                health, enemies, bonuses, wallet, () => seed);

            // Stand in for the player's aim: kill a deterministic share of the enemies that wake up.
            int woken = 0;
            enemies.Spawned += _ => woken++;

            var random = new System.Random(seed);
            enemies.Spawned += agent =>
            {
                if (random.NextDouble() < marksmanship)
                {
                    agent.TakeDamage(float.MaxValue);
                }
            };

            run.Prepare();
            run.StartRun();

            int steps = 0;
            while (!stateMachine.IsFinished && steps < MaxSteps)
            {
                run.Tick(FixedStep);
                steps++;
            }

            string outcome = stateMachine.Current.ToString();
            float seconds = steps * FixedStep;

            return $"  seed {seed}, marksmanship {marksmanship:P0} -> {outcome} " +
                   $"at {progress.Distance:0} m / {run.Plan.Length:0} m in {seconds:0.0}s, " +
                   $"hp {health.Current:0}/{health.Max:0}, kills {run.KillCount}, coins {wallet.Coins}, " +
                   $"spawned {woken}/{run.Plan.Enemies.Length}";
        }
    }
}
