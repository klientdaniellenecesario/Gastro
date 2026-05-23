// TASTECEBU - Main JavaScript

// ===== AOS ANIMATION =====
if (window.AOS) {
    AOS.init({ duration: 800, once: true, offset: 100 });
}

// ===== NAVBAR SCROLL EFFECT =====
const navbar = document.getElementById('navbar');
window.addEventListener('scroll', () => {
    if (!navbar) return;
    navbar.classList.toggle('scrolled', window.scrollY > 100);
});

// ===== BACK TO TOP =====
const backToTop = document.getElementById('backToTop');
window.addEventListener('scroll', () => {
    if (!backToTop) return;
    backToTop.classList.toggle('visible', window.scrollY > 500);
});
if (backToTop) {
    backToTop.addEventListener('click', () => window.scrollTo({ top: 0, behavior: 'smooth' }));
}

// ===== MOBILE DRAWER =====
const mobileMenuBtn = document.getElementById('mobileMenuBtn');
const mobileDrawer = document.getElementById('mobileDrawer');
const drawerOverlay = document.getElementById('drawerOverlay');
const closeDrawerBtn = document.getElementById('closeDrawer');

function openDrawer() {
    if (!mobileDrawer || !drawerOverlay) return;
    mobileDrawer.classList.add('open');
    drawerOverlay.classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeDrawerFunc() {
    if (!mobileDrawer || !drawerOverlay) return;
    mobileDrawer.classList.remove('open');
    drawerOverlay.classList.remove('open');
    document.body.style.overflow = '';
}

if (mobileMenuBtn) mobileMenuBtn.addEventListener('click', openDrawer);
if (closeDrawerBtn) closeDrawerBtn.addEventListener('click', closeDrawerFunc);
if (drawerOverlay) drawerOverlay.addEventListener('click', closeDrawerFunc);

// ===== DARK / LIGHT MODE TOGGLE =====
// NOTE: The theme is applied before paint by an inline script in _Layout.cshtml
// so there is no flash. Here we just wire up the toggle button.
const themeToggle = document.getElementById('themeToggle');
const htmlEl = document.documentElement;

function updateThemeIcon(theme) {
    if (!themeToggle) return;
    const icon = themeToggle.querySelector('i');
    if (!icon) return;
    icon.className = theme === 'light' ? 'fas fa-moon' : 'fas fa-sun';
}

// Sync icon with whatever theme the pre-paint script already applied
updateThemeIcon(htmlEl.getAttribute('data-theme') || 'dark');

if (themeToggle) {
    themeToggle.addEventListener('click', () => {
        const current = htmlEl.getAttribute('data-theme') || 'dark';
        const next = current === 'light' ? 'dark' : 'light';
        htmlEl.setAttribute('data-theme', next);
        localStorage.setItem('theme', next);
        updateThemeIcon(next);
        showToast(`${next === 'dark' ? '🌙 Dark' : '☀️ Light'} mode activated`);
    });
}

// ===== TOAST NOTIFICATION =====
function showToast(message, duration = 3000) {
    const toast = document.createElement('div');
    toast.className = 'toast-notification';
    toast.innerHTML = message;
    document.body.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }, duration);
}

// ===== VIBE FILTER =====
window.filterByVibe = function (vibe, btn) {
    const cards = document.querySelectorAll('.restaurant-card');
    const buttons = document.querySelectorAll('.vibe-btn');
    buttons.forEach(b => b.classList.remove('active'));
    // support both filterByVibe('x', this) and filterByVibe('x') via onclick
    const activeBtn = btn || (typeof event !== 'undefined' && event.target ? event.target.closest('.vibe-btn') : null);
    if (activeBtn) activeBtn.classList.add('active');

    cards.forEach(card => {
        const show = vibe === 'all' || card.dataset.vibe === vibe;
        card.style.display = show ? 'block' : 'none';
        if (show) card.style.animation = 'fadeIn 0.3s ease';
    });

    showToast(`Showing ${vibe} places 🍽️`);
};

// ===== I TRIED THIS BUTTON =====
window.triedDish = async function (dishName, dishId) {
    try {
        const response = await fetch(`/api/dishes/${dishId}/tried`, { method: 'POST' });
        if (response.status === 401) {
            showGuestModal();
            return;
        }
        if (window.Swal) {
            Swal.fire({
                title: 'Yum!',
                text: `${dishName} added to your food journey!`,
                icon: 'success',
                background: getComputedStyle(document.documentElement).getPropertyValue('--bg-card'),
                color: getComputedStyle(document.documentElement).getPropertyValue('--text-primary'),
                confirmButtonColor: '#E76F51',
                timer: 2000,
                showConfirmButton: false
            });
        } else {
            showToast(`${dishName} added to your food journey!`);
        }
    } catch {
        showToast('Could not save. Please try again.');
    }
};

