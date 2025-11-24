// Global CSRF token management for secure AJAX requests
// Usage: await csrf.fetch('/api/endpoint', { method: 'POST', ... })

window.csrf = {
    token: null,

    /**
     * Ensures a CSRF token is available, fetching it if necessary
     * @returns {Promise<string>} The CSRF token
     */
    async ensureToken() {
        if (this.token) return this.token;

        // Check if token exists in cookie
        const cookieToken = document.cookie
            .split('; ')
            .find(row => row.startsWith('XSRF-TOKEN='))
            ?.split('=')[1];

        if (cookieToken) {
            this.token = cookieToken;
            return cookieToken;
        }

        // Fetch token from server
        await fetch('/antiforgery/token');
        this.token = document.cookie
            .split('; ')
            .find(row => row.startsWith('XSRF-TOKEN='))
            ?.split('=')[1];

        return this.token;
    },

    /**
     * Wrapper around fetch() that automatically includes CSRF token
     * @param {string} url - The URL to fetch
     * @param {object} options - Fetch options (method, headers, body, etc.)
     * @returns {Promise<Response>} The fetch response
     */
    async fetch(url, options = {}) {
        const token = await this.ensureToken();

        if (!token) {
            throw new Error('Unable to obtain CSRF token');
        }

        return fetch(url, {
            ...options,
            headers: {
                ...options.headers,
                'X-XSRF-TOKEN': token
            }
        });
    }
};
