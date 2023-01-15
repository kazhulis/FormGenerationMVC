using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Dapper;
using Microsoft.Extensions.Configuration;
using Serilog;
using SpolisShared.Resource;
using CommandType = System.Data.CommandType;

namespace SpolisShared.Helpers
{
    public static class Sql
    {
        public static SqlConnection CreateConnection
        {
            get
            {
                var config = Services.GetService<IConfiguration>();
                return new SqlConnection(config.GetConnectionString(SpolisParameters.ConnectionStringName));
            }
        }

        public static IEnumerable<TEntity> Query<TEntity>(string sql, DynamicParameters parameters = null)
        {
            if (string.IsNullOrWhiteSpace(sql)) throw new NotSupportedException();

            if (parameters == null) parameters = new DynamicParameters();

            using (var con = CreateConnection)
            {
                try
                {
                    return con.Query<TEntity>(sql, parameters, null, true, 60);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to execute query!");
                    Log.Error(ex, $"{nameof(TEntity)}.{sql}");
                    throw;
                }
            }
        }

        public static IEnumerable<TFirst> QueryTwo<TFirst, TSecond>(string sql, string propertyName, string splidId, DynamicParameters parameters = null )
        {
            if (string.IsNullOrWhiteSpace(sql)) throw new NotSupportedException();

            if (parameters == null) parameters = new DynamicParameters();

            using (var con = CreateConnection)
            {
                try
                {
                    return con.Query<TFirst, TSecond, TFirst>(sql, (p, c) => { p.GetType().GetProperty(propertyName).SetValue(p,c,null); return p; }, parameters, null,true, splitOn: splidId,  60);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to execute query!");
                    Log.Error(ex, $"{nameof(TFirst)}.{sql}");
                    throw;
                }
            }
        }

        public static IEnumerable<dynamic> Query(string sql, DynamicParameters parameters = null)
        {
            return Query<dynamic>(sql, parameters);
        }

        public static IEnumerable<TEntity2> QueryProcedure<TEntity2>(string procedureName, DynamicParameters parameters)
        {
            if (string.IsNullOrWhiteSpace(procedureName)) throw new NotSupportedException();

            using (var con = CreateConnection)
            {
                try
                {
                    return con.Query<TEntity2>(procedureName, parameters, commandType: CommandType.StoredProcedure);
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to execute procedure!");
                    Log.Error(ex, $"{nameof(TEntity2)}.{procedureName}");
                    throw;
                }
            }
        }


    }

    namespace SqlConditions
    {
        public abstract class SqlCondition
        {
            public abstract string GetQuery();

            public virtual void UpdateParameters(DynamicParameters parameters) { }
        }

        public class SqlSelect : SqlCondition
        {
            public SqlSelect([NotNull] SqlSelectCondition select, SqlWhereCondition where = null, SqlOrderCondition order = null, SqlLimitCondition limit = null)
            {
                this.Select = select;
                this.Where = where;
                this.Order = order;
                this.Limit = limit;

                if (order == null && limit != null)
                {
                    throw new ArgumentException("Parameter 'order' must be set if using limit.");
                }

            }
            public SqlSelectCondition Select { get; }
            public SqlWhereCondition Where { get; }
            public SqlOrderCondition Order { get; }
            public SqlLimitCondition Limit { get; }

            public override string GetQuery()
            {
                return $"{Select.GetQuery()} {Where?.GetQuery()} {Order?.GetQuery()} {Limit?.GetQuery()}";
            }
            public string GetQuery2D(IEnumerable<SQLJoins> sQLJoins)
            {
                var select = Select.GetQuery();
                var sb = new StringBuilder();
                sb.AppendLine(select + " AS vw ");
                foreach (var f in sQLJoins)
                {

                    var joinLine = $"{f.JoinTranslator} {f.Schema}.{f.Table} AS {f.Table.Substring(0, 3)} ON vw.{f.Key} = {f.Table.Substring(0, 3)}.{f.ForeignKey} ";
                    sb.AppendLine(joinLine);
                }
                select = sb.ToString();
                return select;
            }