// ===== GUEST SAVE MODAL =====
function showGuestModal(returnUrl) {
    const modal = document.getElementById('guestSaveModal');
    if (!modal) return;
    const base = returnUrl || window.location.pathname;
    document.getElementById('guestModalJoin').href = '/Account/Register?returnUrl=' + encodeURIComponent(base);
    document.getElementById('guestModalSignIn').href = '/Account/Login?returnUrl=' + encodeURIComponent(base);
    modal.style.display = 'flex';
    modal.addEventListener('click', function (e) {
        if (e.target === modal) closeGuestModal();
    }, { once: true });
}
window.closeGuestModal = function () {
    const modal = document.getElementById('guestSaveModal');
    if (modal) modal.style.display = 'none';
};
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeGuestModal();
});

// ===== ADD TO CRAVINGS / BOOKMARKS =====
window.addToCravings = async function (itemName, itemId, type) {
    const numericId = Number.parseInt(itemId, 10) || 1;
    try {
        const response = await fetch('/api/bookmarks', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ itemType: type, itemId: numericId, itemName })
        });
        if (response.status === 401) {
            showGuestModal();
            return;
        }
        const result = await response.json();
        showToast(result.message || `${itemName} bookmark updated.`);
    } catch {
        showToast('Could not update bookmark. Please try again.');
    }
};

// ===== SUBMIT REVIEW =====
window.submitServerReview = async function (targetType, targetId, rating, text) {
    try {
        const response = await fetch('/api/reviews', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ targetType, targetId, rating, text })
        });
        if (response.status === 401) {
            window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
            return false;
        }
        const result = await response.json();
        showToast(result.message || 'Review submitted.');
        return response.ok;
    } catch {
        showToast('Could not submit review.');
        return false;
    }
};

// ===== REGISTER FOR EVENT =====
window.registerForEvent = async function (eventId) {
    try {
        const response = await fetch(`/api/events/${eventId}/register`, { method: 'POST' });
        if (response.status === 401) {
            showGuestModal();
            return;
        }
        const result = await response.json();
        showToast(result.message || 'Registered for event.');
    } catch {
        showToast('Could not register for event. Please try again.');
    }
};

// ===== STAR RATING =====
window.setRating = function (rating) {
    const stars = document.querySelectorAll('.star-rating i');
    stars.forEach((star, index) => {
        if (index < rating) {
            star.className = 'fas fa-star';
            star.style.color = '#F4A261';
        } else {
            star.className = 'far fa-star';
            star.style.color = '';
        }
    });
    const rv = document.getElementById('ratingValue');
    if (rv) rv.value = rating;
};

// ===== LOAD MORE =====
window.loadMore = function (button) {
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Loading...';
    setTimeout(() => {
        button.innerHTML = 'Load More →';
        showToast('More delicious content coming soon!');
    }, 1500);
};

// ===== COUNTDOWN TIMER =====
function startCountdown(targetDate, elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;
    function tick() {
        const dist = targetDate - Date.now();
        if (dist < 0) { el.innerHTML = 'Event Started!'; return; }
        const d = Math.floor(dist / 86400000);
        const h = Math.floor((dist % 86400000) / 3600000);
        const m = Math.floor((dist % 3600000) / 60000);
        const s = Math.floor((dist % 60000) / 1000);
        el.innerHTML = `${d}d ${h}h ${m}m ${s}s`;
    }
    tick();
    setInterval(tick, 1000);
}

// ===== SKELETON LOADING =====
function showSkeleton(containerId) {
    const container = document.getElementById(containerId);
    if (!container) return;
    container.innerHTML = `
        <div class="skeleton-card">
            <div class="skeleton skeleton-image" style="height:200px;border-radius:20px;"></div>
            <div class="skeleton skeleton-text" style="height:20px;width:80%;margin:1rem;"></div>
            <div class="skeleton skeleton-text" style="height:15px;width:60%;margin:0 1rem 1rem;"></div>
        </div>
    `.repeat(6);
}

// ===== SMOOTH SCROLL FOR ANCHOR LINKS =====
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        const href = this.getAttribute('href');
        if (href !== '#') {
            e.preventDefault();
            const target = document.querySelector(href);
            if (target) target.scrollIntoView({ behavior: 'smooth' });
        }
    });
});

// ===== NEW BADGE FLASHING =====
document.querySelectorAll('.new-badge').forEach(badge => {
    setInterval(() => {
        badge.style.opacity = badge.style.opacity === '0.5' ? '1' : '0.5';
    }, 1000);
});

// Public API
window.TasteCebu = { showToast, addToCravings, triedDish, startCountdown };
