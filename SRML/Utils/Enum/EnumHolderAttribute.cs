using System;

namespace SRML.Utils.Enum
{
    public abstract class EnumHolderAttribute : Attribute
    {
        internal readonly bool shouldCategorize = true;

        public EnumHolderAttribute() { }
        public EnumHolderAttribute(bool shouldCategorize) => this.shouldCategorize = shouldCategorize;
    }
}
