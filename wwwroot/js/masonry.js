/**
 * Horizontal-First Masonry Grid
 * Places items in rows (A B C, D E F) while allowing variable heights
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

        if (width >= 1200) columns = 3;
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
     * Layout a single masonry grid with horizontal-first ordering
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

        // Position each item
        items.forEach((item, index) => {
            // Horizontal-first: place in column based on index modulo
            const col = index % columnCount;

            // Calculate position
            const left = col * (columnWidth + GAP);

            // For the top position, we need the height of all items above in this column
            // Find all previous items in this column
            let top = 0;
            for (let i = col; i < index; i += columnCount) {
                top += items[i].offsetHeight + GAP;
            }

            // Apply styles
            item.style.position = 'absolute';
            item.style.left = `${left}px`;
            item.style.top = `${top}px`;
            item.style.width = `${columnWidth}px`;

            // Update column height
            columnHeights[col] = top + item.offsetHeight;
        });

        // Set grid height to tallest column
        grid.style.height = `${Math.max(...columnHeights)}px`;
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
