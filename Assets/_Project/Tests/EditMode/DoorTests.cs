using System.Reflection;
using Game.Building;
using Game.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DoorTests
{
    private GameObject root;
    private GameObject visualGo;
    private StructureInstance structureInstance;
    private BoxCollider2D doorCollider;
    private Door door;
    private StructureDefinition definition;
    private Sprite closedSprite;
    private Sprite openSprite;

    [SetUp]
    public void SetUp()
    {
        definition = ScriptableObject.CreateInstance<StructureDefinition>();
        var definitionSo = new SerializedObject(definition);
        definitionSo.FindProperty("footprint").vector2IntValue = new Vector2Int(1, 2);
        definitionSo.ApplyModifiedPropertiesWithoutUndo();

        root = new GameObject("wood_door");
        root.transform.position = Vector3.zero;
        structureInstance = root.AddComponent<StructureInstance>();
        structureInstance.Initialize(definition, Vector2Int.zero);

        doorCollider = root.AddComponent<BoxCollider2D>();
        doorCollider.size = new Vector2(1, 2);
        doorCollider.offset = new Vector2(0.5f, 1f);

        visualGo = new GameObject("Visual");
        visualGo.transform.SetParent(root.transform);
        var renderer = visualGo.AddComponent<SpriteRenderer>();

        var tex = new Texture2D(1, 1);
        closedSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);
        openSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);

        door = root.AddComponent<Door>();
        var doorSo = new SerializedObject(door);
        doorSo.FindProperty("doorCollider").objectReferenceValue = doorCollider;
        doorSo.FindProperty("visual").objectReferenceValue = renderer;
        doorSo.FindProperty("closedSprite").objectReferenceValue = closedSprite;
        doorSo.FindProperty("openSprite").objectReferenceValue = openSprite;
        doorSo.ApplyModifiedPropertiesWithoutUndo();

        // Edit Mode에서는 Unity가 MonoBehaviour의 Awake/Start를 부르지 않는다(Play Mode에서만 돈다) —
        // 직렬화 참조를 다 채운 뒤 Awake를 직접 호출해 실제 Instantiate 흐름을 흉내 낸다.
        InvokeAwake(door);
    }

    private static void InvokeAwake(Object target)
    {
        MethodInfo awake = target.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(awake, $"{target.GetType().Name}.Awake를 리플렉션으로 찾지 못했습니다.");
        awake.Invoke(target, null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(closedSprite.texture);
    }

    private static GameObject CreatePlayerBody(Vector2 position)
    {
        var go = new GameObject("PlayerBody") { layer = GameLayers.Player };
        go.transform.position = position;
        go.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 1.6f);
        Physics2D.SyncTransforms();
        return go;
    }

    [Test]
    public void Interact_TogglesStateAndCollider()
    {
        Assert.AreEqual(0, structureInstance.State);
        Assert.IsTrue(doorCollider.enabled);

        door.Interact(null);
        Assert.AreEqual(1, structureInstance.State);
        Assert.IsFalse(doorCollider.enabled);
        Assert.IsTrue(door.IsOpen);

        door.Interact(null);
        Assert.AreEqual(0, structureInstance.State);
        Assert.IsTrue(doorCollider.enabled);
        Assert.IsFalse(door.IsOpen);
    }

    [Test]
    public void InteractPrompt_SwitchesBetweenOpenAndClose()
    {
        string beforeOpen = door.InteractPrompt;
        door.Interact(null);
        string afterOpen = door.InteractPrompt;

        Assert.AreNotEqual(beforeOpen, afterOpen);
    }

    [Test]
    public void CanClose_FalseWhilePlayerStandsInDoorway()
    {
        door.Interact(null); // 연다
        GameObject player = CreatePlayerBody(new Vector2(0.5f, 1f)); // 문 칸(0..1, 0..2) 안
        try
        {
            Assert.IsFalse(door.CanClose());

            door.Interact(null); // 닫으려 해도 끼임 방지로 막혀야 한다
            Assert.IsTrue(door.IsOpen);
            Assert.IsFalse(doorCollider.enabled);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void CanClose_TrueWhenDoorwayIsClear()
    {
        door.Interact(null); // 연다
        GameObject player = CreatePlayerBody(new Vector2(10f, 10f)); // 문 칸 밖
        try
        {
            Assert.IsTrue(door.CanClose());

            door.Interact(null); // 비어 있으니 닫혀야 한다
            Assert.IsFalse(door.IsOpen);
            Assert.IsTrue(doorCollider.enabled);
        }
        finally
        {
            Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void RefreshVisual_ReflectsRestoredOpenState()
    {
        // BuildGrid.RestoreOne이 Instantiate 이후 State를 되돌리는 상황을 흉내 낸다.
        structureInstance.State = 1;

        door.RefreshVisual();

        Assert.IsFalse(doorCollider.enabled);
        Assert.AreSame(openSprite, visualGo.GetComponent<SpriteRenderer>().sprite);
    }

    [Test]
    public void RefreshVisual_ReflectsRestoredClosedState()
    {
        structureInstance.State = 0;

        door.RefreshVisual();

        Assert.IsTrue(doorCollider.enabled);
        Assert.AreSame(closedSprite, visualGo.GetComponent<SpriteRenderer>().sprite);
    }
}
