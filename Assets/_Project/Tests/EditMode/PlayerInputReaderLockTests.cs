using Game.Player;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// SetLocked/IsLocked/IsLockedByOthers는 actions(InputActionAsset)를 쓰지 않으므로 Awake 없이도
/// 안전하게 테스트할 수 있다. 이번에 고친 버그: 잠금을 bool 하나로 두면 두 출처가 같이 잠근 상태에서
/// 한쪽이 풀면 다른 쪽 잠금까지 같이 풀렸다 — 출처별 집합으로 바꿔 서로 간섭하지 않게 했다.
/// </summary>
public sealed class PlayerInputReaderLockTests
{
    private GameObject go;
    private PlayerInputReader reader;
    private object sourceA;
    private object sourceB;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("InputReader");
        reader = go.AddComponent<PlayerInputReader>();
        sourceA = new object();
        sourceB = new object();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    [Test]
    public void SetLocked_SingleSource_LocksAndUnlocks()
    {
        Assert.IsFalse(reader.IsLocked);

        reader.SetLocked(sourceA, true);
        Assert.IsTrue(reader.IsLocked);

        reader.SetLocked(sourceA, false);
        Assert.IsFalse(reader.IsLocked);
    }

    [Test]
    public void SetLocked_OneSourceUnlocking_DoesNotReleaseAnotherSourcesLock()
    {
        reader.SetLocked(sourceA, true);
        reader.SetLocked(sourceB, true);

        reader.SetLocked(sourceA, false);

        Assert.IsTrue(reader.IsLocked, "B가 아직 잠궈 둔 상태인데 A를 풀었다고 전체가 풀리면 안 된다.");
    }

    [Test]
    public void IsLockedByOthers_FalseWhenOnlySelfIsLocking()
    {
        reader.SetLocked(sourceA, true);

        Assert.IsFalse(reader.IsLockedByOthers(sourceA));
    }

    [Test]
    public void IsLockedByOthers_TrueWhenAnotherSourceIsLocking()
    {
        reader.SetLocked(sourceB, true);

        Assert.IsTrue(reader.IsLockedByOthers(sourceA));
    }

    [Test]
    public void IsLockedByOthers_FalseWhenNothingIsLocked()
    {
        Assert.IsFalse(reader.IsLockedByOthers(sourceA));
    }

    [Test]
    public void SetLocked_SameSourceTwice_DoesNotDoubleCount()
    {
        reader.SetLocked(sourceA, true);
        reader.SetLocked(sourceA, true);

        reader.SetLocked(sourceA, false);

        Assert.IsFalse(reader.IsLocked);
    }

    [Test]
    public void ItemUse_AllowedWhenOnlyItemUseAllowedSourceLocks()
    {
        reader.SetLocked(sourceA, true, allowItemUse: true);

        Assert.IsTrue(reader.IsLocked);
        Assert.IsFalse(reader.IsItemUseLocked);
    }

    [Test]
    public void ItemUse_BlockedWhenAnotherSourceAlsoLocks()
    {
        reader.SetLocked(sourceA, true, allowItemUse: true);
        reader.SetLocked(sourceB, true);

        Assert.IsTrue(reader.IsItemUseLocked);

        reader.SetLocked(sourceB, false);

        Assert.IsFalse(reader.IsItemUseLocked);
    }

    [Test]
    public void ItemUse_RelockingWithoutAllow_BlocksAgain()
    {
        reader.SetLocked(sourceA, true, allowItemUse: true);
        reader.SetLocked(sourceA, true);

        Assert.IsTrue(reader.IsItemUseLocked);

        reader.SetLocked(sourceA, false);

        Assert.IsFalse(reader.IsItemUseLocked);
        Assert.IsFalse(reader.IsLocked);
    }
}
