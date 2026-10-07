const menuButton = document.querySelector("[data-menu-toggle]");
const sidebar = document.querySelector(".sidebar");
const scrim = document.querySelector("[data-menu-scrim]");

function setMenuOpen(isOpen) {
	sidebar?.classList.toggle("open", isOpen);
	scrim?.classList.toggle("visible", isOpen);
	menuButton?.setAttribute("aria-expanded", String(isOpen));
	menuButton?.setAttribute("aria-label", isOpen ? "Close navigation" : "Open navigation");
}

menuButton?.addEventListener("click", () => {
	setMenuOpen(sidebar?.classList.contains("open") !== true);
});

scrim?.addEventListener("click", () => setMenuOpen(false));

// Write your JavaScript code.
