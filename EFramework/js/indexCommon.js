//ADD HERE ONLY FUNCTIONS RELATED & UNIVERSAL TO INDEX GENERATION!
//FOR "MySpecialSnowflakeGridSave" CREATE A NEW JS FILE!

//Default button handler.
function indexButtonControlHandler(e, url) {
    e.sender.element[0].setAttribute("disabled", true);

    if ($("#loader").length) {
        $("#loader").show();
    }

    var callback = function (data) {
        e.sender.element[0].removeAttribute("disabled");

        if ($("#loader").length) {
            $("#loader").hide();
        }
        var redirect = JSON.parse(data).JsonRedirect;
        redirect = JSON.stringify(redirect);
        handleJsonRedirect(redirect);
    }

    try {
        compareControlHashes(url, e.sender.element[0].form.id, null, callback)
    }
    catch (e) {
        e.sender.element[0].removeAttribute("disabled");
        if ($("#loader").length) {
            $("#loader").hide();
        }
        throw (e);
    }
}

function buttonControlHandler(e, url, type, winId) {
    e.sender.element[0].setAttribute("disabled", true);
    try {
        if (type != null && e != null) {

            var frmId = e.sender.element[0].form.id;

            if (type == "JSON") {
                function callBack(data) {
                    if (data.indexOf("ERR:") < 0) {
                        kendoGridRefresh(gridId);
                        window.close();
                    }
                    handleMessage(JSON.parse(data).message);
                }
                ajaxPost(url, callBack, frmId);
            }
            else if (type == "Redirect") {
                window.location.href = url;
            }
            else if (type == "Dialog") {

                e.preventDefault();
                var windowId = winId;

                function callBack(data) {
                    try {
                        obj = JSON.parse(data);
                        handleMessage(obj.message);
                        indexCloseDialog(null);
                        return;
                    }
                    catch (e) { }

                    $("#IndexDialog").kendoWindow({
                        title: "",
                        modal: true,
                        actions: ["Close"],
                        visible: false,
                        refresh: function () {

                        },
                        close: function (e) {
                            $(this.element).empty();
                        }
                    });

                    var dg = document.getElementsByClassName("k-widget k-window")[0];
                    dg.style.maxHeight = "445px";
                    dg.style.maxWidth = "743px";
                    var windowData = $("#IndexDialog").data("kendoWindow");
                    windowData.content(data);
                    windowData.center();
                    handleRequiredLabel();
                    windowData.open();
                    updateDialogStyles();

                }
                ajaxGet(url, null, callBack);
            }
        }

    } catch (e) {
        throw (e);
        e.sender.element[0].removeAttribute("disabled");
    }

    e.sender.element[0].removeAttribute("disabled");
}

function updateDialogStyles() {
    var frmElem = document.getElementById("IndexElement_EditForm_ChangePasswordViewModel").getElementsByClassName("form-group")[0];
    frmElem.classList.remove("row");

    var containers = frmElem.querySelectorAll('[ctrl="Container"]');

    containers.forEach(ctrl => {
        ctrl.removeAttribute("class");
        ctrl.style.paddingBottom = "5px";

        var label = ctrl.getElementsByClassName("col-sm-4")[0];

        if (label != null) {
            label.style.textAlign = "center!important";

            label.classList.remove("col-sm-4");
            label.classList.add("col-sm-5");
        }


        var inputDiv = ctrl.getElementsByClassName("col-sm-8")[0];

        if (inputDiv != null) {
            inputDiv.classList.remove("col-sm-8");
            inputDiv.classList.add("col-sm-7");
        }

    });
}

function indexReadGrid() {
    var url = this.element.attr("url");
    var frmId = this.element.closest("form")[0].id;
    var gridId = this.element.attr("gridId");

    function callBack(data) {
        kendoGridRefresh(gridId);
        $("#" + gridId).data("kendoGrid").dataSource.page(1); //if on second page after filter refresh page to first
    }
    ajaxPostWithoutValidation(url, callBack, frmId);
}

