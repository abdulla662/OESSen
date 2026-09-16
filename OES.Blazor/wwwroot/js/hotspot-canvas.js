const hotspotCanvas = {
    imageData: null,
    currentZoom: 1,
    canvasStates: new Map(),

    initializeCanvas: function (canvasId, imageUrl) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error('Canvas not found:', canvasId);
            return Promise.reject(new Error('Canvas not found: ' + canvasId));
        }
        const ctx = canvas.getContext('2d');
        const img = new Image();

        return new Promise((resolve, reject) => {
            img.onload = function () {
                canvas.width = img.width;
                canvas.height = img.height;
                ctx.drawImage(img, 0, 0);

                this.imageData = {
                    width: img.width,
                    height: img.height,
                    url: imageUrl
                };

                this.canvasStates.set(canvasId, {
                    imageUrl: imageUrl,
                    zoom: 1,
                    offsetX: 0,
                    offsetY: 0
                });

                resolve({ width: img.width, height: img.height });
            }.bind(this);

            img.onerror = function (error) {
                console.error('Failed to load image:', imageUrl, error);
                reject(new Error('Failed to load image: ' + imageUrl));
            };
            img.src = imageUrl;
        });
    },

    drawRectangle: function (canvasId, x, y, width, height, isCorrect, showFill = true) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        const color = isCorrect ? '#4CAF50' : '#2196F3';

        ctx.strokeStyle = color;
        ctx.lineWidth = 3;
        ctx.strokeRect(x, y, width, height);

        if (showFill) {
            ctx.fillStyle = isCorrect ? 'rgba(76, 175, 80, 0.2)' : 'rgba(33, 150, 243, 0.2)';
            ctx.fillRect(x, y, width, height);
        }
    },

    clearCanvas: function (canvasId, imageUrl) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return Promise.resolve();

        const ctx = canvas.getContext('2d');
        const img = new Image();

        return new Promise((resolve) => {
            img.onload = function () {
                ctx.clearRect(0, 0, canvas.width, canvas.height);
                ctx.drawImage(img, 0, 0);
                resolve();
            };
            img.onerror = function () {
                resolve(); 
            };
            img.src = imageUrl;
        });
    },

    getMousePosition: function (canvasId, clientX, clientY) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return null;

        const rect = canvas.getBoundingClientRect();
        const state = this.canvasStates.get(canvasId);
        const zoom = state ? state.zoom : 1;

        return {
            X: Math.round((clientX - rect.left) * (canvas.width / rect.width) / zoom),
            Y: Math.round((clientY - rect.top) * (canvas.height / rect.height) / zoom),
            DisplayX: Math.round(clientX - rect.left),
            DisplayY: Math.round(clientY - rect.top)
        };
    },

    setZoom: function (canvasId, zoomLevel) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        canvas.style.transform = `scale(${zoomLevel})`;
        canvas.style.transformOrigin = 'top left';

        const state = this.canvasStates.get(canvasId);
        if (state) {
            state.zoom = zoomLevel;
        }

        this.currentZoom = zoomLevel;
    },

    getZoom: function (canvasId) {
        const state = this.canvasStates.get(canvasId);
        return state ? state.zoom : 1;
    },

    redrawHotspots: function (canvasId, imageUrl, hotspots) {
        this.clearCanvas(canvasId, imageUrl);

        setTimeout(() => {
            hotspots.forEach(hotspot => {
                const coords = JSON.parse(hotspot.coordinatesJson);

                if (hotspot.shapeType === 0) { // Rectangle
                    this.drawRectangle(
                        canvasId,
                        coords.x || coords.X,
                        coords.y || coords.Y,
                        coords.width || coords.Width,
                        coords.height || coords.Height,
                        hotspot.isCorrectAnswer
                    );
                } else if (hotspot.shapeType === 1) { // Circle
                    this.drawCircle(
                        canvasId,
                        coords.centerX || coords.CenterX,
                        coords.centerY || coords.CenterY,
                        coords.radius || coords.Radius,
                        hotspot.isCorrectAnswer
                    );
                }
            });
        }, 100);
    },

    dispose: function (canvasId) {
        this.canvasStates.delete(canvasId);
    }
};

window.hotspotCanvas = hotspotCanvas;
