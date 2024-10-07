using UnityEngine;

namespace ScriptableStatSystem
{
    [CreateAssetMenu(fileName = "New Float Stat Definition", menuName = "Character Stats/Definition/Float Stat Definition")]
    public class FloatStatDefinition : NumericStatDefinition<float> { }
}