            public override void UpdateParameters(DynamicParameters parameters)
            {
                Select.UpdateParameters(parameters);
                Where?.UpdateParameters(parameters);
                Order?.UpdateParameters(parameters);
                Limit?.UpdateParameters(parameters);
            }
        }
        public class SQLJoins
        {
            public Joins Join { get; set; }
            public string Table { get; set; }
            public string Schema { get; set; }
            public string ForeignKey { get; set; }
            public string Key { get; set; } = "Id";
            public string JoinTranslator
            {
                get
                {
                    if (Join == Joins.Inner)
                        return " INNER JOIN ";
                    if (Join == Joins.Left)
                        return " LEFT JOIN ";
                    if (Join == Joins.Right)
                        return " RIGHT JOIN ";
                    if (Join == Joins.Full)
                        return " FULL JOIN ";
                    return null;
                }
            }
            public enum Joins
            {
                Inner,
                Left,
                Right,
                Full
            }
        }
        public class SqlSelectCondition : SqlCondition
        {
            public SqlSelectCondition(string source, uint top, string columns, bool distict = false)
           : this(source, top, columns.Split(",").ToList(), distict) { }

            public SqlSelectCondition(string source, uint top = 0, IEnumerable<string> columns = null, bool distict = false)
            {
                this.Source = source;
                this.Top = top;
                this.Columns = columns;
                this.Distinct = distict;
            }

            public string Source { get; }
            public uint Top { get; }
            public IEnumerable<string> Columns { get; }
            public bool Distinct { get; }

            public override string GetQuery()
            {
                var topQuery = "";
                if (Top > 0)
                {
                    topQuery = $"TOP {Top}";
                }

                var columnQuery = "*";
                if (Columns != null && Columns.Any())
                {
                    columnQuery = string.Join(", ", Columns);
                }
                return $"SELECT {(Distinct ? "DISTINCT" : String.Empty)} {topQuery} {columnQuery} FROM {Source}";
            }

        }

        public class SqlWherePartCondition : SqlCondition
        {
            //Pārkopēts no ATD 12.11.2021 15:15
            public static class CommonPatterns
            {
                public static WherePattern IntegerEqual { get; } = new WherePattern("([{0}] = @{0}1)", 1, typeof(Int16), typeof(Int32), typeof(UInt32), typeof(Int64));
                public static WherePattern IntegerBetween { get; } = new WherePattern("([{0}] >= @{0}1 AND [{0}] <= @{0}2)", 2, typeof(Int16), typeof(Int32), typeof(UInt32), typeof(Int64), typeof(float), typeof(double), typeof(decimal));
                public static WherePattern IntegerLarger { get; } = new WherePattern("([{0}] > @{0}1)", 12, typeof(Int16), typeof(Int32), typeof(UInt32), typeof(Int64), typeof(float), typeof(double), typeof(decimal));
                public static WherePattern IntegerSmaller { get; } = new WherePattern("([{0}] < @{0}1)", 1, typeof(Int16), typeof(Int32), typeof(UInt32), typeof(Int64), typeof(float), typeof(double), typeof(decimal));
                public static WherePattern DateTimeBetween { get; } = new WherePattern("([{0}] >= @{0}1 AND [{0}] <= @{0}2)", 2, typeof(DateTime), typeof(DateTimeOffset));
                public static WherePattern TimeSpanBetween { get; } = new WherePattern("([{0}] >= @{0}1 AND [{0}] <= @{0}2)", 2, typeof(TimeSpan));
                public static WherePattern TimeSpanEqual { get; } = new WherePattern("([{0}] = @{0}1)", 1, typeof(TimeSpan));
                public static WherePattern DateTimeBetweenOrIsNull { get; } = new WherePattern("([{0}] is null OR [{0}] >= @{0}1 AND [{0}] <= @{0}2)", 2, typeof(DateTime), typeof(DateTimeOffset));
                public static WherePattern DateTimeEqual { get; } = new WherePattern("([{0}] = @{0}1)", 1, typeof(DateTime), typeof(DateTimeOffset));

