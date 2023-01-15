using SpolisShared.Resource;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Spolis.Attributes
{
    public static class ResourceHelper
    {

        public static string GetResourceLookup(Type resourceType, string resourceName)
        {
            if ((resourceType != null) && (resourceName != null))
            {
                PropertyInfo property = resourceType.GetProperty(resourceName, BindingFlags.Public | BindingFlags.Static);
                if (property == null)
                {
                    //throw new InvalidOperationException(string.Format("Resource Type Does Not Have Property"));
                    return null;
                }
                if (property.PropertyType != typeof(string))
                {
                    //throw new InvalidOperationException(string.Format("Resource Property is Not String Type"));
                    return null;
                }
                return (string)property.GetValue(null, null);
            }
            return null;
        }


        public static readonly Type ResourceType = typeof(Resources);
        public static List<Type> ResourceList = new List<Type>();
        public static readonly string ResourceFormat = "{0}{1}"; //"{key}{name}"

        public static string GetResourceValue(string key, string name, bool throwExceptions = true)
        {

            //if resource name is number then label is invisible....??
            if (int.TryParse(name, out _))
            {
                return name;
            }
            ResourceList.Add(ResourceType);
            string resurceName = null;
            string value = null;


            foreach (var f in ResourceList.ToArray())
            {
                if (key == null)
                {
                    resurceName = name;
                    value = ResourceHelper.GetResourceLookup(f, resurceName);
                    //value ??= ResourceHelper.GetResourceLookup(srpResourceType, resurceName);
                    if (value == null)
                    {
                        resurceName = "_" + name;
                        value = ResourceHelper.GetResourceLookup(f, resurceName);
                        //value ??= ResourceHelper.GetResourceLookup(srpResourceType, resurceName);
                    }

                }
                if (value == null)
                {
                    resurceName = string.Format(ResourceFormat, key, name);
                    value = ResourceHelper.GetResourceLookup(f, resurceName);
                    //value ??= ResourceHelper.GetResourceLookup(srpResourceType, resurceName);

                }
                if (value is not null)
                {
                    break;
                }
            }
            if (value == null)
            {
                if (throwExceptions)
                {
                    throw new Exception($"Resource of type {ResourceType.Name} is missing key '{resurceName}'!");
                }
                return name;
            }

            return value;
        }
    }
}

