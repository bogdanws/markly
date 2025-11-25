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
