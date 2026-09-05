using System;

namespace CrazyDriver.Game.Input
{
    /// <summary>
    /// Raw pointer input, already reduced to the two things the game actually needs: taps, and a
    /// horizontal drag offset measured from wherever the drag began.
    /// <para>
    /// Gameplay code depends on this interface rather than on the Input System, so the control
    /// scheme can be replaced -- or driven from a test -- without touching a line of it.
    /// </para>
    /// </summary>
    public interface IInputService
    {
        /// <summary>A press landed. Used to start a run and to dismiss a result screen.</summary>
        event Action Tapped;

        /// <summary>A drag began. The turret anchors its aim here.</summary>
        event Action DragStarted;

        /// <summary>
        /// The pointer moved while held down. The payload is the horizontal offset from the drag's
        /// starting point, expressed as a fraction of the screen width: -1 is a full screen width
        /// to the left, +1 a full width to the right.
        /// </summary>
        event Action<float> Dragged;

        event Action DragEnded;
    }
}