function createNavigationUrl(url, add) {
    if (url.indexOf("?") >= 0 && url[url.length - 2] !== '<') url = url + "&";
    var tabCtrl = $('[ctrl="TabRoot"]').data("kendoTabStrip");
    var postQuery = decodeURIComponent(window.location.search);
    if (tabCtrl != null) {
        var idx = tabCtrl.select().index();
        var parts = postQuery.split("<");
        if (parts.length >= 1) {
            if (parts[0].indexOf("tab=") >= 0) {
                var tabVal = parts[0].split("tab=")[1];
                parts[0] = parts[0].replace("tab=" + tabVal, "tab=" + idx);
            }
            else if (add) {
                var subParts = parts[0].split("&");
                var subIndex = subParts.length - 1;
                if (subIndex < 0) subIndex = 0;
                subParts.splice(subIndex, 0, "tab=" + idx);
                parts[0] = subParts.join("&");
            }

        }
        postQuery = parts.join("<");
    }
    if (add) postQuery = "?<" + window.location.pathname + postQuery;
    url = (url + postQuery).replace("&?", "&");
    var parts = url.split("?</?");
    if (parts.length > 1) url = parts.shift() + "?</";
    return url;
}

function indexDefault(e) {
    e.preventDefault();
    var url = this.element.attr("url");
    url = createNavigationUrl(url, true);
    safeHref(url);
}

function indexDownload(e) {
    e.preventDefault();
    var url = this.element.attr("url");
    url = createNavigationUrl(url, true);
    showUnsavedChangesWarning = false;
    document.location.href=url;
}

function indexDefaultGet(e) {
    e.preventDefault();
    var url = this.element.attr("url");
    var callback = function (data) {
        if (!handleJsonRedirect(data)) {
            url = createNavigationUrl(url, true);
            safeHref(url);
        }
    }
    ajaxGet(url, null, callback);
}

function indexCreate(e) {
    e.preventDefault();
    var urlAllow = this.element.attr("urlAllow");
    var url = this.element.attr("url");

    if (urlAllow == null) {
        url = createNavigationUrl(url, false);
        safeHref(url);
    }
    else {
        function callBack(data) {
            if (data.indexOf("SPL_ERR:") < 0) {
                url = createNavigationUrl(url, true);
                safeHref(url);
            }
            else {

                handleJsonRedirect(data);
            }
        }
        ajaxGet(urlAllow, null, callBack);
    }
}

function indexCreateConfirm() {
    var url = this.element.attr("url");
    var frmId = this.element.attr("frmId");
    var callback = function (data) {
        showUnsavedChangesWarning = false;
        elementChangeLog = [];
        handleJsonRedirect(data);
    };
    ajaxPost(url, callback, frmId);
}

function indexCreateConfirmJson() {
    var url = this.element.attr("url");
    var modelStructure = this.element.attr("modelStructure");
    var modelName = this.element.attr("modelName");
    var callBack = function (data) {
        showUnsavedChangesWarning = false;
        elementChangeLog = [];
        handleJsonRedirect(data);
    };
    const json = JSON.parse(modelStructure);
    Object.keys(json).forEach(item => {
        var objValue = $("#IndexElement_EditInput_" + item + "_" + modelName).val();
        if (objValue !== undefined) {
            json[item] = $("#IndexElement_EditInput_" + item + "_" + modelName).val();
            if (json[item] === "") {
                json[item] = null;
            }
        }
        else if ($("#Model_" + item).data("kendoSwitch") !== undefined) {
            json[item] = $("#Model_" + item).data("kendoSwitch").value()
        }
        else if ($("#Model_" + item).data("kendoColorPicker") !== undefined) {
            json[item] = $("#Model_" + item).data("kendoColorPicker").value()
        }

    });
    json["Id"] = $("#IndexElement_EditInput_MetaId__" + modelName).val();
    ajaxPostDiv(url, callBack, JSON.stringify(json));
}

//Old version from site.js
//function indexCreateConfirm() {
//    var url = this.element.attr("url");
//    var urlEdit = this.element.attr("urlEdit");
//    var frmId = this.element.attr("frmId");
//    function callBack(data) {
//        var res = JSON.parse(data);
//        if (res.success === true) {
//            var params = "?Id=" + res.id + "&Message=" + res.responseText;
//            window.location.href = urlEdit + params;
//        } else {
//            handleJsonMessage(data);
//        }
//    }
//    ajaxPost(url, callBack, frmId);
//}
function handleJsonRedirect(data) {
    handleJsonRedirect(data, false);
}
function handleJsonRedirect(data, addToNavigationUrl) {
    var res = null;
    try {
        res = JSON.parse(data);
    }
    catch (e) {
        return false;
    }
    if (res.link != null) {
        var url = createNavigationUrl(res.link, addToNavigationUrl);
        safeHref(url);
    } else {
        var error = res.error;
        if (error == null)
            error = res.Error;
        handleMessage(error);
    }
    return true;
};

