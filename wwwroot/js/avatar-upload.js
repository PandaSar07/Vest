(function () {
    const fileInput = document.getElementById('avatarFile');
    const uploadBtn = document.getElementById('avatarUploadBtn');
    const removeBtn = document.getElementById('avatarRemoveBtn');
    const errEl = document.getElementById('avatarError');
    const imgEl = document.getElementById('settingsAvatarImg');
    const initialsEl = document.getElementById('settingsAvatarInitials');

    if (!fileInput || !uploadBtn) return;

    function showError(msg) {
        if (!errEl) return;
        errEl.textContent = msg || '';
        errEl.style.display = msg ? 'block' : 'none';
    }

    function setPreview(url) {
        if (url && imgEl && initialsEl) {
            imgEl.src = url;
            imgEl.style.display = 'block';
            initialsEl.style.display = 'none';
        } else if (imgEl && initialsEl) {
            imgEl.removeAttribute('src');
            imgEl.style.display = 'none';
            initialsEl.style.display = 'flex';
        }
        if (removeBtn) removeBtn.style.display = url ? 'inline-block' : 'none';
    }

    uploadBtn.addEventListener('click', () => fileInput.click());

    fileInput.addEventListener('change', async () => {
        const file = fileInput.files && fileInput.files[0];
        if (!file) return;
        showError('');
        uploadBtn.disabled = true;
        if (removeBtn) removeBtn.disabled = true;

        const form = new FormData();
        form.append('file', file);

        try {
            const r = await fetch('/api/profile/avatar', {
                method: 'POST',
                body: form,
                credentials: 'same-origin',
            });
            const data = await r.json().catch(() => ({}));
            if (!r.ok) {
                showError(data.error || 'Upload failed. Please try again.');
                return;
            }
            setPreview(data.avatarUrl);
            document.querySelectorAll('.nav-avatar-img').forEach(el => {
                el.src = data.avatarUrl;
            });
            document.querySelectorAll('.nav-avatar:not(.user-avatar-img)').forEach(el => {
                if (el.tagName === 'DIV') el.style.display = 'none';
            });
        } catch {
            showError('Network error. Please try again.');
        } finally {
            fileInput.value = '';
            uploadBtn.disabled = false;
            if (removeBtn) removeBtn.disabled = false;
        }
    });

    if (removeBtn) {
        removeBtn.addEventListener('click', async () => {
            showError('');
            removeBtn.disabled = true;
            uploadBtn.disabled = true;
            try {
                const r = await fetch('/api/profile/avatar', {
                    method: 'DELETE',
                    credentials: 'same-origin',
                });
                const data = await r.json().catch(() => ({}));
                if (!r.ok) {
                    showError(data.error || 'Could not remove profile picture.');
                    return;
                }
                setPreview(null);
                document.querySelectorAll('.nav-avatar-img').forEach(el => el.remove());
                document.querySelectorAll('.nav-avatar').forEach(el => {
                    if (el.tagName === 'DIV') el.style.display = '';
                });
            } catch {
                showError('Network error. Please try again.');
            } finally {
                removeBtn.disabled = false;
                uploadBtn.disabled = false;
            }
        });
    }
})();
