using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using SpolisShared.Helpers.SqlConditions;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.ComponentModel;
using EFramework.Models;

namespace Spolis.Controllers
{
    /// <summary>
    /// Form generation logic base controller.
    /// Generates index, edit and create forms from ViewModel.
    /// </summary>
    /// <typeparam name="ViewModel"></typeparam>
    public abstract class hIndexController<ViewModel>
        : Controller
        where ViewModel : class, iModelMeta
    {

        public IValidationHelper ValidationHelper { get; }
        public IAuthorizationService AuthorizationService { get; }
        public IUserHelper UserHelper { get; }

        protected static IndexViewInstructions<ViewModel> Instructions = new IndexViewInstructions<ViewModel>();

        protected hIndexController()
        {
            ValidationHelper = SpolisShared.Helpers.Services.GetService<IValidationHelper>();
            AuthorizationService = SpolisShared.Helpers.Services.GetService<IAuthorizationService>();
            UserHelper = SpolisShared.Helpers.Services.GetService<IUserHelper>();
        }

        public static AutoProcedure<ViewModel> Procedure { get; } = AutoProcedure.Of<ViewModel>();

        protected virtual ViewModel CreateViewModel()
        {
            var model = (ViewModel)Activator.CreateInstance(typeof(ViewModel));
            return model;
        }

        /// <summary>
        /// Generates index page, that contains filter and grid.
        /// </summary>
        /// <param name="showLayout">Use to disable layout, if this view is displayed in another view.</param>
        /// <returns></returns>
        [HttpGet]
        public virtual IActionResult Index([FromQuery] bool showLayout = true)
        {
            IActionResult access = ValidateAccessAsync(ModelPolicy.ePolicyType.Read).Result;
            if (access != null) return access;

            var indexModel = new IndexViewModel<ViewModel>(Instructions);
            indexModel.ShowLayout = showLayout;
            indexModel.Location = hIndexViewModel.eLocation.Index;


            //Grid height
            if (indexModel.FilterKeys.Count == 0)
            {
                var where = TranslateFilter(indexModel);
                var count = Procedure.GetCount(where);
                if (count < 3)
                    indexModel.IndexInstructions.Settings.Grid.Height = 150;
                //extractIndexModel(result)?.Meta.Add("GridHeight", 100);
                else if (count < 13)
                    indexModel.IndexInstructions.Settings.Grid.Height = count * 50;
                //extractIndexModel(result)?.Meta.Add("GridHeight", count * 50);
            }

            LoadFilter(indexModel);

            var result = View(Instructions.ViewIndex, indexModel);
            indexModel.ResultId = Requests.Set(UserHelper.UserId, result);
            return result;

        }

        protected async Task<IActionResult> ValidateAccessAsync(ModelPolicy.ePolicyType policyType)
        {
            return await ValidateAccessAsync(policyType.ToString());
        }

        public async Task<IActionResult> ValidateAccessAsync(string policyType)
        {
            var policy = GetPolicy(policyType);
            if (policy == null)
            {
                return null; //Allow if ppolicy not set.
            }
            if (policy == SpolisPolicy.Ignore)
            {
                return null; //Allow if ppolicy not set.
            }
            if (!(await AuthorizationService.AuthorizeAsync(User, policy)).Succeeded)
                return RedirectToAction(nameof(AccountController.AccessDenied), nameof(AccountController));
            return null;
        }

        public static string GetPolicy(string policyType)
        {
            var attributes = typeof(ViewModel).GetCustomAttributes<ModelPolicy>();
            return (from f in attributes
                    where f.Type.ToString() == policyType || (f.Type == ModelPolicy.ePolicyType.Custom && f.CustomType == policyType)
                    select f.Policy).FirstOrDefault();
        }

        #region "Filters"

        public virtual bool LoadFilter(IndexViewModel<ViewModel> model)
        {
            var serializeValue = this.HttpContext.Session.GetString(FilterSerializerName());

            if (!string.IsNullOrEmpty(serializeValue))
            {
                model.FilterValues.Clear();
                JsonConvert.PopulateObject(serializeValue, model.FilterValues);

                //Fill missing keys.
                foreach (var f in model.FilterKeys)
                {
                    if (!model.FilterValues.ContainsKey(f))
                    {
                        model.FilterValues.Add(f, null);
                    }
                }
                return true;
            }
            return false;
        }

        public string FilterSerializerName()
        {
            return $"{this.GetType().Name}@{typeof(ViewModel).Name}";
        }

        #endregion

        [HttpPost]
        public virtual IActionResult ApplyFilter(Dictionary<string, string> filterValues)
        {
            IActionResult access = ValidateAccessAsync(ModelPolicy.ePolicyType.Read).Result;
            if (access != null) return access;

            this.HttpContext.Session.SetString(FilterSerializerName(), JsonConvert.SerializeObject(filterValues));
            return Json(new { responseText = SpolisParameters.Success });
        }

        [HttpGet]
        public virtual IActionResult ClearFilter()
        {
            IActionResult access = ValidateAccessAsync(ModelPolicy.ePolicyType.Read).Result;
            if (access != null) return access;

            HttpContext.Session.Remove(FilterSerializerName());
            var filterModule = new IndexViewModel<ViewModel>(Instructions);
            //filterModule.IndexInstructions = Instructions;
            filterModule.Location = hIndexViewModel.eLocation.Index;
            return PartialView(Instructions.ViewIndexFilters, filterModule);
        }

        public virtual IActionResult ReadGrid([DataSourceRequest] DataSourceRequest request)
        {
            //Create index model.
            var indexModel = new IndexViewModel<ViewModel>(Instructions);
            LoadFilter(indexModel);

            //Translate WHERE conditions from filters.
            var where = TranslateFilter(indexModel);

            //Create ORDER.
            var sortKey = indexModel.GridKeys.First(f => indexModel.ModelMap.ContainsKey(f));
            var sortDir = SqlOrderCondition.eOrder.ASC;
            if (request.Sorts.Any())
            {
                try
                {
                    sortKey = indexModel.ModelMap[request.Sorts.First().Member];
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw new Exception($"Property {request.Sorts.First().Member} is not mapped in {typeof(ViewModel).Name}", e);
                }
                sortDir = request.Sorts.First().SortDirection == 0 ? SqlOrderCondition.eOrder.ASC : SqlOrderCondition.eOrder.DESC;
            }
            var order = new SqlOrderCondition(sortKey, sortDir);

            //Create LIMIT.
            var limit = new SqlLimitCondition(((request.Page - 1) * request.PageSize), request.PageSize);

            //Get data.
            var totals = Procedure.GetCount(where);
            var data = Procedure.GetRange(where, order, limit);
            var trimmedData = new List<dynamic>(totals);
            foreach (var f in data)
            {
                dynamic fTrimmed = new System.Dynamic.ExpandoObject();
                ((IDictionary<string, object>)fTrimmed).Add(nameof(iModelMeta.Id), f.Id);
                foreach (var fKey in indexModel.GridKeys)
                {
                    var fProp = indexModel.Properties[fKey];
                    ((IDictionary<string, object>)fTrimmed).Add(fKey, fProp.GetValue(f));
                }
                trimmedData.Add(fTrimmed);

            }
            //TODO: TreeList
            var result = new DataSourceResult();
            //Create result.
            switch (Instructions.Settings.Grid.GridType)
            {
                case IndexViewInstructions.ViewSettings.GridSettings.eGridType.Grid:
                    result = new DataSourceResult() { Data = trimmedData, Total = totals };
                    break;
                case IndexViewInstructions.ViewSettings.GridSettings.eGridType.TreeList:
                    var parentPropertyName = typeof(ViewModel).GetCustomAttribute<TreeListParent>()?.ParentIdPropertyName;
                    if (!indexModel.Properties.TryGetValue(parentPropertyName, out var parentPropertyInfo))
                        throw new Exception("Cannot use treelist because Model {} has invalid value {}");

                    //result = data.ToTreeDataSourceResult(request, f => f.Id.Value, f => f.ParentId);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }

            return Json(result);
        }

        protected virtual SqlWhereCondition TranslateFilter(IndexViewModel<ViewModel> indexModel)
        {
            var conditions = new List<SqlWherePartCondition>();
            foreach (var fKey in indexModel.FilterValues.Keys)
            {
                var fFilterValue = indexModel.FilterValues[fKey];
                if (fFilterValue != null && !string.IsNullOrEmpty(fFilterValue.ToString()))
                {
                    fFilterValue = fFilterValue.ToLower();
                    var fProp = indexModel.Properties[fKey];

                    var map = string.Empty;
                    if (!indexModel.ModelMap.TryGetValue(fKey, out map))
                    {
                        if (!indexModel.ModelMap.TryGetValue("_" + fKey, out map))
                        {
                            throw new ArgumentException($"Cannot filter values by {indexModel.ModelType}.{fKey}, because property is not mapped!");
                        }
                    }

                    bool temp = false;

                    if (indexModel.GetFilterTemplate(fProp.Name) == SpolisShared.Templates.FilterTemplates.NumberPicker)
                    {
                        double val1 = int.MinValue;
                        double val2 = int.MaxValue;

                        var parts = fFilterValue.Split("|");
                        if (parts.Length >= 1 && parts[0].Length > 0) double.TryParse(parts[0], out val1);
                        if (parts.Length >= 2 && parts[1].Length > 0) double.TryParse(parts[1], out val2);

                        conditions.Add(new SqlWherePartCondition(map, new List<double>(new double[] { val1, val2 })));
                    }
                    else if (indexModel.GetFilterTemplate(fProp.Name) == SpolisShared.Templates.FilterTemplates.DatePicker)
                    {
                        DateTime date1 = new DateTime(1753, 1, 1); //Because sql min value.
                        DateTime date2 = DateTime.MaxValue;

                        var parts = fFilterValue.Split("|");
                        if (parts.Length >= 1 && parts[0].Length > 0) DateTime.TryParse(parts[0], out date1);
                        if (parts.Length >= 2 && parts[1].Length > 0) DateTime.TryParse(parts[1], out date2);
                        if (date2 < DateTime.MaxValue) date2 = date2.Date.AddDays(1).AddMilliseconds(-1);
                        conditions.Add(new SqlWherePartCondition(map, new List<DateTime>(new DateTime[] { date1, date2 })));
                    }
                    else if (indexModel.GetFilterTemplate(fProp.Name) == SpolisShared.Templates.FilterTemplates.MultiDropDownList)
                    {
                        var parts = fFilterValue.Split("|");
                        var query = "({0} LIKE '%'+'" + parts[0] + "'+'%'";
                        for (int i = 1; i < parts.Length; i++)
                        {
                            query += " OR {0} LIKE '%'+'" + parts[i] + "'+'%'";
                        }
                        query += ")";
                        var where = new SqlWherePartCondition.WherePattern(query, 1, typeof(string));
                        conditions.Add(new SqlWherePartCondition(map, new List<string>(parts), where));
                    }
                    else if (fProp.PropertyType == typeof(Guid))
                    {
                        conditions.Add(new SqlWherePartCondition(map, Guid.Parse(fFilterValue)));
                    }
                    else if ((fProp.PropertyType == typeof(bool) || fProp.PropertyType == typeof(bool?)))
                    {
                        //conditions.Add(new SqlWherePartCondition(map, bool.Parse(fFilterValue)));
                        temp = true;
                    }
                    else if (fProp.PropertyType.Name == typeof(StringList<>).Name)
                    {
                        var type = fProp.PropertyType.GetGenericArguments().First();
                        var genericType = typeof(StringList<>).MakeGenericType(type);
                        var values = ((IStringList)Activator.CreateInstance(genericType, new object[] { fFilterValue })).ToStringArray();
                        if (fFilterValue != null && values.Any())
                        {
                            var fIdx = 0;
                            var subParts = new string[values.Length];
                            foreach (var f in values)
                            {
                                fIdx++;//starts with 1.
                                subParts[fIdx - 1] = @"(@{0}" + fIdx + " IN (SELECT * FROM skv.fnSplit({0}, '" + StringList.DefaultSeperator + "')))";
                            }
                            var pattern = string.Join(" OR ", subParts);
                            var patternObject = new SqlWherePartCondition.WherePattern(pattern, values.Count(), type);
                            conditions.Add(new SqlWherePartCondition(map, values.ToList(), patternObject));
                        }
                    }
                    else
                    {
                        if (!temp)
                            conditions.Add(new SqlWherePartCondition(map, fFilterValue.ToString()));
                    }
                }
            }

            return new SqlWhereCondition(conditions);
        }



        public virtual JsonConfirmResult AllowCreate()
        {
            return new JsonConfirmResult(Guid.Empty, SpolisResources._Empty, true);
        }
        [HttpGet]
        public virtual IActionResult Create()
        {
            IActionResult access = ValidateAccessAsync(ModelPolicy.ePolicyType.Create).Result;
            if (access != null) return access;

            var model = CreateViewModel();
            var indexModel = new IndexViewModel<ViewModel>(Instructions);
            indexModel.Model = model;
            indexModel.Location = hIndexViewModel.eLocation.Create;

            var result = View(Instructions.ViewEdit, indexModel);
            indexModel.ResultId = Requests.Set(UserHelper.UserId, result);
            return result;
        }

        /// <summary>
        /// Removes model validations from ModelState, that is not relative to current post.
        /// Must be called from form post method, generated by _IndexEdit.cshtml.
        /// Reference global search by "!Validator".
        /// </summary>
        protected void ApplyValidatorsToModelState()
        {
            if (Request.HasFormContentType)
            {
                var validators = new HashSet<string>(Request.Form.Where(f => f.Key.Contains("!Validator")).Select(f => f.Value.ToString()));
                foreach (var fState in ModelState)
                {
                    if (!validators.Contains(fState.Key)) ModelState.Remove(fState.Key);
                }
            }
        }

        [HttpPost]
        public virtual JsonConfirmResult CreateConfirm(ViewModel model)
        {
            IActionResult access = ValidateAccessAsync(ModelPolicy.ePolicyType.Create).Result;
            if (access != null) return new JsonConfirmResult(model.Id, SpolisResources.AccAccessDeniedDescription, false);

            ApplyValidatorsToModelState();
            if (!ModelState.IsValid)
            {
                return new JsonConfirmResult(model.Id, ValidationHelper.ModelStateErrorsToList(ModelState.Values), false);
            }

            var validationError = Procedure.Validate(model);
            if (validationError != null)
            {
                var message = TranslateValidationError(model, validationError);
                if (message == null) message = validationError;
                if (message.Substring(0, SpolisParameters.ErrorPrefix.Length) != SpolisParameters.ErrorPrefix)
                {
                    message = SpolisParameters.ErrorPrefix + message;
                }
                return new JsonConfirmResult(model.Id, message, false);
            }

            try
            {
                Guid? id = Procedure.Insert(model);
                var message = id.HasValue ? SpolisResources._CreateConfirmSuccess : SpolisResources._CreateConfirmFailure + " (Id null)";
                return new JsonConfirmResult(id, message, id.HasValue);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new JsonConfirmResult(model.Id, SpolisResources._CreateConfirmFailure, false);
            }
        }

        public virtual JsonConfirmResult AllowEdit()
        {
            return new JsonConfirmResult(Guid.Empty, SpolisResources._Empty, true);
        }
        [HttpGet]
        public virtual IActionResult Edit(Guid id, string message)
        {
            IActionResult access = ValidateAccessAsync(ModelPolicy.ePolicyType.Read).Result;
            if (access != null) return access;

            IndexControllerHelpers.SetEditId(HttpContext, this.GetType(), id);

            ViewModel model = Procedure.Get(id);

            if (model != null)
            {
                model.Id = id;
                var indexModel = new IndexViewModel<ViewModel>(Instructions);
                indexModel.Model = model;
                indexModel.Location = hIndexViewModel.eLocation.Edit;
                indexModel.Message = message;

                var result = View(Instructions.ViewEdit, indexModel);
                indexModel.ResultId = Requests.Set(UserHelper.UserId, result);
                return result;
            }
            else
            {
                ErrorViewModel errorView = new ErrorViewModel
                {
                    ErrorMessages = new List<string> {
                        SpolisResources.GenError
                    }
                };
                return View(Instructions.ViewError, errorView);

            }
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public virtual JsonConfirmResult EditConfirm(ViewModel model)
        {
            IActionResult access = ValidateAccessAsync(ModelPolicy.ePolicyType.Update).Result;
            if (access != null) return new JsonConfirmResult(model.Id, SpolisResources.AccAccessDeniedDescription, false);

            ApplyValidatorsToModelState();
            if (!ModelState.IsValid)
            {
                return new JsonConfirmResult(model.Id, ValidationHelper.ModelStateErrorsToList(ModelState.Values), false);
            }

            var validationError = Procedure.Validate(model);
            if (validationError != null)
            {
                var message = TranslateValidationError(model, validationError);
                if (message.Substring(0, SpolisParameters.ErrorPrefix.Length) != SpolisParameters.ErrorPrefix)
                {
                    message = SpolisParameters.ErrorPrefix + message;
                }
                return new JsonConfirmResult(model.Id, message, false);
            }

            try
            {
                Procedure.Update(model);
                return new JsonConfirmResult(model.Id, SpolisResources._EditConfirmSuccess, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new JsonConfirmResult(model.Id, SpolisResources._EditConfirmFailure, false);
            }
        }

        /// <summary>
        /// Gets saved ViewResult from resultId and renders html in back-end.
        /// This method is a part of generated form dynamic udate logic.
        /// REF: SpolisShared/Doc/IndexGeneration/DynamicUpdate.txt
        /// </summary>
        /// <param name="model">Current state of form.</param>
        /// <param name="resultId">Request if of ViewResult, form has been created from.</param>
        /// <returns>Serialized array of htmlCtrl.</returns>
        [HttpPost]
        public virtual JsonResult DynamicUpdate(ViewModel model, Guid resultId)
        {
            var result = Requests.Get(UserHelper.UserId, resultId);
            var indexModel = extractIndexModel(result);
            var sourceModel = extractViewModel(result);

            //Update model.
            foreach (var f in ModelState.Keys)
            {
                if (f.Split(".")[0] == "model")
                {
                    var property = typeof(ViewModel).GetProperty(f.Split(".")[1]);
                    if (property.GetCustomAttribute<DynamicUpdateIgnore>() == null)
                    {
                        if (property.CanRead && property.CanWrite)
                        {
                            property.SetValue(sourceModel, property.GetValue(model));
                        }
                    }
                }
            }

            //Parse to html
            var html = IndexControllerHelpers.Render(result, this);

            //Find controls.
            var startParts = html.Split(indexModel.GetControlContainerStartTag()).ToList(); ///ĀĀĀĀĀĀ!!!!
            startParts.RemoveAt(0);
            var parts = startParts.Select(f => f.Split(indexModel.GetControlContainerEndTag()).First());
            parts = parts.Select(f => f.Trim());

            //Create control collection.
            var ctrls = parts.Select(f => new htmlCtrl() { Outer = f }).ToArray();

            //Find container ids.
            foreach (var f in ctrls)
            {
                f.Id = f.Outer.Split("id=\"")[1].Split("\"")[0];
            }

            //Get inner html-s.
            foreach (var f in ctrls)
            {
                var containerDivStartLen = f.Outer.Split(">").First().Length + 1;
                f.Inner = f.Outer.Substring(containerDivStartLen);
                var containerDivEndLen = "</div>".Length;
                f.Inner = f.Inner.Substring(0, f.Inner.Length - containerDivEndLen);

                f.Concated = f.Inner;
                //Remove child forms
                if (f.Inner.Contains("<form"))
                {
                    var idxStart = f.Inner.Split("<form")[0].Length + 1;
                    var lenEnd = f.Inner.Split("</form>").Last().Length;
                    var idxEnd = f.Inner.Length - lenEnd;
                    var strStart = f.Inner.Substring(0, idxStart + 1);
                    var strEnd = f.Inner.Substring(idxEnd, lenEnd - 1);
                    f.Concated = strStart + strEnd;
                }
            }

            //Set hashes.
            foreach (var f in ctrls)
            {
                f.Hash = f.Concated.GetHashCode().ToString();
            }

            //Clean before sand to decrese size.
            foreach (var f in ctrls)
            {
                f.Outer = null;
                f.Concated = null;
            }

            return Json(ctrls);
        }

        private class htmlCtrl
        {
            public string Outer;
            public string Inner;
            public string Concated;
            public string Id;
            public string Hash;
        }

        [HttpGet]
        public virtual IActionResult Delete(Guid id)
        {
            IActionResult access = ValidateAccessAsync(ModelPolicy.ePolicyType.Delete).Result;
            if (access != null) return access;

            ViewModel model = Procedure.Get(id);
            var indexModel = new IndexViewModel<ViewModel>(Instructions)
            {
                Location = hIndexViewModel.eLocation.Delete,
                Model = model
            };
            if (model != null)
            {
                return View(Instructions.ViewDelete, indexModel);
            }
            else
            {
                return Json(new { responseText = SpolisResources._GetDataNotFound });
            }

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual IActionResult DeleteConfirm(ViewModel model)
        {
            IActionResult access = ValidateAccessAsync(ModelPolicy.ePolicyType.Delete).Result;
            if (access != null) return access;

            try
            {
                bool success = Procedure.Delete(model.Id.Value);
                if (success)
                {
                    return Json(new { responseText = SpolisResources._DeleteConfirmSuccess });
                }
                else
                {
                    return Json(new { responseText = SpolisResources._DeleteConfirmFailure });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(new { responseText = SpolisResources._DeleteConfirmFailure });
            }

        }

        [HttpGet]
        public IActionResult DownloadGrid()
        {
            IActionResult access = ValidateAccessAsync(ModelPolicy.ePolicyType.Read).Result;
            if (access != null) return access;
            return Json(new { responseText = SpolisParameters.Success });
        }


        [HttpPost]
        public IActionResult DownloadGridPost(string contentType, string base64, string fileName)
        {
            IActionResult access = ValidateAccessAsync(ModelPolicy.ePolicyType.Read).Result;
            if (access != null) return access;

            byte[] fileContents = Convert.FromBase64String(base64);
            return File(fileContents, contentType, fileName);
        }



        protected virtual string TranslateValidationError(ViewModel model, string message)
        {
            return string.Format(SpolisResources.DefaultValidationError, message);
        }

        //Helpers

        protected IndexViewModel<ViewModel> extractIndexModel(IActionResult result)
        {
            if (result.GetType() == typeof(ViewResult))
            {
                var viewResult = (ViewResult)result;
                try
                {
                    var indexModel = (IndexViewModel<ViewModel>)viewResult.Model;
                    return indexModel;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        protected ViewModel extractViewModel(IActionResult result)
        {
            try
            {
                return (ViewModel)extractIndexModel(result)?.Model;
            }
            catch
            {
                return null;
            }
        }

    }


    public static class IndexControllerHelpers
    {

        public static void SetEditId(HttpContext context, Type controllerType, Guid id)
        {
            while (controllerType != typeof(object))
            {
                context.Session.SetString($"{controllerType.Name}.{"EditId"}", id.ToString());
                controllerType = controllerType.BaseType;
            }
        }

        public static Guid? GetEditId<Controller>(HttpContext context) where Controller : Microsoft.AspNetCore.Mvc.Controller
        {
            var controllerType = typeof(Controller);
            string guidStr = context.Session.GetString($"{controllerType.Name}.{"EditId"}");
            if (guidStr == null) return null;
            if (string.IsNullOrEmpty(guidStr)) { return Guid.Empty; }
            return Guid.Parse(guidStr);
        }

        /// <summary>
        /// Renders raw html in back end.
        /// </summary>
        public static string Render(ViewResult result, Controller controller)
        {
            using (var writer = new StringWriter())
            {
                IViewEngine viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;

                //Get view method 1.
                IView view = viewEngine.FindView(controller.ControllerContext, result.ViewName.Replace("~/Views/", ""), false).View;
                //Get view method 2.
                view ??= viewEngine.GetView(result.ViewName, result.ViewName, false).View;

                if (view == null)
                {
                    throw new Exception($"A view with the name {result.ViewName} could not be found");
                }

                ViewContext viewContext = new ViewContext(
                    controller.ControllerContext,
                    view,
                    result.ViewData,
                    result.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                var task = view.RenderAsync(viewContext);
                task.Wait();

                var html = writer.GetStringBuilder().ToString();
                return html;
            }
        }
    }

}

