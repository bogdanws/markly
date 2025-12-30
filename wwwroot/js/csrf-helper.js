// Global CSRF token management for secure AJAX requests
// Usage: await csrf.fetch('/api/endpoint', { method: 'POST', ... })

window.csrf = {
    _refreshPromise: null,
    _lastRefresh: 0,

    /**
     * Gets the current CSRF token from cookie, fetching if necessary
     * @returns {Promise<string>} The CSRF token
     */
    async ensureToken() {
        // Always read fresh from cookie to avoid stale token issues
        const cookieRow = document.cookie
            .split('; ')
            .find(row => row.startsWith('XSRF-TOKEN='));

        if (cookieRow) {
            // URL-decode the token as ASP.NET Core may encode special characters
            return decodeURIComponent(cookieRow.split('=')[1]);
        }

        // Fetch token from server if not in cookie
        await fetch('/antiforgery/token');
        const newCookieRow = document.cookie
            .split('; ')
            .find(row => row.startsWith('XSRF-TOKEN='));

        if (newCookieRow) {
            return decodeURIComponent(newCookieRow.split('=')[1]);
        }

        return null;
    },

    /**
     * Forces a fresh token from the server (debounced, deduped)
     * @returns {Promise<string|null>} The new CSRF token
     */
    async refreshToken() {
        const now = Date.now();

        // If we refreshed within the last 2 seconds, just return current cookie
        if (now - this._lastRefresh < 2000) {
            const cookieRow = document.cookie
                .split('; ')
                .find(row => row.startsWith('XSRF-TOKEN='));
            return cookieRow ? decodeURIComponent(cookieRow.split('=')[1]) : null;
        }

        // If a refresh is already in progress, wait for it
        if (this._refreshPromise) {
            return this._refreshPromise;
        }

        // Start a new refresh
        this._refreshPromise = (async () => {
            try {
                this._lastRefresh = now;
                await fetch('/antiforgery/token');
                const cookieRow = document.cookie
                    .split('; ')
                    .find(row => row.startsWith('XSRF-TOKEN='));
                return cookieRow ? decodeURIComponent(cookieRow.split('=')[1]) : null;
            } finally {
                this._refreshPromise = null;
            }
        })();

        return this._refreshPromise;
    },

    /**
     * Wrapper around fetch() that automatically includes CSRF token
     * @param {string} url - The URL to fetch
     * @param {object} options - Fetch options (method, headers, body, etc.)
     * @returns {Promise<Response>} The fetch response
     */
    async fetch(url, options = {}) {
        let token = await this.ensureToken();

        if (!token) {
            token = await this.refreshToken();
        }

        if (!token) {
            throw new Error('Unable to obtain CSRF token');
        }

        const response = await fetch(url, {
            ...options,
            headers: {
                ...options.headers,
                'X-XSRF-TOKEN': token
            }
        });

        // If we get a 400 due to antiforgery, refresh token and retry once
        if (response.status === 400) {
            const newToken = await this.refreshToken();
            if (newToken && newToken !== token) {
                return fetch(url, {
                    ...options,
                    headers: {
                        ...options.headers,
                        'X-XSRF-TOKEN': newToken
                    }
                });
            }
        }

        return response;
    }
};
