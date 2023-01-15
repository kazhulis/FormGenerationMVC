using Spolis.Attributes;
using Spolis.Helpers;
using SpolisShared.Resource;
using System;
using System.Collections.Generic;
using static Spolis.Index.hIndexModel;

namespace Spolis.Index
{
    public abstract class IndexInstructions
    {
        public ViewsSettings Views { get; } = new ViewsSettings();
        public class ViewsSettings
        {
            public string Index = "~/Views/Shared/_Index.cshtml";
            public string IndexButtons = "~/Views/Shared/_IndexButtonsPartial.cshtml";
            public string IndexFilters = "~/Views/Shared/_IndexFilterPartial.cshtml";
            public string IndexGrid = "~/Views/Shared/_IndexGridPartial.cshtml";
            public string Edit = "~/Views/Shared/_IndexEdit.cshtml";
            public string EditControls = "~/Views/Shared/_IndexEditControls.cshtml";
            public string Delete = "~/Views/Shared/_IndexDeletePartial.cshtml";
            public string PopupWindow = "~/Views/Shared/PopupWindow.cshtml";
            public string ExtendedPopupWindow = "~/Views/Shared/ExtendedPopupWindow.cshtml";
            public string Error = "~/Views/Shared/Error.cshtml";
            public string DialoguePartial = "~/Views/Shared/DialoguePartial.cshtml";
            public string Return = "~/Views/Shared/_IndexReturnDialog.cshtml";
        }

        public class AllowSettings
        {
            public bool Read { get; set; } = true;
            public bool Open { get; set; } = true;
            public bool Create { get; set; } = true;
            public bool Edit { get; set; } = true;
            public bool Delete { get; set; } = true;
        }
        public AllowSettings Allow { get; } = new AllowSettings();

        public List<Button> Buttons = new List<Button>();
        public enum eButtonLocation
        {
            IndexGrid,
            IndexBottom,
            EditBottom,
            CreateBottom,
            Other
        }

        public static eButtonLocation TranslateLocation(eLocation location)
        {
            return location switch
            {
                eLocation.Index => eButtonLocation.IndexGrid,
                eLocation.Edit => eButtonLocation.EditBottom,
                eLocation.Create => eButtonLocation.CreateBottom,
                _ => Enum.Parse<eButtonLocation>(location.ToString())
            };
        }

        public class FileUpload
        {
            public string Controller { get; set; }
            public string AsyncSave { get; set; }
            public string[] AllowedExtensions { get; set; } = new[] { ".xlsx" };
        }
        public class Button
        {
            public eButtonLocation Location { get; set; }
            public string Title { get; set; }
            public string LabelName { get; set; }
            public string Script { get; set; } = "indexDefault";
            public Dictionary<string, object> Args { get; } = new Dictionary<string, object>();
            public string Policy { get; set; }
            public int OrderPriority { get; set; } = 1;
            public bool Enabled { get; set; } = true;
            public bool Primary { get; set; } = false;
            public bool FileUploadFlag { get; set; } = false;
            public FileUpload FileUpl { get; set; } = new FileUpload();
            public ConditionDelegate Condition { get; set; } = null;
            public delegate bool ConditionDelegate(hIndexModel model);

            public override string ToString()
            {
                return $"{Title} @ {Location} [{Script} for {Policy}]";
            }
        }


        public ViewSettings Settings { get; } = new ViewSettings();

        public class ViewSettings
        {
            public GridSettings Grid { get; } = new GridSettings();
            public FilterSettings Filter { get; } = new FilterSettings();

            public class GridSettings
            {
                public int? Height { get; set; }
                public string DefaultSortKey { get; set; }
                public eSortDirection DefaultSortDirection { get; set; } = eSortDirection.Ascending;
                public enum eSortDirection
                {
                    Ascending,
                    Descending
                }
                public eGridType GridType { get; set; } = eGridType.Grid;
                public enum eGridType
                {
                    Grid,
                    TreeList
                }
                public bool ColumnsResizable { get; set; } = true;
                public bool RowsSelectable { get; set; } = true;

            }
            public class FilterSettings
            {
                public bool ShowGroups { get; set; } = true;
                public bool ShowFilterTitle { get; set; } = true;
                public List<string> PanelBar { get; set; } = new List<string>();
            }

            public bool ConfirmReturnOnUsavedChanges { get; set; } = true;
            public bool UseDynamicUpdate { get; set; } = true;
            public bool ReloadEditAfterSave { get; set; } = false;

            public bool ShowEditInDialog { get; set; } = false;
            public bool ShowCreateInDialog { get; set; } = false;
        }
    }

