// Layout JavaScript for IIN Swalayan

// Wait for DOM to be ready
document.addEventListener('DOMContentLoaded', function () {
    initializeLayout();
});

// Initialize layout functionality
function initializeLayout() {
    loadCartCount();
    initializeSearchEnhancements();
    initializeScrollEffects();
}

// Load cart count for authenticated users
function loadCartCount() {
    // This will be populated by the server-side rendering
    // Check if user is authenticated from server-side data
    const cartLink = document.querySelector('.cart-link');
    if (cartLink) {
        // Make AJAX call to get cart count
        fetch('/Cart/GetCartCount')
            .then(response => response.json())
            .then(data => {
                updateCartCount(data.cartCount || 0);
            })
            .catch(error => {
                console.error('Error loading cart count:', error);
            });
    }
}

// Update cart count globally
function updateCartCount(count) {
    const cartBadge = document.querySelector('.cart-badge');
    if (cartBadge) {
        cartBadge.textContent = count;
        // Add animation effect
        cartBadge.style.transform = 'scale(1.2)';
        setTimeout(() => {
            cartBadge.style.transform = 'scale(1)';
        }, 200);
    }
}

// Search functionality enhancement
function initializeSearchEnhancements() {
    const searchInput = document.querySelector('.search-input');
    const searchBtn = document.querySelector('.search-btn');

    if (searchInput) {
        // Focus effects
        searchInput.addEventListener('focus', function () {
            this.style.transform = 'scale(1.02)';
            this.parentElement.style.boxShadow = '0 5px 15px rgba(231, 76, 60, 0.2)';
        });

        searchInput.addEventListener('blur', function () {
            this.style.transform = 'scale(1)';
            this.parentElement.style.boxShadow = 'none';
        });

        // Enter key support
        searchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                if (searchBtn) {
                    searchBtn.click();
                }
            }
        });
    }
}

// Initialize scroll effects
function initializeScrollEffects() {
    let lastScrollTop = 0;
    const header = document.querySelector('.main-header');

    if (header) {
        window.addEventListener('scroll', function () {
            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;

            // Add shadow on scroll
            if (scrollTop > 10) {
                header.style.boxShadow = '0 4px 20px rgba(0,0,0,0.15)';
            } else {
                header.style.boxShadow = '0 2px 10px rgba(0,0,0,0.1)';
            }

            lastScrollTop = scrollTop;
        });
    }
}

// Global alert function
function showAlert(type, message, duration = 5000) {
    // Remove existing alerts
    const existingAlerts = document.querySelectorAll('.global-alert');
    existingAlerts.forEach(alert => alert.remove());

    const alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show position-fixed global-alert" 
             style="top: 20px; right: 20px; z-index: 9999; min-width: 300px; max-width: 400px;" role="alert">
            <i class="fas fa-${type === 'success' ? 'check-circle' : type === 'warning' ? 'exclamation-triangle' : 'exclamation-circle'}"></i> 
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;

    document.body.insertAdjacentHTML('beforeend', alertHtml);

    // Auto-hide after specified duration
    setTimeout(() => {
        const alert = document.querySelector('.global-alert');
        if (alert) {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }
    }, duration);
}

// Add to cart function (global)
function addToCart(productId, quantity = 1) {
    // Get anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    const formData = new FormData();
    formData.append('productId', productId);
    formData.append('quantity', quantity);
    if (token) {
        formData.append('__RequestVerificationToken', token);
    }

    fetch('/Cart/AddToCart', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateCartCount(data.cartCount);
                showAlert('success', data.message);
            } else {
                showAlert('danger', data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showAlert('danger', 'Terjadi kesalahan saat menambahkan produk ke keranjang.');
        });
}

// Smooth scroll for anchor links
function initializeSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
}

// Initialize smooth scroll
initializeSmoothScroll();

// Loading state helper
function setLoadingState(element, isLoading, originalText = null) {
    if (isLoading) {
        element.dataset.originalText = originalText || element.textContent;
        element.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Memproses...';
        element.disabled = true;
    } else {
        element.innerHTML = element.dataset.originalText || originalText;
        element.disabled = false;
    }
}

// Form validation helper
function validateForm(formElement) {
    const requiredFields = formElement.querySelectorAll('[required]');
    let isValid = true;

    requiredFields.forEach(field => {
        if (!field.value.trim()) {
            field.classList.add('is-invalid');
            isValid = false;
        } else {
            field.classList.remove('is-invalid');
        }
    });

    return isValid;
}

// Format currency (Indonesian Rupiah)
function formatCurrency(amount) {
    return new Intl.NumberFormat('id-ID', {
        style: 'currency',
        currency: 'IDR',
        minimumFractionDigits: 0
    }).format(amount);
}

