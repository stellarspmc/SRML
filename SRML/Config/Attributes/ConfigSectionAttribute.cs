using System;

namespace SRML.Config.Attributes
{
    public class ConfigSectionAttribute : Attribute
    {
        internal string SectionName;
        public ConfigSectionAttribute()
        {
        }

        public ConfigSectionAttribute(string sectionname)
        {
            SectionName = sectionname;
        }
    }
}
