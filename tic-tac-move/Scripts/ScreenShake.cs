using Godot;

namespace TicTacMove;

/// <summary>
/// Handles screen shake effects for impacts and wins.
/// </summary>
public partial class ScreenShake : Node
{
    private CanvasLayer _targetLayer;
    private Vector2 _originalOffset;
    private float _shakeAmount;
    private float _shakeDuration;
    private float _shakeTimer;

    public void Initialize(CanvasLayer layer)
    {
        _targetLayer = layer;
        _originalOffset = layer.Offset;
    }

    public override void _Process(double delta)
    {
        if (_shakeTimer > 0)
        {
            _shakeTimer -= (float)delta;

            float progress = _shakeTimer / _shakeDuration;
            float currentShake = _shakeAmount * progress;

            _targetLayer.Offset = _originalOffset + new Vector2(
                (float)GD.RandRange(-currentShake, currentShake),
                (float)GD.RandRange(-currentShake, currentShake)
            );

            if (_shakeTimer <= 0)
            {
                _targetLayer.Offset = _originalOffset;
            }
        }
    }

    /// <summary>
    /// Trigger a screen shake effect.
    /// </summary>
    public void Shake(float amount = 8f, float duration = 0.2f)
    {
        _shakeAmount = amount;
        _shakeDuration = duration;
        _shakeTimer = duration;
    }

    /// <summary>
    /// Small shake for piece placement.
    /// </summary>
    public void ShakeSmall()
    {
        Shake(3f, 0.1f);
    }

    /// <summary>
    /// Medium shake for collisions.
    /// </summary>
    public void ShakeCollision()
    {
        Shake(10f, 0.25f);
    }

    /// <summary>
    /// Big shake for wins.
    /// </summary>
    public void ShakeWin()
    {
        Shake(15f, 0.4f);
    }
}
