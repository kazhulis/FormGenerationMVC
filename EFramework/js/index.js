//This file is part of index generation dynamic update logic.
//The goal is to dynamicly update html on user data change.

var elementDataRegistry = {}; //Stores key/value pair of last form data for given form. Key = formId; Data = FormData object.
var elementHashRegistry = {}; //Stores key/value pair of last element server side hash code. Key = containerId; Data = Hash value.

var runtimeFormUpdateRegistry = {}; //Lock array.

var eventOnDynamicUpdated = []; //Event - calls all functions after update has completed.

var elementChangeLog = []; //Stores latest changes made to each control. Used to generate return confirm dialog.
var showUnsavedChangesWarning = false;

$(window).load(function () {
    OnControlChange();

    window.onbeforeunload = function () {
        if (showUnsavedChangesWarning)
            return _IndexReturnMsg();
    };

});

//Executes default scripts and starts monitoring for user changes.
function prepareDocument(url, frmId) {
    if (document.getElementById(frmId) == null) {
        console.log("Invalid form id: '" + frmId + "', monitoring disabled.");
        return;
    }

    updateControlHashes(url, frmId);
    startChangeMonitor(url, frmId);
}

//Applies default scripts. Required on every control change.
function OnControlChange() {
    showHideContainers();
    showHideGroups();
    showHideTabs();

    fixControlValidation();
    handleRequiredLabel();
}

//Executes user change monitor for current form.
function startChangeMonitor(url, frmId) {

    elementDataRegistry[frmId] = new FormData(document.getElementById(frmId));

    var handler = function () { handleControlChange(url, frmId); };

    var observer = new MutationObserver(function (mutations) {
        mutations.forEach(handler);
    });

    var config = { attributes: true, childList: true, subtree: true };
    observer.observe(document.getElementById(frmId), config);

    document.addEventListener("keyup", handler);

    console.log("[DynamicUpdate] Change monitoring started for: " + frmId);
}

//Does not allow to calculate changes more then once per timeout.
const checkTimeout = 200;
var isCheckTimeoutSet = {};
function handleControlChange(url, frmId) {
    if (isCheckTimeoutSet[frmId] === true) return;
    isCheckTimeoutSet[frmId] = true;
    setTimeout(function () {
        calculateControlChange(url, frmId);
        isCheckTimeoutSet[frmId] = false;
    }, checkTimeout);
}

//Valuates and posts if user changes has been made.
function calculateControlChange(url, frmId) {

    if (runtimeFormUpdateRegistry[frmId] === true) return;
    runtimeFormUpdateRegistry[frmId] = true;

    try {
        var elem = document.getElementById(frmId);

        if (elem == null) return;

        var newData = new FormData(elem);
        var newDataEnteries = newData.entries();

        var curData = elementDataRegistry[frmId];
        var curDataEnteries = curData.entries();

        var newDataVal = newDataEnteries.next();
        var curDataVal = curDataEnteries.next();

        var isDifferent = [];

        try {
            var breakLoop = false;
            while (!breakLoop) {

                if (newDataVal.done != curDataVal.done) {
                    console.log("[DynamicUpdate] Post data count differs (" + curDataVal.done + " => " + newDataVal.done + ")");
                    breakLoop = true;
                }
                else if (newDataVal.value[0] != curDataVal.value[0]) {
                    console.log("[DynamicUpdate] Post data structure differs!");
                    isDifferent.push(newDataVal.value[0]);
                    breakLoop = true;
                }
                else {
                    if (isValueChanged(newDataVal.value[1], curDataVal.value[1])) {
                        if (newDataVal.value[0] != "__RequestVerificationToken") {
                            //For debug.
                            console.log("[DynamicUpdate] Changed " + curDataVal.value[0] + ": " + curDataVal.value[1] + " => " + newDataVal.value[1]);
                            isDifferent.push(curDataVal.value[0]);
                            logElementChanges(curDataVal.value[0], curDataVal.value[1], newDataVal.value[1]);
                        }
                    }
                }
                newDataVal = newDataEnteries.next();
                curDataVal = curDataEnteries.next();

                if (newDataVal.done === true && curDataVal.done === true) {
                    breakLoop = true;
                }

            }

            //Filter out ignore controls.
            var newIsDifferent = [];
            isDifferent.forEach(function (fName) {
                var element = document.getElementsByName(fName)[0];
                if (element != null) {
                    var container = element.closest('[ctrl="Container"]');
                    if (container != null) {
                        var ignore = (container.getAttribute("dynamicUpdate") == "ignore"); //Search for ignore in first level container.
                        if (!ignore) {
                            container = container.parentNode.closest('[ctrl="Container"]');
                            if (container != null) {
                                var ignore = (container.getAttribute("dynamicUpdate") == "ignore"); //Search for ignore in parent level container.
                            }
                        }
                        if (!ignore) {
                            newIsDifferent.push(fName);
                        }
                    }
                }
            });
            isDifferent = newIsDifferent;

            if (newIsDifferent.length > 0) {
                eventOnDynamicUpdated.forEach(function (f) { f(); })
            }

        }
        catch (e) {
            console.error(e);
        }
        if (isDifferent.length > 0) {
            elementDataRegistry[frmId] = newData;

            var ignoreList = [];
            isDifferent.forEach(function (fName) {
                var element = document.getElementsByName(fName)[0];
                if (element != null) {
                    var container = element.closest('[ctrl="Container"]');
                    if (container != null) {
                        ignoreList.push(container.id);
                    }
                }
            });
            compareControlHashes(url, frmId, ignoreList);
        }

    }
    catch (e) { console.error(e); }
    finally {
        runtimeFormUpdateRegistry[frmId] = false;
    }
}

