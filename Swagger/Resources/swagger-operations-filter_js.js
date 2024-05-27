(function () {
    console.log('swagger-operations-filter.js loaded.');

    swaggerExtender.registerDefinitionLoadedListener(injectOperationsFilterUI);

    function initRouteData(callback) {
        let getRouteData = () => {
            let results = [];
            results.maxPathSegments = 0;
            let opSections = Array.from(document.querySelectorAll(".opblock-tag-section"));
            for (let i = 0; i < opSections.length; i++) {
                let sectionData = { section: opSections[i], routes: [] };
                let opBlocks = Array.from(opSections[i].querySelectorAll(".opblock"));
                for (let k = 0; k < opBlocks.length; k++) {
                    let verb = opBlocks[k].querySelector(".opblock-summary-method").innerText;
                    let segments = opBlocks[k].querySelector(".opblock-summary-path").innerText.replace(/^\//g, "").split('/');
                    if (segments.length > results.maxPathSegments) results.maxPathSegments = segments.length; //store longest path
                    sectionData.routes.push([verb].concat(segments));
                }
                results.push(sectionData);
            }
            return results;
        };

        let collapsedOpBlocks = Array.from(document.querySelectorAll(".opblock-tag[data-is-open=false]"));
        if (collapsedOpBlocks.length) {
            for (let i = 0; i < collapsedOpBlocks.length; i++) collapsedOpBlocks[i].click(); //expand
            setTimeout(() => {
                window.routeData = getRouteData();
                for (let i = 0; i < collapsedOpBlocks.length; i++) collapsedOpBlocks[i].click(); //collapse
                setTimeout(callback);
            }, 30);
        }
        else {
            window.routeData = getRouteData();
            setTimeout(callback);
        }
    }

    function injectOperationsFilterUI() {
        initRouteData(() => {
            const optionTerminatorValue = "{";
            //Add Operation Fade-In Style and Select Style//
            let style = document.createElement("style");
            style.textContent = `
.operation-tag-content{
	opacity: 1;
	animation-name: operationFadeIn;
	animation-iteration-count: 1;
	animation-timing-function: ease-in;
	animation-duration: 0.3s;
}
@keyframes operationFadeIn {
	0% {
		opacity: 0;
        height: 0;
	}
	100% {
		opacity: 1;
        height: 100%;
	}
}
.select.container + .select.container > select {
	margin-left: -3px;
	border-radius: 0 4px 4px 0;
}`;
            document.head.appendChild(style); //when operation blocks are added they will fade in to avoid ugliness from popping in and then being filtered out

            //Add Filter Change Listener//
            let filterContainer = getFilterContainer();
            filterContainer.addEventListener("change", function (ev) { //filter changed listener
                let selects = Array.from(this.querySelectorAll("select"));
                let changedIndex = selects.indexOf(ev.target);
                selects = selects.slice(0, changedIndex + 1); //trims selects at the changed select
                let selectionValues = selects.map(v => v.value);
                updateFilterUI(selectionValues, optionTerminatorValue);
            });

            //Add Clear Filter Button Listener//
            let clearFilterButton = getClearFilterButton();
            clearFilterButton.addEventListener("click", function () {
                updateFilterUI([], optionTerminatorValue);
            });

            //Add Operation Expand/Collapse Listener//
            document.querySelector(".block-desktop > div").addEventListener("click", function (ev) {
                let target = ev.target;
                for (let i = 0; i < 4; i++) {
                    if (target.classList.contains("opblock-tag")) {
                        setTimeout(updateOperationVisibility);
                        return;
                    }
                    target = target.parentElement;
                }
                //ignore other click events..
            });

            //Add Filter Help Icon//
            getHelpIcon();

            //Initial Filter UI Update//
            updateFilterUI([], optionTerminatorValue);
        });
    }

    function getClearFilterButton() {
        getSegmentContainers(); //called to ensure segmentContainers are created before clearFilterButton
        let button = document.querySelector(".clear-filter.button");
        if (button == null) { //ensures verb container//
            button = document.createElement("button");
            button.className = "clear-filter button btn";
            button.textContent = "Clear";
            button.style = "color:#41444e; border-color:#41444e; margin:0 1.2em;";
            let filterContainer = getFilterContainer();
            filterContainer.appendChild(button);
        }
        return button;
    }

    function getFilterContainer() {
        let container = document.querySelector(".filter.wrapper");
        if (container == null) {
            let schemeContainer = document.querySelector(".scheme-container");
            container = document.createElement("section");
            container.className = "filter wrapper";
            container.style = "margin-top:1.4em;";
            var label = document.createElement("span");
            label.style = "font-size:12px; font-weight:700; display:block;";
            label.appendChild(document.createTextNode("Filter"));
            container.appendChild(label);
            schemeContainer.appendChild(container);
        }
        return container;
    }

    function getHelpIcon() {
        getClearFilterButton();//called to ensure clearFilterButton is created before helpIcon
        let icon = document.querySelector(".filter-help.icon");
        if (icon == null) { //ensures verb container//
            icon = document.createElement("span");
            icon.className = "filter-help icon";
            icon.textContent = "?";
            icon.title = "Select filters to show only desired operations. \nFilters with a single option are automatically selected.";
            icon.style = "color:#fff; font-family:sans-serif; font-size:14px; background: #7d8492; border-radius:57px; padding:2px 5px; font-weight:bold; cursor:help;";
            let filterContainer = getFilterContainer();
            filterContainer.appendChild(icon);
        }
        return icon;
    }

    function getIncludedRouteDataRoutes() {
        let included = [];
        for (let b = 0; b < routeData.length; b++) {
            let block = routeData[b];
            if (!block.excluded) {
                for (let r = 0; r < block.routes.length; r++) {
                    let route = block.routes[r];
                    if (!route.excluded) included.push(route);
                }
            }
        }
        return included;
    }

    function getSegmentContainers() {
        getVerbContainer(); //called to ensure verbContainer is created before segmentContainers
        let segmentsCount = window.routeData.maxPathSegments;
        let containers = new Array(segmentsCount);
        for (let i = 0; i < segmentsCount; i++) { //ensures segment containers//
            containers[i] = document.querySelector(`.path.segment.segment-${i}.container`);
            if (containers[i] == null) { //ensures segment container
                containers[i] = document.createElement("span");
                containers[i].className = `path segment segment-${i} select container`;
                let filterContainer = getFilterContainer();
                filterContainer.appendChild(containers[i]);
            }
        }
        return containers;
    }

    function getVerbContainer() {
        let container = document.querySelector(".verb.container");
        if (container == null) { //ensures verb container//
            container = document.createElement("span");
            container.className = "verb method select container";
            let filterContainer = getFilterContainer();
            filterContainer.appendChild(container);
        }
        return container;
    }

    function updateFilterUI(selections, endValue) {
        if (selections == null) selections = [];
        while (selections.length && selections[selections.length - 1] === "") selections.pop(); //remove last entry
        if (selections.length === 0) selections.push(""); //initialize with verb unselected
        selections.push(""); //adds unspecified final filter if more than 1 route

        //Ensures All UI Containers//
        //let filterContainer = getFilterContainer();
        //let verbContainer = getVerbContainer();
        //let segmentContainers = getSegmentContainers();

        //Builds OptionSets with Rules//
        // #1: Auto-selects option when only 1 option exists that all remaining routes match
        // #2: Adds a single trailing unselected option-set if more than 1 route remains
        // #3: Avoids unnecessary option-sets i.e. only 1 route remaining
        let optionSets = [];
        while (true) {
            updateRouteDataWithSelections(selections, endValue);
            optionSets = routeData.optionSets;
            let lastIndex = optionSets.length - 1;
            if (optionSets.length && optionSets[lastIndex].allRoutesMatch) {
                let includedRoutes = getIncludedRouteDataRoutes();
                if (optionSets.length === selections.length) {
                    if (includedRoutes.length > 1) {
                        selections[lastIndex] = optionSets[lastIndex][0]; //auto-select only item option
                        selections.push(""); //if last item was auto-selected push new empty selection to pull next segment options
                    }
                    else if (includedRoutes.length === 1) { //narrowed to a single route
                        optionSets.pop(); //dump trailing/unnecessary optionSet
                        break;
                    }
                    else break;
                }
                else break;
            }
            else break;
        }
        for (let i = 0; i < optionSets.length; i++) optionSets[i].unshift(""); //adds empty option as first option

        //Control Operation Block and Operation Visibility//
        updateOperationVisibility();

        //Dynamically Create Select Filters//
        updateSelectFilters(selections, optionSets);
    }

    function updateOperationVisibility() {
        for (let b = 0; b < routeData.length; b++) {
            if (routeData[b].excluded) routeData[b].section.style.display = "none"; //show operation block
            else {
                routeData[b].section.style.display = ""; //hide operation block
                if (routeData[b].section.classList.contains("is-open")) {
                    let excludedOperations = routeData[b].routes.filter(operation => operation.excluded);
                    let allOperationEls = Array.from(routeData[b].section.querySelectorAll(".opblock"));
                    let hideOperationEls = allOperationEls.filter(v => {
                        for (let o = 0; o < excludedOperations.length; o++) { //check element route against each excluded operation route
                            let verb = v.querySelector(".opblock-summary-method").innerText;
                            let segments = v.querySelector(".opblock-summary-path").innerText.replace(/^\//g, "").split('/');
                            segments.unshift(verb);
                            if (segments.length == excludedOperations[o].length) { //check that number of segments is the same
                                let isMatch = true;
                                for (let i = 0; i < segments.length; i++) { //check that each segment matches
                                    if (excludedOperations[o][i] !== segments[i]) {
                                        isMatch = false;
                                        break;
                                    }
                                }
                                if (isMatch) return true; //each route segment matches
                            }
                        }
                        return false; //checked all excluded operation; no match was found
                    });
                    for (let i = 0; i < allOperationEls.length; i++) allOperationEls[i].style.display = ""; //show all operations
                    for (let i = 0; i < hideOperationEls.length; i++) hideOperationEls[i].style.display = "none"; //hide specific operations
                }
            }
        }
    }

    function updateRouteDataWithSelections(selections, terminatorValue /*does not include options that contain the terminatorValue*/) {
        let selectedRoute = selections;
        //Clear Exclusions//
        for (let b = 0; b < routeData.length; b++) {
            routeData[b].excluded = false; //marks operation block as not excluded
            for (let o = 0; o < routeData[b].routes.length; o++) {
                routeData[b].routes[o].excluded = false; //marks operation as not excluded
            }
        }
        //Clear Options Sets//
        routeData.optionSets = [];

        for (let i = 0; i < selectedRoute.length; i++) {
            //Add Options Sets//
            let options = getIncludedRouteDataRoutes()
                .map(route => route[i]) //gets segments at current position; may be undefined if route is shorter than selected route
                .filter((segment, i, arr) => arr.indexOf(segment) === i); //gets distinct segments
            let allRoutesMatch = options.length === 1;
            options = options.filter(option => option !== undefined && option.indexOf(terminatorValue) === -1); //remove undefined
            options.allRoutesMatch = allRoutesMatch;
            if (options.length) routeData.optionSets[i] = options; //as long as options exist continue
            else break; //no options remaining; so finish
            //Add Exclusions//
            if (selectedRoute[i] !== "") { //ignore blank route segments; indicates "no selection"
                for (let b = 0; b < routeData.length; b++) {
                    let block = routeData[b];
                    for (let r = 0; r < block.routes.length; r++) {
                        let route = block.routes[r];
                        if (selectedRoute[i] !== route[i]) route.excluded = true; //marks route that does not match selectedRoute as excluded
                    }
                    if (!block.routes.some(r => !r.excluded)) block.excluded = true; //marks block with no included routes as excluded
                }
            }
        }
        routeData.selectedRoute = selectedRoute;
    }

    function updateSelectFilters(selections, optionSets) {
        let filterContainer = getFilterContainer();
        let verbContainer = getVerbContainer();
        let segmentContainers = getSegmentContainers();
        //Empty Dropdown Containers//
        let containers = Array.from(filterContainer.querySelectorAll("span.container"));
        for (let i = 0; i < containers.length; i++) while (containers[i].hasChildNodes()) containers[i].removeChild(containers[i].lastChild);
        //Adds UI Filters//
        for (let i = 0; i < selections.length; i++) {
            let container = i === 0 ? verbContainer : segmentContainers[i - 1]; //verb first selection is before segments
            let select = document.createElement("select");
            for (let k = 0; k < optionSets[i].length; k++) {
                option = document.createElement("option");
                option.text = optionSets[i][k];
                option.value = option.text;
                select.options.add(option);
            }
            container.appendChild(select);
            select.value = selections[i];
        }
    }
})();