function indexEdit(e) {
    e.preventDefault();
    var gridId = this.element.attr("gridId");
    var grid = $("#" + gridId).data("kendoGrid");

    if (grid == null) {
        grid = $("#" + gridId).data("kendoTreeList");
    }
    var selectedItem = grid.dataItem(grid.select());
    // Check if multiple rows selected
    var selected = [];
    grid.select().each(function () {
        selected.push(grid.dataItem(this));
    });
    if (selectedItem === null) {
        handleMessage($("#IndexNotSelectedMessage").val());
    }
    else if (selected.length > 1) {
        handleMessage($("#IndexManySelectedMessage").val());
    }
    else {
        var params = "?id=" + selectedItem.Id;
        var urlAllow = $(e.sender.element).attr("urlAllow");
        var url = $(e.sender.element).attr("url");

        if (urlAllow == null) {
            url = createNavigationUrl(url + params, true); //false
            safeHref(url);
        }
        else {
            function callBack(data) {
                if (data.indexOf("SPL_ERR:") < 0) {
                    url = createNavigationUrl(url + params, true);
                    safeHref(url);
                }
                else {
                    handleJsonMessage(data);
                }
            }
            ajaxGet(urlAllow, null, callBack);
        }
    }
}

function indexEditJson(e) {
    e.preventDefault();
    var gridId = this.element.attr("gridId");
    var grid = $("#" + gridId).data("kendoGrid");

    if (grid == null) {
        grid = $("#" + gridId).data("kendoTreeList");
    }
    var selectedItem = grid.dataItem(grid.select());
    // Check if multiple rows selected
    var selected = [];
    grid.select().each(function () {
        selected.push(grid.dataItem(this));
    });
    if (selectedItem === null) {
        handleMessage($("#IndexNotSelectedMessage").val());
    }
    else if (selected.length > 1) {
        handleMessage($("#IndexManySelectedMessage").val());
    }
    else {
        var params = "?id=" + selectedItem.Id;
        var urlAllow = $(e.sender.element).attr("urlAllow");
        var url = $(e.sender.element).attr("url");
            url = createNavigationUrl(url + params, true); //false

       
            function callBack(data) {
                if (data.indexOf("SPL_ERR:") < 0) {
                    handleJsonRedirect(data, true);
                }
                else {
                    handleJsonRedirect(data);
                }
            }
            ajaxGet(url, null, callBack);
        }
}

function indexEditConfirm() {
    var url = this.element.attr("url");
    var frmId = this.element.attr("frmId");
    function callBack(data) {
        elementChangeLog = [];
        showUnsavedChangesWarning = false;
        handleJsonRedirect(data, true);
        indexCloseDialog(null);
        kendoGridRefreshAll();
    }
    ajaxPost(url, callBack, frmId);
}

function indexEditMultiple(e) {
    e.preventDefault();
    var gridId = this.element.attr("gridId");
    var grid = $("#" + gridId).data("kendoGrid");

    if (grid == null) {
        grid = $("#" + gridId).data("kendoTreeList");
    }
    var selectedItem = grid.dataItem(grid.select());
//multi selekt
    var selected = [];
    grid.select().each(function () {
        selected.push(grid.dataItem(this).Id);
    });

    var selectedItem = grid.dataItem(grid.select());
    if (selectedItem === null) {
        handleMessage($("#IndexNotSelectedMessage").val());
    } else {
        var params = "?id=" + selected.join(".");// { id: selected.join(".") };
        var url = $(e.sender.element).attr("url");
        var urlAllow = $(e.sender.element).attr("urlAllow");

        if (urlAllow == null) {
            url = createNavigationUrl(url + params, true); //false

            function callBack(data) {
                if (data.indexOf("SPL_ERR:") < 0) {
                    handleJsonRedirect(data, true);
                   // url = createNavigationUrl(url + params, true); //false
                   // safeHref(url); 
                }
                else {
                    handleJsonRedirect(data);
                }
            }
            ajaxGet(url, null, callBack);

        }
        else {
            function callBack(data) {
                if (data.indexOf("SPL_ERR:") < 0) {
                    url = createNavigationUrl(url, true);
                    safeHref(url);
                }
                else {
                    handleJsonMessage(data);
                }
            }
            ajaxGet(url, params, callBack);
        }
    }
}
function indexCreateEdit(e) {
    e.preventDefault();
    var gridId = this.element.attr("gridId");
    var grid = $("#" + gridId).data("kendoGrid");

    if (grid == null) {
        grid = $("#" + gridId).data("kendoTreeList");
    }
    var selectedItem = grid.dataItem(grid.select());
    // Check if multiple rows selected
    var selected = [];
    grid.select().each(function () {
        selected.push(grid.dataItem(this));
    });
    if (selected.length > 1) {
        handleMessage($("#IndexManySelectedMessage").val());
    }
    else {
        //var params = "?id=" + selectedItem.Id;
        var urlAllow = $(e.sender.element).attr("urlAllow");
        var url = $(e.sender.element).attr("url");


        if (urlAllow == null) {
            if (selectedItem === null) {
                url = $(e.sender.element).attr("url2");
                url = createNavigationUrl(url, true);
            }
            else
                url = createNavigationUrl(url + "?id=" + selectedItem.Id, true);
            safeHref(url);
        }
        else {
            function callBack(data) {
                if (data.indexOf("SPL_ERR:") < 0) {
                    if (selectedItem === null)
                        url = createNavigationUrl(url, true);
                    else
                        url = createNavigationUrl(url + "?id=" + selectedItem.Id, true);
                    safeHref(url);
                }
                else {
                    handleJsonMessage(data);
                }
            }
            ajaxGet(urlAllow, null, callBack);
        }
    }
}

