(function () {
    'use strict';

    const isAuthenticated = window.isAuthenticated || false;

    // ==================== Save to Category ====================

    document.addEventListener('click', async function (e) {
        const btn = e.target.closest('.save-btn');
        if (!btn) return;

        e.preventDefault();
        e.stopPropagation();

        if (!isAuthenticated) {
            window.location.href = '/Account/Login?ReturnUrl=' + encodeURIComponent(window.location.pathname);
            return;
        }

        const bookmarkId = parseInt(btn.dataset.bookmarkId, 10);
        if (!bookmarkId) return;

        // Show dropdown if exists, or create it
        let dropdown = btn._dropdown;
        if (dropdown && dropdown.classList.contains('show')) {
            dropdown.classList.remove('show');
            return;
        }

        // Close any other open dropdowns
        document.querySelectorAll('.save-dropdown.show').forEach(d => d.classList.remove('show'));

        if (!dropdown) {
            dropdown = createDropdown(btn, bookmarkId);
        }

        // Position the dropdown
        positionDropdown(btn, dropdown);

        dropdown.classList.add('show');
        await loadCategories(dropdown, bookmarkId);
    });

    function positionDropdown(btn, dropdown) {
        const rect = btn.getBoundingClientRect();
        const dropdownHeight = 300; // Approximate max height
        const dropdownWidth = 220;
        const padding = 8;

        // Default: below and aligned to right edge of button
        let top = rect.bottom + padding;
        let left = rect.right - dropdownWidth;

        // If dropdown would go off the left edge, align to left of button
        if (left < padding) {
            left = rect.left;
        }

        // If dropdown would go off the right edge
        if (left + dropdownWidth > window.innerWidth - padding) {
            left = window.innerWidth - dropdownWidth - padding;
        }

        // If dropdown would go off bottom, show above the button
        if (top + dropdownHeight > window.innerHeight - padding) {
            top = rect.top - dropdownHeight - padding;
            // If still doesn't fit, just use available space below
            if (top < padding) {
                top = rect.bottom + padding;
            }
        }

        dropdown.style.top = `${top}px`;
        dropdown.style.left = `${left}px`;
    }

    // Close dropdowns when clicking outside
    document.addEventListener('click', function (e) {
        if (!e.target.closest('.save-btn') && !e.target.closest('.save-dropdown')) {
            document.querySelectorAll('.save-dropdown.show').forEach(d => d.classList.remove('show'));
        }
    });

    function createDropdown(btn, bookmarkId) {
        const dropdown = document.createElement('div');
        dropdown.className = 'save-dropdown';
        dropdown.dataset.forButton = bookmarkId;
        dropdown.innerHTML = `
            <div class="save-dropdown__header">Save to category</div>
            <div class="save-dropdown__list">
                <div class="save-dropdown__loading">
                    <span class="spinner-border spinner-border-sm"></span>
                </div>
            </div>
            <div class="save-dropdown__footer">
                <button type="button" class="save-dropdown__create-btn">
                    <i class="bi bi-plus-lg"></i> New category
                </button>
            </div>
            <div class="save-dropdown__create-form d-none">
                <input type="text" class="form-control form-control-sm" placeholder="Category name">
                <div class="save-dropdown__create-actions">
                    <button type="button" class="btn btn-sm btn-light save-dropdown__cancel">Cancel</button>
                    <button type="button" class="btn btn-sm btn-primary save-dropdown__submit">Create</button>
                </div>
            </div>
        `;

        // Append to body for fixed positioning
        document.body.appendChild(dropdown);

        // Store reference on button
        btn._dropdown = dropdown;

        // Create form handlers
        const createBtn = dropdown.querySelector('.save-dropdown__create-btn');
        const createForm = dropdown.querySelector('.save-dropdown__create-form');
        const input = createForm.querySelector('input');
        const cancelBtn = dropdown.querySelector('.save-dropdown__cancel');
        const submitBtn = dropdown.querySelector('.save-dropdown__submit');
        const footer = dropdown.querySelector('.save-dropdown__footer');

        createBtn.addEventListener('click', () => {
            footer.classList.add('d-none');
            createForm.classList.remove('d-none');
            input.focus();
        });

        cancelBtn.addEventListener('click', () => {
            createForm.classList.add('d-none');
            footer.classList.remove('d-none');
            input.value = '';
        });

        submitBtn.addEventListener('click', async () => {
            const name = input.value.trim();
            if (!name) return;

            submitBtn.disabled = true;
            try {
                const response = await csrf.fetch('/Categories/CreateQuick', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ name, isPublic: false })
                });

                const data = await response.json();
                if (response.ok && data.success) {
                    // Add to list and select it
                    await loadCategories(dropdown, bookmarkId);
                    // Toggle the new category
                    await toggleCategory(bookmarkId, data.category.id, dropdown);
                    // Reset form
                    cancelBtn.click();
                } else {
                    showToast(data.message || 'Failed to create category', 'danger');
                }
            } catch (error) {
                showToast('Failed to create category', 'danger');
            } finally {
                submitBtn.disabled = false;
            }
        });

        input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                submitBtn.click();
            }
        });

        return dropdown;
    }

    async function loadCategories(dropdown, bookmarkId) {
        const list = dropdown.querySelector('.save-dropdown__list');
        list.innerHTML = '<div class="save-dropdown__loading"><span class="spinner-border spinner-border-sm"></span></div>';

        try {
            const response = await csrf.fetch(`/Categories/GetUserCategories?bookmarkId=${bookmarkId}`);
            const data = await response.json();

            if (!response.ok || !data.success) {
                list.innerHTML = '<div class="save-dropdown__empty">Failed to load categories</div>';
                return;
            }

            if (data.categories.length === 0) {
                list.innerHTML = '<div class="save-dropdown__empty">No categories yet</div>';
                return;
            }

            list.innerHTML = data.categories.map(cat => `
                <button type="button" class="save-dropdown__item ${cat.isInCategory ? 'active' : ''}"
                        data-category-id="${cat.id}">
                    <i class="bi ${cat.isInCategory ? 'bi-check-circle-fill' : 'bi-circle'}"></i>
                    <span>${escapeHtml(cat.name)}</span>
                </button>
            `).join('');

            // Add click handlers
            list.querySelectorAll('.save-dropdown__item').forEach(item => {
                item.addEventListener('click', async () => {
                    const categoryId = parseInt(item.dataset.categoryId, 10);
                    await toggleCategory(bookmarkId, categoryId, dropdown);
                });
            });
        } catch (error) {
            console.error('Error loading categories:', error);
            list.innerHTML = '<div class="save-dropdown__empty">Failed to load categories</div>';
        }
    }

    async function toggleCategory(bookmarkId, categoryId, dropdown) {
        const item = dropdown.querySelector(`[data-category-id="${categoryId}"]`);
        if (item) item.classList.add('loading');

        try {
            const response = await csrf.fetch('/Categories/ToggleBookmark', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ bookmarkId, categoryId })
            });

            const data = await response.json();
            if (response.ok && data.success) {
                if (item) {
                    item.classList.toggle('active', data.isInCategory);
                    const icon = item.querySelector('i');
                    icon.className = data.isInCategory ? 'bi bi-check-circle-fill' : 'bi bi-circle';
                }
                const action = data.isInCategory ? 'Added to' : 'Removed from';
                showToast(`${action} "${data.categoryName}"`, 'success');
            } else {
                showToast(data.message || 'Failed to update', 'danger');
            }
        } catch (error) {
            console.error('Error toggling category:', error);
            showToast('Failed to update', 'danger');
        } finally {
            if (item) item.classList.remove('loading');
        }
    }

    // ==================== Share ====================

    document.addEventListener('click', async function (e) {
        const btn = e.target.closest('.share-btn');
        if (!btn) return;

        e.preventDefault();
        e.stopPropagation();

        const bookmarkId = btn.dataset.bookmarkId;
        const bookmarkTitle = btn.dataset.bookmarkTitle || 'Bookmark';
        const shareUrl = `${window.location.origin}/Bookmarks/Details/${bookmarkId}`;

        // Try native share first
        if (navigator.share) {
            try {
                await navigator.share({
                    title: bookmarkTitle,
                    url: shareUrl
                });
                return;
            } catch (err) {
                // User cancelled or error - fall back to copy
                if (err.name === 'AbortError') return;
            }
        }

        // Fallback: copy to clipboard
        try {
            await navigator.clipboard.writeText(shareUrl);
            showToast('Link copied to clipboard!', 'success');
        } catch (err) {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = shareUrl;
            textArea.style.position = 'fixed';
            textArea.style.opacity = '0';
            document.body.appendChild(textArea);
            textArea.select();
            try {
                document.execCommand('copy');
                showToast('Link copied to clipboard!', 'success');
            } catch (e) {
                showToast('Failed to copy link', 'danger');
            }
            document.body.removeChild(textArea);
        }
    });

    // ==================== Helpers ====================

    function showToast(message, type = 'info') {
        if (typeof window.showToast === 'function') {
            window.showToast(message, type);
            return;
        }

        let toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toastContainer';
            toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            toastContainer.style.zIndex = '1100';
            document.body.appendChild(toastContainer);
        }

        const toastId = 'toast-' + Date.now();
        const bgClass = type === 'danger' ? 'bg-danger' : type === 'success' ? 'bg-success' : 'bg-primary';

        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" role="alert">
                <div class="d-flex">
                    <div class="toast-body">${escapeHtml(message)}</div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastEl = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastEl, { autohide: true, delay: 3000 });
        toast.show();

        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
})();
