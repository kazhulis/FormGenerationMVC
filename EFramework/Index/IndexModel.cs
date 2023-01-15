using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Dapper.FluentMap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Spolis.Attributes;
//using Spolis.Controllers;
//using Spolis.Interface;
//using Spolis.Interface.StoredProcedure;
//using Spolis.ViewModels.Shared;
using SpolisShared.Helpers;
using SpolisShared.Interfaces;
using SpolisShared.Resource;
using static Spolis.Index.hIndexModel;

namespace Spolis.Index
{

    public abstract class hIndexModel
    {
        

        public const string ErrorDisplayMissing = "ErrorDisplayMissing";

        public Dictionary<string, PropertyInfo> Properties { get; protected set; } = new Dictionary<string, PropertyInfo>();
        public Dictionary<string, string> Names { get; } = new Dictionary<string, string>();

        public List<string> FilterKeys { get; } = new List<string>();
        public List<string> GridKeys { get; } = new List<string>();
        public List<string> EditKeys { get; } = new List<string>();

        public Dictionary<string, string> FilterValues { get; } = new Dictionary<string, string>();

        public bool ShowLayout { get; set; } = false;

        public abstract Type ModelType { get; }
        public  Controller Controller { get; protected set; }
        public string Action { get; protected set; }
        public IndexInstructions Instructions { get; protected set; }

        public bool IsKeyVisible(string key)
        {
            var value = AuthorisePolicy(ModelPolicy.ePolicyType.Read);
            if (value)
            {
                var show = Properties[key]?.GetCustomAttribute<Show2>();
                if (show == null) value = false;
                else value = show.IsVisibleIn(LocationToIn(Location), ModelType, Model);
            }
            return value;
        }

        public bool IsKeyEditable(string key)
        {

            var policy = ModelPolicy.ePolicyType.Read;
            switch (Location)
            {
                case eLocation.Create:
                    policy = ModelPolicy.ePolicyType.Create;
                    break;
                case eLocation.Edit:
                    policy = ModelPolicy.ePolicyType.Update;
                    break;
                case eLocation.Delete:
                    policy = ModelPolicy.ePolicyType.Delete;
                    break;
                case eLocation.Other:
                    policy = ModelPolicy.ePolicyType.Custom;
                    break;
            }
            var value = AuthorisePolicy(policy);
            if (value)
            {
                var show = Properties[key]?.GetCustomAttribute<Show2>();
                if (show == null) value = false;
                else value = show.IsEditableIn(LocationToIn(Location), ModelType, Model);
            }
            return value;
        }

        public eLocation Location = eLocation.Other;
        public enum eLocation
        {
            Default,
            Index,
            Create,
            Edit,
            Delete,
            Other
        }

        public static In LocationToIn(eLocation location)
        {
            switch (location)
            {
                case eLocation.Default: return In.All;
                case eLocation.Index: return In.Grid | In.Filter;
                case eLocation.Create: return In.Create;
                case eLocation.Edit: return In.Edit;
                case eLocation.Other: return In.Other;
                default: return In.None;
            }
        }

        public virtual Dictionary<string, object> GenerateEditorControlAttributes(string key)
        {
            var info = Properties[key];
            var attributes = new Dictionary<string, object>();

            Show2 show = info.GetCustomAttribute<Show2>();
            if (show != null && Enum.TryParse(Location.ToString(), false, out In val))
            {
                if (!show.IsEditableIn(val, ModelType, Model))
                {
                    attributes.Add("readonly", "readonly");
                }
            }
            if (Location != eLocation.Index)
            {
                ValidateResource displayResourceAttribute = info.GetCustomAttribute<ValidateResource>();
                displayResourceAttribute?.SetEditorAttributes(attributes);
            }

            return attributes;
        }

        public string GetFilterTemplate(string key)
        {
            var show = GetAttribute<Show2>(key);
            if (show == null)
            {
                return null;
            }
            if (show.IsVisibleIn(In.Filter) == false || show.FilterControl == null)
            {
                return null;
            }
            return show.FilterControl;
        }
       
        public string GetEditTemplate(string key)
        {
            var show = GetAttribute<Show2>(key);
            if (show == null)
            {
                return null;
            }
            if (show.IsVisibleIn(In.Edit) == false || show.EditControl == null)
            {
                return null;
            }
            return show.EditControl;
        }


