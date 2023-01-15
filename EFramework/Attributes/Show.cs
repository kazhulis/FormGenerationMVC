using Microsoft.AspNetCore.Http;
using Spolis.Helpers;
using Spolis.Index;
using SpolisShared.Helpers;
using SpolisShared.Helpers.Extensions;
using SpolisShared.Interfaces;
using SpolisShared.Resource;
using SpolisShared.Templates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;


namespace Spolis.Attributes
{
    public class Show3 : Show2
    {
        public Show3(In visibleIn = In.All, In editableIn = In.Filter | In.Create | In.Edit | In.Other,
            string visibleIf = null, string editableIf = null, string settingsPopertyName = "{key}Settings") : base(Show2.eTemplate2.Show3, null, visibleIn, editableIn, visibleIf, editableIf)
        {
            SettingsPopertyName = settingsPopertyName;

        }
        public string SettingsPopertyName { get; set; }
        public ControlSettings Settings { get; protected set; }

        public override void Initialize(PropertyInfo info, hIndexModel indexModel)
        {
            
            var propInfo = indexModel.ModelType.GetProperty(SettingsPopertyName.Replace("{key}", info.Name));
            if (propInfo is not null)
                //Ignore non static settings in static context(indexModel.Model is null)
                if (!propInfo.GetMethod.IsStatic && indexModel.Model is null)
                    Settings = null;
                else
                    Settings = (ControlSettings)propInfo.GetValue(indexModel.Model);

            Settings ??= ControlSettings.DefaultControls.FirstOrDefault(f => f.ValidFor(info.PropertyType));
            EditControl = Settings.EditControl;
            FilterControl = Settings.FilterControl;
            Format = Settings.GridFormat;
            ControlWidth = Settings.ControlWidth;
        }
    }
    public class ControlSettings
    {
        public static ControlSettings[] DefaultControls;

        static ControlSettings()
        {
            var controlTypes = Assembly.GetExecutingAssembly().GetTypes().Where(f => f.HasBaseType(typeof(ControlSettings)))
                   .Where(f => f.GetConstructors().Any(ff => !ff.GetParameters().Where(fff => !fff.IsOptional).Any()));
            DefaultControls = controlTypes.Select(f => (ControlSettings)Activator.CreateInstance(f)).OrderBy(f => f.Priority).ToArray();
        }
        public virtual int Priority { get; } = 999;
        public string EditControl { get; set; }
        public string FilterControl { get; set; }
        public string GridControl { get; set; }
        public int? ColumnWidth { get; set; } = null;
        public string GridFormat { get; set; }
        public byte ControlWidth { get; set; } = 6;
        public virtual bool ValidFor(Type type)
        {
            return true;
        }
    }
    public class DropDownListSettings : ControlSettings
    {
        public DropDownListSettings()
        {
            FilterControl = FilterTemplates.DropDownList;
            EditControl = EditTemplates.DropDownList;

        }
    }
    public class EditableGridSettings : ControlSettings
    {
        public string ControllerName { get; set; }
        public EditableGridSettings(string controllerName)
        {
            GridControl = EditTemplates.EditableGrid;
            this.ControllerName = controllerName;
        }
    }
    public class DateTimeSettings : ControlSettings
    {
        public DateTimeSettings()
        {
            FilterControl = FilterTemplates.DatePicker;
            EditControl = EditTemplates.DatePicker;
            GridFormat = SpolisParameters.KendoDateFormat;
        }
        public bool Large { get; set; } = false;
        public bool ShowLabel { get; set; } = true;
        public bool TimePicker { get; set; } = false;
        public DateTime? Min { get; set; } = new DateTime(1900, 1, 1);
        public DateTime? Max { get; set; } = new DateTime(2099, 12, 31);
        public override int Priority => 0;

