// Shared utility functions

/**
 * Escapes HTML special characters to prevent XSS
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Animates a number from 0 to target value
 * @param {HTMLElement} element - Element containing the number
 * @param {number} target - Target number to animate to
 * @param {number} duration - Animation duration in ms
 */
function animateCounter(element, target, duration = 800) {
    const start = 0;
    const startTime = performance.now();

    function easeOutQuart(t) {
        return 1 - Math.pow(1 - t, 4);
    }

    function update(currentTime) {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1);
        const easedProgress = easeOutQuart(progress);
        const current = Math.floor(start + (target - start) * easedProgress);

        element.textContent = current.toLocaleString();

        if (progress < 1) {
            requestAnimationFrame(update);
        } else {
            element.textContent = target.toLocaleString();
            element.classList.add('animated');
        }
    }

    requestAnimationFrame(update);
}

/**
 * Initialize admin panel animated counters
 */
function initAdminCounters() {
    const counters = document.querySelectorAll('.admin-stat-value[data-count]');

    if (counters.length === 0) return;

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const element = entry.target;
                const target = parseInt(element.dataset.count, 10);

                // Stagger animations based on card position
                const card = element.closest('.col-6, .col-lg-3');
                const delay = card ? Array.from(card.parentElement.children).indexOf(card) * 100 : 0;

                setTimeout(() => {
                    animateCounter(element, target);
                }, delay);

                observer.unobserve(element);
            }
        });
    }, { threshold: 0.5 });

    counters.forEach(counter => observer.observe(counter));
}

/**
 * Initialize admin navigation sliding indicator
 */
function initAdminNavIndicator() {
    const nav = document.querySelector('.admin-nav');
    if (!nav) return;

    const activeItem = nav.querySelector('.admin-nav-item.active');
    if (!activeItem) return;

    // Create indicator element if it doesn't exist
    let indicator = nav.querySelector('.admin-nav-indicator');
    if (!indicator) {
        indicator = document.createElement('div');
        indicator.className = 'admin-nav-indicator';
        nav.insertBefore(indicator, nav.firstChild);
    }

    // Position indicator on active item
    function updateIndicator(item) {
        const navRect = nav.getBoundingClientRect();
        const itemRect = item.getBoundingClientRect();

        indicator.style.left = (itemRect.left - navRect.left) + 'px';
        indicator.style.width = itemRect.width + 'px';
    }

    // Initial position
    updateIndicator(activeItem);

    // Update on window resize
    window.addEventListener('resize', () => updateIndicator(activeItem));

    // Hover effect - move indicator to hovered item
    nav.querySelectorAll('.admin-nav-item').forEach(item => {
        item.addEventListener('mouseenter', () => updateIndicator(item));
        item.addEventListener('mouseleave', () => updateIndicator(activeItem));
    });
}

// Initialize admin features when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initAdminCounters();
    initAdminNavIndicator();
});

/**
 * Shows a confirmation modal dialog (uses the modal from _ConfirmModal.cshtml)
 * @param {Object} options - Modal configuration
 * @param {string} options.title - Modal title
 * @param {string} options.message - Main message (displayed as lead text)
 * @param {string} [options.detail] - Additional detail text (displayed as muted text)
 * @param {string} [options.confirmText='Confirm'] - Text for confirm button
 * @param {string} [options.cancelText='Cancel'] - Text for cancel button
 * @param {Function} options.onConfirm - Callback when confirmed
 * @param {Function} [options.onCancel] - Optional callback when cancelled
 */
function showConfirmModal({
    title,
    message,
    detail = '',
    confirmText = 'Confirm',
    cancelText = 'Cancel',
    onConfirm,
    onCancel
}) {
    const modalEl = document.getElementById('confirmModal');
    if (!modalEl) {
        return;
    }

    // Populate modal content
    document.getElementById('confirmModalTitle').textContent = title;
    document.getElementById('confirmModalMessage').textContent = message;
    document.getElementById('confirmModalDetail').textContent = detail;
    document.getElementById('confirmModalConfirmBtn').textContent = confirmText;
    document.getElementById('confirmModalCancelBtn').textContent = cancelText;

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    const confirmBtn = document.getElementById('confirmModalConfirmBtn');

    // Remove any existing listeners by cloning the button
    const newConfirmBtn = confirmBtn.cloneNode(true);
    confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);

    let confirmed = false;

    newConfirmBtn.addEventListener('click', () => {
        confirmed = true;
        modal.hide();
    });

    const handleHidden = () => {
        modalEl.removeEventListener('hidden.bs.modal', handleHidden);
        if (confirmed) {
            onConfirm?.();
        } else {
            onCancel?.();
        }
    };

    modalEl.addEventListener('hidden.bs.modal', handleHidden);

    modal.show();
}