        public T GetAttribute<T>(string key) where T : Attribute
        {
            return (T)GetAttribute(key, typeof(T));
        }

        public Attribute GetAttribute(string key, Type type)
        {
            if (!typeof(Attribute).IsAssignableFrom(type))
                throw new ArgumentException();
            if (Properties.ContainsKey(key))
            {
                var info = Properties[key];
                var val = info.GetCustomAttribute(type);
                if (val is IAttributeRequiresInitialization initialization) initialization.Initialize(info, this);
                return val;
            }
            return null;
        }


        protected static Dictionary<string, PropertyInfo> findProperties(Type sourceModule)
        {

            var baseTypes = new List<Type>();
            var currentType = sourceModule;
            while (currentType != typeof(object))
            {
                baseTypes.Insert(0, currentType);
                currentType = currentType.BaseType;
            }

            var results = new Dictionary<string, PropertyInfo>();

            foreach (var fType in baseTypes)
            {

                foreach (var f in fType.GetProperties())
                {
                    if (results.ContainsKey(f.Name))
                    {
                        results.Remove(f.Name);
                    }
                    results.Add(f.Name, f);
                }

            }

            return results;
        }

        public static string GetElementName<ViewModel>(string name, string modifier = null)
        {
            var val = $"IndexElement_{name}_{typeof(ViewModel).Name}";
            if (modifier != null) val += "_" + modifier;
            return val;
        }

        public static string GetElementName<ViewModel>(eCommonNames name, string modifier = null)
        {
            if (!Enum.IsDefined(typeof(eCommonNames), name)) throw new InvalidEnumArgumentException();
            var val = GetElementName<ViewModel>(Enum.GetName(typeof(eCommonNames), name));
            if (modifier != null) val += "_" + modifier;
            return val;
        }

        /// <summary>
        /// Generates unique id for for element of this name, based on type.
        /// Set GetElementNameModifier property to generate unique ids for same types and names.
        /// </summary>
        /// <param name="name">Seed name</param>
        /// <returns>String of format: "IndexElement_{name}_{typeof(ViewModel).Name}_{GetElementNameModifier}"</returns>
        public abstract string GetElementName(string name);

        /// <summary>
        /// Generates unique id for for element of this name, based on type.
        /// These enum values are used in generation logic to identify elements.
        /// Set GetElementNameModifier property to generate unique ids for same types and names.
        /// </summary>
        /// <param name="name">Seed name</param>
        /// <returns>String of format: "IndexElement_{name}_{typeof(ViewModel).Name}_{ElementNameModifier}"</returns>
        public abstract string GetElementName(eCommonNames name);

        /// <summary>
        /// Generates specal html comment that idecates generated control start location in html.
        /// This is used by form dynamic update system, for processing html in back-end.
        /// Find all references for this method for more info.
        /// </summary>
        public string GetControlContainerStartTag() => $"<!--CTRLSTART-{GetElementName(eCommonNames.CtrlContainer)}-->";

        /// <summary>
        /// Generates specal html comment that idecates generated control end location in html.
        /// This is used by form dynamic update system, for processing html in back-end.
        /// Find all references for this method for more info.
        /// </summary>
        public string GetControlContainerEndTag() => $"<!--CTRLEND-{GetElementName(eCommonNames.CtrlContainer)}-->";

        /// <summary>
        /// Modifies GetElementName() results to generate unique ids for same types and names.
        /// </summary>
        public string ElementNameModifier { get; set; } = null;

        public enum eCommonNames
        {
            Grid,
            IndexForm,
            FilterForm,
            FilterFormContainer,
            EditForm,
            DeleteForm,
            DeleteWindow,
            CtrlContainer
        }

        public abstract string GetPolicy(ModelPolicy.ePolicyType policyType);
        public bool AuthorisePolicy(ModelPolicy.ePolicyType policyType)
        {
            var value = true;
            var policy = GetPolicy(policyType);
            if (!string.IsNullOrEmpty(policy))
            {
                var authorizationService = Services.GetService<IAuthorizationService>();
                value = authorizationService.AuthorizeAsync(Services.Context.User, policy).Result.Succeeded;
            }
            return value;
        }

