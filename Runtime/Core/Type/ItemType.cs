using UnityEngine;

namespace ScriptableStatSystem
{
    [CreateAssetMenu(fileName = "New Item Type", menuName = "Character Stats/Type/Item Type")]
    public class ItemType : ScriptableObject
    {
        [SerializeField] private string typeName;
        public string TypeName => typeName;
    }
}