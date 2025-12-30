(function () {
    'use strict';

    const INFINITE_SCROLL_THRESHOLD = 600;

    const feedContainer = document.getElementById('browseFeedContainer');
    const bookmarkGrid = document.getElementById('bookmarkGrid');
    const sortSelect = document.getElementById('sortSelect');
    const timeSelect = document.getElementById('timeSelect');

    if (!feedContainer) return;

    let currentPage = parseInt(feedContainer.dataset.page) || 1;
    let totalPages = parseInt(feedContainer.dataset.totalPages) || 1;
    let currentSort = feedContainer.dataset.sort || 'recent';
    let currentTag = feedContainer.dataset.tag || '';
    let currentCategory = feedContainer.dataset.category || '';
    let currentTime = feedContainer.dataset.time || '';
    let isLoadingMore = false;
    let infiniteScrollObserver = null;

    function buildUrl(page) {
        const params = new URLSearchParams();
        if (currentSort && currentSort !== 'recent') params.set('sort', currentSort);
        if (currentTag) params.set('tag', currentTag);
        if (currentCategory) params.set('category', currentCategory);
        if (currentTime) params.set('time', currentTime);
        if (page > 1) params.set('page', page);

        const queryString = params.toString();
        return '/Bookmarks/BrowseFeed' + (queryString ? '?' + queryString : '');
    }

    function buildBrowseUrl(overrides = {}) {
        const params = new URLSearchParams();
        const sort = overrides.sort !== undefined ? overrides.sort : currentSort;
        const tag = overrides.tag !== undefined ? overrides.tag : currentTag;
        const category = overrides.category !== undefined ? overrides.category : currentCategory;
        const time = overrides.time !== undefined ? overrides.time : currentTime;

        if (sort && sort !== 'recent') params.set('sort', sort);
        if (tag) params.set('tag', tag);
        if (category) params.set('category', category);
        if (time) params.set('time', time);

        const queryString = params.toString();
        return '/Bookmarks/Browse' + (queryString ? '?' + queryString : '');
    }

    function waitForImages(container, timeout = 3000) {
        const images = container.querySelectorAll('img');
        const imagePromises = Array.from(images).map(img => {
            if (img.complete) return Promise.resolve();
            return new Promise(resolve => {
                img.addEventListener('load', resolve, { once: true });
                img.addEventListener('error', resolve, { once: true });
            });
        });

        const timeoutPromise = new Promise(resolve => setTimeout(resolve, timeout));
        return Promise.race([Promise.all(imagePromises), timeoutPromise]);
    }

    function relayout() {
        if (window.MasonryGrid && typeof window.MasonryGrid.layout === 'function') {
            window.MasonryGrid.layout();
        }
    }

    async function loadMoreBookmarks() {
        if (isLoadingMore || currentPage >= totalPages || !bookmarkGrid) return;

        isLoadingMore = true;
        const nextPage = currentPage + 1;

        try {
            const response = await fetch(buildUrl(nextPage), {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (!response.ok) throw new Error('Failed to load more bookmarks');

            const html = await response.text();
            const temp = document.createElement('div');
            temp.innerHTML = html;

            const newCards = Array.from(temp.querySelectorAll('.col'));

            if (newCards.length > 0) {
                newCards.forEach(card => {
                    bookmarkGrid.appendChild(card);
                });

                currentPage = nextPage;
                feedContainer.dataset.page = currentPage;

                // Layout immediately, then again after images load
                relayout();
                await waitForImages(bookmarkGrid);
                relayout();

                // Remove old trigger and check if we need a new one
                const oldTrigger = document.getElementById('infiniteScrollTrigger');
                if (oldTrigger) oldTrigger.remove();

                // Check if there's a new trigger in the loaded content
                const newTrigger = temp.querySelector('.infinite-scroll-trigger');
                if (newTrigger) {
                    feedContainer.appendChild(newTrigger);
                    setupInfiniteScroll();
                } else if (currentPage >= totalPages) {
                    showEndOfResults();
                }
            }
        } catch (error) {
            console.error('Load more error:', error);
        } finally {
            isLoadingMore = false;
        }
    }

    function setupInfiniteScroll() {
        if (infiniteScrollObserver) {
            infiniteScrollObserver.disconnect();
        }

        const trigger = document.getElementById('infiniteScrollTrigger');
        if (!trigger) return;

        infiniteScrollObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting && !isLoadingMore) {
                    loadMoreBookmarks();
                }
            });
        }, {
            rootMargin: `${INFINITE_SCROLL_THRESHOLD}px`
        });

        infiniteScrollObserver.observe(trigger);
    }

    function showEndOfResults() {
        const existing = feedContainer.querySelector('.end-of-results');
        if (existing) return;

        const endDiv = document.createElement('div');
        endDiv.className = 'end-of-results text-center py-4';
        endDiv.innerHTML = `
            <div class="end-of-results-line"></div>
            <span class="end-of-results-text text-muted small">
                <i class="bi bi-check-circle me-1"></i>You've reached the end
            </span>
        `;
        feedContainer.appendChild(endDiv);
    }

    // Setup filter change handlers
    function setupFilters() {
        if (sortSelect) {
            // Set the correct selected option
            const sortOptions = sortSelect.querySelectorAll('option');
            sortOptions.forEach(opt => {
                opt.selected = opt.value === currentSort;
            });

            sortSelect.addEventListener('change', function () {
                window.location.href = buildBrowseUrl({ sort: this.value });
            });
        }

        if (timeSelect) {
            // Set the correct selected option
            const timeOptions = timeSelect.querySelectorAll('option');
            timeOptions.forEach(opt => {
                opt.selected = opt.value === currentTime;
            });

            timeSelect.addEventListener('change', function () {
                window.location.href = buildBrowseUrl({ time: this.value });
            });
        }
    }

    // Initialize
    setupFilters();
    setupInfiniteScroll();
})();
