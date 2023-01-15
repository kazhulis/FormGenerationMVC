using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using SpolisShared.Resource;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Spolis.Helpers
{
    [Obsolete("Izmantojam lūdzu, JsonRedirect")]
    public class JsonConfirmResult : JsonResult
    {

        public JsonConfirmResult(Guid? id, [NotNull] string responseText, bool success)
            : this(id, new string[] { responseText }, success)
        {
            if (responseText is null)
                throw new ArgumentException("responceText not set!");
        }

        public JsonConfirmResult(Guid? id, [NotNull] IEnumerable<string> responseText, bool success)
            : base(null)
        {
            if (!responseText.Any())
                throw new ArgumentException("responceText not set!");

            this.Id = id;
            this.ResponseText = responseText;
            this.Success = success;

            RebuildValue();
        }

        protected void RebuildValue()
        {
            var dic = new Dictionary<string, object>();
            dic.Add("id", Id);
            dic.Add("responseText", ResponseText);
            dic.Add("success", Success);
            foreach (var f in CustomValues)
            {
                dic.Add(f.Key, f.Value);
            }
            Value = dic;
        }

        public Guid? Id { get; }
        public IEnumerable<string> ResponseText { get; }
        public bool Success { get; }

        protected Dictionary<string, object> CustomValues { get; } = new Dictionary<string, object>();
        public void SetCustomValue(string name, object value)
        {
            if (CustomValues.ContainsKey(name))
            {
                CustomValues.Remove(name);
            }
            CustomValues.Add(name, value);
            RebuildValue();
        }


        
    }


}
