/**
 * Shared stop-loss / take-profit controls for Stocks & Crypto trade panels.
 */
window.VestTradeRisk = (function () {
    function $(id) { return document.getElementById(id); }

    function clearInputs() {
        ['riskSlPct', 'riskTpPct', 'riskSlPrice', 'riskTpPrice'].forEach(id => {
            const el = $(id);
            if (el) el.value = '';
        });
    }

    function setRiskMode(mode) {
        const pctRow = $('riskPctRow');
        const priceRow = $('riskPriceRow');
        if (pctRow) pctRow.style.display = mode === 'pct' ? 'grid' : 'none';
        if (priceRow) priceRow.style.display = mode === 'price' ? 'grid' : 'none';
        const radio = document.querySelector(`input[name="riskMode"][value="${mode}"]`);
        if (radio) radio.checked = true;
    }

    function collectPayload() {
        const enabled = $('riskEnabled')?.checked;
        if (!enabled) return {};
        const mode = document.querySelector('input[name="riskMode"]:checked')?.value || 'pct';
        if (mode === 'pct') {
            const sl = parseFloat($('riskSlPct')?.value);
            const tp = parseFloat($('riskTpPct')?.value);
            const body = {};
            if (!isNaN(sl) && sl > 0) body.stopLossPct = sl;
            if (!isNaN(tp) && tp > 0) body.takeProfitPct = tp;
            return body;
        }
        const sl = parseFloat($('riskSlPrice')?.value);
        const tp = parseFloat($('riskTpPrice')?.value);
        const body = {};
        if (!isNaN(sl) && sl > 0) body.stopLossPrice = sl;
        if (!isNaN(tp) && tp > 0) body.takeProfitPrice = tp;
        return body;
    }

    function renderActiveRisk(risk, entryPrice) {
        const banner = $('activeRiskBanner');
        if (!banner) return;
        if (!risk) {
            banner.style.display = 'none';
            banner.innerHTML = '';
            return;
        }
        const parts = [];
        if (risk.stopLossPrice != null) parts.push(`SL $${Number(risk.stopLossPrice).toFixed(2)}`);
        else if (risk.stopLossPct != null) parts.push(`SL −${risk.stopLossPct}%`);
        if (risk.takeProfitPrice != null) parts.push(`TP $${Number(risk.takeProfitPrice).toFixed(2)}`);
        else if (risk.takeProfitPct != null) parts.push(`TP +${risk.takeProfitPct}%`);
        banner.style.display = 'block';
        banner.innerHTML = `<strong style="color:#fbbf24;">Active risk</strong> · Entry $${Number(risk.entryPrice || entryPrice || 0).toFixed(2)} · ${parts.join(' · ')}`;
    }

    function applyRiskToForm(risk) {
        const enabled = $('riskEnabled');
        const fields = $('riskFields');
        if (!enabled || !fields) return;

        if (!risk) {
            enabled.checked = false;
            fields.style.display = 'none';
            clearInputs();
            renderActiveRisk(null);
            return;
        }

        enabled.checked = true;
        fields.style.display = 'block';

        const usePct = risk.stopLossPct != null || risk.takeProfitPct != null;
        setRiskMode(usePct ? 'pct' : 'price');

        if (usePct) {
            if ($('riskSlPct')) $('riskSlPct').value = risk.stopLossPct ?? '';
            if ($('riskTpPct')) $('riskTpPct').value = risk.takeProfitPct ?? '';
        } else {
            if ($('riskSlPrice')) $('riskSlPrice').value = risk.stopLossPrice ?? '';
            if ($('riskTpPrice')) $('riskTpPrice').value = risk.takeProfitPrice ?? '';
        }
        renderActiveRisk(risk);
    }

    async function loadRisk(symbol) {
        try {
            const r = await fetch(`/portfolio/risk?symbol=${encodeURIComponent(symbol)}`, { credentials: 'same-origin' });
            if (!r.ok) return null;
            return await r.json();
        } catch {
            return null;
        }
    }

    async function removeRisk(symbol) {
        await fetch(`/portfolio/risk?symbol=${encodeURIComponent(symbol)}`, { method: 'DELETE', credentials: 'same-origin' });
        applyRiskToForm(null);
    }

    async function saveRisk(symbol, entryPrice) {
        const payload = collectPayload();
        if (!Object.keys(payload).length) {
            await removeRisk(symbol);
            return { ok: true };
        }
        const r = await fetch('/portfolio/risk', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin',
            body: JSON.stringify({ symbol, entryPrice, ...payload }),
        });
        const data = await r.json();
        if (!r.ok) return { ok: false, error: data.error || 'Could not save risk settings.' };
        applyRiskToForm(data);
        return { ok: true };
    }

    function bindUi(getEntryPrice) {
        const enabled = $('riskEnabled');
        const fields = $('riskFields');
        if (!enabled || !fields) return;

        const toggleFields = () => {
            fields.style.display = enabled.checked ? 'block' : 'none';
        };
        enabled.addEventListener('change', toggleFields);
        toggleFields();

        document.querySelectorAll('input[name="riskMode"]').forEach(radio => {
            radio.addEventListener('change', () => setRiskMode(radio.value));
        });

        $('riskRemoveBtn')?.addEventListener('click', async () => {
            if (typeof currentSymbol === 'undefined') return;
            await removeRisk(currentSymbol);
            enabled.checked = false;
            toggleFields();
        });

        $('riskApplyBtn')?.addEventListener('click', async () => {
            if (typeof currentSymbol === 'undefined') return;
            const entry = typeof getEntryPrice === 'function' ? getEntryPrice() : 0;
            const res = await saveRisk(currentSymbol, entry);
            if (!res.ok && typeof showToast === 'function') showToast(res.error, false);
            else if (res.ok && typeof showToast === 'function') showToast('Risk settings saved.', true);
        });
    }

    function onTabSwitch(mode) {
        const panel = $('riskPanel');
        if (!panel) return;
        panel.style.display = (mode === 'buy' || mode === 'sell') ? 'block' : 'none';
        const fields = $('riskFields');
        const enabled = $('riskEnabled');
        const applyBtn = $('riskApplyBtn');
        if (mode === 'sell') {
            if (fields) fields.style.display = 'none';
            if (enabled) enabled.disabled = true;
            if (applyBtn) applyBtn.style.display = 'none';
        } else if (mode === 'limit') {
            panel.style.display = 'none';
        } else {
            if (enabled) enabled.disabled = false;
            if (applyBtn) applyBtn.style.display = '';
            if (enabled && fields) fields.style.display = enabled.checked ? 'block' : 'none';
        }
    }

    return {
        collectPayload,
        renderActiveRisk,
        applyRiskToForm,
        loadRisk,
        removeRisk,
        saveRisk,
        bindUi,
        onTabSwitch,
    };
})();
