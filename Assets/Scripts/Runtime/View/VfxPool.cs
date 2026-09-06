using System.Collections.Generic;
using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>
    /// Plays pooled one-shot particle bursts.
    /// <para>
    /// Instances are recycled on a timer rather than by polling <c>ParticleSystem.IsAlive</c>. The
    /// polled version is the reason bursts stopped appearing: <c>IsAlive</c> depends on where the
    /// system is in its own simulation, so its answer on the frame a burst is started is not
    /// something a pool can safely act on, and a burst recycled on its first frame is a burst
    /// nobody ever sees. A duration the pool computes itself cannot be wrong about this.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VfxPool : MonoBehaviour
    {
        private struct Live
        {
            public VfxBurstView View;
            public float ExpiresAt;
        }

        [SerializeField] private VfxBurstView _burstPrefab;
        [SerializeField] private Transform _root;
        [SerializeField, Min(0)] private int _prewarm = 12;

        [SerializeField, Min(0f), Tooltip("Extra seconds kept alive beyond the burst's own duration.")]
        private float _lifetimeMargin = 0.25f;

        private readonly Stack<VfxBurstView> _free = new();
        private readonly List<Live> _live = new(16);

        private void Awake()
        {
            if (_root == null)
            {
                _root = transform;
            }

            for (int i = 0; i < _prewarm; i++)
            {
                _free.Push(Create());
            }
        }

        /// <summary>Fires a burst at <paramref name="position"/> in the given colour.</summary>
        public void Play(Vector3 position, Color tint)
        {
            if (_burstPrefab == null)
            {
                return;
            }

            VfxBurstView view = _free.Count > 0 ? _free.Pop() : Create();

            view.gameObject.SetActive(true);
            view.Play(position, tint);

            _live.Add(new Live
            {
                View = view,
                ExpiresAt = Time.time + view.Duration + _lifetimeMargin
            });
        }

        private void Update()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                if (Time.time < _live[i].ExpiresAt)
                {
                    continue;
                }

                VfxBurstView view = _live[i].View;
                view.gameObject.SetActive(false);
                _free.Push(view);

                int last = _live.Count - 1;
                _live[i] = _live[last];
                _live.RemoveAt(last);
            }
        }

        private VfxBurstView Create()
        {
            VfxBurstView view = Instantiate(_burstPrefab, _root);
            view.gameObject.SetActive(false);
            return view;
        }
    }
}
