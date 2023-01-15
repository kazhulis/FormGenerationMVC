using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spolis.Helpers
{
    public static class CustomStringExtensions
    {

        public static string CorrectStringJoin(string Seperator, string[] Items)
        {
            var finalString = new StringBuilder();
            for (var i = 0; i < Items.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(Items[i]))
                {
                    if (i > 0)
                    {
                        finalString.Append($" {Seperator}{Items[i]}");
                    }
                    else finalString.Append(Items[i]);
                }
            }
            return finalString.ToString();
        }
    }
}