function indexEditConfirmWithoutValidation() {
    var url = this.element.attr("url");
    var frmId = this.element.attr("frmId");
    function callBack(data) {
        elementChangeLog = [];
        showUnsavedChangesWarning = false;
        handleJsonRedirect(data, true);
        indexCloseDialog(null);
        kendoGridRefreshAll();
    }
    ajaxPostWithoutValidation(url, callBack, frmId);
}

function indexDelete(e) {
    e.preventDefault();

    var gridId = this.element.attr("gridId");
    var grid = $("#" + gridId).data("kendoGrid");
    if (grid == null) {
        grid = $("#" + gridId).data("kendoTreeList");
    }
    //multi selekt
    var selected = [];
    grid.select().each(function () {
        selected.push(grid.dataItem(this).Id);
    });

    var selectedItem = grid.dataItem(grid.select());
    if (selectedItem === null) {
        handleMessage($("#IndexNotSelectedMessage").val());
    } else {

        var params = { id: selected.join(".") };
        var url = this.element.attr("url");

        function callBack(data) {
            try {
                obj = JSON.parse(data);
                handleMessage(obj.message);
                return;
            }
            catch (e) { }
            $("#_IndexWindow").kendoWindow({
                title: "",
                modal: true,
                actions: ["Close"],
                visible: false,
                refresh: function () {

                },
                close: function (e) {
                    $(this.element).empty();
                }
            });
            var windowData = $("#_IndexWindow").data("kendoWindow");
            windowData.content(data);
            windowData.center();
            handleRequiredLabel();
            //-Teodors 18.05.2022 engine šis window nav vajadzīgs, ar roku veidotajām formām ir
            if (!url.includes("Multiple")) {
                windowData.open();
            }


        }
        ajaxGet(url, params, callBack);
    }
}

function indexDeleteConfirm() {
    var url = this.element.attr("url");
    var frmId = this.element.attr("frmId");
    var gridId = this.element.attr("gridId");
    var window = $("#_IndexWindow").data("kendoWindow");
    function callBack(data) {
        kendoGridRefresh(gridId);
        handleJsonMessage(data);
        window.close();
        handleJsonRedirect(data);
    }
    ajaxPost(url, callBack, frmId);
}


function indexDeleteConfirmEngine(e) {
    var url = e.attr("url");
    var frmId = e.attr("frmId");
    var gridId = e.attr("gridId");
    var window = $("#_IndexWindow").data("kendoWindow");
    function callBack(data) {
        kendoGridRefresh(gridId);
        handleJsonMessage(data);
        window.close();
        handleJsonRedirect(data);
    }
    ajaxPost(url, callBack, frmId);
}


function indexDownloadGrid() {
    var url = this.element.attr("url");
    var gridId = this.element.attr("gridId");
    var params = {};
    function callBack(data) {
        if (data.indexOf("ERR:") < 0) {
            kendoExcelExport(gridId);
        }
    }
    ajaxGet(url, params, callBack);
}

function indexReturn() {

    ////To disable return warning
    //var showUnchanged = this.element.attr("showUnchanged");
    //showUnsavedChangesWarning = showUnchanged;

    //Calculate return url.
    var href = "";
    var query = decodeURIComponent(window.location.search);
    query = query.replace("<=", "<");
    var parts = query.split("<");
    if (parts.length > 0) {
        parts.shift(); //remove first element becaus it is '?'
        href = parts.join("<");
    }
    if (href == "") {
        href = "/";
    }

    safeHref(href, null);
}

