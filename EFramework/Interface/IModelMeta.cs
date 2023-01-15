using Dapper;
using Dapper.FluentMap;
using Microsoft.Extensions.Configuration;
using Serilog;
using SpolisModels.Attributes;
using SpolisShared.Resource;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using SpolisModels.ViewModels;
using SpolisShared.Helpers;
using SpolisShared.Helpers.SqlConditions;

namespace SpolisShared.Interfaces
{
    public interface iModelMeta
    {
        Guid? Id { get; set; }
    }

    public interface iEditableModelMeta : iModelMeta
    {
        public bool IsNew { get; set; }
        public bool IsUpdated { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsModified => (IsNew || IsUpdated || IsDeleted);
    }

    public abstract class AutoProcedure
    {
        static AutoProcedure()
        {
            var objectType = typeof(object);
            var procedureType = typeof(AutoProcedure);
            var procedureTypeName = procedureType.Name + "`1";

            foreach (var f in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (f.IsClass && !f.IsAbstract && !f.ContainsGenericParameters)
                {
                    var baseType = f.BaseType;
                    while (baseType != objectType)
                    {
                        if (baseType.Name == procedureTypeName && baseType.BaseType == procedureType)
                        {
                            var genericType = baseType.GetGenericArguments().FirstOrDefault();
                            if (genericType != null)
                            {
                                Register.Add(genericType, f);
                                break;
                            }
                        }
                        baseType = baseType.BaseType;
                    }
                }
            }
        }

        protected static Dictionary<Type, Type> Register { get; } = new Dictionary<Type, Type>();

        public static AutoProcedure<ViewModel> Of<ViewModel>() where ViewModel : iModelMeta
        {
            return (AutoProcedure<ViewModel>)Of(typeof(ViewModel));
        }

        public static AutoProcedure Of([NotNull] Type modelType)
        {
            if (!modelType.GetInterfaces().Contains(typeof(iModelMeta)))
                throw new ArgumentException("ModelType must implement iModelMeta.");

            var procType = typeof(AutoProcedure<>).MakeGenericType(modelType);
            if (Register.ContainsKey(modelType)) procType = Register[modelType];
            return (AutoProcedure)Activator.CreateInstance(procType);
        }
    }


    public class AutoProcedure<ViewModel> : AutoProcedure where ViewModel : iModelMeta
    {

        public const string DefaultCheckProcedureName = "[skv].[spcDefaultCheckProcedure]";

        public virtual ViewModel Get(Guid id)
        {
            string sql;
            DynamicParameters parameters = new DynamicParameters();

            var procedureName = GetProcedureName(nameof(Get));
            if (CheckProcedure(procedureName))
            {
                //Executed dedicated get procedure.
                parameters.Add("@Id", id);
                var qResult = Helpers.Sql.QueryProcedure<ViewModel>(procedureName, parameters);
                return qResult.FirstOrDefault();
            }
            else
            {
                //Execute generated get query.
                var select = new SqlSelectCondition(DataSource, 1);
                var wherePart = new SqlWherePartCondition("Id", id);
                var where = new SqlWhereCondition(new SqlWherePartCondition[] { wherePart });
                var query = new SqlSelect(select, where);
                query.UpdateParameters(parameters);
                sql = query.GetQuery();
                var qResult = Helpers.Sql.Query<ViewModel>(sql, parameters);
                return qResult.FirstOrDefault();
            }

        }

        public virtual IEnumerable<ViewModel> GetRange(SqlWhereCondition where = null, SqlOrderCondition order = null, SqlLimitCondition limit = null)
        {
            string sql;
            DynamicParameters parameters = new DynamicParameters();

            var select = new SqlSelectCondition(DataSource);
            var query = new SqlSelect(select, where, order, limit);

            query.UpdateParameters(parameters);
            sql = query.GetQuery();

            var qResult = Helpers.Sql.Query<ViewModel>(sql, parameters);
            return qResult;

        }



