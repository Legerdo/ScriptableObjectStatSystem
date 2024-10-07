using UnityEngine;

namespace ScriptableStatSystem
{
    [CreateAssetMenu(fileName = "New String Stat Definition", menuName = "Character Stats/Definition/String Stat Definition")]
    public class StringStatDefinition : NonNumericStatDefinition<string> { }
}