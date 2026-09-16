
(function () {
    const savedTheme = localStorage.getItem('OEStheme');

    if (savedTheme === 'Dark') {
        document.documentElement.classList.add('dark-theme');
    }
})();

