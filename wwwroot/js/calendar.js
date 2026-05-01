function openCreateModal() {
    const modal = document.getElementById("createModal");
    if (!modal) return;

    modal.classList.remove("hidden");
    modal.classList.add("flex");
}

function closeCreateModal() {
    const modal = document.getElementById("createModal");
    if (!modal) return;

    modal.classList.add("hidden");
    modal.classList.remove("flex");
}