using System.Collections.Generic;
using CrazyDriver.Combat;
using CrazyDriver.Config;
using CrazyDriver.View;
using UnityEngine;
using VContainer;

namespace CrazyDriver.Logic
{
    /// <summary>
    /// Fires, moves and resolves every projectile.
    /// <para>
    /// Each shot is swept with a sphere cast along the segment it covers this frame rather than
    /// being given a collider and a Rigidbody. At these speeds a collider-based bullet passes clean
    /// through a stickman between two fixed-update steps; a sweep cannot miss, costs one cast per
    /// live shot, and needs no physics body at all. Hits still resolve by collider, as the brief
    /// requires -- it is only the bullet that has none.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Projectiles)]
    public sealed class ProjectileController : RunPhaseBehaviour
    {
        private struct Shot
        {
            public ProjectileView View;
            public Vector3 Position;
            public Vector3 Direction;
            public float RemainingLifetime;
        }

        [SerializeField] private Transform _root;

        private readonly Stack<ProjectileView> _free = new();
        private readonly List<Shot> _live = new(32);
        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private GameConstantsSO _constants;
        private LevelSO _level;

        [Inject]
        public void Construct(GameConstantsSO constants, LevelSO level)
        {
            _constants = constants;
            _level = level;
        }

        protected override void OnAwake()
        {
            if (_root == null)
            {
                _root = transform;
            }
        }

        public void Fire(Vector3 origin, Quaternion rotation)
        {
            ProjectileView view = _free.Count > 0 ? _free.Pop() : Create();

            view.gameObject.SetActive(true);
            view.OnRented(origin, rotation);

            _live.Add(new Shot
            {
                View = view,
                Position = origin,
                Direction = rotation * Vector3.forward,
                RemainingLifetime = _constants.Weapon.ProjectileLifetime
            });
        }

        public void Clear()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Release(i);
            }
        }

        private void Update()
        {
            WeaponSettings weapon = _constants.Weapon;
            float deltaTime = Time.deltaTime;
            float step = weapon.ProjectileSpeed * deltaTime;

            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Shot shot = _live[i];

                shot.RemainingLifetime -= deltaTime;
                if (shot.RemainingLifetime <= 0f)
                {
                    Release(i);
                    continue;
                }

                if (TryResolveHit(shot, step, weapon))
                {
                    Release(i);
                    continue;
                }

                shot.Position += shot.Direction * step;
                shot.View.transform.position = shot.Position;
                _live[i] = shot;
            }
        }

        private bool TryResolveHit(Shot shot, float step, WeaponSettings weapon)
        {
            int count = Physics.SphereCastNonAlloc(
                shot.Position,
                weapon.HitRadius,
                shot.Direction,
                _hits,
                step,
                _constants.ProjectileHitMask,
                QueryTriggerInteraction.Collide);

            // NonAlloc does not sort its results, and punching through the enemy you aimed at to hit
            // the one behind would feel broken, so take the nearest of whatever the sweep found.
            IDamageable nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i].distance >= nearestDistance)
                {
                    continue;
                }

                if (!_hits[i].collider.TryGetComponent(out IDamageable target) || !target.IsAlive)
                {
                    continue;
                }

                nearest = target;
                nearestDistance = _hits[i].distance;
            }

            if (nearest == null)
            {
                return false;
            }

            nearest.TakeDamage(weapon.Damage);
            return true;
        }

        private ProjectileView Create()
        {
            ProjectileView view = Instantiate(_level.ProjectilePrefab, _root).GetComponent<ProjectileView>();
            view.gameObject.SetActive(false);
            return view;
        }

        private void Release(int index)
        {
            ProjectileView view = _live[index].View;
            view.gameObject.SetActive(false);
            _free.Push(view);

            int last = _live.Count - 1;
            _live[index] = _live[last];
            _live.RemoveAt(last);
        }
    }
}