    public class IndexInstructions<ViewModel> : IndexInstructions
    {
        public IndexInstructions()
        {
            Buttons.Add(DefaultButtons.IndexCreate());
            Buttons.Add(DefaultButtons.IndexEdit());
            Buttons.Add(DefaultButtons.IndexDelete());

            Buttons.Add(DefaultButtons.CreateSave());
            Buttons.Add(DefaultButtons.CreateReturn());

            Buttons.Add(DefaultButtons.EditSave());
            Buttons.Add(DefaultButtons.EditReturn());
        }

        public static class DefaultButtons
        {
            public static Button IndexCreate()
            {
                var btn = new Button() { Location = eButtonLocation.IndexGrid, Primary = true, Title = SpolisResources.BtnAdd, Script = "indexCreate", Policy = ModelPolicy.ePolicyType.Create.ToString(), OrderPriority = 0 };
                btn.Condition = new Button.ConditionDelegate((model) => model.Instructions.Allow.Create);
                btn.Args.Add("url", new Link() { Action = "Create" });
                btn.Args.Add("urlAllow", new Link() { Action = "AllowCreate" });
                return btn;
            }

            public static Button IndexEdit()
            {
                var btn = new Button() { Location = eButtonLocation.IndexGrid, Title = SpolisResources.BtnOpen, Script = "indexEdit", Policy = ModelPolicy.ePolicyType.Read.ToString(), OrderPriority = 1 };
                btn.Condition = new Button.ConditionDelegate((model) => model.Instructions.Allow.Open);
                btn.Args.Add("url", new Link() { Action = "Edit" });
                btn.Args.Add("gridId", GetElementName<ViewModel>(eCommonNames.Grid));
                btn.Args.Add("urlAllow", new Link() { Action = "AllowEdit" });
                return btn;
            }

            public static Button IndexDelete()
            {
                var btn = new Button() { Location = eButtonLocation.IndexGrid, Title = SpolisResources.BtnDelete, Script = "indexDelete", Policy = ModelPolicy.ePolicyType.Delete.ToString(), OrderPriority = 2 };
                btn.Condition = new Button.ConditionDelegate((model) => model.Instructions.Allow.Delete);
                btn.Args.Add("url", new Link() { Action = "DeleteMultiple" });
                btn.Args.Add("gridId", GetElementName<ViewModel>(eCommonNames.Grid));
                btn.Args.Add("windowId", GetElementName<ViewModel>(eCommonNames.DeleteWindow));
                return btn;
            }

            public static Button IndexGridDownload()
            {
                var btn = new Button() { Location = eButtonLocation.IndexGrid, Title = SpolisResources.BtnDownload, Script = "indexDownloadGrid", Policy = ModelPolicy.ePolicyType.Read.ToString(), OrderPriority = 3 };
                btn.Args.Add("url", new Link() { Action = "DownloadGrid" });
                btn.Args.Add("gridId", hIndexModel.GetElementName<ViewModel>(eCommonNames.Grid));
                return btn;
            }

            public static Button CreateSave()
            {
                var btn = new Button() { Location = eButtonLocation.CreateBottom, Primary = true, Title = SpolisResources.BtnCreate, Script = "indexCreateConfirm", Policy = ModelPolicy.ePolicyType.Create.ToString(), OrderPriority = 0 };
                btn.Condition = new Button.ConditionDelegate((model) => model.Instructions.Allow.Create);
                btn.Args.Add("url", new Link() { Action = "CreateConfirm" });
                btn.Args.Add("urlEdit", new Link() { Action = "Edit" });
                btn.Args.Add("frmId", GetElementName<ViewModel>(eCommonNames.EditForm));
                return btn;
            }

            public static Button CreateReturn()
            {
                var btn = new Button() { Location = eButtonLocation.CreateBottom, Title = SpolisResources.BtnBack, Script = "indexReturn", Policy = ModelPolicy.ePolicyType.Read.ToString(), OrderPriority = 10 };
                btn.Args.Add("url", new Link() { Action = "Return" });
                return btn;
            }

            public static Button EditSave()
            {
                var btn = new Button() { Location = eButtonLocation.EditBottom, Primary = true, Title = SpolisResources.BtnSave, Script = "indexEditConfirm", Policy = ModelPolicy.ePolicyType.Update.ToString(), OrderPriority = 0 };
                btn.Condition = new Button.ConditionDelegate((model) => model.Instructions.Allow.Edit);
                btn.Args.Add("url", new Link() { Action = "EditConfirm" });
                btn.Args.Add("urlEdit", new Link() { Action = "Edit" });
                btn.Args.Add("frmId", hIndexModel.GetElementName<ViewModel>(eCommonNames.EditForm));
                return btn;
            }

            public static Button EditReturn()
            {
                var btn = new Button() { Location = eButtonLocation.EditBottom, Title = SpolisResources.BtnBack, Script = "indexReturn", Policy = ModelPolicy.ePolicyType.Read.ToString(), OrderPriority = 10 };
                btn.Args.Add("url", new Link() { Action = "Return" });
                return btn;
            }
        }
    }
}
