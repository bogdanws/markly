document.addEventListener('DOMContentLoaded', function () {
    const commentsSection = document.getElementById('commentsSection');
    if (!commentsSection) return;

    const commentComposer = document.getElementById('commentComposer');
    const composerTrigger = document.getElementById('composerTrigger');
    const cancelBtn = document.getElementById('cancelComment');
    const commentForm = document.getElementById('commentForm');
    const commentContent = document.getElementById('commentContent');
    const charCount = document.getElementById('charCount');
    const submitBtn = document.getElementById('submitComment');
    const commentsList = document.getElementById('commentsList');
    const commentCountBadge = document.getElementById('commentCount');

    function getAvatarInitial(firstName) {
        const trimmed = (firstName || '').trim();
        return trimmed ? trimmed[0].toUpperCase() : 'U';
    }

    // Expand/Collapse composer
    function expandComposer() {
        if (!commentComposer) return;
        commentComposer.classList.add('comment-composer--expanded');
        setTimeout(() => {
            commentContent?.focus();
        }, 50);
    }

    function collapseComposer() {
        if (!commentComposer || !commentContent) return;
        if (commentContent.value.trim()) return; // Don't collapse if there's content
        commentComposer.classList.remove('comment-composer--expanded');
    }

    if (composerTrigger) {
        composerTrigger.addEventListener('click', expandComposer);
    }

    if (cancelBtn) {
        cancelBtn.addEventListener('click', function () {
            if (commentContent) {
                commentContent.value = '';
                updateCharCounter(0);
            }
            if (commentComposer) {
                commentComposer.classList.remove('comment-composer--expanded');
            }
        });
    }

    // Collapse when clicking outside (if empty)
    document.addEventListener('click', function (e) {
        if (!commentComposer?.classList.contains('comment-composer--expanded')) return;
        if (commentComposer.contains(e.target)) return;
        collapseComposer();
    });

    // Character counter with microinteractions
    const charCounter = document.getElementById('charCounter');
    const maxChars = 2000;
    let lastCharCount = 0;

    function updateCharCounter(length) {
        if (!charCount || !charCounter) return;

        // Update text
        charCount.textContent = length;

        // Remove all state classes
        charCounter.classList.remove('char-counter--warning', 'char-counter--danger', 'char-counter--shake');

        // Add appropriate state class based on thresholds
        if (length >= maxChars) {
            charCounter.classList.add('char-counter--danger');
            if (lastCharCount < maxChars) {
                charCounter.classList.add('char-counter--shake');
            }
        } else if (length >= maxChars * 0.9) {
            charCounter.classList.add('char-counter--danger');
        } else if (length >= maxChars * 0.75) {
            charCounter.classList.add('char-counter--warning');
        }

        // Bump animation on changes
        if (length !== lastCharCount && length > 0) {
            charCounter.classList.add('char-counter--bump');
            setTimeout(() => charCounter.classList.remove('char-counter--bump'), 150);
        }

        lastCharCount = length;

        // Update submit button state
        if (submitBtn) {
            submitBtn.disabled = length === 0;
        }
    }

    if (commentContent) {
        commentContent.addEventListener('input', function () {
            updateCharCounter(this.value.length);
        });

        // Initialize counter state
        updateCharCounter(commentContent.value.length);
    }

    // Submit new comment
    if (commentForm) {
        commentForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const content = commentContent.value.trim();
            if (!content) {
                showToast('Please enter a comment.', 'warning');
                return;
            }

            const bookmarkId = parseInt(commentForm.dataset.bookmarkId);
            submitBtn.disabled = true;

            try {
                const response = await csrf.fetch('/Comments/Create', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ bookmarkId, content })
                });

                const data = await response.json();

                if (response.ok && data.success) {
                    const noCommentsMessage = document.getElementById('noCommentsMessage');
                    if (noCommentsMessage) {
                        noCommentsMessage.remove();
                    }

                    const commentHtml = createCommentHtml(data.comment);
                    commentsList.insertAdjacentHTML('afterbegin', commentHtml);

                    updateCommentCount(1);

                    // Success animation
                    submitBtn.classList.add('comment-submit-btn--success');
                    setTimeout(() => {
                        submitBtn.classList.remove('comment-submit-btn--success');
                    }, 1500);

                    commentContent.value = '';
                    updateCharCounter(0);

                    // Collapse composer after posting
                    setTimeout(() => {
                        if (commentComposer) {
                            commentComposer.classList.remove('comment-composer--expanded');
                        }
                    }, 800);

                    showToast('Comment posted successfully!', 'success');
                } else {
                    showToast(data.message || 'Failed to post comment.', 'danger');
                }
            } catch (error) {
                console.error('Error posting comment:', error);
                showToast('Failed to post comment. Please try again.', 'danger');
            } finally {
                submitBtn.disabled = false;
            }
        });
    }

    // Delegate events for existing comments
    commentsList.addEventListener('click', function (e) {
        const commentItem = e.target.closest('.comment-item');
        if (!commentItem) return;

        if (e.target.closest('.edit-comment-btn')) {
            toggleEditMode(commentItem, true);
        }

        if (e.target.closest('.cancel-edit-btn')) {
            toggleEditMode(commentItem, false);
        }

        if (e.target.closest('.save-edit-btn')) {
            saveComment(commentItem);
        }

        if (e.target.closest('.delete-comment-btn')) {
            deleteComment(commentItem);
        }
    });

    function toggleEditMode(commentItem, isEditing) {
        const contentDiv = commentItem.querySelector('.comment-content');
        const editForm = commentItem.querySelector('.comment-edit-form');

        if (isEditing) {
            contentDiv.classList.add('d-none');
            editForm.classList.remove('d-none');
            editForm.querySelector('.edit-textarea').focus();
        } else {
            contentDiv.classList.remove('d-none');
            editForm.classList.add('d-none');
            const originalContent = contentDiv.querySelector('.comment-text').textContent;
            editForm.querySelector('.edit-textarea').value = originalContent;
        }
    }

    async function saveComment(commentItem) {
        const commentId = parseInt(commentItem.dataset.commentId);
        const editForm = commentItem.querySelector('.comment-edit-form');
        const textarea = editForm.querySelector('.edit-textarea');
        const content = textarea.value.trim();

        if (!content) {
            showToast('Comment cannot be empty.', 'warning');
            return;
        }

        const saveBtn = editForm.querySelector('.save-edit-btn');
        saveBtn.disabled = true;

        try {
            const response = await csrf.fetch('/Comments/Edit', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: commentId, content })
            });

            const data = await response.json();

            if (response.ok && data.success) {
                const contentText = commentItem.querySelector('.comment-text');
                contentText.textContent = data.comment.content;

                const timeSpan = commentItem.querySelector('small.text-muted');
                if (!timeSpan.querySelector('.fst-italic')) {
                    timeSpan.insertAdjacentHTML('beforeend', ' <span class="fst-italic">(edited)</span>');
                }

                toggleEditMode(commentItem, false);
                showToast('Comment updated successfully!', 'success');
            } else {
                showToast(data.message || 'Failed to update comment.', 'danger');
            }
        } catch (error) {
            console.error('Error updating comment:', error);
            showToast('Failed to update comment. Please try again.', 'danger');
        } finally {
            saveBtn.disabled = false;
        }
    }

    function deleteComment(commentItem) {
        showConfirmModal({
            title: 'Delete Comment',
            message: 'Are you sure you want to delete this comment?',
            detail: 'This action cannot be undone.',
            confirmText: 'Delete',
            onConfirm: () => performDelete(commentItem)
        });
    }

    async function performDelete(commentItem) {
        const commentId = parseInt(commentItem.dataset.commentId);
        const deleteBtn = commentItem.querySelector('.delete-comment-btn');
        deleteBtn.disabled = true;

        try {
            const response = await csrf.fetch('/Comments/Delete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: commentId })
            });

            const data = await response.json();

            if (response.ok && data.success) {
                commentItem.remove();
                updateCommentCount(-1);

                if (!commentsList.querySelector('.comment-item')) {
                    commentsList.innerHTML = `
                        <div class="text-center py-5 text-muted" id="noCommentsMessage">
                            <i class="bi bi-chat-square-text fs-1 d-block mb-2 opacity-50"></i>
                            <p class="mb-0">No comments yet. Be the first to share your thoughts!</p>
                        </div>
                    `;
                }

                showToast('Comment deleted.', 'success');
            } else {
                showToast(data.message || 'Failed to delete comment.', 'danger');
            }
        } catch (error) {
            console.error('Error deleting comment:', error);
            showToast('Failed to delete comment. Please try again.', 'danger');
        } finally {
            deleteBtn.disabled = false;
        }
    }

    function updateCommentCount(delta) {
        if (commentCountBadge) {
            const current = parseInt(commentCountBadge.textContent) || 0;
            commentCountBadge.textContent = Math.max(0, current + delta);
        }
    }

    function createCommentHtml(comment) {
        const editedText = comment.updatedAt ? '<span class="fst-italic">(edited)</span>' : '';
        const createdAt = new Date(comment.createdAt).toLocaleString('en-GB', {
            day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit'
        });

        const profileUrl = `/Profile/Index/${encodeURIComponent(comment.authorUserName)}`;
        const avatarContent = comment.authorProfilePictureUrl
            ? `<img src="${escapeHtml(comment.authorProfilePictureUrl)}" alt="${escapeHtml(comment.authorName)}" class="comment-avatar-img" />`
            : `<div class="comment-avatar-placeholder">${escapeHtml(getAvatarInitial(comment.authorFirstName))}</div>`;
        const avatarHtml = `<a href="${profileUrl}" class="text-decoration-none">${avatarContent}</a>`;

        const ownerActions = comment.isOwner ? `
            <div class="comment-actions">
                <button class="btn btn-sm btn-outline-secondary edit-comment-btn"
                        title="Edit comment" aria-label="Edit comment">
                    <i class="bi bi-pencil"></i>
                </button>
                <button class="btn btn-sm btn-outline-danger delete-comment-btn"
                        title="Delete comment" aria-label="Delete comment">
                    <i class="bi bi-trash"></i>
                </button>
            </div>
        ` : '';

        const editForm = comment.isOwner ? `
            <div class="comment-edit-form d-none">
                <textarea class="form-control mb-2 edit-textarea" rows="3" maxlength="2000">${escapeHtml(comment.content)}</textarea>
                <div class="d-flex justify-content-end gap-2">
                    <button type="button" class="btn btn-sm btn-light cancel-edit-btn">Cancel</button>
                    <button type="button" class="btn btn-sm btn-primary save-edit-btn">Save</button>
                </div>
            </div>
        ` : '';

        return `
            <div class="comment-item card border-0 shadow-sm mb-3" data-comment-id="${comment.id}">
                <div class="card-body">
                    <div class="d-flex gap-3">
                        <div class="comment-item__avatar flex-shrink-0">
                            ${avatarHtml}
                        </div>
                        <div class="flex-grow-1">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <div>
                                    <a href="${profileUrl}" class="fw-semibold text-dark text-decoration-none comment-author-link">${escapeHtml(comment.authorName)}</a>
                                    <small class="text-muted ms-2">
                                        ${createdAt}
                                        ${editedText}
                                    </small>
                                </div>
                                ${ownerActions}
                            </div>
                            <div class="comment-content">
                                <p class="mb-0 text-break comment-text">${escapeHtml(comment.content)}</p>
                            </div>
                            ${editForm}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    function showToast(message, type = 'info') {
        const toastContainer = document.getElementById('toastContainer') || createToastContainer();

        const toastId = 'toast-' + Date.now();
        const iconClass = {
            'success': 'bi-check-circle-fill text-success',
            'danger': 'bi-exclamation-triangle-fill text-danger',
            'warning': 'bi-exclamation-circle-fill text-warning',
            'info': 'bi-info-circle-fill text-info'
        }[type] || 'bi-info-circle-fill text-info';

        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        <i class="bi ${iconClass} me-2"></i>
                        ${escapeHtml(message)}
                    </div>
                    <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastEl = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastEl, { autohide: true, delay: 3000 });
        toast.show();

        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    }

    function createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        container.style.zIndex = '1100';
        document.body.appendChild(container);
        return container;
    }
});
