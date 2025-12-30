/**
 * Shortest-Column-First Masonry Grid
 * Places each item in the column with the smallest height
 */
(function() {
    'use strict';

    const MASONRY_SELECTOR = '.masonry-grid';
    const ITEM_SELECTOR = '.masonry-grid > .col';
    const GAP = 24; // 1.5rem in pixels

    /**
     * Get the number of columns based on viewport width and grid settings
     */
    function getColumnCount(grid) {
        const width = window.innerWidth;
        let columns;

        if (width >= 2000) columns = 5;
        else if (width >= 1400) columns = 4;
        else if (width >= 1200) columns = 3;
        else if (width >= 768) columns = 2;
        else columns = 1;

        // Check for CSS variable override (--masonry-max-columns)
        const maxColumns = getComputedStyle(grid).getPropertyValue('--masonry-max-columns');
        if (maxColumns) {
            columns = Math.min(columns, parseInt(maxColumns, 10));
        }

        return columns;
    }

    /**
     * Find the index of the shortest column
     */
    function getShortestColumn(columnHeights) {
        let minIndex = 0;
        let minHeight = columnHeights[0];
        for (let i = 1; i < columnHeights.length; i++) {
            if (columnHeights[i] < minHeight) {
                minHeight = columnHeights[i];
                minIndex = i;
            }
        }
        return minIndex;
    }

    /**
     * Layout a single masonry grid with shortest-column-first ordering
     * Items are placed into the column with the smallest current height
     */
    function layoutGrid(grid) {
        const items = Array.from(grid.querySelectorAll(ITEM_SELECTOR));
        if (items.length === 0) return;

        const columnCount = getColumnCount(grid);

        // For single column, just use natural flow
        if (columnCount === 1) {
            grid.style.position = '';
            grid.style.height = '';
            items.forEach(item => {
                item.style.position = '';
                item.style.left = '';
                item.style.top = '';
                item.style.width = '';
            });
            return;
        }

        // Get container width and calculate column width
        const gridWidth = grid.offsetWidth;
        const columnWidth = (gridWidth - (GAP * (columnCount - 1))) / columnCount;

        // Track the height of each column
        const columnHeights = new Array(columnCount).fill(0);

        // Set up grid for absolute positioning
        grid.style.position = 'relative';

        // Position each item in the shortest column
        items.forEach((item) => {
            // Set width first so we can measure accurate height
            item.style.width = `${columnWidth}px`;
            item.style.position = 'absolute';

            // Find the shortest column
            const col = getShortestColumn(columnHeights);

            // Calculate position
            const left = col * (columnWidth + GAP);
            const top = columnHeights[col];

            // Apply position
            item.style.left = `${left}px`;
            item.style.top = `${top}px`;

            // Update column height (add gap for next item)
            columnHeights[col] = top + item.offsetHeight + GAP;
        });

        // Set grid height to tallest column (subtract the extra gap added after last item)
        const maxHeight = Math.max(...columnHeights);
        grid.style.height = `${maxHeight > 0 ? maxHeight - GAP : 0}px`;
    }

    /**
     * Layout all masonry grids on the page
     */
    function layoutAllGrids() {
        const grids = document.querySelectorAll(MASONRY_SELECTOR);
        grids.forEach(layoutGrid);
    }

    /**
     * Debounce function for resize events
     */
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    /**
     * Initialize masonry layout
     */
    function init() {
        // Initial layout
        layoutAllGrids();

        // Re-layout on resize
        window.addEventListener('resize', debounce(layoutAllGrids, 100));

        // Re-layout when images load
        document.querySelectorAll(`${MASONRY_SELECTOR} img`).forEach(img => {
            if (img.complete) return;
            img.addEventListener('load', layoutAllGrids);
            img.addEventListener('error', layoutAllGrids);
        });
    }

    // Expose for external use
    window.MasonryGrid = {
        layout: layoutAllGrids,
        layoutGrid: layoutGrid
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