                public static WherePattern DateTimeEqualOrLarger { get; } = new WherePattern("([{0}] <= @{0}1)", 1, typeof(DateTime), typeof(DateTimeOffset));
                public static WherePattern DateTimeEqualOrLargerOrDBTimeIsNull { get; } = new WherePattern("([{0}] <= @{0}1 OR [{0}] is null)", 1, typeof(DateTime), typeof(DateTimeOffset));
                public static WherePattern DateTimeSmaller { get; } = new WherePattern("([{0}] > @{0}1)", 1, typeof(DateTime), typeof(DateTimeOffset));
                public static WherePattern DateTimeLarger { get; } = new WherePattern("([{0}] < @{0}1)", 1, typeof(DateTime), typeof(DateTimeOffset));
                public static WherePattern DateTimeEqualOrSmaller { get; } = new WherePattern("([{0}] >= @{0}1)", 1, typeof(DateTime), typeof(DateTimeOffset));
                public static WherePattern DateTimeEqualOrSmallerOrDBTimeIsNull { get; } = new WherePattern("([{0}] >= @{0}1 OR [{0}] is null)", 1, typeof(DateTime), typeof(DateTimeOffset));
                public static WherePattern GuidEqual { get; } = new WherePattern("([{0}] = @{0}1)", 1, typeof(Guid));
                public static WherePattern GuidNotEqual { get; } = new WherePattern("([{0}] != @{0}1)", 1, typeof(Guid));
                public static WherePattern BoolEqual { get; } = new WherePattern("([{0}] = @{0}1)", 1, typeof(bool));
                public static WherePattern StringLike { get; } = new WherePattern("([{0}] LIKE '%' + @{0}1 + '%')", 1, typeof(string));
                public static WherePattern StringLikeLower { get; } = new WherePattern("(LOWER([{0}]) LIKE '%' + LOWER(@{0}1) + '%')", 1, typeof(string));
                public static WherePattern StringEqual { get; } = new WherePattern("([{0}] = @{0}1)", 1, typeof(string));
                public static WherePattern IsNull { get; } = new WherePattern("([{0}] is null)", 1, typeof(string));
                public static WherePattern IsNotNull { get; } = new WherePattern("([{0}] is not null)", 1, typeof(string));
                public static WherePattern StringNotEqual { get; } = new WherePattern("([{0}] != @{0}1)", 1, typeof(string));
                public static WherePattern GuidIntIncludes { get; } = new WherePattern("([{0}] IN (@{0}1))", 1, typeof(Int16[]), typeof(Int32[]), typeof(Int64[]), typeof(Guid[]));
                public static WherePattern GuidIntExcludes { get; } = new WherePattern("NOT ([{0}] IN (@{0}1))", 1, typeof(Int16[]), typeof(Int32[]), typeof(Int64[]), typeof(Guid[]));
                public static WherePattern StringIncludesEqual { get; } = new WherePattern("([{0}] IN  (@{0}1))", 1, typeof(string[]));
                public static WherePattern StringExcludesEqual { get; } = new WherePattern("([{0}] IN (@{0}1))", 1, typeof(string[]));
            }

            public SqlWherePartCondition(string name, object value, WherePattern pattern = null)
           : this(name, new List<object>(new object[] { value }), pattern) { }

            public SqlWherePartCondition(string name, [NotNull] IList values, WherePattern pattern = null)
            {
                if (pattern == null)
                {
                    var actPatterns = CommonWherePatterns.Where(f => f.ParamCount == values.Count);
                    for (var i = 0; i < values.Count; i++)
                    {
                        pattern = actPatterns.Where(f => f.ValueTypes.Contains(values[i]?.GetType()))?.FirstOrDefault();
                        if (pattern != null) break;
                    }
                }

                this.Name = name;
                this.Values = values;
                this.Pattern = pattern ?? throw new Exception("Failed to find pattern!");
            }

            public string Name { get; set; }
            public IList Values { get; set; }
            public WherePattern Pattern { get; set; }


            public override string GetQuery()
            {
                return string.Format(Pattern.Query, Name);
            }

            //public override void UpdateParameters(DynamicParameters parameters)
            //{
            //    if (Values != null)
            //    {
            //        for (var i = 0; i < Values.Count; i++)
            //        {
            //            var f = Values[i];
            //            DbType valType = DbType.Object;
            //            Enum.TryParse<DbType>(f.GetType().Name, out valType);
            //            if (f.GetType().GetGenericArguments().Length > 0)
            //            {
            //                Enum.TryParse<DbType>(f.GetType().GetGenericArguments().First().Name, out valType);
            //            }