        protected static HashSet<Type> SupportedTypes = new HashSet<Type>(new Type[] {
        typeof(DateTime),
        typeof(DateTime?),
        typeof(DateTimeOffset),
        typeof(DateTimeOffset?)
        });
        public override bool ValidFor(Type type) => SupportedTypes.Contains(type);
    }
    public class TextboxSettings : ControlSettings
    {
        public TextboxSettings()
        {
            FilterControl = FilterTemplates.TextBox;
            EditControl = EditTemplates.TextBox;
        }
        public string PlaceHolder { get; set; } = "";
        public bool MultiLine { get; set; } = false;
        public bool ShowLabel { get; set; } = true;
        public bool isEmpty { get; set; } = false;
        public override int Priority => 0;
        public override bool ValidFor(Type type) => (type == typeof(string));

    }
    public class RadioButtonSettings : ControlSettings
    {
        public RadioButtonSettings()
        {
            //FilterControl = FilterTemplates.RadioButton;
            EditControl = EditTemplates.RadioButton;
        }
        public bool OnSameRow { get; set; } = false;

    }
    public class NumberPickerSettings : ControlSettings
    {
        public NumberPickerSettings()
        {
            FilterControl = FilterTemplates.NumberPicker;
            EditControl = EditTemplates.NumberPicker;
        }
        public int Step { get; set; } = 1;
        public int? Max { get; set; } = null;
        public int? Min { get; set; } = null;
        protected static HashSet<Type> SupportedTypes = new HashSet<Type>(new Type[] {
        typeof(int),
        typeof(int?),
        typeof(double),
        typeof(double?),
        typeof(float),
        typeof(float?)
        });
        public override bool ValidFor(Type type) => SupportedTypes.Contains(type);

    }
    public class TextAreaSettings : ControlSettings
    {
        public TextAreaSettings()
        {
            FilterControl = FilterTemplates.TextAreaShow3;
            EditControl = EditTemplates.TextAreaShow3;
        }
        public string PlaceHolder { get; set; } = "";
        public double Rows { get; set; } = 1;
        public double MaxLength { get; set; } = 1000;
        public override bool ValidFor(Type type) => (type == typeof(string));

    }

    public class ColorPickerSettings : ControlSettings
    {
        public ColorPickerSettings()
        {
           
            EditControl = EditTemplates.ColorPicker;
        }
      
        public override bool ValidFor(Type type) => (type == typeof(string));
        public string[] Palette = new string[] { SpolisParameters.DisabledColor , "#FFFF00", "#90EE90", "#ADD8E6", "#FF0000",
                "#EE82EE", "#FFA500", "#A52A2A", "#808080",
                "#eddeed", "#999666", "#5000ff"};

    }
    public class IntegerSettings : ControlSettings
    {
        public IntegerSettings()
        {
            FilterControl = FilterTemplates.NumberPickerShow3;
            EditControl = EditTemplates.NumberPicker;
        }
        public int Min { get; set; } = 0;
        public double Max { get; set; } = int.MaxValue;
        public override bool ValidFor(Type type) => (type == typeof(int));

    }

    public class GroupSettings : ControlSettings
    {
        public GroupSettings()
        {
            EditControl = EditTemplates.Group;
        }
    }
    public class SwitchSettings : ControlSettings, iSettingsWithSource
    {
        public SwitchSettings()
        {
            FilterControl = FilterTemplates.Default;
            EditControl = EditTemplates.Switch;
            SelectList = SelectListTemplates.YesNoValues();

        }
        public SelectList<bool> SelectList { get; set; }
        public override int Priority => 0;
        public override bool ValidFor(Type type) => (type == typeof(bool));
        public int Width { get; set; } = 65;
    }

    public interface iSettingsWithSource
    {
        SelectList<bool> SelectList { get; set; }
    }

    [Obsolete("Use show2, bļe")]
    public class Show : Show2
    {
        public Show(eTemplate template = eTemplate.None,
            bool inGrid = false, bool inFilter = false, bool inEdit = false,
            string filterControl = null, string editControl = null,
            eEditState editStates = eEditState.All, string sourcePropertyName = null,
            string conditionPropertyName = null) : base((eTemplate2)template, sourcePropertyName, In.None, In.None,
            conditionPropertyName, null, filterControl, editControl)
        {
            if (inFilter) this.VisibleIn += (int)In.Filter;
            if (inGrid) this.VisibleIn += (int)In.Grid;
            if (inEdit) this.VisibleIn += (int)In.Edit + (int)In.Create + (int)In.Other;

            if (editStates == eEditState.All) EditableIn = In.All;
            else if (editStates == eEditState.None) EditableIn = In.None;
            else
            {
                if ((editStates & eEditState.Create) == eEditState.Create) this.EditableIn += (int)In.Create;
                if ((editStates & eEditState.Edit) == eEditState.Edit) this.EditableIn += (int)In.Edit;
                if ((editStates & eEditState.Other) == eEditState.Other) this.EditableIn += (int)In.Other;
            }
        }

