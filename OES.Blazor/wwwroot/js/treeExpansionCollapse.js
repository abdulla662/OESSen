function expandAllNodes() {
    document.querySelectorAll('.tree-container li').forEach(node => {
        const expandIcon = node.querySelector('span');
        if (expandIcon && expandIcon.innerText === '►') {
            expandIcon.innerText = '▼';
            const ul = node.querySelector('ul');
            if (ul) ul.style.display = 'block';
        }
    });
}

function collapseAllNodes() {
    document.querySelectorAll('.tree-container li').forEach(node => {
        const expandIcon = node.querySelector('span');
        if (expandIcon && expandIcon.innerText === '▼') {
            expandIcon.innerText = '►';
            const ul = node.querySelector('ul');
            if (ul) ul.style.display = 'none';
        }
    });
}