function safeHref(hrefUrl, actionUrl = null) {
    hrefUrl = decodeURIComponent(hrefUrl);

    //Not index page.
    if (_IndexUsed() !== "true") {
        window.location.href = resolveUrl(hrefUrl);
        return;
    }

    //Prepare data.
    var location = _IndexLocation();

    //Get default return url.
    if (actionUrl == null) {
        actionUrl = _IndexReturnUrl();
    }

    //Post callback.
    var callBack = function (data) {
        try {
            //Try deparse as json.
            obj = JSON.parse(data);
            if (handleMessage(obj.message) == 0) //Case 1: Responded as a message.
                window.location.href = resolveUrl(obj.Url); //Case 2: Returned url.
            return;
        }
        catch (e) { } //Case 3: retuned html or exeption (unhandled).
        $("#_IndexWindow").kendoWindow({
            title: "",
            modal: true,
            actions: ["Close"],
            visible: false,
            refresh: function () {
            },
            close: function (e) {
                $(this.element).empty();
            }
        });
        var windowData = $("#_IndexWindow").data("kendoWindow");
        windowData.content(data);
        windowData.center();
        //windowData.open();
    }

    //Prepare post data.
    var changes = [];
    for (var name in elementChangeLog) {
        changes.push(elementChangeLog[name]);
    }

    //Ajax does not work with full model for some reason.
    var postModel = {
        Changes: changes,
        Location: location,
        Url: hrefUrl
    };

    //Post return.
    ajaxPostJson(actionUrl, callBack, JSON.stringify(postModel));
}

function indexCloseDialog(e) {
    if (e != null) {
        e.preventDefault();
    }

    var dialog = $("#IndexDialog").data("kendoWindow");
    if (dialog != null) {
        dialog.close();
    }

}

//Gets value from item source. Used in kendo grids, to translate values.
function calculateGridField(values, texts, key, data) {

    var dataArray = [];
    var displayValues = [];

    if (data instanceof Array) {
        dataArray = data
    }
    else if (data instanceof Object) {
        var properties = Object.keys(data);
        for (var i = 0; i < properties.length; i++) {
            if (properties[i] == i) {
                dataArray.push(data[i]);
            }
        }
    }
    else {
        data = (data + "").toLowerCase();
        dataArray.push(null);
        if (dataArray != null) dataArray = data.split("|");
    }


    var valuesArray = values.split("|");
    var textsArray = texts.split("|");

    for (var iData = 0; iData < dataArray.length; iData++) {
        for (var iVal = 0; iVal < valuesArray.length; iVal++) {
            var fValue = (valuesArray[iVal] + "").toLowerCase()
            if (fValue == dataArray[iData] || (dataArray[iData] == "null" && fValue.length === 0)) {
                displayValues.push(textsArray[iVal]);
            }
        }
    }

    if (displayValues.length === 0)
        return "";
    else if (displayValues.length === 1)
        if (displayValues[0] == null || displayValues[0].length === 0 || !(data instanceof Object && data instanceof Array))
            return displayValues[0];
        else
            return displayValues[0] + ";";
    else
        return displayValues.join("; ") + ";";
}

//Used in Index NumberPicker controls
function IndexControl_FilterNumberPicker_limitMinMaxValue(input, min, max) {
    var limit = function () {
        if (min != null && this.value < min) {
            this.value = min;
        }
        if (max != null && this.value > max) {
            this.value = max;
        }
    }

    input.keyup(limit);
    input.keydown(limit);
    input.change(limit);
}

function IndexControl_FilterNumberPicker_OnValueChange(key, forID) {
    var num1 = "";
    var num2 = "";

    var jdtp1Name = "numberpicker1" + key;
    var jdtp2Name = "numberpicker2" + key;

    if ($("#" + jdtp1Name).val() !== undefined) {
        num1 = $("#" + jdtp1Name).val();
        $("#" + jdtp2Name).data("kendoNumericTextBox").min(num1);
    }

    if ($("#" + jdtp2Name).val() != undefined) {
        num2 = $("#" + jdtp2Name).val();
        $("#" + jdtp1Name).data("kendoNumericTextBox").max(num2);
    }

    $("#" + forID).val(num1 + "|" + num2);
}