        /// <summary>
        /// Gets or sets title of current form.
        /// By setting this value you will override title set on ViewModel by DisplayResourceHeader.
        /// </summary>
        public string Title
        {
            get => _Title ?? GetTitle();
            set => _Title = value;
        }
        private string _Title;
        protected abstract string GetTitle();

        /// <summary>
        /// Gets or sets context of current form.
        /// By setting this value you will override title set on ViewModel by DisplayContext.
        /// </summary>
        public string[] Context
        {
            get => _Context ?? GetContext();
            set => _Context = value;
        }
        private string[] _Context;
        protected abstract string[] GetContext();

        public iModelMeta Model { get; set; }
        public List<Guid> SelectedRowIds { get; set; } = new List<Guid>();

        public Dictionary<string, object> Meta { get; } = new Dictionary<string, object>();

        public string Message { get; set; }

      

        public virtual ReadOnlyDictionary<string, string> ModelMap { get; }
        public virtual ModelHeader ModelHeader { get; }

        public Guid ResultId { get; set; }

    }

    public class IndexModel<TModel> : hIndexModel where TModel : iModelMeta
    {

        public IndexModel(Controller controller, [NotNull] string Action, IndexInstructions<TModel> instructions = null)
        {
            instructions ??= new IndexInstructions<TModel>();

            this.Controller = controller;
            this.Action = Action;
            this.Instructions = instructions;

            var FilterKeysList = FilterKeys;
            var GridKeysList = GridKeys;
            var EditKeysList = EditKeys;

            Properties = findProperties(typeof(TModel));

            foreach (var f in Properties.Values)
            {
                var displayAttribute = f.GetCustomAttribute<DisplayAttribute>();
                string name = displayAttribute?.GetName();
                DisplayResource displayNameAttribute = (DisplayResource)f.GetCustomAttributes(typeof(DisplayResource), false).FirstOrDefault();
                name ??= displayNameAttribute?.DisplayName;

                if (name == null) name = ErrorDisplayMissing;

                Names.Add(f.Name, name);

                Show2 show = f.GetCustomAttribute<Show2>();

                if (show != null)
                {
                    if (show.IsVisibleIn(In.Filter))
                    {
                        FilterKeysList.Add(f.Name);
                        //For filter Default Values
                        string fFilterValue = null;
                        if (show.FilterValue != null && show.FilterValue != "true" && show.FilterValue != "")
                        {
                            fFilterValue = show.GetFilterPropertyValue(show.FilterValue, ModelType, Model);
                        }
                        else if (show.FilterValue == "true")
                        {
                            fFilterValue = show.FilterValue;
                        }
                        FilterValues.Add(f.Name, fFilterValue);
                    }
                    if (show.IsVisibleIn(In.Grid))
                    {
                        GridKeysList.Add(f.Name);
                    }
                    if (show.IsVisibleIn(In.Create) || show.IsVisibleIn(In.Edit))
                    {
                        EditKeysList.Add(f.Name);
                    }
                }
            }
            foreach (var f in Properties.Values)
            {
                var indexAttributes = f.GetCustomAttributes<IndexAttribute>().ToList();
                if (indexAttributes != null)
                {
                    foreach (var indexAttribute in indexAttributes)
                    {
                        var index = (int)indexAttribute.Index;

                        if (indexAttribute != null)
                        {
                            if (index > Properties.Values.Count)
                            {
                                throw new Exception($"Index out of range");
                            }
                            if (indexAttribute.In == In.Filter)
                            {
                                FilterKeysList.Remove(f.Name);
                                if (indexAttribute.Position == IndexAttribute.ePosition.Last) index = FilterKeysList.Count;
                                FilterKeysList.Insert(index, f.Name);
                            }
                            if (indexAttribute.In == In.Grid)
                            {
                                GridKeysList.Remove(f.Name);
                                if (indexAttribute.Position == IndexAttribute.ePosition.Last) index = GridKeysList.Count;
                                GridKeysList.Insert(index, f.Name);
                            }
                            if (indexAttribute.In == (In.Create | In.Edit) || indexAttribute.In == In.Create || indexAttribute.In == In.Edit)
                            {
                                EditKeysList.Remove(f.Name);
                                if (indexAttribute.Position == IndexAttribute.ePosition.Last) index = EditKeysList.Count;
                                EditKeysList.Insert(index, f.Name);
                            }
                        }
                    }
                }
            }
        }

