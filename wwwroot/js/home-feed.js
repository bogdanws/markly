(function () {
    'use strict';

    const INFINITE_SCROLL_THRESHOLD = 600;

    const feedContainer = document.getElementById('bookmarkFeedContainer');
    const bookmarkGrid = document.getElementById('bookmarkGrid');

    if (!feedContainer || !bookmarkGrid) return;

    let currentPage = parseInt(feedContainer.dataset.page) || 1;
    let totalPages = parseInt(feedContainer.dataset.totalPages) || 1;
    let currentFilter = feedContainer.dataset.filter || 'recent';
    let isLoadingMore = false;
    let infiniteScrollObserver = null;

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
        if (isLoadingMore || currentPage >= totalPages) return;

        isLoadingMore = true;
        const nextPage = currentPage + 1;

        try {
            const response = await fetch(`/Home/Feed?filter=${encodeURIComponent(currentFilter)}&page=${nextPage}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (!response.ok) throw new Error('Failed to load more bookmarks');

            const html = await response.text();
            const temp = document.createElement('div');
            temp.innerHTML = html;

            const feedContent = temp.querySelector('#feedContent');
            if (feedContent) {
                const newCards = Array.from(feedContent.querySelectorAll('.col'));

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

                    if (currentPage >= totalPages) {
                        removeInfiniteScrollTrigger();
                        showEndOfResults();
                    }
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

    function removeInfiniteScrollTrigger() {
        const trigger = document.getElementById('infiniteScrollTrigger');
        if (trigger) {
            trigger.remove();
        }
        if (infiniteScrollObserver) {
            infiniteScrollObserver.disconnect();
        }
    }

    function showEndOfResults() {
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

    setupInfiniteScroll();
})();
