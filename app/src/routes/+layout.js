import popups from "$lib/dom/popups.js"

// Check for each page is still connected
export const load = async () => {
    popups.init();
};

export const prerender = false;
export const ssr = false;