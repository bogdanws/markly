(function () {
    'use strict';

    // Constants
    const DEBOUNCE_DELAY = 300;
    const RECENT_SEARCHES_KEY = 'markly_recent_searches';
    const MAX_RECENT_SEARCHES = 8;
    const INFINITE_SCROLL_THRESHOLD = 200;
    const LOADING_DELAY = 200;

    // DOM Elements
    const searchForm = document.getElementById('searchForm');
    const searchInput = document.getElementById('searchQuery');
    const searchResults = document.getElementById('searchResults');
    const loadingIndicator = document.querySelector('.search-loading-indicator');
    const clearQueryBtn = document.getElementById('clearQueryBtn');
    const recentSearchesContainer = document.getElementById('recentSearches');
    const recentSearchesList = document.getElementById('recentSearchesList');
    const skeletonTemplate = document.getElementById('skeletonTemplate');

    // State
    let searchTimeout = null;
    let abortController = null;
    let currentPage = 1;
    let totalPages = 1;
    let isLoadingMore = false;
    let infiniteScrollObserver = null;
    let loadingDelayTimeout = null;

    // Utility Functions
    function debounce(func, wait) {
        return function (...args) {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => func.apply(this, args), wait);
        };
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function highlightText(text, query) {
        if (!query || !text) return escapeHtml(text);
        const escaped = escapeHtml(text);
        const regex = new RegExp(`(${query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
        return escaped.replace(regex, '<mark class="search-highlight">$1</mark>');
    }

    // Recent Searches
    function getRecentSearches() {
        try {
            return JSON.parse(localStorage.getItem(RECENT_SEARCHES_KEY) || '[]');
        } catch {
            return [];
        }
    }

    function saveRecentSearch(query) {
        if (!query || query.trim().length < 2) return;
        const searches = getRecentSearches().filter(s => s !== query.trim());
        searches.unshift(query.trim());
        localStorage.setItem(RECENT_SEARCHES_KEY, JSON.stringify(searches.slice(0, MAX_RECENT_SEARCHES)));
    }

    function removeRecentSearch(query) {
        const searches = getRecentSearches().filter(s => s !== query);
        localStorage.setItem(RECENT_SEARCHES_KEY, JSON.stringify(searches));
        renderRecentSearches();
    }

    function renderRecentSearches() {
        const searches = getRecentSearches();
        if (searches.length === 0) {
            recentSearchesContainer.classList.add('d-none');
            return;
        }

        recentSearchesContainer.classList.remove('d-none');
        recentSearchesList.innerHTML = searches.map(query => `
            <button type="button" class="btn recent-search-btn" data-query="${escapeHtml(query)}">
                <span>${escapeHtml(query)}</span>
                <i class="bi bi-x recent-search-remove" data-remove="${escapeHtml(query)}"></i>
            </button>
        `).join('');
    }

    // Search Functions
    function scheduleLoading() {
        cancelLoadingDelay();
        loadingIndicator.classList.remove('d-none');

        loadingDelayTimeout = setTimeout(() => {
            searchResults.innerHTML = skeletonTemplate.innerHTML;
        }, LOADING_DELAY);
    }

    function cancelLoadingDelay() {
        if (loadingDelayTimeout) {
            clearTimeout(loadingDelayTimeout);
            loadingDelayTimeout = null;
        }
    }

    function hideLoading() {
        cancelLoadingDelay();
        loadingIndicator.classList.add('d-none');
    }

    function getFormData(page = 1) {
        const formData = new FormData(searchForm);
        const params = new URLSearchParams();
        for (const [key, value] of formData.entries()) {
            if (value && key !== 'page') params.append(key, value);
        }
        if (page > 1) {
            params.append('page', page);
        }
        return params;
    }

    async function performSearch(updateUrl = true) {
        if (abortController) {
            abortController.abort();
        }
        abortController = new AbortController();

        currentPage = 1;

        const params = getFormData();
        const query = params.get('q');

        if (updateUrl) {
            const newUrl = params.toString() ? `?${params.toString()}` : window.location.pathname;
            history.pushState({}, '', newUrl);
        }

        clearQueryBtn.classList.toggle('d-none', !query);

        if (!params.toString() || (!query && !params.get('tag') && !params.get('dateRange') &&
            !params.get('author') && !params.get('minVotes') && !params.get('contentType'))) {
            location.reload();
            return;
        }

        scheduleLoading();

        try {
            const response = await fetch(`/Search/Results?${params.toString()}`, {
                signal: abortController.signal,
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (!response.ok) throw new Error('Search failed');

            let html = await response.text();

            if (query) {
                html = applyHighlighting(html, query);
                saveRecentSearch(query);
            }

            searchResults.innerHTML = html;
            hideLoading();

            updatePaginationState();
            setupInfiniteScroll();
            bindResultsHandlers();

        } catch (error) {
            if (error.name === 'AbortError') return;
            console.error('Search error:', error);
            hideLoading();
            searchResults.innerHTML = `
                <div class="alert alert-danger">
                    <i class="bi bi-exclamation-triangle me-2"></i>
                    An error occurred while searching. Please try again.
                </div>
            `;
        }
    }

    function updatePaginationState() {
        const content = document.getElementById('searchResultsContent');
        if (content) {
            currentPage = parseInt(content.dataset.page) || 1;
            totalPages = parseInt(content.dataset.totalPages) || 1;
        }
    }

    async function loadMoreResults() {
        if (isLoadingMore || currentPage >= totalPages) return;

        isLoadingMore = true;
        const nextPage = currentPage + 1;
        const params = getFormData(nextPage);
        const query = params.get('q');

        try {
            const response = await fetch(`/Search/Results?${params.toString()}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (!response.ok) throw new Error('Failed to load more');

            let html = await response.text();

            if (query) {
                html = applyHighlighting(html, query);
            }

            const temp = document.createElement('div');
            temp.innerHTML = html;

            const newCards = temp.querySelectorAll('.search-results-grid .col');
            const resultsGrid = document.getElementById('resultsGrid');

            if (resultsGrid && newCards.length > 0) {
                newCards.forEach((card) => {
                    resultsGrid.appendChild(card);
                });

                currentPage = nextPage;

                const content = document.getElementById('searchResultsContent');
                if (content) {
                    content.dataset.page = currentPage;
                }

                if (currentPage >= totalPages) {
                    removeInfiniteScrollTrigger();
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
                    loadMoreResults();
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
        const content = document.getElementById('searchResultsContent');
        const totalResults = content?.dataset.totalResults || 0;

        if (totalResults > 10) {
            const endDiv = document.createElement('div');
            endDiv.className = 'end-of-results text-center py-4';
            endDiv.innerHTML = `
                <div class="end-of-results-line"></div>
                <span class="end-of-results-text text-muted small">
                    <i class="bi bi-check-circle me-1"></i>You've seen all ${totalResults} results
                </span>
            `;
            const grid = document.getElementById('resultsGrid');
            if (grid) {
                grid.parentNode.insertBefore(endDiv, grid.nextSibling);
            }
        }
    }

    function applyHighlighting(html, query) {
        const temp = document.createElement('div');
        temp.innerHTML = html;

        temp.querySelectorAll('.bookmark-card__title').forEach(el => {
            el.innerHTML = highlightText(el.textContent, query);
        });

        temp.querySelectorAll('.bookmark-card__description').forEach(el => {
            el.innerHTML = highlightText(el.textContent, query);
        });

        temp.querySelectorAll('.bookmark-card__text-preview').forEach(el => {
            el.innerHTML = highlightText(el.textContent, query);
        });

        return temp.innerHTML;
    }

    function bindResultsHandlers() {
        const clearBtn = document.getElementById('clearSearchBtn');
        if (clearBtn) {
            clearBtn.addEventListener('click', () => {
                searchForm.reset();
                performSearch();
            });
        }

        document.querySelectorAll('.remove-filter-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                const filter = btn.dataset.filter;
                removeFilter(filter);
            });
        });
    }

    function removeFilter(filterName) {
        const input = searchForm.querySelector(`[name="${filterName}"]`);
        if (input) {
            if (input.type === 'select-one') {
                input.selectedIndex = 0;
            } else {
                input.value = '';
            }
            performSearch();
        }
    }

    // Event Handlers
    function handleFormSubmit(e) {
        e.preventDefault();
        performSearch();
    }

    function handleInputChange() {
        debounce(performSearch, DEBOUNCE_DELAY)();
    }

    function handleKeyboardShortcut(e) {
        if (e.key === '/' && !['INPUT', 'TEXTAREA', 'SELECT'].includes(document.activeElement.tagName)) {
            e.preventDefault();
            searchInput.focus();
            searchInput.select();
        }

        if (e.key === 'Escape' && document.activeElement === searchInput) {
            searchInput.blur();
        }
    }

    function handleFilterChipRemove(e) {
        const removeBtn = e.target.closest('.filter-chip-remove');
        if (!removeBtn) return;

        removeFilter(removeBtn.dataset.filter);
    }

    function handleRecentSearchClick(e) {
        const btn = e.target.closest('.recent-search-btn');
        const removeIcon = e.target.closest('.recent-search-remove');

        if (removeIcon) {
            e.stopPropagation();
            removeRecentSearch(removeIcon.dataset.remove);
            return;
        }

        if (btn) {
            searchInput.value = btn.dataset.query;
            performSearch();
        }
    }

    function handleClearQuery() {
        searchInput.value = '';
        searchInput.focus();
        clearQueryBtn.classList.add('d-none');
        performSearch();
    }

    // Initialize
    function init() {
        searchForm.addEventListener('submit', handleFormSubmit);
        searchInput.addEventListener('input', handleInputChange);

        ['sortOrder', 'dateRange', 'contentType', 'tagFilter', 'authorFilter', 'minVotes'].forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.addEventListener('change', () => performSearch());
            }
        });

        document.addEventListener('keydown', handleKeyboardShortcut);
        document.getElementById('filterChips')?.addEventListener('click', handleFilterChipRemove);
        recentSearchesList.addEventListener('click', handleRecentSearchClick);
        renderRecentSearches();
        clearQueryBtn.addEventListener('click', handleClearQuery);

        window.addEventListener('popstate', () => {
            location.reload();
        });

        updatePaginationState();
        setupInfiniteScroll();
        bindResultsHandlers();
    }

    // Start
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