        public TModel ExactModel { get => (TModel)base.Model; set => base.Model = value; }

        public override Type ModelType => typeof(TModel);
          
        public override string GetPolicy(ModelPolicy.ePolicyType policyType)
        {
            var attributes = typeof(TModel).GetCustomAttributes<ModelPolicy>();
            var policy = (from f in attributes where f.Type == policyType select f.Policy).FirstOrDefault();
            if (policy == null) policy = string.Empty;
            return policy;
        }

        protected override string GetTitle()
        {
            var titleAttributes = (IEnumerable<DisplayResourceHeader>)ModelType.GetCustomAttributes(typeof(DisplayResourceHeader), true);
            var target = titleAttributes.FirstOrDefault(f => f.Location == Location);
            if (target != null) return target.DisplayName;
            target = titleAttributes.FirstOrDefault(f => f.Location == eLocation.Default);
            return target?.DisplayName;
        }

        protected override string[] GetContext()
        {
            var contextAttributes = (IEnumerable<DisplayContext>)ModelType.GetCustomAttributes(typeof(DisplayContext), true);
            var targets = contextAttributes.Where(f => f.Location == Location || f.Location == eLocation.Default);

            var results = new List<string>();
            foreach (var f in targets)
            {
                //Find static property.
                var fProp = ModelType.GetProperty(f.PropertyName, BindingFlags.Public | BindingFlags.Static);
                if (fProp == null && Model != null)
                {
                    //Find not static property.
                    fProp = ModelType.GetProperty(f.PropertyName, BindingFlags.Public | BindingFlags.Instance);
                }
                if (fProp == null)
                {
                    throw new Exception($"Cannot find {((Model == null) ? "static" : "")} property '{f.PropertyName}' in {ModelType.Name} as requested by {nameof(DisplayContext)} attribute.");
                }
                results.Add(fProp.GetValue(Model).ToString());
            }

            return results.ToArray();
        }

        public override string GetElementName(string name)
        {
            return GetElementName<TModel>(name, ElementNameModifier);
        }

        public override string GetElementName(eCommonNames name)
        {
            return GetElementName<TModel>(name, ElementNameModifier);
        }


        /// <summary>
        /// Gets database table/view column names linked to property names in ViewModel.
        /// Key = Property name.
        /// Value = Db column name.
        /// Leazy loads collection in static context.
        /// </summary>
        public override ReadOnlyDictionary<string, string> ModelMap
        {
            get
            {
                if (_ModelMap == null) LoadModelMap();
                return _ModelMap;
            }
        }
        private static ReadOnlyDictionary<string, string> _ModelMap = null;
        private static void LoadModelMap()
        {
            var tempMap = new Dictionary<string, string>();

            var map = FluentMapper.EntityMaps.GetValueOrDefault(typeof(TModel));
            if (map == null) throw new Exception($"Type '{typeof(TModel).Name}' has no map assigned.");

            foreach (var f in map.PropertyMaps)
            {
                try
                {
                    tempMap.Add(f.PropertyInfo.Name, f.ColumnName);
                }
                catch (ArgumentException ex)
                {
                    //Copy-paste make fingers chopy-chop
                    throw new ArgumentException($"Check mapping '{f.PropertyInfo.Name}' in '{typeof(TModel).Name}'. Do you have same map on multiple properties?", ex);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            _ModelMap = new ReadOnlyDictionary<string, string>(tempMap);
        }


        public override ModelHeader ModelHeader => typeof(TModel).GetCustomAttribute<ModelHeader>();

    }



    public static class Requests
    {
        private static readonly RegistryD2<int, Guid, ViewResult> ViewRegistry = new RegistryD2<int, Guid, ViewResult>();

        public static Guid Set(int userId, ViewResult view)
        {
            var requestId = Guid.NewGuid();
            ViewRegistry.Set(userId, requestId, view);
            return requestId;
        }

        public static ViewResult Get(int userId, Guid requsetId)
        {
            return ViewRegistry.Get(userId, requsetId);
        }

        public static ViewResult GetFrom<TModel>(int userId) where TModel: iModelMeta
        {

            return ViewRegistry.GetRange(userId).Reverse().FirstOrDefault(f => (f.Model is IndexModel<TModel>));
        }
    }

}