        public enum eTemplate
        {
            None = 1,
            TextBox = 2,
            TextArena = 3,
            DatePicker = 4,
            NumberPicker = 5,
            DropDownList = 6,
            Hidden = 7,
            WhiteSpace = 8,
            IndexLink = 9
        }

        [Flags]
        public enum eEditState
        {
            All = 0,
            Create = 1,
            Edit = 2,
            Other = 4,
            None = 8
        }
    }

    [DisplayName("Index")]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class IndexAttribute : Attribute
    {
        public IndexAttribute(In @in, uint index, ePosition position = ePosition.None)
        {
            In = @in;
            Index = index;
            Position = position;
        }

        public In In { get; }
        public uint Index { get; }
        public ePosition Position { get; }
        public enum ePosition
        {
            None = 0,
            First = 1,
            Last = 2
        }

        public override object TypeId { get { return this; } }
    }


    /// <summary>
    /// Instructs IndexController views to draw value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Show2 : Attribute, IAttributeRequiresInitialization
    {
        /// <summary>
        /// Instructs IndexController views to draw value.
        /// </summary>
        /// <param name="template">
        /// Sets filter, grid and edit templates for current value.
        /// Default value = Auto - Sets templates based on property type and sourcePropertyName.
        /// See Show2.ApplyTemplate() for reference.
        /// </param>
        /// <param name="sourcePropertyName">
        /// Sets source values to display in DropDownList.
        /// If template is Auto, by setting this property, template will be applied as DropDownList.
        /// This must be name static or not static property in same ViewModel, that returns SelectList<T>.
        /// </param>
        /// <param name="visibleIn"></param>
        /// Sets in what views this value must be visible.
        /// This is flag enum. Combine values like "In.Filters | In.Grid".
        /// <param name="editableIn"></param>
        /// Sets in what views this value must be editable.
        /// This is flag enum. Combine values like "In.Filters | In.Grid".
        /// This is not applicable for Filters and Grid.
        /// <param name="visibleIf"></param>
        /// Sets visible condition to display property.
        /// This must be name static or not static property in same ViewModel, that returns bool.
        /// This condition will be validated only for locations included in visibleIn.
        /// For changes to take effect in runtime, view must be reloaded.
        /// <param name="editableIf"></param>
        /// Sets editable condition to display property.
        /// This must be name static or not static property in same ViewModel, that returns bool.
        /// This condition will be validated only for locations included in editableIn.
        /// For changes to take effect in runtime, view must be reloaded.
        /// <param name="filterControl"></param>
        /// Allows set custom control for filter.
        /// This must be relative location of View: "~\Views\Example\ExampleControl".
        /// This will override control set by template.
        /// <param name="editControl">
        /// Allows set custom control for edit.
        /// This must be relative location of View: "~\Views\Example\ExampleControl".
        /// This will override control set by template.
        /// </param>
        /// <param name="controlWidth">
        /// Control width in bootstarp values (1-12).
        /// 0 is auto.
        /// </param>
        public Show2(eTemplate2 template = eTemplate2.Auto, string sourcePropertyName = null,
            In visibleIn = In.All, In editableIn = In.Filter | In.Create | In.Edit | In.Other,
            string visibleIf = null, string editableIf = null,
            string filterControl = null, string editControl = null,
            byte controlWidth = 0, string filterValue = null, string filterValueEnable = null, int columnWidth = 0)
        {
            Template = template;
            SourcePropertyName = sourcePropertyName;

            VisibleIn = visibleIn;
            VisibleIf = visibleIf;

            EditableIn = editableIn;
            EditableIf = editableIf;

            FilterControl ??= filterControl;
            EditControl ??= editControl;

            FilterValue = filterValue;
            FilterValueEnable = filterValueEnable;
            ColumnWidth = columnWidth;

            if (controlWidth > 12) throw new ArgumentException("Value cannot be larger then 12!", nameof(controlWidth));
            ControlWidth = controlWidth;
        }

        public bool IsVisibleIn(In @in)
        {
            if (VisibleIn == In.None) return false;
            if ((VisibleIn & In.All) == In.All) return true;
            return @in.GetFlags().Any(f => VisibleIn.GetFlags().Contains(f) && (In)f != In.None);
            //return ((VisibleIn & @in) == VisibleIn || (VisibleIn & @in) == @in);
        }

        public bool IsEditableIn(In @in)
        {
            if (EditableIn == In.None) return false;
            if ((EditableIn & In.All) == In.All) return true;
            return @in.GetFlags().Any(f => EditableIn.GetFlags().Contains(f) && (In)f != In.None);
            //return ((EditableIn & @in) == @in);
        }

        public bool IsVisibleIn(In @in, [NotNull] Type modelType, iModelMeta model)
        {
            return IsVisibleIn(@in) && GetBoolPropertyValue(VisibleIf, modelType, model);
        }

        public bool IsEditableIn(In @in, [NotNull] Type modelType, iModelMeta model)
        {
            return IsEditableIn(@in) && GetBoolPropertyValue(EditableIf, modelType, model);
        }


        private eTemplate2 GetAutoTemplate(PropertyInfo info)
        {

            //Dropdown list, with server side filtering
            if (!string.IsNullOrEmpty(SourcePropertyName))
            {
                var sourceProperty = info.DeclaringType.GetProperty(SourcePropertyName);
                if (sourceProperty?.PropertyType == typeof(DropDownListFilterSource))
                    return eTemplate2.FilterDropDownList;
            }

            //Dropdwon list, which does not have server side filtering
            if (!string.IsNullOrEmpty(SourcePropertyName))
            {
                if (info.PropertyType.Name == typeof(StringList<>).Name)
                    return eTemplate2.MultiDropDownList;
                else
                    return eTemplate2.DropDownList;
            }


            if (info.PropertyType == typeof(string))
            {
                return eTemplate2.TextBox;
            }

            if (info.PropertyType == typeof(DateTime) || Nullable.GetUnderlyingType(info.PropertyType) == typeof(DateTime))
            {
                return eTemplate2.DatePicker;
            }

            if (info.PropertyType == typeof(Guid) || Nullable.GetUnderlyingType(info.PropertyType) == typeof(Guid))
            {
                return eTemplate2.Hidden;
            }

            if (info.PropertyType == typeof(IFormFile) || Nullable.GetUnderlyingType(info.PropertyType) == typeof(IFormFile))
            {
                return eTemplate2.FileUpload;
            }

            if (info.PropertyType.IsValueType)
            {
                var type = info.PropertyType;
                if (Nullable.GetUnderlyingType(type) != null)
                {
                    type = Nullable.GetUnderlyingType(type);
                }

                var val = Activator.CreateInstance(type);
                if (int.TryParse(val.ToString(), out var newVal))
                {
                    if (newVal == 0) return eTemplate2.NumberPicker;
                }
            }

            if (info.PropertyType.IsValueType && Nullable.GetUnderlyingType(info.PropertyType) == null)
            {
                var val = Activator.CreateInstance(info.PropertyType);
                if (int.TryParse(val.ToString(), out var newVal))
                {
                    if (newVal == 0) return eTemplate2.NumberPicker;
                }
            }

            if (info.PropertyType == typeof(Url))
            {
                return eTemplate2.IndexLink;
            }

            if (info.PropertyType == typeof(Link))
            {
                return eTemplate2.Button;
            }

            return eTemplate2.None;
        }

        public virtual void Initialize(PropertyInfo info, hIndexModel indexModel)
        {
            var tempFilterControl = FilterControl;
            var temEditControl = EditControl;
            if (Template == eTemplate2.Auto)
            {
                ApplyTemplate(GetAutoTemplate(info));
            }
            else
            {
                ApplyTemplate(Template);
            }

            if (!string.IsNullOrEmpty(tempFilterControl)) FilterControl = tempFilterControl;
            if (!string.IsNullOrEmpty(temEditControl)) EditControl = temEditControl;

        }

        private void ApplyTemplate(eTemplate2 template)
        {

            switch (template)
            {
                case eTemplate2.TextBox:
                    FilterControl = FilterTemplates.TextBox;
                    EditControl = EditTemplates.TextBox;
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;

                case eTemplate2.TextArena:
                    FilterControl = FilterTemplates.TextArea;
                    EditControl = EditTemplates.TextArea;
                    if (ControlWidth == 0) ControlWidth = 12;
                    break;

                case eTemplate2.DatePicker:
                    FilterControl = FilterTemplates.DatePicker;
                    EditControl = EditTemplates.DatePicker;
                    Format ??= SpolisParameters.KendoDateFormat;
                    GridControl = "KendoDatePicker";
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;

                case eTemplate2.NumberPicker:
                    FilterControl = FilterTemplates.NumberPicker;
                    EditControl = EditTemplates.NumberPicker;
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;

                case eTemplate2.DropDownList:
                    FilterControl = FilterTemplates.DropDownList;
                    EditControl = EditTemplates.DropDownList;
                    GridControl = "KendoDropDown";
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;
                case eTemplate2.FilterDropDownList:
                    //FilterControl = FilterTemplates.DropDownListServerFilter;
                    EditControl = EditTemplates.DropDownListServerFilter;
                    //GridControl = "KendoDropDown";
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;
                case eTemplate2.Hidden:
                    FilterControl = null;
                    EditControl = EditTemplates.HiddenTextBox;
                    ControlWidth = 0;
                    break;

                case eTemplate2.WhiteSpace:
                    FilterControl = FilterTemplates.WhiteSpace;
                    EditControl = EditTemplates.WhiteSpace;
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;

                case eTemplate2.IndexLink:
                    FilterControl = null;
                    EditControl = EditTemplates.IndexLink;
                    if (ControlWidth == 0) ControlWidth = 12;
                    break;

                case eTemplate2.CheckBox:
                    FilterControl = FilterTemplates.CheckBox;
                    EditControl = EditTemplates.CheckBox;
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;
                case eTemplate2.CheckBoxLong:
                    FilterControl = null;
                    EditControl = EditTemplates.CheckBoxLong;
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;
                case eTemplate2.Switch:
                    FilterControl = FilterTemplates.Switch;
                    EditControl = EditTemplates.Switch;
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;
                case eTemplate2.FileUpload:
                    FilterControl = null;
                    EditControl = EditTemplates.FileUpload;
                    if (ControlWidth == 3) ControlWidth = 6;
                    break;

                case eTemplate2.ListBox:
                    FilterControl = null;
                    EditControl = EditTemplates.ListBox;
                    if (ControlWidth == 0) ControlWidth = 12;
                    break;

                case eTemplate2.Button:
                    FilterControl = null;
                    EditControl = EditTemplates.Button;
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;

                case eTemplate2.MultiDropDownList:
                    FilterControl = FilterTemplates.MultiDropDownList;
                    EditControl = EditTemplates.MultiDropDownList;
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;

                case eTemplate2.ButtonLabel:
                    FilterControl = null;
                    EditControl = EditTemplates.ButtonLabel;
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;
                case eTemplate2.TimePicker:
                    FilterControl = null;
                    EditControl = EditTemplates.TimePicker;
                    Format ??= SpolisParameters.KendoTimeFormat;
                    if (ControlWidth == 0) ControlWidth = 6;
                    break;
                case eTemplate2.DropDownListLong:
                    FilterControl = null;
                    EditControl = EditTemplates.DropDownListLong;
                    if (ControlWidth == 0) ControlWidth = 12;
                    break;
                case eTemplate2.RadioButton:
                    FilterControl = null;
                    EditControl = EditTemplates.RadioButton;
                    if (ControlWidth == 0) ControlWidth = 12;
                    break;
                case eTemplate2.TextField:
                    FilterControl = null;
                    EditControl = EditTemplates.TextField;
                    if (ControlWidth == 0) ControlWidth = 12;
                    break;
                case eTemplate2.Group:
                    FilterControl = null;
                    EditControl = EditTemplates.Group;
                    if (ControlWidth == 0) ControlWidth = 12;
                    break;

                case eTemplate2.None:
                    break;

                default: throw new InvalidEnumArgumentException();

            }
        }

        public eTemplate2 Template { get; }
        public string SourcePropertyName { get; }

        public In VisibleIn { get; protected set; }
        public In EditableIn { get; protected set; }
        public string VisibleIf { get; protected set; }
        public string EditableIf { get; protected set; }

        public string FilterControl { get; set; }
        public string EditControl { get; set; }
        public string FilterValue { get; set; }
        public string FilterValueEnable { get; set; }
        //Izmantojam TIKAi 6 vai 12
        public byte ControlWidth { get; set; }
        public int ColumnWidth { get; set; } = 0;
        public string Format { get; set; }
        public string GridControl { get; set; }

        public object GetSourcePropertyValue([NotNull] Type modelType, object model = null)
        {
            if (SourcePropertyName == null) return null;
            var property = GetProperty(modelType, SourcePropertyName);
            if (property == null)
            {
                throw new ArgumentException($"Property with name {SourcePropertyName} was not found in model {modelType.Name}.");
            }
            return property.GetValue(model);
        }

        public bool GetBoolPropertyValue(string propertyName, [NotNull] Type modelType, object model = null)
        {
            if (propertyName == null) return true;
            var property = GetProperty(modelType, propertyName);
            if (property == null) throw new ArgumentException($"Property with name {SourcePropertyName} was not found in model {modelType.Name}.");
            if (property.PropertyType != typeof(bool)) throw new ArgumentException($"Property with name {SourcePropertyName} in model {modelType.Name} requres return type of bool, because it is used as condition.");

            if (property.GetAccessors().Any(x => x.IsStatic))
            {
                return (bool)property.GetValue(null);
            }
            else if (model != null)
            {
                return (bool)property.GetValue(model);
            }
            else
            {
                return true;
            }
        }
        public string GetFilterPropertyValue(string propertyName, [NotNull] Type modelType, object model = null)
        {
            if (propertyName == null) return null;
            var property = GetProperty(modelType, propertyName);
            if (property == null) throw new ArgumentException($"Property with name {SourcePropertyName} was not found in model {modelType.Name}.");
            if (property.PropertyType != typeof(string)) throw new ArgumentException($"Property with name {SourcePropertyName} in model {modelType.Name} requres return type of bool, because it is used as condition.");

            if (model != null)
            {
                return property.GetValue(model).ToString();
            }
            if (property.GetAccessors().Any(x => x.IsStatic))
            {
                return property.GetValue(null)?.ToString();
            }
            return null;
        }
        private static PropertyInfo GetProperty([NotNull] Type modelType, string propertyName)
        {
            PropertyInfo property = null;
            var currentType = modelType;
            while (currentType != typeof(object))
            {
                property = currentType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (property != null) break;
                currentType = currentType.BaseType;
            }
            return property;
        }

        public enum eTemplate2
        {
            Show3 = -1,
            Auto = 0,
            None = 1,
            TextBox = 2,
            TextArena = 3,
            DatePicker = 4,
            NumberPicker = 5,
            DropDownList = 6,
            Hidden = 7,
            WhiteSpace = 8,
            IndexLink = 9,
            CheckBox = 10,
            FileUpload = 11,
            ListBox = 12,
            Button = 13,
            MultiDropDownList = 14,
            FilterDropDownList = 15,
            ButtonLabel = 16,
            CheckBoxLong = 17,
            Switch = 18,
            TimePicker = 19,
            DropDownListLong = 20,
            Group = 21,
            RadioButton = 22,
            TextField = 23
        }
    }

    [Flags]
    public enum In
    {
        None = 0,
        All = 1,
        Filter = 2,
        Grid = 4,
        Create = 8,
        Edit = 16,
        Other = 32,
    }


    public interface IAttributeRequiresInitialization
    {
        void Initialize(PropertyInfo info, hIndexModel indexModel);
    }


}
