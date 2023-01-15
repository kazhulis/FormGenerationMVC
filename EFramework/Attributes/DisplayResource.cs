using SpolisShared.Resource;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using static Spolis.Index.hIndexModel;

namespace Spolis.Attributes
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class DisplayResourceHeader : DisplayNameAttribute
    {
        public DisplayResourceHeader(string key = null, eLocation location = eLocation.Default, string name = "Title")
        {
            this.Key = key;
            this.Name = name;
            this.Location = location;
        }

        public string Key { get; }
        public string Name { get; }
        public eLocation Location { get; }

        public override string DisplayName
        {
            get
            {
                if (Key == null)
                {
                    return ResourceHelper.GetResourceValue(Key, Name);
                }

                return ResourceHelper.GetResourceValue(Key, Name + Location.ToString()); 
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class DisplayContext : Attribute
    {
        public DisplayContext(string propertyName, eLocation location = eLocation.Default)
        {
            this.PropertyName = propertyName;
            this.Location = location;
        }

        public string PropertyName { get; }
        public eLocation Location { get; }

    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class DisplayResource : DisplayNameAttribute
    {

        public DisplayResource(string key = null, string group = null, string tab = null, [CallerMemberName] string name = null)
        {
            this.Key = key;
            this.Name = name;
            this.Group = group;
            this.Tab = tab;
        }

        public string Key { get; }
        public string Name { get; }
        public string Group { get; }
        public string Tab { get; }

        public override string DisplayName { get => ResourceHelper.GetResourceValue(Key, Name); }
        public string DisplayGroup { get => (Group != null) ? ResourceHelper.GetResourceValue("", Group) : null; }
        public string DisplayTab { get => (Tab != null) ? ResourceHelper.GetResourceValue("", Tab) : null; }

    }


}