function isValueChanged(newVal, oldVal) {

    if (newVal instanceof File && oldVal instanceof File) {
        return newVal.size != oldVal.size;
    }

    return newVal != oldVal;
}

//Gets and saves current raw html hash values for every control.
//Must be used only on document load.
function updateControlHashes(url, frmId) {

    var callBack = function (data) {
        runtimeFormUpdateRegistry[frmId] = true;

        try {
            var result = JSON.parse(data).Controls;

            for (var i = 0; i < result.length; i++) {
                var obj = result[i];
                elementHashRegistry[obj.Id] = obj.Hash;
            }
        }
        catch (e) { console.error(e); }
        finally { runtimeFormUpdateRegistry[frmId] = false; }
    }
    ajaxPostWithoutValidation(url, callBack, frmId);
}

//Compares and updates controls that has been changed.
function compareControlHashes(url, frmId, ignoreList, callback) {

    var ajaxCallBack = function (data) {

        runtimeFormUpdateRegistry[frmId] = true;
        var wasUpdated = false;
        try {
            var result = JSON.parse(data).Controls;

            for (var i = 0; i < result.length; i++) {
                var obj = result[i];
                var curHash = elementHashRegistry[obj.Id];
                var newHash = obj.Hash;
                if (curHash != newHash) {
                    if (ignoreList == null || !ignoreList.includes(obj.Id)) {
                        var element = document.getElementById(obj.Id);
                        var container = element.closest('[ctrl="Container"]');
                        var ignore = (container == null || container.getAttribute("dynamicUpdate") === "ignore")
                        if (!ignore) { //Do not update controls with ignore. 
                            console.log("[DynamicUpdate] Updating: " + obj.Id);
                            $(element).html(obj.Inner);
                            wasUpdated = true;
                            //Add validations for new elements
                            container.querySelectorAll('[data-val="true"]')
                                .forEach(f => $("#" + f.id)
                                    .rules('add', { required: true, messages: { required: f.getAttribute('data-val-required') } }));
                        }
                    }
                    elementHashRegistry[obj.Id] = newHash;
                }
            }
        }
        catch (e) { console.error(e); }
        finally {
            runtimeFormUpdateRegistry[frmId] = false;
            if (callback != null) { callback(data); }
        }

        if (wasUpdated) {
            OnControlChange();
            $("#" + frmId).validate().resetForm();
        }
    }
    ajaxPostWithoutValidation(url, ajaxCallBack, frmId, false);
}

//Hides control containers that has no content.
function showHideContainers() {
    document.querySelectorAll('[ctrl="Container"]').forEach(fCtrl => {
        var isEmpty = !fCtrl.innerHTML.includes("<");
        var dispaly = null;
        if (isEmpty) dispaly = "none";
        fCtrl.style.display = dispaly;
    });
}

//Hides groups that controls has no contents.
function showHideGroups() {
    document.querySelectorAll('[ctrl="Group"]').forEach(fGroup => {
        var isEmpty = true;
        fGroup.querySelectorAll('[ctrl="Container"]').forEach(fCtrl => {
            if (fCtrl.innerHTML.includes("<")) {
                isEmpty = false;
                return;
            }
        });
        var dispaly = null;
        if (isEmpty) dispaly = "none";
        fGroup.style.display = dispaly;
    });
}

//Hides tabs that controls has no contents.
function showHideTabs() {
    document.querySelectorAll('[ctrl="Tab"]').forEach(fTab => {
        var isEmpty = true;
        fTab.querySelectorAll('[ctrl="Group"]').forEach(fGroup => {
            fGroup.querySelectorAll('[ctrl="Container"]').forEach(fCtrl => {
                if (fCtrl.innerHTML.includes("<")) {
                    isEmpty = false;
                    return;
                }
            });
        });

        var fTabIdx = fTab.getAttribute("TabIdx");
        var fTabHead = document.querySelector('[ctrl="TabHead"][tabIdx="' + fTabIdx + '"]');
        var dispaly = null;
        if (isEmpty) dispaly = "none";
        fTab.style.display = dispaly;
        fTabHead.style.display = dispaly;
    });
}

//Disables validation for controls, that are readonly.
function fixControlValidation() {
    document.querySelectorAll('[ctrl="Container"]').forEach(fCtrl => {
        var validator = fCtrl.querySelector('[ctrl="Validator"]');
        if (validator == null) {
            fCtrl.querySelectorAll('[data-val="true"]').forEach(fValidCtrl => {
                fValidCtrl.setAttribute("data-val", "false");
                fValidCtrl.classList.add('data-val-ignore')
            });
            fCtrl.querySelectorAll('[data-valmsg-replace="true"]').forEach(fValidSpan => {
                fValidSpan.setAttribute("data-valmsg-replace", "false");
            });
        }
    });
}

function logElementChanges(key, oldValue, newValue) {
    //Log changes.
    var isReadonly = $('[name="' + key + '"]').prop('readonly');
    if (!isReadonly) {
        if (elementChangeLog[key] == null) {
            elementChangeLog[key] = {};
            elementChangeLog[key].Key = key;
            elementChangeLog[key].OldValue = oldValue;
        }
        elementChangeLog[key].NewValue = newValue;
        var changes = 0;
        for (var fProp in elementChangeLog) {
            if (Object.prototype.hasOwnProperty.call(elementChangeLog, fProp)) {
                if (elementChangeLog[fProp].OldValue !== elementChangeLog[fProp].NewValue) {
                    if (fProp.indexOf('FilterValues[') === -1) {
                        changes += 1;
                    }
                }
            }
        }
    }
    showUnsavedChangesWarning = (changes > 0);
}
