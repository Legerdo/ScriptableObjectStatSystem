using UnityEngine;
using System.Collections.Generic;

#if USE_SCRIPTABLE_OBJECT_ARCHITECTURE
using ScriptableObjectArchitecture;
#endif

namespace ScriptableStatSystem
{
    public class NumericStatDefinition<T> : StatDefinition
    {
#if USE_SCRIPTABLE_OBJECT_ARCHITECTURE
        public GenericReference<T, GenericVariable<T>> baseValue;
#else
        public T baseValue;
#endif
        public T min;
        public T max;

        public T GetMin()
        {
            #if USE_SCRIPTABLE_OBJECT_ARCHITECTURE
            if ( baseValue.UseConstant )
            {
                return min;
            }
            else
            {
                return baseValue.Variable.GetMin();
            }
            #else
            return min;     
            #endif
        }

        public T GetMax()
        {
            #if USE_SCRIPTABLE_OBJECT_ARCHITECTURE
            if ( baseValue.UseConstant )
            {
                return max;
            }
            else
            {
                return baseValue.Variable.GetMax();
            }
            #else
            return max;     
            #endif
        }
    }
}