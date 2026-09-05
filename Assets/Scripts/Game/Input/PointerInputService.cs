using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace CrazyDriver.Game.Input
{
    /// <summary>
    /// Reads the active pointer through the Input System.
    /// <para>
    /// <see cref="Pointer.current"/> covers touch and mouse with one code path, so the game is
    /// playable in the editor without a second control scheme or a separate action asset.
    /// </para>
    /// </summary>
    public sealed class PointerInputService : IInputService, ITickable
    {
        private bool _isDown;
        private float _originX;

        public event Action Tapped;
        public event Action DragStarted;
        public event Action<float> Dragged;
        public event Action DragEnded;

        public void Tick()
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
                DragEnded?.Invoke();
                return;
            }

            if (_isDown)
            {
                // Normalising by screen width is what keeps the sensitivity identical between a
                // phone and the editor's Game view, whatever their pixel dimensions.
                float width = Mathf.Max(1f, Screen.width);
                Dragged?.Invoke((x - _originX) / width);
            }
        }
    }
}
