using UnityEngine;

[CreateAssetMenu(fileName = "CaseFileItem", menuName = "CaseFile/Item Data")]
public class CaseFileItemData : ScriptableObject
{
    public string itemID;
    public string itemName;

    [Tooltip("하단 슬롯 아이템 스프라이트")]
    public Sprite itemSprite;

    [Tooltip("중앙에 스폰할 3D 오브젝트 프리팹")]
    public GameObject prefab3D;

    [Tooltip("우측에 표시할 글자 스프라이트")]
    public Sprite textSprite;
}