            //            parameters.Add($"@{Name}{i + 1}", f, valType, ParameterDirection.Input, size: int.MaxValue);
            //        }
            //    }
            //}
            public override void UpdateParameters(DynamicParameters parameters)
            {
                if (Values != null)
                {
                    for (var i = 0; i < Values.Count; i++)
                    {
                        var f = Values[i];
                        DbType valType = DbType.Object;

                        if (f == null || f.GetType().IsArray)
                        {
                            valType = DbType.Object;
                        }
                        else
                        {
                            Enum.TryParse<DbType>(f.GetType().Name, out valType);
                            if (f.GetType().GetGenericArguments().Length > 0)
                            {
                                Enum.TryParse<DbType>(f.GetType().GetGenericArguments().First().Name, out valType);
                            }
                        }

                        parameters.Add($"{Name}{i + 1}", f, valType, ParameterDirection.Input, size: int.MaxValue);
                    }
                }
            }
            static SqlWherePartCondition()
            {
                CommonWherePatterns.Add(CommonPatterns.IntegerEqual);
                CommonWherePatterns.Add(CommonPatterns.IntegerBetween);
                CommonWherePatterns.Add(CommonPatterns.DateTimeBetween);
                CommonWherePatterns.Add(CommonPatterns.DateTimeEqual);
                CommonWherePatterns.Add(CommonPatterns.GuidEqual);
                CommonWherePatterns.Add(CommonPatterns.BoolEqual);
                CommonWherePatterns.Add(CommonPatterns.GuidIntIncludes);
                CommonWherePatterns.Add(CommonPatterns.StringLike);
                CommonWherePatterns.Add(CommonPatterns.TimeSpanBetween);
                CommonWherePatterns.Add(CommonPatterns.TimeSpanEqual);
            }

            public readonly static List<WherePattern> CommonWherePatterns = new List<WherePattern>();

            public sealed class WherePattern
            {
                public WherePattern(string query, int paramCount, params Type[] valueTypes)
                {
                    this.Query = query;
                    this.ParamCount = paramCount;
                    this.ValueTypes = valueTypes;
                }

                public string Query;
                public int ParamCount;
                public Type[] ValueTypes;
            }



            public static SqlWherePartCondition None { get; } = new SqlWherePartCondition("", null, new WherePattern("(0=1)", 0, null));
        }

        public class SqlWhereCondition : SqlCondition
        {

            public SqlWhereCondition()
            {
                this.Parts = new List<SqlWherePartCondition>();
            }
            public SqlWhereCondition(IList<SqlWherePartCondition> parts)
            {
                this.Parts = parts;
            }
            public SqlWhereCondition(SqlWherePartCondition part)
            {
                this.Parts = new List<SqlWherePartCondition>();
                this.Parts.Add(part);
            }

            public SqlWhereCondition(string name, object value, SqlWherePartCondition.WherePattern pattern = null) :
            this(new SqlWherePartCondition(name, value, pattern))
            { }

            public SqlWhereCondition(string name, [NotNull] IList values, SqlWherePartCondition.WherePattern pattern = null) :
            this(new SqlWherePartCondition(name, values, pattern))
            { }

            public IList<SqlWherePartCondition> Parts { get; }

            public override string GetQuery()
            {
                if (Parts.Any())
                {
                    return $"WHERE " + string.Join($" {Operator.ToString()} ", (from f in Parts select $"({f.GetQuery()})").ToArray());
                }
                return string.Empty;
            }
            public enum eOperator
            {
                AND,
                OR
            }
            public eOperator Operator { get; set; } = eOperator.AND;
            public override void UpdateParameters(DynamicParameters parameters)
            {
                foreach (var f in Parts)
                {
                    f.UpdateParameters(parameters);
                }
            }

        }

        public class SqlOrderCondition : SqlCondition
        {

            public SqlOrderCondition([NotNull] string name, eOrder order = eOrder.ASC)
            {
                if (!(Enum.IsDefined(typeof(eOrder), order)))
                {
                    throw new InvalidEnumArgumentException($"Invalid order.");
                }
                this.Name = name;
                this.Order = order;
            }

            public string Name { get; }
            public eOrder Order { get; }

            public override string GetQuery()
            {
                return $"ORDER BY {Name} {Order.ToString()}";
            }

            public enum eOrder
            {
                ASC = 1,
                DESC = 2
            }

        }

        public class SqlLimitCondition : SqlCondition
        {

            public SqlLimitCondition(int offset, int limit)
            {
                this.Offset = offset;
                this.Limit = limit;
            }

            public int Offset { get; }
            public int Limit { get; }

            public override string GetQuery()
            {
                return $"OFFSET {Offset} ROWS FETCH NEXT {Limit} ROWS ONLY";
            }

        }
    }
}
