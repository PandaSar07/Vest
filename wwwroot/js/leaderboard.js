(function () {
    const tbody = document.getElementById('lbTableBody');
    if (!tbody) return;

    const pageSize = 25;
    let currentPage = 1;
    let loading = false;
    let hasMore = true;
    let currentSearch = '';
    const initialRows = tbody.querySelectorAll('tr').length;
    if (initialRows >= pageSize) currentPage = 2;
    else if (initialRows > 0) hasMore = false;

    function rankClass(rank) {
        if (rank === 1) return 'gold';
        if (rank === 2) return 'silver';
        if (rank === 3) return 'bronze';
        return '';
    }

    function escapeHtml(s) {
        const d = document.createElement('div');
        d.textContent = s;
        return d.innerHTML;
    }

    function appendRows(entries) {
        const empty = document.getElementById('lbEmptyState');
        if (empty) empty.style.display = 'none';

        entries.forEach(function (e) {
            const pctClass = e.returnPct >= 0 ? 'positive' : 'negative';
            const sign = e.returnPct >= 0 ? '+' : '';
            const sub = e.displayName
                ? '<span class="lb-sub d-block">' + escapeHtml(e.displayName) + '</span>'
                : '';
            const href = e.profileUrl || ('/Leaderboard/Profile/' + encodeURIComponent(e.username));
            const tr = document.createElement('tr');
            tr.className = 'lb-row';
            tr.dataset.href = href;
            tr.innerHTML =
                '<td class="lb-rank ' + rankClass(e.rank) + '">' + e.rank + '</td>' +
                '<td><span class="lb-user">' + escapeHtml(e.username) + '</span>' + sub + '</td>' +
                '<td class="text-end lb-value">$' + Number(e.totalValue).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + '</td>' +
                '<td class="text-end lb-pct ' + pctClass + ' lb-hide-sm">' + sign + Number(e.returnPct).toFixed(2) + '%</td>' +
                '<td class="text-end lb-hide-sm text-secondary">' + e.tradeCount + '</td>';
            tbody.appendChild(tr);
        });
    }

    tbody.addEventListener('click', function (ev) {
        const row = ev.target.closest('.lb-row');
        if (row && row.dataset.href) window.location.href = row.dataset.href;
    });

    async function loadPage(page) {
        if (loading || !hasMore) return;
        loading = true;
        const loadBtn = document.getElementById('lbLoadMore');
        if (loadBtn) loadBtn.disabled = true;

        try {
            let url = '/api/leaderboard?page=' + page + '&pageSize=' + pageSize;
            if (currentSearch) url += '&search=' + encodeURIComponent(currentSearch);
            const r = await fetch(url);
            if (!r.ok) return;
            const data = await r.json();

            if (data.updatedAt) {
                const label = document.getElementById('lbUpdatedLabel');
                if (label) {
                    const dt = new Date(data.updatedAt);
                    label.textContent = 'Last updated ' + dt.toLocaleString(undefined, {
                        month: 'short', day: 'numeric', year: 'numeric',
                        hour: 'numeric', minute: '2-digit'
                    });
                }
            }

            if (page === 1 && data.entries.length === 0 && tbody.children.length === 0) {
                const empty = document.getElementById('lbEmptyState');
                if (empty) empty.style.display = 'block';
            }

            if (page > 1 || (page === 1 && initialRows === 0)) {
                appendRows(data.entries);
            }

            hasMore = data.hasMore;
            currentPage = page + 1;
        } catch (err) {
            console.warn('Leaderboard load failed', err);
        } finally {
            loading = false;
            if (loadBtn) {
                loadBtn.disabled = false;
                loadBtn.style.display = hasMore ? 'block' : 'none';
            }
        }
    }

    const sentinel = document.getElementById('lbSentinel');
    const loadBtn = document.getElementById('lbLoadMore');

    if (loadBtn) {
        loadBtn.addEventListener('click', function () { loadPage(currentPage); });
        loadBtn.style.display = hasMore ? 'block' : 'none';
    }

    if (sentinel && 'IntersectionObserver' in window) {
        const obs = new IntersectionObserver(function (entries) {
            if (entries.some(function (e) { return e.isIntersecting; })) {
                loadPage(currentPage);
            }
        }, { rootMargin: '120px' });
        obs.observe(sentinel);
    }

    const searchInput = document.getElementById('lbSearchInput');
    let searchTimeout = null;
    if (searchInput) {
        searchInput.addEventListener('input', function (e) {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(function () {
                currentSearch = e.target.value.trim();
                tbody.innerHTML = ''; // clear table
                hasMore = true;
                currentPage = 1;
                loadPage(1);
            }, 300);
        });
    }

    async function loadMe() {
        const banner = document.getElementById('lbMeBanner');
        if (!banner) return;
        try {
            const r = await fetch('/api/leaderboard/me');
            if (!r.ok) return;
            const me = await r.json();
            banner.style.display = 'block';
            const rankEl = document.getElementById('lbMeRank');
            const valEl = document.getElementById('lbMeValue');
            const valBox = document.getElementById('lbMeValueBox');
            const enableBox = document.getElementById('lbEnableProfileBox');

            if (me.onLeaderboard && me.rank) {
                rankEl.textContent = '#' + me.rank;
            } else if (me.isPublic) {
                rankEl.textContent = 'Not ranked yet';
            } else {
                rankEl.textContent = 'Private';
            }

            if (me.isPublic) {
                if (enableBox) enableBox.style.display = 'none';
                if (valBox) valBox.style.display = 'block';
                if (me.totalValue != null) {
                    valEl.textContent = '$' + Number(me.totalValue).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                } else {
                    valEl.textContent = '—';
                }
            } else {
                if (valBox) valBox.style.display = 'none';
                if (enableBox) enableBox.style.display = 'block';
            }
        } catch { /* optional */ }
    }

    const enableBtn = document.getElementById('btnEnablePublicProfile');
    if (enableBtn) {
        enableBtn.addEventListener('click', async function () {
            enableBtn.disabled = true;
            enableBtn.textContent = 'Enabling...';
            try {
                const r = await fetch('/api/leaderboard/privacy', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ isPublic: true })
                });
                if (r.ok) {
                    window.location.reload();
                } else {
                    console.warn('Failed to enable public profile: server returned ' + r.status);
                    alert('Could not enable public profile. The database table (user_prefs) might be missing.');
                }
            } catch (err) {
                console.warn('Failed to enable public profile', err);
            } finally {
                enableBtn.disabled = false;
                enableBtn.textContent = 'Enable Public Profile';
            }
        });
    }

    loadMe();
})();
