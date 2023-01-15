using Dapper;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpolisShared.Helpers.Extensions
{
    public static class Main
    {
        //Enums.
        public static IEnumerable<Enum> GetFlags(this Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }

        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static bool IsNullableEnum(this Type t)
        {
            if (t == null) return false;
            Type u = Nullable.GetUnderlyingType(t);
            return (u != null) && u.IsEnum;
        }

        public static Type[] GetNestedTypesRecursive(this Type type, BindingFlags flags = BindingFlags.Public | BindingFlags.Static, Func<Type, bool> validator = null, List<Type> list = null)
        {
            if (list == null) list = new List<Type>();

            IEnumerable<Type> nested = type.GetNestedTypes(flags);
            if (validator != null)
                nested = nested.Where(f => validator.Invoke(f)).ToArray();
            list.AddRange(nested);

            foreach (var f in nested)
            {
                GetNestedTypesRecursive(f, flags, validator, list);
            }

            return list.ToArray();
        }

        //String.
        public static string FormatFrom(this string s, [NotNull] object obj)
        {
            foreach (Match f in Regex.Matches(s, @"(?<=\{)(\w|\d)+"))
            {
                var fProp = obj.GetType().GetProperty(f.Value);
                if (fProp != null)
                    s = s.Replace($"{{{f.Value}}}", fProp.GetValue(obj)?.ToString());
            }
            return s;
        }

        //Reflection.
        public static bool HasBaseType(this Type type, [NotNull] Type searchType)
        {
            if (type == searchType) return true;
            while (type.BaseType != null)
            {
                if (type.BaseType == searchType) return true;
                type = type.BaseType;
            }
            return type.GetInterfaces().Contains(type);
        }

        public static Type GetNotNullable(this Type type)
        {
            var trueType = Nullable.GetUnderlyingType(type);
            trueType ??= type; //Assign if is null.
            return trueType;
        }

        public static PropertyInfo GetPropertyRecursive([NotNull] this Type type, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
        {
            PropertyInfo property = null;
            var currentType = type;

            while (currentType != typeof(object))
            {
                var splitProperty = propertyName.Split('.');

                if (splitProperty.Length > 1)
                {
                    foreach (var fPropertyName in splitProperty)
                    {
                        property = GetPropertyRecursive(currentType, fPropertyName, bindingFlags);
                        if (property != null)
                            break;
                    }

                    if (property != null) break;
                }
                else
                {
                    property = currentType.GetProperty(propertyName, bindingFlags);
                    if (property != null) break;
                    currentType = currentType.BaseType;
                }

            }
            return property;
        }

        public static object GetDefaultValue(this Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            var e = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Default(type), typeof(object)));
            return e.Compile()();
        }

        //String.
        public static string RemovePostfix(this string s, string postfix, bool caseSensative = true)
        {
            if (s == null || postfix == null) return s;
            if (s.Length < postfix.Length) return s;

            var remove = false;

            if (!caseSensative)
                remove = s.Substring(s.Length - postfix.Length, postfix.Length) == postfix;
            else
                remove = s.ToUpper().Substring(s.Length - postfix.Length, postfix.Length) == postfix.ToUpper();

            if (remove)
                s = s.Substring(0, s.Length - postfix.Length);

            return s;
        }

        public static string RemovePrefix(this string s, string prefix, bool caseSensative = true)
        {
            if (s == null || prefix == null) return s;
            if (s.Length < prefix.Length) return s;

            var remove = false;

            if (!caseSensative)
                remove = s.Substring(0, prefix.Length) == prefix;
            else
                remove = s.ToUpper().Substring(0, prefix.Length) == prefix.ToUpper();

            if (remove)
                s = s.Substring(prefix.Length, s.Length - prefix.Length);

            return s;
        }

        public static string RemoveSpecialCharacters(this string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string RemoveSpecialCharactersUsingRegex(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }

        //Dapper.
        public static DynamicValue[] GetAll(this DynamicParameters parameters)
        {
            if (parameters == null) return null;

            var paramsField = typeof(DynamicParameters).GetField("parameters", BindingFlags.Instance | BindingFlags.NonPublic);
            var paramsCollection = (IDictionary)paramsField.GetValue(parameters);

            var list = new List<DynamicValue>(paramsCollection.Count);
            foreach (var f in paramsCollection.Values)
            {
                list.Add(new DynamicValue(f));
            }
            return list.ToArray();
        }

        public class DynamicValue
        {
            public DynamicValue(object param)
            {
                this.Param = param;
            }
            public object Param { get; }
            public string Name
            {
                get => (string)Param.GetType().GetProperty("Name").GetValue(Param);
                set => Param.GetType().GetProperty("Name").SetValue(Param, value);
            }
            public object Value
            {
                get => (object)Param.GetType().GetProperty("Value").GetValue(Param);
                set => Param.GetType().GetProperty("Value").SetValue(Param, value);
            }
            public DbType? DBType
            {
                get => (DbType?)Param.GetType().GetProperty("DbType").GetValue(Param);
                set => Param.GetType().GetProperty("DbType").SetValue(Param, value);
            }
        }

        //Collections
        public static object[] ToObjectArray(this IEnumerable enumerable)
        {
            return (from object f in enumerable select f).ToArray();
        }



        //Other.
        public static IEnumerable<string> GetErrors(this ModelStateDictionary modelState)
        {
            List<string> errors = modelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            foreach (var err in errors)
            {
                yield return Resource.SpolisParameters.ErrorPrefix + err;
            }
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
        (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static Type GetEnumeratedType(this Type type)
        {
            // provided by Array
            var elType = type.GetElementType();
            if (null != elType) return elType;

            // otherwise provided by collection
            var elTypes = type.GetGenericArguments();
            if (elTypes.Length > 0) return elTypes[0];

            // otherwise is not an 'enumerated' type
            return null;
        }

        public static bool IsNonStringEnumerable(this PropertyInfo pi)
        {
            return pi != null && pi.PropertyType.IsNonStringEnumerable();
        }

        public static bool IsNonStringEnumerable(this object instance)
        {
            return instance != null && instance.GetType().IsNonStringEnumerable();
        }

        public static bool IsNonStringEnumerable(this Type type)
        {
            if (type == null || type == typeof(string))
                return false;
            return typeof(IEnumerable).IsAssignableFrom(type);
        }

        public static bool HasItems<T>(this IEnumerable<T> source)
        {
            return (source?.Any() ?? false);
        }

        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }
    }
}

