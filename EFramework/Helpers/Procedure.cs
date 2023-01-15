using Dapper;
using Dapper.FluentMap;
using Dapper.FluentMap.Mapping;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SpolisShared.Helpers
{
    public static class Mappings
    {
       

        public static DynamicParameters BuildParametersFromMap<TEntity>(TEntity model, ParameterDirection? parameterDirection = null)
        {
            DynamicParameters parameters = new DynamicParameters();
            var map = GetMap<TEntity>();

            foreach (var f in map.PropertyMaps)
            {
                DbType valType = DbType.Object;
                Enum.TryParse<DbType>(f.PropertyInfo.PropertyType.Name, out valType);
                if (f.PropertyInfo.PropertyType == typeof(byte[])) valType = DbType.Binary;

                if (f.PropertyInfo.PropertyType == typeof(TimeSpan)) valType = DbType.Time;

                if (f.PropertyInfo.PropertyType.GetGenericArguments().Length > 0)
                {
                    Enum.TryParse<DbType>(f.PropertyInfo.PropertyType.GetGenericArguments().First().Name, out valType);
                }

                parameters.Add($"@{f.ColumnName}", f.PropertyInfo.GetValue(model), valType, parameterDirection, size: int.MaxValue);
            }
            return parameters;
        }


        /// <summary>
        /// Builds DynamicParameters object using procedure as base.
        /// Allows automated parameter construction for procedures that does not use all mapped properties from ViewModel. 
        /// </summary>
        /// <typeparam name="TEntity">Generic model type containing mapped properties.</typeparam>
        /// <param name="model">Input ViewModel.</param>
        /// <param name="procedureName">Target procedure.</param>
        /// <returns>List of parameters with values for current procedure.</returns>
        public static DynamicParameters BuildParametersFromProcedure<TEntity>(TEntity model, [NotNull] string procedureName, bool ignoreMissingMaps = false)
        {
            //Get main variables.
            var procedureParams = GetProcedureParameters(procedureName);
            var parameters = new DynamicParameters();
            var map = GetMap<TEntity>();

            //Check received procedure params.
            if (procedureParams == null || !procedureParams.Any())
            {
                throw new ArgumentException($"Procedure '{procedureName}' does not exist or contains no parameters", nameof(procedureName));
            }

            //Validate model for missing maps.
            if (!ignoreMissingMaps)
            {
                var allMapNames = new SortedSet<string>(map.PropertyMaps.Select(f => "@" + f.ColumnName));
                var missingProperties = procedureParams.Where(f => !allMapNames.Contains(f.PARAMETER_NAME));
                if (missingProperties.Any())
                {
                    var message = $"Model of type {typeof(TEntity).Name} does not contain mapped properties as requested by procedure '{procedureName}':" + System.Environment.NewLine;
                    foreach (var f in missingProperties)
                    {
                        message += $"- Procedure parameter '{f.PARAMETER_NAME}' with type '{f.DATA_TYPE}';" + System.Environment.NewLine;
                    }
                    throw new InvalidOperationException(message);
                }
            }

            //Fill Dynamic parameters object.
            var requiredMaps = procedureParams.Select(f => new object[] { f, map.PropertyMaps.FirstOrDefault(ff => "@" + ff.ColumnName == f.PARAMETER_NAME) });
     
            foreach (var f in requiredMaps)
            {
                var param = f[0];
                var prop = (IPropertyMap)f[1];
                if (prop == null) continue;
                var value = prop.PropertyInfo.GetValue(model);

                //Get param type (from model).
                DbType valType = DbType.Object;
                if (prop.PropertyInfo.PropertyType.Name == "Byte[]")
                {
                    valType = DbType.Binary;
                }
                else if (prop.PropertyInfo.PropertyType.Name == "StringList`1")
                {
                    valType = DbType.String;
                    if (value != null)
                        value = value.ToString();
                }
                else if (prop.PropertyInfo.PropertyType.Name == "TimeSpan")
                {
                    valType = DbType.Time;
                 
                }
                else 
                {
                    Enum.TryParse<DbType>(prop.PropertyInfo.PropertyType.Name, out valType);
                    if (prop.PropertyInfo.PropertyType.GetGenericArguments().Length > 0)
                    {
                        Enum.TryParse<DbType>(prop.PropertyInfo.PropertyType.GetGenericArguments().First().Name, out valType);
                    }
                }                

                //Get param direction.
                ParameterDirection? direction = null;
                switch (param.PARAMETER_MODE)
                {
                    case "IN":
                        direction = ParameterDirection.Input;
                        break;
                    case "OUT":
                        direction = ParameterDirection.Output;
                        break;
                    case "INOUT":
                        direction = ParameterDirection.InputOutput;
                        break;
                }

                //Set param.
                parameters.Add($"{param.PARAMETER_NAME}", value, valType, direction, size: int.MaxValue);
            }
            return parameters;
        }


        public static IEnumerable<information_schema> GetProcedureParameters([NotNull] string procedureName)
        {
            //Check params.
            if (string.IsNullOrWhiteSpace(procedureName))
            {
                throw new ArgumentException($"Procedure name cannot be empty!", nameof(procedureName));
            }

            //Get procedure param list.
            IEnumerable<information_schema> procedureParams =null;
            var sql = string.Format(ReadParamsSql, procedureName.Split(".").Last());
            using (var con = Sql.CreateConnection)
            {
                var schema = procedureName.Split(".").First();
                procedureParams = con.Query<information_schema>(string.Format(sql)).ToArray().Where(w => w.SPECIFIC_SCHEMA == schema);
            }             
            return procedureParams;
        }


        private const string ReadParamsSql = "SELECT * FROM information_schema.parameters WHERE SPECIFIC_NAME = '{0}'";

        public class ProcedureParamModel
        {
            public string PARAMETER_NAME { get; set; }
            public string PARAMETER_MODE { get; set; }
            public string DATA_TYPE { get; set; }
        }

        public static void SaveParametersToModel<TEntity>(DynamicParameters parameters, TEntity model)
        {
            var map = GetMap<TEntity>();
            foreach (var f in map.PropertyMaps)
            {
                if (parameters.ParameterNames.Contains(f.ColumnName))
                {
                    var val = parameters.Get<object>(f.ColumnName);

                    if (f.PropertyInfo.SetMethod != null)
                    {
                        if (f.PropertyInfo.PropertyType.Name == "StringList`1")
                        {
                            if (Nullable.GetUnderlyingType(f.PropertyInfo.PropertyType.GetGenericArguments().First()) != null)
                            { 
                                throw new Exception("Cannot use nullable type in stringlist"); 
                            }

                            val ??= String.Empty;
                            val = Activator.CreateInstance(f.PropertyInfo.PropertyType, new[] { val }); // Convert.ChangeType(val, f.PropertyInfo.PropertyType);
                        }                     
                        f.PropertyInfo.SetValue(model, val);
                    }
                }
            }
        }

        public static IEntityMap GetMap<TEntity>()
        {
            var map = FluentMapper.EntityMaps.GetValueOrDefault(typeof(TEntity));
            if (map == null) throw new Exception($"Type '{typeof(TEntity).Name}' has no map assigned.");
            return map;
        }

        public static string GetMapColumnName<TEntity>(string propertyName)
        {
            var map = GetMap<TEntity>();
            return map.PropertyMaps.First(f => f.PropertyInfo.Name == propertyName).ColumnName;
        }

    }
}