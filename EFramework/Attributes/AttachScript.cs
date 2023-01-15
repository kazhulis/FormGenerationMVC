using System;
using System.Collections.Generic;
using System.Text;
using static Spolis.Index.hIndexModel;

namespace Spolis.Attributes
{
[AttributeUsage(validOn: AttributeTargets.Class, AllowMultiple =true, Inherited =true)]
   public class AttachScript : Attribute
    {
        public AttachScript(string scriptName, eLocation location = eLocation.Default)
        {
            this.ScriptName = scriptName;
            this.Location = location;
        }
        public string ScriptName { get; }
        public eLocation Location { get; }
    }
}