        public virtual string DataSource
        {
            get
            {
                return typeof(ViewModel).GetCustomAttribute<ModelHeader>().Source;
            }
        }
        public virtual string DataSchema
        {
            get
            {
                return typeof(ViewModel).GetCustomAttribute<ModelHeader>().Shema;
            }
        }

        public virtual int GetCount(SqlWhereCondition where)
        {
            string sql;
            DynamicParameters parameters = new DynamicParameters();

            var select = new SqlSelectCondition(DataSource, 0, "COUNT(*)");
            var query = new SqlSelect(select, where);

            query.UpdateParameters(parameters);
            sql = query.GetQuery();

            var qResult = Sql.Query<int>(sql, parameters);

            return qResult.First();

        }

        public virtual Guid? Insert(ViewModel model)
        {
            //if (model.Id != null) throw new ArgumentException("Model already has Id.");
            string procedureName = null;

            if (CheckProcedure(GetProcedureName(nameof(Insert))))
            {
                procedureName = GetProcedureName(nameof(Insert));
                var idParam = Mappings.GetProcedureParameters(procedureName).FirstOrDefault(f => f.PARAMETER_NAME == "@Id");
                if (idParam == null || !idParam.PARAMETER_MODE.Contains("OUT"))
                {
                    throw new InvalidOperationException($"Insert procedure '{procedureName}' requires output parameter called '@Id'!");
                }
                var parameters = Mappings.BuildParametersFromProcedure<ViewModel>(model, procedureName);
                Sql.QueryProcedure<ViewModel>(procedureName, parameters);
                Mappings.SaveParametersToModel(parameters, model);
                return model.Id;
            }
            else if (CheckProcedure(GetProcedureName("Save")))
            {
                model.Id = Save(model);
                return model.Id;
            }
            else
            {
                return DirectInsert(model);
            }

        }
        protected Guid? DirectInsert(ViewModel model, string customSource = null)
        {
            customSource ??= DataSource;

            model.Id = Guid.NewGuid();
            var parameters = Mappings.BuildParametersFromMap(model);
            var map = Mappings.GetMap<ViewModel>();
            var columnNames = map.PropertyMaps.Select(f => f.ColumnName);
            var paramNames = columnNames.Select(f => "@" + f);
            var sql = $"INSERT INTO {customSource} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", paramNames)})";
            Sql.Query<Guid>(sql, parameters);
            return model.Id;
        }


        public virtual void Update(ViewModel model)
        {
            if (model.Id == null) throw new ArgumentException("Model does not have Id.");

            string procedureName = null;
            if (CheckProcedure(GetProcedureName(nameof(Update))))
            {
                procedureName = GetProcedureName(nameof(Update));
                var parameters = Mappings.BuildParametersFromProcedure<ViewModel>(model, procedureName);
                Sql.QueryProcedure<ViewModel>(procedureName, parameters);
                Mappings.SaveParametersToModel(parameters, model);
            }
            else if (CheckProcedure(GetProcedureName("Save")))
            {
                Save(model);
            }
            else
            {
                DirectUpdate(model);
            }
        }
        protected Guid? DirectUpdate(ViewModel model, string customSource = null)
        {
            customSource ??= DataSource;

            var parameters = Mappings.BuildParametersFromMap(model);
            var map = Mappings.GetMap<ViewModel>();
            var columnNames = map.PropertyMaps.Select(f => $"{f.ColumnName} = @{f.ColumnName}");
            var sql = $"UPDATE {DataSource} SET {string.Join(", ", columnNames)} WHERE Id = @Id";
            Helpers.Sql.Query<Guid>(sql, parameters);
            return model.Id;
        }

        public virtual Guid? Save(ViewModel model)
        {
            CheckProcedure(GetProcedureName(nameof(Save)), true);
            var procedureName = GetProcedureName(nameof(Save));
            var parameters = Mappings.BuildParametersFromProcedure<ViewModel>(model, procedureName, true);
            var idParam = Mappings.GetProcedureParameters(procedureName).FirstOrDefault(f => f.PARAMETER_NAME == "@OutId");
            if (idParam == null || !idParam.PARAMETER_MODE.Contains("OUT"))
            {
                throw new InvalidOperationException($"Save procedure '{procedureName}' requires output parameter called '@OutId'!");
            }
            parameters.Add("@OutId", dbType: DbType.Guid, direction: ParameterDirection.Output);
            Sql.QueryProcedure<ViewModel>(procedureName, parameters);
            model.Id = parameters.Get<Guid?>("@OutId");
            return model.Id;
        }


        public virtual string Validate(ViewModel model)
        {

            string procedureName = GetProcedureName(nameof(Validate));
            if (CheckProcedure(procedureName))
            {
                var errorParam = Mappings.GetProcedureParameters(procedureName).FirstOrDefault(f => f.PARAMETER_NAME == "@Error");
                if (errorParam == null || !errorParam.PARAMETER_MODE.Contains("OUT"))
                {
                    throw new InvalidOperationException($"Validate procedure '{procedureName}' requires output parameter called '@Error'!");
                }

                var parameters = Mappings.BuildParametersFromProcedure(model, procedureName, true);
                parameters.Add("@Error", value: string.Empty, dbType: DbType.String, direction: ParameterDirection.Output);

                Sql.QueryProcedure<ViewModel>(procedureName, parameters);
                return parameters.Get<string>("@Error");
            }

            return null;
        }

        public virtual bool Delete(Guid id)
        {
            DynamicParameters parameters = new();
            parameters.Add("@Id", id);

            var procedureName = GetProcedureName(nameof(Delete));
            if (CheckProcedure(procedureName))
            {
                var qResult = Sql.QueryProcedure<ViewModel>(procedureName, parameters);
                return (qResult != null);
            }
            else
            {
                return DirectDelete(id);
            }
        }
        public virtual bool DirectDelete(Guid id, string customSource = null)
        {
            DynamicParameters parameters = new();
            parameters.Add("@Id", id);
            customSource ??= DataSource;

            var dataSource = typeof(ViewModel).GetCustomAttribute<ModelHeader>().Source;
            if (customSource != null) dataSource = customSource;
            var sql = $"DELETE FROM {dataSource} WHERE Id = @Id";
            var qResult = Sql.Query<Guid>(sql, parameters);
            return (qResult != null);

        }

        public virtual bool DeleteRange([NotNull] SqlWhereCondition where, string customSource = null)
        {
            customSource ??= DataSource;
            DynamicParameters parameters = new();
            where.UpdateParameters(parameters);
            var sql = $"DELETE FROM {customSource} {where.GetQuery()}";
            var qResult = Sql.Query(sql, parameters);
            return (qResult != null);
        }

        protected string GetProcedureName(string methodName)
        {
            var procedureAttribute = typeof(ViewModel).GetCustomAttribute<ModelHeader>();
            return procedureAttribute.GetProcedureName(methodName);
        }


        protected bool CheckProcedure(string procedureName, bool throwOnError = false)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ProcedureName", procedureName.Split(".")[1]);
            parameters.Add("@ProcedureSchema", procedureName.Split(".")[0]);
            parameters.Add("@Result", value: 0, dbType: DbType.Int32, direction: ParameterDirection.Output);

            Sql.QueryProcedure<ViewModel>(DefaultCheckProcedureName, parameters);
            var val = parameters.Get<int>("@Result");
            var procedureExists = (val > 0);
            if (!procedureExists && throwOnError)
            {
                throw new ArgumentException($"Procedure {procedureName} does not exist!");
            }

            return procedureExists;
        }

        public ViewModel ExecuteProcedure(string procedureName, ViewModel model)
        {
            CheckProcedure(procedureName, true);
            var parameters = Mappings.BuildParametersFromProcedure<ViewModel>(model, procedureName);
            Sql.QueryProcedure<ViewModel>(procedureName, parameters);
            Mappings.SaveParametersToModel(parameters, model);
            return model;
        }
        public void ExecuteDeleteProcedure(string procedureName, Guid id)
        {
            CheckProcedure(procedureName, true);
            var parameters = new DynamicParameters();
            parameters.Add("@Id", id);
            Sql.QueryProcedure<ViewModel>(procedureName, parameters);
        }

    }

}
