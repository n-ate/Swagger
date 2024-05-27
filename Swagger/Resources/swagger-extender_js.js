window.swaggerExtender = (function () {
    console.log('swagger-extentender.js loaded.');

    function whenReady(name, readyFunction, callbackFunction, maxChecks) {
        if (maxChecks == 0) console.log('whenReady function reached the maximum number of checks. Name: ' + name);
        else if (readyFunction()) callbackFunction();
        else setTimeout(() => { whenReady(name, readyFunction, callbackFunction, maxChecks--); }, 50);
    }

    whenReady(
        "when page loaded, trigger definition loaded event",
        () => !!document.querySelector(".opblock-tag-section") && !!document.querySelector(".information-container .info .main .url"), //rendered correctly
        triggerDefinitionChangedListeners,
        100
    );

    whenReady(
        "when page erred, trigger definition erred event",
        () => !!document.querySelector(".swagger-ui .loading-container .errors-wrapper:not(.swagger-extender-handled) .errors"), //rendered with errors
        triggerDefinitionErredListeners,
        100
    );

    whenReady( //registers a change listener on the definition dropdown
        "when definition dropdown, add listener",
        () => !!document.querySelector("label.select-label select#select"),
        () => {
            document.querySelector("label.select-label select#select").addEventListener("change", (ev) => {
                let newDefinition = (ev.originalTarget ?? ev.currentTarget).value;
                whenReady(
                    "when definition loaded, trigger definition loaded event",
                    () => (!!document.querySelector(".opblock-tag-section") && !!document.querySelector(".information-container .info .main .url") && document.querySelector(".information-container .info .main .url").innerText == newDefinition), //page has updated to newly selected definition..
                    triggerDefinitionChangedListeners,
                    100
                );
                whenReady(
                    "when definition erred, trigger definition erred event",
                    () => !!document.querySelector(".swagger-ui .loading-container .errors-wrapper:not(.swagger-extender-handled) .errors"), //rendered with errors
                    triggerDefinitionErredListeners,
                    100
                );
            });
        },
        100
    );

    //function findClosestReactNode(node, conditionFunction) {
    //    if (!conditionFunction) conditionFunction = () => { return true; };
    //    while (node !== window) {
    //        for (var key in node) {
    //            if (key.startsWith("__reactInternalInstance$")) {
    //                if (conditionFunction(node[key])) return node[key];
    //            }
    //        }
    //        node = node.parentNode;
    //    }
    //    return null;
    //}

    function _setReactBoundElementValue(element, value) {
        const valueSetter = Object.getOwnPropertyDescriptor(element, 'value').set;
        const prototype = Object.getPrototypeOf(element);
        const prototypeValueSetter = Object.getOwnPropertyDescriptor(prototype, 'value').set;

        if (valueSetter && valueSetter !== prototypeValueSetter) {
            prototypeValueSetter.call(element, value);
        } else {
            valueSetter.call(element, value);
        }
        element.dispatchEvent(new Event('input', { bubbles: true }));
    }

    function triggerDefinitionChangedListeners() {
        for (let i = 0; i < props.listeners.definitionLoaded.length; i++) {
            props.listeners.definitionLoaded[i]();
        }
    }

    function triggerDefinitionErredListeners() {
        document.querySelector(".swagger-ui .loading-container .errors-wrapper:not(.swagger-extender-handled)").classList.add("swagger-extender-handled"); //marks the error as handled to prevent multiple events
        for (let i = 0; i < props.listeners.definitionErred.length; i++) {
            props.listeners.definitionErred[i]();
        }
    }

    //private properties//
    let props = {
        autoAuthorization: false,
        listeners: {
            definitionLoaded: [],
            definitionErred: []
        },
        stackTraceFormatFields: []
    };

    //public class members//
    let pub = {
        appendExplanationHtml(html) {
            let container = document.querySelector(".information-container .info");
            container.className += " injected explanation";
            let div = document.createElement("div");
            div.innerHTML = html;
            while (div.childNodes.length) {
                if (div.childNodes[0].nodeName === "SCRIPT") {
                    let script = document.createElement("script");
                    script.append(document.createTextNode(div.childNodes[0].innerHTML));
                    container.append(script);
                    div.childNodes[0].remove();
                }
                else container.append(div.childNodes[0]);
            }
        },
        appendSubtitle(subtitle) {
            let host = document.querySelector("h2.title");
            let container = document.createElement('em');
            container.innerText = subtitle;
            container.className = "injected subtitle";
            host.appendChild(container);
        },
        autoAuthorize() {
            if (props.autoAuthorization) return;
            props.autoAuthorization = true;
            pub.registerDefinitionLoadedListener(automaticallyAuthorize);
        },
        getVersion() {
            let container = document.querySelector(".information-container .info .version");
            return container == null ? null : container.innerText.trim();
        },
        registerResultStackTraceFormatter(stackTraceFieldName) {
            props.stackTraceFormatFields.push(stackTraceFieldName);
        },
        registerDefinitionLoadedListener(callback) {
            if (typeof callback !== "function") throw new Error("registerDefinitionLoadedListener argument must be a function.");
            props.listeners.definitionLoaded.push(callback);
        },
        registerDefinitionErredListener(callback) {
            if (typeof callback !== "function") throw new Error("registerDefinitionErredListener argument must be a function.");
            props.listeners.definitionErred.push(callback);
        },
        setReactBoundElementValue(element, value) {
            return _setReactBoundElementValue(element, value);
        }
    };

    //auto-authorize extension//
    function automaticallyAuthorize() {
        let button = document.querySelector(".btn.authorize.unlocked");
        if (button) {
            button.click();
            whenReady( //yield back to allow modal to pop
                "when modal pops, authorize",
                () => !!document.querySelector("div.auth-container div.scopes label") && !!document.querySelector("div.auth-container div.scopes span.item"),
                () => {
                    let segments = document.querySelector("div.auth-container div.scopes label").getAttribute('for').split('/').reverse().filter((segment) => { return /^[-0-9a-z]{36}$/.test(segment); });
                    if (segments.length) {
                        let clientIdInput = document.getElementById('client_id');
                        let scopeCheckbox = document.querySelector("div.auth-container div.scopes span.item");

                        _setReactBoundElementValue(clientIdInput, segments[0]); // set client_id
                        scopeCheckbox.click(); //set scope checkbox

                        setTimeout(() => { //allow form to rest before clicking authorize button that will launch a pop-up tab
                            document.querySelector(".modal-btn.authorize.button").click(); // submit authorization
                            setTimeout(() => { //allow modal script to launch pop-up tab before closing
                                document.querySelector(".modal-btn.auth.btn-done.button").click(); // close modal
                                setTimeout(() => { //allow modal to close and verify lock icon
                                    let authSuccess = !!document.querySelector(".auth-wrapper .btn.authorize.locked");
                                    if (!authSuccess) {
                                        createPopupNotice();
                                        setTimeout(() => { //lock icon is slow in edge so verify again after delay, then remove notice if authorization worked
                                            let authSuccess = !!document.querySelector(".auth-wrapper .btn.authorize.locked");
                                            if (authSuccess) document.querySelector(".popup-notice.container").remove();
                                            else {
                                                setTimeout(() => { //lock icon is slow in edge so verify again after delay, then remove notice if authorization worked
                                                    let authSuccess = !!document.querySelector(".auth-wrapper .btn.authorize.locked");
                                                    if (authSuccess) document.querySelector(".popup-notice.container").remove();
                                                }, 500);
                                            }
                                        }, 500);
                                    }
                                }, 800);
                            }, 20);
                        }, 20);
                    }
                    else console.log("client_id not found!");
                },
                100
            );
        }
    }
    function createPopupNotice() {
        let agent = navigator.userAgent.split(" ");
        let product = agent[agent.length - 1].split("/")[0].toLowerCase();

        let notice = document.createElement("div");
        notice.className = `popup-notice container browser-${product}`;

        let closeNotice = document.createElement("div");
        closeNotice.className = "close action";
        closeNotice.innerText = "X";
        closeNotice.addEventListener("click", function () { this.parentElement.remove(); });
        notice.appendChild(closeNotice);

        let arrowIcon = document.createElement("span");
        arrowIcon.className = "arrow icon";
        arrowIcon.innerText = "▲";
        notice.appendChild(arrowIcon);

        let summary = document.createElement("p");
        summary.className = "summary";
        summary.innerText = "Pop-ups may be blocked!";
        notice.appendChild(summary);

        let popupBlockedIcon = document.createElement("img");
        popupBlockedIcon.className = "popup-block icon";
        popupBlockedIcon.src = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABgAAAAfCAYAAAD9cg1AAAABhGlDQ1BJQ0MgcHJvZmlsZQAAKJF9kT1Iw0AcxV9TRakVh3YQcchQnSyIijiWKhbBQmkrtOpgcukXNGlIUlwcBdeCgx+LVQcXZ10dXAVB8APE1cVJ0UVK/F9SaBHjwXE/3t173L0DhGaVqWZPDFA1y0gn4mIuvyr2vSKAAYTgx6TETD2ZWczCc3zdw8fXuyjP8j735xhUCiYDfCJxjOmGRbxBPLtp6Zz3icOsLCnE58QTBl2Q+JHrsstvnEsOCzwzbGTT88RhYrHUxXIXs7KhEs8QRxRVo3wh57LCeYuzWq2z9j35C4MFbSXDdZqjSGAJSaQgQkYdFVRhIUqrRoqJNO3HPfwjjj9FLplcFTByLKAGFZLjB/+D392axekpNykYB3pfbPtjDOjbBVoN2/4+tu3WCeB/Bq60jr/WBOY+SW90tMgRMLQNXFx3NHkPuNwBhp90yZAcyU9TKBaB9zP6pjwQugUCa25v7X2cPgBZ6mr5Bjg4BMZLlL3u8e7+7t7+PdPu7weeiHK4lcvDdgAAAAZiS0dEAAAAAAAA+UO7fwAAAAlwSFlzAAAOwwAADsMBx2+oZAAAAAd0SU1FB+cKCxMOL3EWE8QAAAGISURBVEjH7ZXPSgJRFIc/nRaOy2ihT9FGJBCTjDYm+QhBy2wwisi0v0ZD0qaQWg7YGwRjG1FQUSJm41MUiEJF6maohRmWVpMyO3/bc+/57v2dc++xRPZP1oAU5kiympgcIGXFZI0BY8DomjCyaHHeh98zgyAIA+O6rpMv35HJFYYD+D0z7J1d0Gy1B8btoo3jrcjwAEEQkKMb5lkEsH4g/xo/P4oZB/zl+cg3+O75T6frVa3eMA4Y5LldtNFstZnW7pm8venbU0nEmQ6EqLrcxmrQ6/nCrIfDTYmn5xcqU3FcD4+UtmOI1woAreUVvKcymtMBRgG9yhbLZItlAOaAUjRG8CqFCmCB4GUKNSwhjtJFvRLTCioQvOrMKjUsIaYV41+FruvYRZtx4ts/uyhfvuN4K9LXprV6g0oizms4wlJSRl2VOhZ92NWtyZ+ATK4w8Nl3a+Db3UFdlT4TqoA3KaMZBfymRiCE5nR8KaiYVtDSCo1AaHRA1eXua8XxRBsDzAdIJuaX3gHKy3+F08awugAAAABJRU5ErkJggg==";
        popupBlockedIcon.alt = "Pop-up block icon should look something like this.";
        summary.prepend(popupBlockedIcon);

        let description = document.createElement("p");
        description.className = "description";
        description.innerText = "Allow pop-ups for this site. Then refresh to auto-authorize.";
        notice.appendChild(description);

        document.body.appendChild(notice);
    }

    //error button helper service//
    function errorButtonHelper() { //When Swashbuckle encounters errors during swagger.json generation they are only visible by navigating to the swagger.json url. This is not apparent so we've added a button to prompt developers to view the erred file.
        pub.registerDefinitionErredListener(function () {
            let errors = document.querySelectorAll(".swagger-ui .loading-container .errors-wrapper .errors .message");
            if (errors.length) {
                let currentDirectory = location.pathname.replace("index.html", "");
                let swaggerJsonUrl = document.querySelector(".swagger-ui .topbar #select option:checked").value;
                let anchor = document.createElement("a");
                anchor.href = swaggerJsonUrl.replace(currentDirectory, "");
                anchor.className = "btn view-swagger-json";
                anchor.style = "text-decoration: none; color: rgb(59, 65, 81);margin: 2em 0 0 calc(50% - 8.7em / 2);display: inline-block;width: 8.7em;"
                anchor.target = "swagger-json";
                anchor.innerText = "view error";
                errors.item(0).parentElement.appendChild(anchor);
            }
            else console.error("Unhandled swagger UI failure encountered.");
        });
    }
    errorButtonHelper();

    //stack trace formatting service//
    function executeAllResultStackTraceFormatters() {
        let unprocessed = document.querySelectorAll(".hljs-attr:not(.processed)");
        for (let i = 0; i < unprocessed.length; i++) {
            if (props.stackTraceFormatFields.some(function (x) { return unprocessed[i].innerText == `"${x}"`; })) {
                let value = unprocessed[i].nextSibling.nextSibling;
                let pre = value;
                while (pre.nodeName != "PRE") pre = pre.parentElement;
                //let code = pre.firstChild;
                pre.style.width = "auto"; //reset to original value
                pre.style.width = pre.clientWidth + "px"; //set to current width
                value.classList.add("stack-trace");
                let lines = value.innerText.replaceAll("\\\\", "\\").split("\\r\\n");
                if (lines.length && lines[0][0] == '"') { //move first quote to its own line..
                    lines[0] = lines[0].substring(1);
                    lines.unshift('"');
                }
                while (value.childNodes.length) value.removeChild(value.childNodes[0]); //clear existing text..
                for (let k = 0; k < lines.length; k++) {
                    let p = document.createElement("p");
                    p.textContent = lines[k];
                    p.classList.add("stack-trace-line");
                    if (k == 0 && lines[0] == '"') p.classList.add("quote-start");
                    value.appendChild(p);
                }
            }
            unprocessed[i].classList.add("processed");
        }
        setTimeout(executeAllResultStackTraceFormatters, 150);
    }
    executeAllResultStackTraceFormatters();

    return pub;
})();