function IndexControl_FilterNumberPicker_LoadData(key, forID) {
    var jdtp1Name = "numberpicker1" + key;
    var jdtp2Name = "numberpicker2" + key;

    var val = $("#" + forID).val();

    if (val != undefined) {
        var parts = String(val).split("|");

        var num1 = parts[0];
        var num2 = parts[1];

        if (num1 !== undefined && num1.length > 0) { $("#" + jdtp1Name).data("kendoNumericTextBox").value(num1); }
        if (num2 !== undefined && num2.length > 0) { $("#" + jdtp2Name).data("kendoNumericTextBox").value(num2); }
    }
}

//Used in Index DatePicker controls
function IndexControl_FilterDatePicker_OnDateChange(key, forID) {
    var date1 = "";
    var date2 = "";

    var jdtp1Name = "datepicker1" + key;
    var jdtp2Name = "datepicker2" + key;

    if ($("#" + jdtp1Name).val() !== undefined) {
        date1 = $("#" + jdtp1Name).val();
        $("#" + jdtp2Name).data("kendoDatePicker").min(date1);
    }

    if ($("#" + jdtp2Name).val() !== undefined) {
        date2 = $("#" + jdtp2Name).val();
        $("#" + jdtp1Name).data("kendoDatePicker").max(date2);
    }

    $("#" + forID).val(date1 + "|" + date2);
}

function IndexControl_FilterDatePicker_LoadData(key, forID) {

    var jdtp1Name = "datepicker1" + key;
    var jdtp2Name = "datepicker2" + key;

    var val = $("#" + forID).val();

    if (val != undefined) {
        var parts = String(val).split("|");

        var date1 = parts[0];
        var date2 = parts[1];

        if (date1 !== undefined && date1.length > 0) { $("#" + jdtp1Name).val(date1); }
        if (date2 !== undefined && date2.length > 0) { $("#" + jdtp2Name).val(date2); }

    }
}

function IndexControl_DatePicker_limitInvalidValues(input) {
    var kendoInput = input.data("kendoDatePicker");
    var limit = function () {
        kendoInput.value(kendoInput.element.val());
    }
    input.change(limit);
}


//WTF IS THIS?
function indexGridEditConfirm(e) {
    var url = this.element.attr("url");
    var gridId = this.element.attr("gridId");
    var grid = $("#" + gridId).data("kendoGrid");
    var selectedItem = grid.dataItem(grid.select());
    var params = {};

    if (selectedItem === null) {
        handleJsonMessage(createJsonMessage("IndexNotSelectedMessage"));
    } else {
        var url = $(e.sender.element).attr("url");
        params = { id: selectedItem.Id };
        function callBack(data) {
            var res = JSON.parse(data);
            handleJsonMessage(data);
            kendoGridRefresh(gridId);
        }
        ajaxGet(url, params, callBack);
    }
}

function groupCopyConfirm(e) {
    e.preventDefault();
    var url = this.element.attr("url");
    var urlEdit = this.element.attr("urlEdit");
    var gridId = this.element.attr("gridId");
    var grid = $("#" + gridId).data("kendoGrid");
    var selectedItem = grid.dataItem(grid.select());

    if (selectedItem === null) {
        handleJsonMessage(createJsonMessage("IndexNotSelectedMessage"));
    }
    else {
        var params = { id: selectedItem.Id };
        function callBack(data) {
            if (data.indexOf("SPL_ERR:") < 0) {
                var res = JSON.parse(data);
                var params2 = "?Id=" + res.id + "&Message=" + res.responseText;
                window.location.href = urlEdit + params2;
            }
            handleJsonMessage(data);
        }
        ajaxGet(url, params, callBack);
    }
}


function serviceCopyConfirm(e) {
    e.preventDefault();
    var url = this.element.attr("url");
    var urlEdit = this.element.attr("urlEdit");
    var gridId = this.element.attr("gridId");
    var grid = $("#" + gridId).data("kendoGrid");
    var selectedItem = grid.dataItem(grid.select());

    if (selectedItem === null) {
        handleJsonMessage(createJsonMessage("IndexNotSelectedMessage"));
    }
    else {
        var params = { id: selectedItem.Id };
        function callBack(data) {
            if (data.indexOf("SPL_ERR:") < 0) {
                var res = JSON.parse(data);
                var params2 = "?Id=" + res.id + "&Message=" + res.responseText;
                window.location.href = urlEdit + params2;
            }
            handleJsonMessage(data);
        }
        ajaxGet(url, params, callBack);
    }
}