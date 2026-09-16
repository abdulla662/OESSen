

function buildDynamicAnchorElement(fileName, fileContent) {

    if (fileName != null
        && fileName.length > 0
        && fileContent != null
        && fileName.length > 0) {
        let fileLink = document.createElement("a");

        fileLink.href = fileContent;

        fileLink.download = fileName;

        fileLink.click();
    }
}


function downloadFileUsingFetch(url, fileName, body) {
    fetch(url, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${localStorage.getItem("token")}`,
            "Content-Type": "application/json"
        },
        body: JSON.stringify(body)
    })
    .then(response => response.blob())
    .then(blob => {
        const fileUrl = URL.createObjectURL(blob);

        const link = document.createElement('a');

        link.href = fileUrl;

        link.download = fileName;

        document.body.appendChild(link);

        link.click();

        document.body.removeChild(link);

        URL.revokeObjectURL(fileUrl);
    })
    .catch();
}


function toggleExpansion(event) {
    const ul = event.target.nextElementSibling;

    if (ul) {
        ul.classList.toggle('hidden');
        event.target.classList.toggle('collapsed');
    }
}



window.registerExpandedTreeViewTemplateComponent = function (referenceObject) {
    window.expandedTreeComponentInstance = referenceObject;
};



async function sendValueToBlazor(event, value) {
    if (window.expandedTreeComponentInstance) {
         await window
        .expandedTreeComponentInstance
        .invokeMethodAsync('GetItemIdFromExpandedTree', value)
        .then()
        .catch();
    event.stopPropagation();
    }
}


function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
}


window.sessionStorageHelper = {
    setItem: function (key, value) {
        sessionStorage.setItem(key, value);
    },
    getItem: function (key) {
        return sessionStorage.getItem(key);
    },
    removeItem: function (key) {
        sessionStorage.removeItem(key);
    }
};


function scrollToElement(id) {
    let element = document.getElementById(id);

    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        element.style.background ="rgb(153 215 240)";
        element.classList.add('search-found-highlight');
        setTimeout(() => {
            element.classList.remove('search-found-highlight');
        }, 3000);
    }
}


window.downloadFile = function (url) {
    const anchor = document.createElement('a');

    anchor.href = url;

    anchor.download = url.split('/').pop();

    document.body.appendChild(anchor);

    anchor.click();

    document.body.removeChild(anchor);
};
