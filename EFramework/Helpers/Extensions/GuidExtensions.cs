using System;

namespace Spolis.Helpers
{

    public static partial class Extensions
    {
        /// <summary>A GUID extension method that query if '@this' is null or empty.</summary>
        /// <param name="this">The @this to act on.</param>
        /// <returns>true if empty, false if not.</returns>
        public static bool IsNullOrEmpty(this Guid? @this)
        {
            if (@this == Guid.Empty || @this is null)
            {
                return true;
            }
            return false;
        }
    }
    //public static class GuidExtensions
    //{
    //    public static bool IsNullOrEmpty(this Guid? guid)
    //    {
    //        if(guid == Guid.Empty || guid is null)
    //        {
    //            return true;
    //        }
    //        return false;
    //    }

    //}
}
