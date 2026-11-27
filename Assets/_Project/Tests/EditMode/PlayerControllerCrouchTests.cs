using System.Reflection;
using Game.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ResolvePreset이 CrouchHeld를 RunHeld보다 먼저 확인해야 한다 — 둘 다 눌려 있으면(앉은 채로 달리기
/// 키를 누르는 실수) 앉아 걷기를 유지해야 한다(docs/design/GAME_DESIGN.md 6.5).
/// </summary>
public sealed class PlayerControllerCrouchTests
{
    private GameObject go;
    private PlayerController controller;
    private GameObject inputGo;
    private PlayerInputReader inputReader;
    private PlayerMovementSettings settings;

    [SetUp]
    public void SetUp()
    {
        settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
        var settingsSo = new SerializedObject(settings);
        settingsSo.FindProperty("walk").FindPropertyRelative("maxSpeedTiles").floatValue = 4f;
        settingsSo.FindProperty("crouch").FindPropertyRelative("maxSpeedTiles").floatValue = 2f;
        settingsSo.ApplyModifiedPropertiesWithoutUndo();

        inputGo = new GameObject("Input");
        inputReader = inputGo.AddComponent<PlayerInputReader>();

        go = new GameObject("Player");
        controller = go.AddComponent<PlayerController>();
        typeof(PlayerController).GetField("inputReader", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, inputReader);
        typeof(PlayerController).GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(controller, settings);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(inputGo);
        Object.DestroyImmediate(settings);
    }

    private void SetCrouchHeld(bool value) =>
        typeof(PlayerInputReader).GetProperty("CrouchHeld").GetSetMethod(true).Invoke(inputReader, new object[] { value });

    private void SetRunHeld(bool value) =>
        typeof(PlayerInputReader).GetProperty("RunHeld").GetSetMethod(true).Invoke(inputReader, new object[] { value });

    [Test]
    public void ResolvePreset_CrouchHeld_ReturnsCrouchPreset_EvenWithoutRun()
    {
        SetCrouchHeld(true);
        SetRunHeld(false);

        MovementPreset result = controller.ResolvePreset();

        Assert.AreEqual(2f, result.MaxSpeedTiles);
    }

    [Test]
    public void ResolvePreset_CrouchAndRunBothHeld_CrouchWins()
    {
        SetCrouchHeld(true);
        SetRunHeld(true);

        MovementPreset result = controller.ResolvePreset();

        Assert.AreEqual(2f, result.MaxSpeedTiles);
    }

    [Test]
    public void ResolvePreset_NeitherHeld_ReturnsWalkPreset()
    {
        SetCrouchHeld(false);
        SetRunHeld(false);

        MovementPreset result = controller.ResolvePreset();

        Assert.AreEqual(4f, result.MaxSpeedTiles);
    }
}
