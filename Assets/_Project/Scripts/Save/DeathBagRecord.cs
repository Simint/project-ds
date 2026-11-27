using System;
using System.Collections.Generic;

namespace Game.Save
{
    /// <summary>
    /// 사망 시 떨어뜨린 소지품 기록 (ARCHITECTURE.md 4.8, docs/design/GAME_DESIGN.md 5.8). 마지막 하나만
    /// 남는다 — 새로 죽으면 그대로 덮어쓴다. mapId가 비어 있으면 "주머니 없음"이다(JsonUtility는 참조형
    /// 필드의 null을 보장하지 않으므로, 빈 문자열을 "없음" 표시로 쓴다).
    /// </summary>
    [Serializable]
    public sealed class DeathBagRecord
    {
        public string mapId = "";
        public float x;
        public float y;
        public List<InventorySlotData> items = new List<InventorySlotData>();
    }
}
