//ADD HERE ONLY FUNCTIONS RELATED TO WHOLE APPLICATION & NOT LINKED TO INDEX GENERATION!

function historyBack() { history.back(); }

$(document).ready(function () {
    var notification = $("#notification").kendoNotification({
        position: {
            pinned: true,
            top: 30,
            right: 30
        },
        autoHideAfter: 15000,
        stacking: "down",
        templates: [{
            type: "info",
            template: $("#infoTemplate").html()
        }, {
            type: "error",
            template: $("#errorTemplate").html()
        }, {
            type: "success",
            template: $("#successTemplate").html()
        }]

    }).data("kendoNotification");
    $(document).one("kendo:pageUnload", function () { if (notification) { notification.hide(); } });

    handleRequiredLabel();
    handleAutoBindFalseGrid();
    handleNavigationOnHover();
    showDialogue();

    handleRedirectMessage("IndexRedirectMessage");

    if ($("#input-container").hasClass("container-fluid")) {
        $("#switchStyleBtn").text(">");
    }
    else {
        $("#switchStyleBtn").text("<");
    }
});

function switchStyleClick(e) {
    var switchId = $("#switchStyleBtn");
    var url = switchId.attr("url");
    if (switchId.text() == "<") {
        switchId.text(">");
    } else {
        switchId.text("<");
    }

    //pārslēdz uz šauro
    var params = {};
    function callBack(data) {
        if ($("#input-container").hasClass("container-fluid")) {
            $("#input-container").attr("class", "container mt-3");
        }
        //pārslēdz uz plato
        else {
            $("#input-container").attr("class", "container-fluid mt-3");
        }
    }
    ajaxGet(url, params, callBack);
}

