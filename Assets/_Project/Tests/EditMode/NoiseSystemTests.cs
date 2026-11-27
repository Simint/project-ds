using Game.Combat;
using NUnit.Framework;
using UnityEngine;

public sealed class NoiseSystemTests
{
    private sealed class RecordingListener : INoiseListener
    {
        public Vector2 LastPosition;
        public float LastRadius;
        public NoiseKind LastKind;
        public int CallCount;

        public void OnNoiseHeard(Vector2 position, float radius, NoiseKind kind)
        {
            LastPosition = position;
            LastRadius = radius;
            LastKind = kind;
            CallCount++;
        }
    }

    [Test]
    public void Emit_NotifiesAllRegisteredListeners()
    {
        var system = new NoiseSystem();
        var a = new RecordingListener();
        var b = new RecordingListener();
        system.Register(a);
        system.Register(b);

        system.Emit(new Vector2(3f, 1f), 6f, NoiseKind.Footstep);

        Assert.AreEqual(1, a.CallCount);
        Assert.AreEqual(1, b.CallCount);
        Assert.AreEqual(new Vector2(3f, 1f), a.LastPosition);
        Assert.AreEqual(6f, b.LastRadius);
    }

    [Test]
    public void Unregister_StopsFurtherNotifications()
    {
        var system = new NoiseSystem();
        var listener = new RecordingListener();
        system.Register(listener);
        system.Unregister(listener);

        system.Emit(Vector2.zero, 5f, NoiseKind.Footstep);

        Assert.AreEqual(0, listener.CallCount);
    }

    [Test]
    public void Register_SameListenerTwice_DoesNotDoubleNotify()
    {
        var system = new NoiseSystem();
        var listener = new RecordingListener();
        system.Register(listener);
        system.Register(listener);

        system.Emit(Vector2.zero, 5f, NoiseKind.Footstep);

        Assert.AreEqual(1, listener.CallCount);
    }
}
