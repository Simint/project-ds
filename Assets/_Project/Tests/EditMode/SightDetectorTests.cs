using Game.AI;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// DoorTests 등이 보여주듯 EditMode에서도 Physics2D 쿼리는 실제로 동작한다 — 콜라이더를 실제로
/// 만들어 Linecast가 막히는지 그대로 확인한다.
/// </summary>
public sealed class SightDetectorTests
{
    private GameObject wall;

    [TearDown]
    public void TearDown()
    {
        if (wall != null) Object.DestroyImmediate(wall);
    }

    [Test]
    public void CanSee_TargetBeyondRange_ReturnsFalse()
    {
        bool result = SightDetector.CanSee(Vector2.zero, new Vector2(20f, 0f), 5f, GameLayers.SolidMask);

        Assert.IsFalse(result);
    }

    [Test]
    public void CanSee_WithinRangeAndNoObstruction_ReturnsTrue()
    {
        bool result = SightDetector.CanSee(Vector2.zero, new Vector2(3f, 0f), 5f, GameLayers.SolidMask);

        Assert.IsTrue(result);
    }

    [Test]
    public void CanSee_ObstructedByWallOnGroundLayer_ReturnsFalse()
    {
        wall = new GameObject("wall");
        wall.layer = GameLayers.Ground;
        var collider = wall.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f, 5f);
        wall.transform.position = new Vector2(2f, 0f);

        bool result = SightDetector.CanSee(Vector2.zero, new Vector2(5f, 0f), 10f, GameLayers.SolidMask);

        Assert.IsFalse(result);
    }

    [Test]
    public void CanSee_ZeroRange_AlwaysFalse()
    {
        bool result = SightDetector.CanSee(Vector2.zero, Vector2.zero, 0f, GameLayers.SolidMask);

        Assert.IsFalse(result);
    }
}
