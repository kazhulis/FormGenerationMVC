using Dapper.FluentMap.Mapping;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Reflection;
using System.Linq;
using SpolisShared.Interfaces;

namespace Spolis.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ModelHeader : Attribute
    {
        public ModelHeader(string flag = null, string source = null, string shema = "skv")
        {
            this.Flag = flag;
            this._source = source;
            this.Shema = shema;
        }
        public string Flag { get; }

        private string _source { get; } = null;
        public string Source { get => (_source != null) ? _source : decodePattern(ViewPattern); }

        public string Shema { get; set; }


        public bool UseDefaultGet { get; set; } = false;
        public bool UseDefaultDelete { get; set; } = false;

        public string ProcedurePattern { get; set; } = "{shema}.spc{flag}{name}";
        public string ViewPattern { get; set; } = "{shema}.vw{flag}";

        public string GetProcedureName(string methodName)
        {
            return decodePattern(ProcedurePattern).Replace("{name}", methodName).Replace("{schema}", Shema);
        }

        private string decodePattern(string s)
        {
            return s.Replace("{flag}", Flag).Replace("{shema}", Shema);
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ModelMap : Attribute
    {
        public ModelMap([CallerMemberName] string columnName = null)
        {
            this.ColumnName = columnName;
        }
        public string ColumnName { get; }
    }

    public static class EntityModelHelper
    {
        public static EntityMap<ViewModel> CreateEntityMap<ViewModel>() where ViewModel : class, iModelMeta, new()
        {
            var map = new BaseMap<ViewModel>();

            foreach (var f in typeof(ViewModel).GetProperties())
            {
                var attrib = f.GetCustomAttributes<ModelMap>().FirstOrDefault();
                if (attrib != null)
                {
                    if (!f.CanWrite) throw  new Exception($"Cannot map model property '{typeof(ViewModel).Name}.{f.Name}' because it has no setter!");
                    map.PropertyMaps.Add(new PropertyMap(f, attrib.ColumnName, false));
                }
            }
            return map;
        }

        public static IEntityMap[] CreateAllEntityMaps()
        {

            IEnumerable<Type> searchTypes = Assembly.GetExecutingAssembly().GetTypes();
            searchTypes = searchTypes.Where(f => f.IsClass && !f.IsNested && !f.IsAbstract && f.IsPublic && f.GetCustomAttribute<ModelHeader>() != null);

            var createMethod = typeof(EntityModelHelper).GetMethod(nameof(CreateEntityMap));
            var maps = searchTypes.Select(f => (IEntityMap)createMethod.MakeGenericMethod(f).Invoke(null, null));
            maps = maps.Where(f => f.PropertyMaps.Count > 0);
            return maps.ToArray();
        }

        private class BaseMap<ViewModel> : EntityMap<ViewModel> where ViewModel : class, iModelMeta, new()
        {
        }
    }

}
