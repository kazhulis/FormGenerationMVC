var setFilterInProgress = false;

function indexApplyFilter() {
    var url = this.element.attr("url");
    var json = getFilterValues(this);
    var gridId = this.element.attr("gridId");

    function callBack(data) {
        kendoGridRefresh(gridId);
    }
    ajaxPostJson(url, callBack, json);
}

function indexClearFilter() {
    var url = this.element.attr("url");
    var containerId = this.element.attr("containerId");
    var gridId = this.element.attr("gridId");

    var params = {};
    function callBack(data) {
        console.log(containerId);
        replaceFilterContainer(containerId, data);
        kendoGridSortClear(gridId);
        kendoGridRefresh(gridId);
    }
    ajaxGet(url, params, callBack);
}

function indexSetFilterValues() {
    var url = this.element.attr("url");
    var containerId = this.element.attr("containerId");
    var gridId = this.element.attr("gridId");
    var selectedId = this.element.data("kendoComboBox").value();
    var json = getFilterValues(this);
    var urlProp = url + "?selectedValue=" + selectedId;

    function callBack(data) {
        replaceFilterContainer(containerId, data);
        setFilterInProgress = false;
        kendoGridSortClear(gridId);
        kendoGridRefresh(gridId);
    }

    setFilterInProgress = true;
    try {
        ajaxPostJson(urlProp, callBack, json);
    }
    catch (e) {
        setFilterInProgress = false;
        console.error(e);
    }
}


function indexSaveFilter() {
    //Call async function with recursion
    indexSaveFilterProgressCheck(this);
}

async function indexSaveFilterProgressCheck(e) {
    if (setFilterInProgress === true) {
        await sleep(100);
        indexSaveFilterProgressCheck(e);
    }
    else {
        var url = e.element.attr("url");
        var url2 = e.element.attr("url2");
        var containerId = e.element.attr("containerId");
        var json = getFilterValues(e);

        function callBack2(data) {
            replaceFilterContainer(containerId, data);
        }

        function callBack(data) {           
            handleMessage(JSON.parse(data).responseText);
            ajaxGet(url2, null, callBack2);
        }

        ajaxPostJson(url, callBack, json);
    }
}

function indexFilterDelete() {
    var url = this.element.attr("url");
    var containerId = this.element.attr("containerId");

    function callBack(data) {
        replaceFilterContainer(containerId, data);
    }
    ajaxGet(url, null, callBack);
}

function getFilterValues(e) {
    var url = e.element.attr("url");
    var containerId = e.element.attr("containerId");

    var obj = [];
    var inputs = $(`#${containerId} :input`);
    inputs.each(function (i, f) {
        if (f.name.indexOf("FilterValues[") >= 0) {
            var name = f.name.replace("FilterValues[", "").replace("]", "");
            obj.push({ Key: name, Value: f.value });
        }
    });

    return JSON.stringify(obj);
}

function replaceFilterContainer(containerId, data) {  
    $("#" + containerId).html(data);
    var validator = $("#" + containerId).closest("form").validate();
    if (validator != null) {
        validator.resetForm();
    }    
}

const sleep = ms => new Promise(res => setTimeout(res, ms));


