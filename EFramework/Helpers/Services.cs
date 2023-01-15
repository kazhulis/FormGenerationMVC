using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpolisShared.Helpers
{
    public static class Services
    {
        public static T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }
        public static object GetService(Type type)
        {
            return Context.RequestServices.GetService(type);
        }
        public static HttpContext Context => new HttpContextAccessor().HttpContext;
    }


    public class Url
    {
        public Url(string controllerName, string actionName, object @params = null)
        {
            this.ControllerName = controllerName;
            this.ActionName = actionName;
            this.Params = @params;
        }

        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public object Params { get; set; }

        public static Url OfIndex(string controllerName)
        {
            return new Url(controllerName, "Index", new { showLayout = false });
        }

        public static string TrimToShortControllerName(string controllerName)
        {
            if (controllerName.Substring(controllerName.Length - "Controller".Length) == "Controller")
            {
                controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);
            }
            return controllerName;
        }

    }

}
