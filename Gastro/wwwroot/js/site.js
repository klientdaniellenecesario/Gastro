// TASTECEBU - Main JavaScript

// Initialize AOS
AOS.init({
    duration: 800,
    once: true,
    offset: 100
});

// ===== NAVBAR SCROLL EFFECT =====
const navbar = document.getElementById('navbar');
window.addEventListener('scroll', () => {
    if (window.scrollY > 100) {
        navbar.classList.add('scrolled');
    } else {
        navbar.classList.remove('scrolled');
    }
});

// ===== BACK TO TOP =====
const backToTop = document.getElementById('backToTop');
window.addEventListener('scroll', () => {
    if (window.scrollY > 500) {
        backToTop.classList.add('visible');
    } else {
        backToTop.classList.remove('visible');
    }
});

backToTop.addEventListener('click', () => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
});

// ===== MOBILE DRAWER =====
const mobileMenuBtn = document.getElementById('mobileMenuBtn');
const mobileDrawer = document.getElementById('mobileDrawer');
const drawerOverlay = document.getElementById('drawerOverlay');
const closeDrawer = document.getElementById('closeDrawer');

function openDrawer() {
    mobileDrawer.classList.add('open');
    drawerOverlay.classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeDrawerFunc() {
    mobileDrawer.classList.remove('open');
    drawerOverlay.classList.remove('open');
    document.body.style.overflow = '';
}

if (mobileMenuBtn) mobileMenuBtn.addEventListener('click', openDrawer);
if (closeDrawer) closeDrawer.addEventListener('click', closeDrawerFunc);
if (drawerOverlay) drawerOverlay.addEventListener('click', closeDrawerFunc);

// ===== DARK MODE TOGGLE =====
const themeToggle = document.getElementById('themeToggle');
const htmlElement = document.documentElement;

const savedTheme = localStorage.getItem('theme');
if (savedTheme) {
    htmlElement.setAttribute('data-theme', savedTheme);
    updateThemeIcon(savedTheme);
}

themeToggle.addEventListener('click', () => {
    const currentTheme = htmlElement.getAttribute('data-theme');
    const newTheme = currentTheme === 'light' ? 'dark' : 'light';
    htmlElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme);
    updateThemeIcon(newTheme);

    // Show toast notification
    showToast(`${newTheme === 'dark' ? '🌙' : '☀️'} ${newTheme === 'dark' ? 'Dark' : 'Light'} mode activated`);
});

function updateThemeIcon(theme) {
    const icon = themeToggle.querySelector('i');
    if (theme === 'light') {
        icon.className = 'fas fa-sun';
    } else {
        icon.className = 'fas fa-moon';
    }
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
window.filterByVibe = function (vibe) {
    const cards = document.querySelectorAll('.restaurant-card');
    const buttons = document.querySelectorAll('.vibe-btn');

    buttons.forEach(btn => btn.classList.remove('active'));
    event.target.closest('.vibe-btn').classList.add('active');

    cards.forEach(card => {
        const cardVibe = card.dataset.vibe;
        if (vibe === 'all' || cardVibe === vibe) {
            card.style.display = 'block';
            card.style.animation = 'fadeIn 0.3s ease';
        } else {
            card.style.display = 'none';
        }
    });

    showToast(`Showing ${vibe} places 🍽️`);
};

// ===== I TRIED THIS BUTTON =====
window.triedDish = function (dishName, dishId) {
    Swal.fire({
        title: '🍜 Yum!',
        text: `You tried ${dishName}! Added to your food journey.`,
        icon: 'success',
        background: getComputedStyle(document.documentElement).getPropertyValue('--bg-card'),
        color: getComputedStyle(document.documentElement).getPropertyValue('--text-primary'),
        confirmButtonColor: '#E76F51',
        timer: 2000,
        showConfirmButton: false
    });

    // Update local storage or API call would go here
    let triedDishes = JSON.parse(localStorage.getItem('triedDishes') || '[]');
    if (!triedDishes.includes(dishId)) {
        triedDishes.push(dishId);
        localStorage.setItem('triedDishes', JSON.stringify(triedDishes));
    }
};

// ===== ADD TO CRAVINGS LIST =====
window.addToCravings = function (itemName, itemId, type) {
    let cravings = JSON.parse(localStorage.getItem('cravings') || '[]');
    if (!cravings.some(c => c.id === itemId)) {
        cravings.push({ id: itemId, name: itemName, type: type, date: new Date().toISOString() });
        localStorage.setItem('cravings', JSON.stringify(cravings));
        showToast(`✨ ${itemName} added to your Cravings List!`);
    } else {
        showToast(`⚠️ ${itemName} is already in your Cravings List`);
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
    document.getElementById('ratingValue').value = rating;
};

// ===== LOAD MORE ANIMATION =====
window.loadMore = function (button) {
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Loading...';
    setTimeout(() => {
        button.innerHTML = 'Load More →';
        showToast('More delicious content coming soon!');
    }, 1500);
};

// ===== COUNTDOWN TIMER =====
function startCountdown(targetDate, elementId) {
    const countdownElement = document.getElementById(elementId);
    if (!countdownElement) return;

    function updateCountdown() {
        const now = new Date().getTime();
        const distance = targetDate - now;

        if (distance < 0) {
            countdownElement.innerHTML = "Event Started!";
            return;
        }

        const days = Math.floor(distance / (1000 * 60 * 60 * 24));
        const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((distance % (1000 * 60)) / 1000);

        countdownElement.innerHTML = `${days}d ${hours}h ${minutes}m ${seconds}s`;
    }

    updateCountdown();
    setInterval(updateCountdown, 1000);
}

// ===== SKELETON LOADING =====
function showSkeleton(containerId) {
    const container = document.getElementById(containerId);
    if (!container) return;

    const skeletonHTML = `
        <div class="skeleton-card">
            <div class="skeleton skeleton-image" style="height: 200px; border-radius: 20px;"></div>
            <div class="skeleton skeleton-text" style="height: 20px; width: 80%; margin: 1rem;"></div>
            <div class="skeleton skeleton-text" style="height: 15px; width: 60%; margin: 0 1rem 1rem;"></div>
        </div>
    `.repeat(6);

    container.innerHTML = skeletonHTML;
}

// ===== SMOOTH SCROLL FOR ANCHOR LINKS =====
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        const href = this.getAttribute('href');
        if (href !== "#") {
            e.preventDefault();
            const target = document.querySelector(href);
            if (target) {
                target.scrollIntoView({ behavior: 'smooth' });
            }
        }
    });
});

// ===== NEW BADGE FLASHING =====
document.querySelectorAll('.new-badge').forEach(badge => {
    setInterval(() => {
        badge.style.opacity = badge.style.opacity === '0.5' ? '1' : '0.5';
    }, 1000);
});

// Export functions for use in console (debugging)
window.TasteCebu = {
    showToast,
    addToCravings,
    triedDish,
    startCountdown
};