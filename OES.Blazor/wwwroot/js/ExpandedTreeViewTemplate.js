

function buildTreeAsHtmlTags(jsonArray) {
    // Step 1: Create a map of nodes by their id:
    const nodeMap = new Map();
    jsonArray.forEach(node => nodeMap.set(node.id, { ...node, children: [] }));

    // Step 2: Populate the children arrays:
    jsonArray.forEach(node => {
        if (node.parentId !== null && node.parentId !== 0) {
            const parent = nodeMap.get(node.parentId);
            if (parent) {
                parent.children.push(nodeMap.get(node.id));
            }
        }
    });

    // Step 3: Find root nodes (parentId is null or 0):
    const rootNodes = jsonArray
        .filter(node => node.parentId === null || node.parentId === 0)
        .map(node => nodeMap.get(node.id));

    // Step 4: Function to sort the nodes by id at each level:
    function sortNodes(nodes) {
        nodes.sort((a, b) => a.id - b.id);
        nodes.forEach(node => {
            if (node.children && node.children.length > 0) {
                sortNodes(node.children);
            }
        });
    }

    // Step 5: Sort the root nodes and their children:
    sortNodes(rootNodes);

    // Step 6: Function to build the tree recursively:
    function buildTreeRec(nodes) {
        if (!nodes || !nodes.length) return '';

        let html = '<ul>';
        nodes.forEach(node => {
            const hasChildren = node.children && node.children.length > 0;
            html += `<li><span onclick="toggleExpansion(event)" class="${hasChildren ? '' : 'no-children'}">${node.name}&nbsp;&nbsp;<input oninput="sendValueToBlazor(event, ${node.id})" class="form-check-input" type="radio" name="selectedValue"></span>`;
            if (hasChildren) {
                html += buildTreeRec(node.children);
            }
            html += '</li>';
        });
        html += '</ul>';
        return html;
    }

    return buildTreeRec(rootNodes);
}


export async function drawTreeAsync(id, stringifiedJsonArray) {

    let treeContainer = document.getElementById(id);

    let jsonArray = JSON.parse(stringifiedJsonArray);

    let result = buildTreeAsHtmlTags(jsonArray);

    if (result.length === 0) {
        treeContainer.innerHTML = `<div class="text-center">NO TREE TO SHOW</div>`;
    }
    else {
        treeContainer.innerHTML = result;
    }
}
