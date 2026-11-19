namespace Game.Core
{
    /// <summary>모든 정의(ScriptableObject)가 갖는 문자열 id. 저장은 에셋 참조 대신 이 id로 한다.</summary>
    public interface IDefinition
    {
        string Id { get; }
    }
}
