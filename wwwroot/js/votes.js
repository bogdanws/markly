(function () {
    'use strict';

    // Check if user is authenticated (set by layout)
    const isAuthenticated = window.isAuthenticated || false;

    // Event delegation for vote buttons
    document.addEventListener('click', async function (e) {
        const btn = e.target.closest('.vote-btn');
        if (!btn) return;

        // Prevent the click from propagating to parent links (stretched-link)
        e.preventDefault();
        e.stopPropagation();

        // Check authentication
        if (!isAuthenticated) {
            window.location.href = '/Account/Login?ReturnUrl=' + encodeURIComponent(window.location.pathname);
            return;
        }

        const bookmarkId = parseInt(btn.dataset.bookmarkId, 10);
        if (!bookmarkId) return;

        // Disable button during request
        btn.disabled = true;

        try {
            const response = await csrf.fetch('/Votes/Toggle', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ bookmarkId: bookmarkId })
            });

            const data = await response.json();

            if (response.ok && data.success) {
                // Update UI
                updateVoteButton(btn, data.isLiked, data.voteCount);
            } else {
                showToast(data.message || 'Failed to update vote', 'danger');
            }
        } catch (error) {
            console.error('Vote error:', error);
            showToast('An error occurred. Please try again.', 'danger');
        } finally {
            btn.disabled = false;
        }
    });

    function updateVoteButton(btn, isLiked, voteCount) {
        const previousCount = parseInt(btn.querySelector('.vote-count')?.textContent || '0', 10);

        // Update data attribute
        btn.dataset.liked = isLiked.toString();

        // Update icon
        const icon = btn.querySelector('i');
        if (icon) {
            if (isLiked) {
                icon.classList.remove('bi-heart');
                icon.classList.add('bi-heart-fill');

                // Trigger burst effect on like
                btn.classList.add('vote-burst');
                setTimeout(() => btn.classList.remove('vote-burst'), 600);
            } else {
                icon.classList.remove('bi-heart-fill');
                icon.classList.add('bi-heart');
            }

            // Add heart pop animation
            icon.classList.add('vote-animate');
            setTimeout(() => icon.classList.remove('vote-animate'), 500);
        }

        // Update count with slide animation
        const countSpan = btn.querySelector('.vote-count');
        if (countSpan) {
            // Determine animation direction based on count change
            const slideClass = voteCount > previousCount ? 'vote-count-up' : 'vote-count-down';

            countSpan.classList.add(slideClass);
            countSpan.textContent = voteCount;

            setTimeout(() => countSpan.classList.remove(slideClass), 250);
        }
    }

    function showToast(message, type = 'info') {
        // Use existing toast function if available, otherwise create simple one
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
