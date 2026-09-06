using System.Collections.Generic;
using UnityEngine;

namespace CrazyDriver.View
{
    /// <summary>
    /// Plays pooled floating numbers -- damage taken, coins gained.
    /// <para>
    /// Recycled on the view's own elapsed timer, which is a plain float it owns, rather than on
    /// anything the renderer reports. That is the same lesson the particle pool learned: a pool must
    /// not ask a subsystem whether it has finished when it can simply know.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PopupPool : MonoBehaviour
    {
        [SerializeField] private FloatingTextView _prefab;
        [SerializeField] private Transform _root;
        [SerializeField] private Camera _viewer;
        [SerializeField, Min(0)] private int _prewarm = 8;

        private readonly Stack<FloatingTextView> _free = new();
        private readonly List<FloatingTextView> _live = new(16);

        private void Awake()
        {
            if (_root == null)
            {
                _root = transform;
            }

            if (_viewer == null)
            {
                _viewer = Camera.main;
            }

            for (int i = 0; i < _prewarm; i++)
            {
                _free.Push(Create());
            }
        }

        /// <param name="scale">Multiplies the prefab's size. Kills and heavier hits read larger.</param>
        public void Play(string text, Color color, Vector3 worldPosition, float scale = 1f)
        {
            if (_prefab == null)
            {
                return;
            }

            FloatingTextView view = _free.Count > 0 ? _free.Pop() : Create();

            view.gameObject.SetActive(true);
            view.Play(text, color, worldPosition, _viewer != null ? _viewer : Camera.main, scale);

            _live.Add(view);
        }

        private void LateUpdate()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                if (!_live[i].IsFinished)
                {
                    continue;
                }

                _live[i].gameObject.SetActive(false);
                _free.Push(_live[i]);

                int last = _live.Count - 1;
                _live[i] = _live[last];
                _live.RemoveAt(last);
            }
        }

        private FloatingTextView Create()
        {
            FloatingTextView view = Instantiate(_prefab, _root);
            view.gameObject.SetActive(false);
            return view;
        }
    }
}