// Format number with thousand separators
function formatNumber(number) {
    return new Intl.NumberFormat('id-ID').format(number);
}

// Debounce function for search
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Initialize live search if search input exists
function initializeLiveSearch() {
    const searchInput = document.querySelector('.search-input');
    if (searchInput) {
        const debouncedSearch = debounce(function (searchTerm) {
            if (searchTerm.length >= 3) {
                // Perform live search
                performLiveSearch(searchTerm);
            }
        }, 300);

        searchInput.addEventListener('input', function () {
            debouncedSearch(this.value);
        });
    }
}

// Perform live search (you can customize this)
function performLiveSearch(searchTerm) {
    // This is a placeholder for live search functionality
    console.log('Searching for:', searchTerm);
    // You can implement AJAX search results here
}

// Handle responsive menu toggle
function initializeResponsiveMenu() {
    const navbarToggler = document.querySelector('.navbar-toggler');
    const navbarCollapse = document.querySelector('.navbar-collapse');

    if (navbarToggler && navbarCollapse) {
        navbarToggler.addEventListener('click', function () {
            navbarCollapse.classList.toggle('show');
        });

        // Close menu when clicking outside
        document.addEventListener('click', function (e) {
            if (!navbarToggler.contains(e.target) && !navbarCollapse.contains(e.target)) {
                navbarCollapse.classList.remove('show');
            }
        });
    }
}

// Initialize all responsive features
function initializeResponsiveFeatures() {
    initializeResponsiveMenu();
    initializeLiveSearch();
}

// Call responsive features
initializeResponsiveFeatures();

// Utility function to get anti-forgery token
function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
}

// Generic AJAX helper
function makeAjaxRequest(url, method = 'GET', data = null, includeToken = true) {
    const options = {
        method: method,
        headers: {
            'Content-Type': 'application/json',
        }
    };

    if (includeToken && (method === 'POST' || method === 'PUT' || method === 'DELETE')) {
        const token = getAntiForgeryToken();
        if (token) {
            options.headers['RequestVerificationToken'] = token;
        }
    }

    if (data) {
        if (data instanceof FormData) {
            delete options.headers['Content-Type']; // Let browser set it for FormData
            options.body = data;
        } else {
            options.body = JSON.stringify(data);
        }
    }

    return fetch(url, options)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        });
}

// Initialize tooltips if Bootstrap is available
function initializeTooltips() {
    if (typeof bootstrap !== 'undefined') {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }
}

// Initialize popovers if Bootstrap is available
function initializePopovers() {
    if (typeof bootstrap !== 'undefined') {
        const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        popoverTriggerList.map(function (popoverTriggerEl) {
            return new bootstrap.Popover(popoverTriggerEl);
        });
    }
}

// Image lazy loading
function initializeLazyLoading() {
    const images = document.querySelectorAll('img[data-src]');

    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src;
                    img.classList.remove('lazy');
                    imageObserver.unobserve(img);
                }
            });
        });

        images.forEach(img => imageObserver.observe(img));
    } else {
        // Fallback for older browsers
        images.forEach(img => {
            img.src = img.dataset.src;
            img.classList.remove('lazy');
        });
    }
}

// Initialize all Bootstrap components
function initializeBootstrapComponents() {
    initializeTooltips();
    initializePopovers();
}

// Call Bootstrap initialization after DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    initializeBootstrapComponents();
    initializeLazyLoading();
});

// Handle form submissions with loading states
function handleFormSubmission(formElement, onSuccess, onError) {
    const submitButton = formElement.querySelector('button[type="submit"]');

    formElement.addEventListener('submit', function (e) {
        e.preventDefault();

        if (!validateForm(formElement)) {
            showAlert('warning', 'Mohon lengkapi semua field yang diperlukan.');
            return;
        }

        if (submitButton) {
            setLoadingState(submitButton, true);
        }

        const formData = new FormData(formElement);

        fetch(formElement.action || window.location.href, {
            method: formElement.method || 'POST',
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                if (submitButton) {
                    setLoadingState(submitButton, false);
                }

                if (data.success) {
                    if (onSuccess) onSuccess(data);
                } else {
                    if (onError) onError(data);
                    showAlert('danger', data.message || 'Terjadi kesalahan.');
                }
            })
            .catch(error => {
                if (submitButton) {
                    setLoadingState(submitButton, false);
                }

                console.error('Error:', error);
                if (onError) onError(error);
                showAlert('danger', 'Terjadi kesalahan saat memproses permintaan.');
            });
    });
}

// Export functions for global use
window.IINSwalayan = {
    updateCartCount,
    showAlert,
    addToCart,
    setLoadingState,
    validateForm,
    formatCurrency,
    formatNumber,
    makeAjaxRequest,
    handleFormSubmission
};