using System;
using System.Collections.Generic;
using System.Text;

namespace Spolis.Attributes
{
    [AttributeUsage(validOn: AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DynamicUpdateIgnore : Attribute
    {

    }
}