function showDialogue() {
    var windowId = "messageDialogueContainer";
    var url = $("#messageDialogueContainer").data("request-url");
    function callBack(data) {
        if (data.length > 3) {
            $("#" + windowId).kendoWindow({
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
            var windowData = $("#" + windowId).data("kendoWindow");
            windowData.content(data);
            windowData.center();
            windowData.open();
        }
    }
    try { ajaxGet(url, null, callBack); }
    catch (e) { }
}
// Kendo tabstrip komponentes activate notikums
function kendoTabStripActivate(e) {
    var tabstripContentId = $(e.item).attr("aria-controls");
    var gridList = $("#" + tabstripContentId).find(".k-grid");
    for (var i = 0; i < gridList.length; i++) {
        var gridId = $(gridList[i]).attr("id");

        // Ja atrastajam scrollbar nav child elementu, tas nozīme, ka tabstrip render
        // nav pareizi nostrādājis. Pārlādē sarakstu
        var childrenLength = $("#" + gridId).find(".k-scrollbar").children().length;
        var gridObject = $("#" + gridId).data("kendoGrid");
        if (gridObject == null) {
            gridObject = $("#" + gridId).data("kendoTreeList");
        }
        var gridData = gridObject._data;
        if (childrenLength === 0 && gridData === undefined) {
            kendoGridRefresh(gridId);
        }
    }
}

//pieglabā kendo tabstripa aktīvo tabu, ja viewā ir elements ar nosaukumu 'Model.ActiveTab'
function saveActiveTab(e) {
    var at = $('input[name="Model.ActiveTab"]');
    if (at != null) {
        at.val($(e.item).index().toString());
    }
}

// Kendo custom validācijas apstrāde
function kendoGridCustomErrorHandler(e) {
    if (e.errors) {
        var arr = [];
        var jsonObj = {};
        $.each(e.errors, function (key, value) {
            if ('errors' in value) {
                $.each(value.errors, function () {
                    arr.push(this);
                });
            }
        });

        jsonObj.responseText = arr;
        handleJsonMessage(JSON.stringify(jsonObj));
    }
}

// Kendo grida kontroļu kļūdu apstrāde
function kendoErrorHandler(e) {
    if (e.errors) {
        var message = "Errors:\n";
        $.each(e.errors, function (key, value) {
            if ('errors' in value) {
                $.each(value.errors, function () {
                    message += this + "\n";
                });
            }
        });
        alert(message);
    }
}

// Kendo grida pārlāde
function kendoGridRefresh(id) {
    var grid = $("#" + id).data("kendoGrid");
    if (grid == undefined) {
        grid = $("#" + id).data("kendoTreeList");
    }
    if (grid !== undefined) {
        // Pielieto saglabāto sort
        var options = sessionStorage[id + "-options"];
        var optionsDef = sessionStorage[id + "-def-options"];
        if (options) {
            grid.dataSource.sort(JSON.parse(options));
        } else if (optionsDef) {
            grid.dataSource.sort(JSON.parse(optionsDef));
        } else {
            grid.dataSource.read();
        }
    }
}

// Kendo grida kolonnu automatiskais platums
function kendoGridAutoFitColumn(e) {
    var grid = $(e.sender.element).data("kendoGrid");
    for (var i = 0; i < grid.columns.length; i++) {
        grid.autoFitColumn(i);
    }
    var KendoGridContainerSize = $("#" + e.sender.element[0].id).width() - 18;
    var gridK = grid.columns;
    var KendoGridCurrentRowSize = 0;
    $.each(gridK, function (index, fCol) { if (fCol.hidden != true) KendoGridCurrentRowSize += fCol.width });
    if (KendoGridCurrentRowSize < KendoGridContainerSize) {
        $("#" + e.sender.element[0].id).find("table").css("width", KendoGridContainerSize);
    }

    //Šis apiet kendo grid bug, kad saraksta header tiek nobīdīts horizontāli, balstoties pēc datiem, neievērojot horizontal scroll pozīciju.
    //Kļūdu var atkārtot kārtojot pēdējās saraksta kolonnas ar vertical scroll.
    var hScroll = $("#" + e.sender.element[0].id + " .k-virtual-scrollable-wrap");
    if (hScroll.length != null) {
        hScroll.scrollLeft(hScroll.scrollLeft() - 1);
    }
}

// Kendo grida kārtošanas saglabāšana
function kendoGridSortSave(gridId, storageId) {
    var grid = $("#" + gridId).data("kendoGrid");
    sessionStorage[storageId] = kendo.stringify(grid.getOptions().dataSource.sort);
}

// Kendo grida kārtošanas attīrīšana
function kendoGridSortClear(gridId) {
    sessionStorage.removeItem(gridId + "-options");
}

// Kendo sarakstu dataBound notikums
function kendoGridDataBound(e) {
    var gridId = e.sender.element[0].id;
    // Saglabā noklusētos grida settingus
    if (sessionStorage.getItem(gridId + "-def-options") === null) {
        kendoGridSortSave(gridId, gridId + "-def-options");
    }
    kendoGridSortSave(gridId, gridId + "-options");
}

// Kendo sarakstu databound un autowidth kolonnu funkcija
function kendoGridDataBoundWithAutoFitColumn(e) {
    kendoGridDataBound(e);
    kendoGridAutoFitColumn(e);
}

// Saspiež toolbar 2D kendo sarakstiem

function kendoGridCompressChilds(gridId) {
    var subGrids = document.querySelectorAll("[id^=" + gridId + "_SubGrid_]");
    subGrids.forEach(f => {
        f.style.display = "flex";
        console.log(f.getElementsByClassName("k-grid-toolbar")[0]);
        f.getElementsByClassName("k-grid-toolbar")[0].style.display = "table-cell";
        f.getElementsByClassName("k-grid-toolbar")[0].style.padding = "0";
        f.parentElement.parentElement.children[0].style.display = "none";
        f.parentElement.colSpan = $("#" + gridId).data("kendoGrid").columns.length + 1;
    });
}

// Kendo multiSelect pārlāde
function kendoMultiSelectRefresh(id) {
    var multiselect = $("#" + id).data("kendoMultiSelect");
    if (multiselect !== undefined) {
        multiselect.dataSource.read();
    }
}

// Kendo listbox pārlāde
function kendoListBoxRefresh(id) {
    var listbox = $("#" + id).data("kendoListBox");
    if (listbox !== undefined) {
        listbox.dataSource.read();
    }
}

// Kendo listbox databound notikums
function kendoListBoxDataBound(e) {
    var listBox2Id = $(e.sender.element).attr("listbox2Id");
    var listBox2ResultId = $(e.sender.element).attr("listbox2ResultId")
    $("#" + listBox2Id).bind("DOMSubtreeModified", function () {
        var selectedItemIds = "";
        var dataItems = $("#" + listBox2Id).data("kendoListBox").dataItems();
        for (var i = 0; i < dataItems.length; i++) {
            selectedItemIds = selectedItemIds + dataItems[i].Value + "|";
        }
        $("#" + listBox2ResultId).val(selectedItemIds);
    });
}

// Kendo popup komponentes izveidošana
function kendoWindowCreate(id, title, url, params, width) {
    $("#" + id).kendoWindow({
        title: title,
        modal: true,
        width: width,
        actions: ["Close"],
        content: {
            url: url,
            type: "GET",
            data: params
        },
        refresh: function () {
            this.center();
            handleRequiredLabel();
        },
        close: function (e) {
            $(this.element).empty();
        },
        error: function (e) { handleError(e.message); }
    });
}

// Kendo Excel eksports
function kendoExcelExport(gridId) {
    $("#" + gridId).data("kendoGrid").saveAsExcel();
}

// Kendo Window aizvēršana 
function kendoBtnWindowClose() {
    $("#" + this.element[0].id).closest(".k-window-content").data("kendoWindow").close();
}


// Kendo grida eksports, lai excelī exportētu kendo grid kolonnas kuram ir uzlikti clientTemplate templeiti
function kendoGridExportWithTemplatesContent(e) {
    var data = e.data;
    var gridColumns = e.sender.columns;
    var sheet = e.workbook.sheets[0];
    var visibleGridColumns = [];
    var columnTemplates = [];

    // Create element to generate templates in.
    var elem = document.createElement("div");

    // Get a list of visible columns
    for (var i = 0; i < gridColumns.length; i++) {
        if (!gridColumns[i].hidden) {
            visibleGridColumns.push(gridColumns[i]);
        }
    }

    // Create a collection of the column templates, together with the current column index
    for (var j = 0; j < visibleGridColumns.length; j++) {
        if (visibleGridColumns[j].template) {
            columnTemplates.push({ cellIndex: j, template: kendo.template(visibleGridColumns[j].template) });
        }
    }

    // Traverse all exported rows.
    for (var k = 1; k < sheet.rows.length; k++) {
        var row = sheet.rows[k];
        // Traverse the column templates and apply them for each row at the stored column position.

        // Get the data item corresponding to the current row.
        var dataItem = data[k - 1];
        for (var f = 0; f < columnTemplates.length; f++) {
            var columnTemplate = columnTemplates[f];
            // Generate the template content for the current cell.
            elem.innerHTML = columnTemplate.template(dataItem);
            if (row.cells[columnTemplate.cellIndex] !== undefined) {
                // Output the text content of the templated cell into the exported cell.
                row.cells[columnTemplate.cellIndex].value = elem.textContent || elem.innerText || "";
            }
        }
    }
}


// Obligātuma label uzstādīšana
function handleRequiredLabel() {
    $("input, textarea").each(function () {
        var req = $(this).attr("data-val");
        var override = $(this).attr("override-required-label-value");
        if (override != null) {
            req = override;
        }
        if (req != null) {
            var label = $("label[for='" + $(this).attr("id") + "']");
            var text = label.text();
            if (req === "true") {
                if (text.length > 0 && label.html().indexOf("<span") < 0) {
                    label.append("<span class='text-danger'>*</span>");
                }
            } else if (req === "false") {
                if (text.length > 0 && label.html().indexOf("<span") >= 0) {
                    label.children("span").remove();
                }
            }
        }

    });
}

// Apstradā kļūdas paziņojumu
// Atgriež:
// 0 = Nav ziņojums;
// 1 = Informatīvs ziņojums;
// 2 = Kļūdas ziņojums;
function handleMessage(messages) {
    endLoading();

    var messageList = "";
    if (messages instanceof Array) {
        for (var i = 0; i < messages.length; i++) {
            var message1 = removeErrorPrefix(messages[i]);
            messageList += message1 + "<br/>";
        }
    } else if (messages != null) {
        var message2 = removeErrorPrefix(messages);
        messageList = message2 + "<br/>";
    } else {
        return 0; //Has no response text.
    }
    if (messages.indexOf("ERR:") >= 0) {
        $("#notification").data("kendoNotification").show({
            message: messageList
        }, "error");
        return 2;
    } else {
        $("#notification").data("kendoNotification").show({
            message: messageList
        }, "success");
        return 1;
    }
}


// Apstradā JSON kļūdas paziņojumu
// Atgriež:
// 0 = Nav ziņojums;
// 1 = Informatīvs ziņojums;
// 2 = Kļūdas ziņojums;
function handleJsonMessage(data) {
    try {
        var result = JSON.parse(data);

        endLoading();

        var messageList = "";
        if (result.responseText instanceof Array) {
            for (var i = 0; i < result.responseText.length; i++) {
                var message1 = removeErrorPrefix(result.responseText[i]);
                messageList += message1 + "<br/>";
            }
        } else if (result.responseText != null) {
            var message2 = removeErrorPrefix(result.responseText);
            messageList = message2 + "<br/>";
        } else {
            return 0; //Has no response text.
        }
        if (data.indexOf("SPL_ERR:") > 0) {
            $("#notification").data("kendoNotification").show({
                message: messageList
            }, "error");
            return 2;
        } else {
            $("#notification").data("kendoNotification").show({
                message: messageList
            }, "success");
            return 1;
        }
    } catch (e) { console.log(e); }
    return 0;
}


// Apstrādā kendo sarakstus ar autobind=false
function handleAutoBindFalseGrid() {
    // Apstrādā 2 variantus
    // 1) saraksti ar autobind=false
    // 2) saraksti ar autobind=false iekš tabstrip kontroles
    var tabStripControls = $("main").find(".k-tabstrip");
    var mainGridList = $("main").find(".k-grid");
    if (tabStripControls.length > 0) {
        for (var i = 0; i < tabStripControls.length; i++) {
            var tabStripId = tabStripControls[i].id;
            var selectedTab = $("#" + tabStripId).data("kendoTabStrip").select();
            var selectTabContentId = $(selectedTab).attr("aria-controls");
            var gridList = $("#" + selectTabContentId).find(".k-grid");
            for (var j = 0; j < gridList.length; j++) {
                var gridId = $(gridList[j]).attr("id");
                var autoBind = $("#" + gridId).data("kendoGrid").options.autoBind;
                var gridData = $("#" + gridId).data("kendoGrid")._data;
                if (!autoBind && gridData === undefined) {
                    kendoGridRefresh(gridId);
                }
            }
        }
    } else {
        if (mainGridList.length > 0) {
            for (var k = 0; k < mainGridList.length; k++) {
                var mainGridId = $(mainGridList[k]).attr("id");
                var mainAutoBind = $("#" + mainGridId).data("kendoGrid").options.autoBind;
                var mainGridData = $("#" + mainGridId).data("kendoGrid")._data;
                if (!mainAutoBind && mainGridData === undefined) {
                    kendoGridRefresh(mainGridId);
                }
            }
        }
    }
}


// Apstrādā navigācijas onhover notikumu
function handleNavigationOnHover() {
    const $dropdown = $(".nav-item.dropdown");
    const $dropdownToggle = $(".dropdown-toggle");
    const $dropdownMenu = $(".dropdown-menu");
    const showClass = "show";

    $(window).on("load resize", function () {
        if (this.matchMedia("(min-width: 768px)").matches) {
            $dropdown.hover(
                function () {
                    const $this = $(this);
                    $this.addClass(showClass);
                    $this.find($dropdownToggle).attr("aria-expanded", "true");
                    $this.find($dropdownMenu).addClass(showClass);
                },
                function () {
                    const $this = $(this);
                    $this.removeClass(showClass);
                    $this.find($dropdownToggle).attr("aria-expanded", "false");
                    $this.find($dropdownMenu).removeClass(showClass);
                }
            );
        } else {
            $dropdown.off("mouseenter mouseleave");
        }
    });
}

// Kļūdu prefiksu noņemšana
function removeErrorPrefix(message) {
    var result = "";
    if (message.indexOf("SPL_ERR:") !== -1) {
        result = message.replace("SPL_ERR:", "");
    } else if (message.indexOf("SUCCESS:") !== -1) {
        result = message.replace("SUCCESS:", "");
    } else {
        result = message;
    }
    return result;
}

// Kļūdu apstrāde
function handleError(error) {
    if (error && error.status === 401) {
        window.location.href = $("#homeUrl").data("request-url");
    } else if (error && error.status === 405) {
        var obj2 = { responseText: $("#genDenied").data("message") };
        handleJsonMessage(JSON.stringify(obj2));
    } else {
        var obj1 = { responseText: $("#genError").data("message") };
        handleJsonMessage(JSON.stringify(obj1));
    }
}

// Paziņojumu uzrādīšana pēc pārlādes
function handleRedirectMessage(id) {
    if ($("#" + id).val()) {
        handleJsonMessage(createJsonMessage(id));
    }
}

// JSON objekta izveide paziņojumu uzrādīšanai
function createJsonMessage(messageId) {
    if ($("#" + messageId).val() !== undefined && $("#" + messageId).val().length > 0) {
        var statusMessage = {
            responseText: $("#" + messageId).val()
        };

        return JSON.stringify(statusMessage);
    }
}

// Login lapa
function login(e) {
    e.preventDefault();

    var url = this.element.attr("url");
    window.location.href = url;
}

// Latvija.lv login
function loginLatviaLv(e) {
    e.preventDefault();

    var url = this.element.attr("url");
    window.location.href = url;
}

//DELETE IF EVERYTHING WORKS
// Ieraksta dzēšana
//function indexDelete(e) {
//    e.preventDefault();

//    var gridId = this.element.attr("gridId");
//    var grid = $("#" + gridId).data("kendoGrid");
//    var windowId = this.element.attr("windowId");
//    if (grid == null) {
//        grid = $("#" + gridId).data("kendoTreeList");
//    }
//    var selectedItem = grid.dataItem(grid.select());
//    if (selectedItem === null) {
//        handleJsonMessage(createJsonMessage("IndexNotSelectedMessage"));
//    } else {
//        var url = $(e.sender.element).attr("url");
//        var params = { id: selectedItem.Id };

//        var url = this.element.attr("url");

//        function callBack(data) {
//            if (handleJsonMessage(data) == 0) {
//                $('#'+windowId).html(data);
//                handleRequiredLabel();
//                $("#" + windowId).kendoWindow({
//                    title: "",
//                    modal: true,
//                    actions: ["Close"],
//                    visible: false,
//                    refresh: function () {

//                    },
//                    close: function (e) {
//                        $(this.element).empty();
//                    }
//                });
//                var windowData = $("#" + windowId);
//                windowData.innerHTML(data);
//                windowData.content(data);
//                windowData.center();
//                windowData.open();
//            }
//        }
//        ajaxGet(url, params, callBack);
//    }
//}


function resolveUrl(url) {
    var virtualPath = _GetVirtualPath(); // .toLower()
    if (url.toLowerCase().indexOf(virtualPath) == -1)
    {
        url = virtualPath + url; // Handle slashes, nulls, etc...
    }
    var parts = url.split("/");
    if (parts.length > 1) {
        if (parts[1] == parts[2]) {
            parts.shift();
            parts.shift();
            url = "/" + parts.join("/");
        }
    }
    return url;
}



// Ajax get izsaukums
function ajaxGet(url, params, callBack) {
    url = resolveUrl(url);
    $.ajax({
        url: url,
        type: "GET",
        dataType: "html",
        contentType: "application/json; charset=utf-8",
        data: params,
        async: true,
        success: function (data) {
            callBack(data);
        },
        error: function (error) {
            handleError(error);
        }
    });
}

// Ajax sync get izsaukums
function ajaxGetSync(url, params, callBack) {
    url = resolveUrl(url);
    $.ajax({
        url: url,
        type: "GET",
        crossDomain: true,
        dataType: "jsonp",
        data: params,
        async: false,
        success: function (data) {
            callBack(data);
        },
        error: function (error) {
            //Console.log(error);
            //handleError(error);
        }
    });
}

// Ajax post izsaukums
function ajaxPost(url, callBack, formId) {
    url = resolveUrl(url);
    if ($("#" + formId).valid()) {
        var formData = new FormData($("#" + formId).get(0));
        startLoading();

        $.ajax({
            url: url,
            type: "POST",
            dataType: "html",
            processData: false,
            contentType: false,
            data: formData,
            success: function (data) {
                callBack(data);
            },
            error: function (error) {
                endLoading();
                handleError(error);
            }
        });
    }
    else {
        console.log("Validation errors:");
        console.log($("#" + formId).validate().errorList);
    }
}

// Ajax post izsaukums bez validācijas
function ajaxPostWithoutValidation(url, callBack, formId, handleErrorVar = true) {
    url = resolveUrl(url);
    var formData = null;
    if (formId != null) {
        formData = new FormData($("#" + formId).get(0));
    }

    $.ajax({
        url: url,
        type: "POST",
        dataType: "html",
        processData: false,
        contentType: false,
        data: formData,
        success: function (data) {
            callBack(data);
        },
        error: function (error) {
            if (handleErrorVar) {
                handleError(error);
            }
        }
    });
}

function ajaxPostJson(url, callBack, dataObj) {
    url = resolveUrl(url);
    $.ajax({
        url: url,
        type: "POST",
        dataType: "html",
        processData: false,
        contentType: "application/json; charset=utf-8",
        data: dataObj,
        success: function (data) {
            callBack(data);
        },
        error: function (error) {
            console.log("error");
            callBack(error.responseText);
        }
    });
}


function ajaxPostDiv(url, callBack, dataObj) {
    url = resolveUrl(url);
        $.ajax({
            url: url,
            type: "POST",
            dataType: "html",
            data: { jsonString: dataObj },
            async: true,
            success: function (data) {
                callBack(data);
            },
            error: function (error) {
                handleError(error);
            }
        });
}

function startLoading() {
    document.getElementById('dual-ring').style.display = 'block';
    document.getElementById('cover').style.display = 'block';
    document.getElementById('input-container').style.zIndex = -1;
    document.getElementById('input-container').style.position = "relative";
}

function endLoading() {
    document.getElementById('dual-ring').style.display = 'none';
    document.getElementById('cover').style.display = 'none';
    document.getElementById('input-container').style.zIndex = -1;
    document.getElementById('input-container').style.position = "unset";
}


function hashCode(str) {
    return str.split('').reduce((prevHash, currVal) =>
        (((prevHash << 5) - prevHash) + currVal.charCodeAt(0)) | 0, 0);
}
