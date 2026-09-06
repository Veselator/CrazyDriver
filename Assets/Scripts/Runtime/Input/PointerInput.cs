using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CrazyDriver.Logic;

namespace CrazyDriver.Input
{
    /// <summary>
    /// Reads the active pointer and reduces it to the two things the game needs: taps, and a
    /// horizontal drag offset measured from wherever the drag began.
    /// <para>
    /// <see cref="Pointer.current"/> covers touch and mouse with one code path, so the game is
    /// playable in the editor without a second control scheme or an action asset.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder.Input)]
    public sealed class PointerInput : MonoBehaviour
    {
        private bool _isDown;
        private float _originX;

        /// <summary>A press landed. Starts a run, and dismisses a result screen.</summary>
        public event Action Tapped;

        /// <summary>A drag began. The turret anchors its aim here.</summary>
        public event Action DragStarted;

        /// <summary>
        /// The pointer moved while held. The payload is the horizontal offset from where the drag
        /// started, as a fraction of screen width: -1 is a full width left, +1 a full width right.
        /// </summary>
        public event Action<float> Dragged;

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            bool isPressed = pointer.press.isPressed;
            float x = pointer.position.ReadValue().x;

            if (isPressed && !_isDown)
            {
                _isDown = true;
                _originX = x;

                Tapped?.Invoke();
                DragStarted?.Invoke();
                return;
            }

            if (!isPressed && _isDown)
            {
                _isDown = false;
                return;
            }

            if (_isDown)
            {
                // Normalising by screen width is what keeps sensitivity identical between a phone
                // and the editor's Game view, whatever their pixel dimensions.
                Dragged?.Invoke((x - _originX) / Mathf.Max(1f, Screen.width));
            }
        }
    }
}
