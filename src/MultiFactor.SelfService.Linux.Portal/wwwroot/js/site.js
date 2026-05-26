// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Password visibility toggle for all password fields.
(function () {
    const eyeOpenIcon = '/images/password-eye-open.svg';
    const eyeClosedIcon = '/images/password-eye-closed.svg';

    function updateToggleState(input, button, icon) {
        const isPasswordVisible = input.type === 'text';
        icon.src = isPasswordVisible ? eyeClosedIcon : eyeOpenIcon;
        button.setAttribute('aria-pressed', isPasswordVisible ? 'true' : 'false');
        button.setAttribute('aria-label', isPasswordVisible ? 'Hide password' : 'Show password');
    }

    function initializePasswordToggles() {
        const passwordInputs = document.querySelectorAll('input[type="password"]');

        passwordInputs.forEach(function (input) {
            if (input.dataset.passwordToggleInitialized === "true") {
                return;
            }

            const host = input.closest('.input') || input.parentElement;
            if (!host) {
                return;
            }

            host.classList.add('password-toggle-host');
            input.classList.add('password-toggle-input');

            if (host.querySelector('.password-toggle-button')) {
                input.dataset.passwordToggleInitialized = "true";
                return;
            }

            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'password-toggle-button';
            button.setAttribute('aria-pressed', 'false');
            button.setAttribute('aria-label', 'Show password');

            const icon = document.createElement('img');
            icon.alt = '';
            button.appendChild(icon);

            updateToggleState(input, button, icon);

            button.addEventListener('click', function () {
                const showPassword = input.type === 'password';
                input.type = showPassword ? 'text' : 'password';
                updateToggleState(input, button, icon);
            });

            host.appendChild(button);
            input.dataset.passwordToggleInitialized = "true";
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializePasswordToggles);
    } else {
        initializePasswordToggles();
    }
})();
