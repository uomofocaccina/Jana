import { transformDateToUnix, writeLogsInPage } from "./utility.js";
import { getDbSchema } from "./dbschema.js";

const API_BASE_URL = import.meta.env.API_BASE_URL;
console.log("API_BASE_URL:", API_BASE_URL);

// These will be set by main.js
const limitApi = window.limitApi || 10;
const jsstoreCon = window.jsstoreCon;
let draggedElement = window.draggedElement;
// Folders deferred across pages because their parent hadn't arrived yet
let pendingFolderSync = [];
// Notes deferred because their folder wasn't in the DOM yet
let pendingNoteSync = [];

function refreshDeleteIcon() {
    // Inline edit icons were removed: folder actions now live in the shared
    // context menu. Kept as a no-op so the existing drag/drop and sync call
    // sites don't need to change.
}

// ===== Shared folder context menu =====
// A single menu node reused for every folder (and the sidebar header).
let folderContextMenuEl = null;

function ensureFolderContextMenu() {
    if (folderContextMenuEl) return folderContextMenuEl;
    const menu = document.createElement("div");
    menu.id = "folderContextMenu";
    menu.className = "folder-context-menu";
    menu.style.display = "none";
    document.body.appendChild(menu);
    folderContextMenuEl = menu;
    return menu;
}

function closeContextMenu() {
    if (folderContextMenuEl) folderContextMenuEl.style.display = "none";
    document.removeEventListener("mousedown", onContextMenuOutside, true);
    document.removeEventListener("keydown", onContextMenuKey, true);
    window.removeEventListener("scroll", closeContextMenu, true);
    window.removeEventListener("resize", closeContextMenu, true);
}

function onContextMenuOutside(event) {
    if (folderContextMenuEl && !folderContextMenuEl.contains(event.target)) closeContextMenu();
}

function onContextMenuKey(event) {
    if (event.key === "Escape") closeContextMenu();
}

// items: array of { icon, label, onClick, danger?, disabled? }
// position: { x, y } in viewport coordinates
function openContextMenu(items, position) {
    const menu = ensureFolderContextMenu();
    menu.innerHTML = "";
    items.forEach((item) => {
        const btn = document.createElement("button");
        btn.type = "button";
        btn.className = "folder-context-item";
        if (item.danger) btn.classList.add("danger");
        if (item.disabled) btn.disabled = true;
        btn.innerHTML = `<i class="${item.icon}"></i><span>${item.label}</span>`;
        if (!item.disabled) {
            btn.addEventListener("click", async (e) => {
                e.stopPropagation();
                closeContextMenu();
                await item.onClick();
            });
        }
        menu.appendChild(btn);
    });

    // Show off-screen first to measure, then clamp inside the viewport.
    menu.style.visibility = "hidden";
    menu.style.display = "block";
    menu.style.left = "0px";
    menu.style.top = "0px";
    const rect = menu.getBoundingClientRect();
    let x = position.x;
    let y = position.y;
    if (x + rect.width > window.innerWidth - 8) x = Math.max(8, window.innerWidth - rect.width - 8);
    if (y + rect.height > window.innerHeight - 8) y = Math.max(8, window.innerHeight - rect.height - 8);
    menu.style.left = `${x}px`;
    menu.style.top = `${y}px`;
    menu.style.visibility = "visible";

    // Defer listener attachment so the opening click doesn't immediately close it.
    setTimeout(() => {
        document.addEventListener("mousedown", onContextMenuOutside, true);
        document.addEventListener("keydown", onContextMenuKey, true);
        window.addEventListener("scroll", closeContextMenu, true);
        window.addEventListener("resize", closeContextMenu, true);
    }, 0);
}

async function boot() {
    registerEvents();
    initDb();
    //await riempiDatiDir();
    //await messageToServiceWorker("syncDataFolder", {limit: limitApi});
    //await messageToServiceWorker("syncDataNote", {limit: limitApi});
    writeLog("🚀");
    await messageToServiceWorker("syncDataNoteAndFolder", { limit: limitApi });
    //await riempiDatiNote();
    await writeDirAndTitle();
}

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", () => boot());
} else {
    boot();
}

// Polyfill for crypto.randomUUID() if unavailable (e.g. on HTTP)
function uuidv4() {
    // https://stackoverflow.com/a/2117523/2715716
    return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
        var r = (Math.random() * 16) | 0,
            v = c === "x" ? r : (r & 0x3) | 0x8;
        return v.toString(16);
    });
}
if (typeof crypto.randomUUID !== "function") {
    crypto.randomUUID = uuidv4;
}

function isScreenLittle() {
    // Returns true if screen width is less than 768px
    return window.innerWidth < 768;
}

async function writeDirAndTitle() {
    await writeDirTree();
    await countNoteForFolder();
    await writeTitleForDir();
}

// Folder IDs currently expanded in the tree
let directoryElementOpen = [];
async function writeDirTree() {
    var dirTable = await jsstoreCon.select({
        from: "Folder",
        where: {
            active: 1,
        },
    });

    const listaDiv = document.getElementById("lista");
    const expandableList = createDirectoryExpandableList(dirTable);
    if (expandableList) {
        listaDiv.appendChild(expandableList);
    }
    if (directoryElementOpen.length > 0) {
        reopenDirectoryElements();
    }
}

function createDirectoryExpandableList(data) {
    const parentMap = {};
    data.forEach((item) => {
        if (!parentMap[item.parent]) {
            parentMap[item.parent] = [];
        }
        parentMap[item.parent].push(item);
    });

    function createList(parentId) {
        if (!parentMap[parentId]) return null;
        const ul = document.createElement("ul");
        parentMap[parentId].forEach((item) => {
            ul.appendChild(addLi(item));
        });
        return ul;
    }

    function addLi(item) {
        const li = createFolderLi(item, !!parentMap[item.id], async (newDir) => {
            addSingleDirectoryToList(newDir, item);
            await messageToServiceWorker("syncAddFolder", newDir);
        });
        const childList = createList(item.id);
        if (childList) {
            childList.style.display = "none";
            li.appendChild(childList);
        }
        return li;
    }

    function addSingleDirectoryToList(newDir, item) {
        const liElement = document.querySelector(`li[data-id="${item.id}"]`);
        let ulElement = liElement.querySelector(":scope > ul");
        if (!ulElement) {
            ulElement = document.createElement("ul");
            liElement.appendChild(ulElement);
        }
        const liNuovo = createFolderLi(newDir, false, async (d) => {
            addSingleDirectoryToList(d, newDir);
            await messageToServiceWorker("syncAddFolder", d);
        });
        // New folders are inserted at the top of the list (above notes and any existing sub-folders)
        ulElement.prepend(liNuovo);
        const chevronEl = liElement.querySelector(".folder-chevron");
        const rowDivEl = liElement.querySelector(".folder-row");
        if (chevronEl) {
            chevronEl.style.visibility = "visible";
            chevronEl.classList.add("open");
        }
        if (rowDivEl) rowDivEl.classList.add("open");
        ulElement.style.display = "block";
        if (!directoryElementOpen.includes(item.id)) directoryElementOpen.push(item.id);
    }

    return createList(null);
}

// Expand/collapse a folder's children. Shared by the chevron and the folder name.
function toggleFolder(li, expandButton, rowDiv, item) {
    const childUl = li.querySelector(":scope > ul");
    if (!childUl) return;
    if (childUl.style.display === "none") {
        childUl.style.display = "block";
        expandButton.classList.add("open");
        rowDiv.classList.add("open");
        if (!directoryElementOpen.includes(item.id)) directoryElementOpen.push(item.id);
    } else {
        childUl.style.display = "none";
        expandButton.classList.remove("open");
        rowDiv.classList.remove("open");
        const index = directoryElementOpen.indexOf(item.id);
        if (index > -1) directoryElementOpen.splice(index, 1);
    }
}

// Open the note editor in "add" mode, pre-filled for the given folder.
async function newNoteInFolder(item) {
    document.getElementById("notaid").value = "";
    document.getElementById("notatitle").value = "";
    document.getElementById("notatext").innerText = "";
    document.getElementById("notadirid").value = item.id;
    document.getElementById("notadirectory").textContent = await getDirCompletePathFromId(item.id);
    showNoteSpace("add");
}

// Build the four context-menu actions for a folder. Reuses the existing
// addFolder / editFolder / deleteDirectory operations.
function folderMenuItems(item, onAdd) {
    const li = document.querySelector(`li[data-id="${item.id}"]`);
    const hasSubfolders = !!(li && li.querySelector(":scope > ul > li[data-id]"));
    return [
        {
            icon: "fa-solid fa-file-circle-plus",
            label: "New note",
            onClick: async () => {
                await newNoteInFolder(item);
            },
        },
        {
            icon: "fa-solid fa-folder-plus",
            label: "New folder",
            onClick: async () => {
                const newDir = await addFolder(item);
                if (newDir) await onAdd(newDir);
            },
        },
        {
            icon: "fa-solid fa-pen",
            label: "Edit folder",
            onClick: async () => {
                const newName = prompt("Enter new name:", item.text);
                if (newName && newName.trim() !== "") {
                    item.text = newName.trim();
                    const nameSpanEl = document.querySelector(`span[dirItemName="${item.id}"]`);
                    if (nameSpanEl) nameSpanEl.textContent = newName.trim();
                    await editFolder(item, newName.trim());
                }
            },
        },
        {
            icon: "fa-solid fa-trash",
            label: "Delete folder",
            danger: true,
            // Keep the existing safeguard: a folder with sub-folders can't be deleted
            // (deleteDirectory does not cascade to sub-folders).
            disabled: hasSubfolders,
            onClick: async () => {
                const sure = confirm("Are you sure you want to delete this folder and all its notes? " + item.text);
                if (!sure) return;
                await deleteDirectory(item.id);
            },
        },
    ];
}

function createFolderLi(item, hasChildren, onAdd) {
    const li = document.createElement("li");
    li.setAttribute("data-id", item.id);
    li.setAttribute("draggable", "true");
    li.addEventListener("dragstart", handleDragStart);
    li.addEventListener("dragover", handleDragOver);
    li.addEventListener("drop", handleDrop);
    li.addEventListener("touchstart", handleTouchStart, { passive: false });
    li.addEventListener("touchmove", handleTouchMove, { passive: false });
    li.addEventListener("touchend", handleTouchEnd, { passive: false });
    li.addEventListener("touchcancel", handleTouchCancel);

    const rowDiv = document.createElement("div");
    rowDiv.classList.add("folder-row");

    const expandButton = document.createElement("span");
    expandButton.innerHTML = '<i class="fa-solid fa-chevron-right"></i>';
    expandButton.classList.add("folder-chevron");
    if (!hasChildren) expandButton.style.visibility = "hidden";
    expandButton.onclick = function () {
        toggleFolder(li, expandButton, rowDiv, item);
    };

    const nameSpan = document.createElement("span");
    nameSpan.textContent = item.text;
    nameSpan.classList.add("folder-name");
    nameSpan.style.cursor = "pointer";
    nameSpan.setAttribute("dirItemName", item.id);
    nameSpan.onclick = function () {
        toggleFolder(li, expandButton, rowDiv, item);
    };

    // Kebab icon: opens the folder context menu just below/right of the icon.
    const menuBtn = document.createElement("span");
    menuBtn.className = "folder-menu-btn";
    menuBtn.innerHTML = '<i class="fa-solid fa-ellipsis-vertical"></i>';
    menuBtn.setAttribute("title", "Folder actions");
    menuBtn.onclick = function (e) {
        e.stopPropagation();
        const r = menuBtn.getBoundingClientRect();
        openContextMenu(folderMenuItems(item, onAdd), { x: r.left, y: r.bottom + 2 });
    };

    // Right-click anywhere on the row opens the same menu at the cursor.
    rowDiv.addEventListener("contextmenu", function (e) {
        e.preventDefault();
        openContextMenu(folderMenuItems(item, onAdd), { x: e.clientX, y: e.clientY });
    });

    const countSpan = document.createElement("span");
    countSpan.textContent = " (0)";
    countSpan.style.fontSize = "0.8em";
    countSpan.style.color = "gray";
    countSpan.setAttribute("data-count-id", item.id);
    countSpan.classList.add("folder-count");

    rowDiv.appendChild(expandButton);
    rowDiv.appendChild(nameSpan);
    rowDiv.appendChild(countSpan);
    rowDiv.appendChild(menuBtn);
    li.appendChild(rowDiv);

    return li;
}

function makeOnAdd(parentId) {
    return async (newDir) => {
        const parentLi = document.querySelector(`li[data-id="${parentId}"]`);
        if (!parentLi) return;
        let ul = parentLi.querySelector(":scope > ul");
        if (!ul) {
            ul = document.createElement("ul");
            parentLi.appendChild(ul);
        }
        // New folders are inserted at the top of the list
        const newLi = createFolderLi(newDir, false, makeOnAdd(newDir.id));
        ul.prepend(newLi);
        // Expand the parent so the new folder is visible immediately
        ul.style.display = "block";
        const chevron = parentLi.querySelector(".folder-chevron");
        const rowDiv = parentLi.querySelector(".folder-row");
        if (chevron) {
            chevron.style.visibility = "visible";
            chevron.classList.add("open");
        }
        if (rowDiv) rowDiv.classList.add("open");
        if (!directoryElementOpen.includes(parentId)) directoryElementOpen.push(parentId);
        refreshDeleteIcon(parentLi);
        await messageToServiceWorker("syncAddFolder", newDir);
    };
}

async function applyFolderBatch(folders) {
    // If the folder list isn't mounted (e.g. user is on the login screen),
    // skip DOM work — data is already persisted in IndexedDB and will render on next mount.
    if (!document.getElementById("lista")) return;
    // New folders whose parent isn't in the DOM yet are deferred and retried.
    // This handles the case where the server returns a child before its parent
    // (e.g. an old folder moved under a newly created parent).
    const deferred = [];
    // Include folders deferred from previous pages so they get a chance to
    // find their parent now that more folders may have arrived.
    const toProcess = [...pendingFolderSync, ...folders];
    pendingFolderSync = [];

    const processFolder = (folder) => {
        if (folder.active === 0) {
            const li = document.querySelector(`li[data-id="${folder.id}"]`);
            if (li) {
                const oldParentLi = li.parentElement?.closest("li[data-id]");
                const oldParentUl = li.parentElement;
                li.remove();
                // Hide chevron on the parent if it no longer has any children
                if (oldParentLi && oldParentUl && oldParentUl.children.length === 0) {
                    const chevron = oldParentLi.querySelector(".folder-chevron");
                    if (chevron) chevron.style.visibility = "hidden";
                }
                refreshDeleteIcon(oldParentLi);
            }
            return true; // consumed
        }
        const existing = document.querySelector(`li[data-id="${folder.id}"]`);
        if (existing) {
            const nameSpan = existing.querySelector(`span[dirItemName="${folder.id}"]`);
            if (nameSpan) nameSpan.textContent = folder.text;
            // Check if the folder was moved to a different parent
            const currentParentLi = existing.parentElement?.closest("li[data-id]");
            const currentParentId = currentParentLi?.getAttribute("data-id") ?? null;
            const newParentId = folder.parent ? String(folder.parent) : null;
            if (currentParentId !== newParentId) {
                if (!newParentId) {
                    // Moved to root
                    let rootUl = document.querySelector("#lista > ul");
                    if (!rootUl) {
                        rootUl = document.createElement("ul");
                        document.getElementById("lista").appendChild(rootUl);
                    }
                    rootUl.prepend(existing);
                } else {
                    // Moved under a new parent folder
                    const newParentLi = document.querySelector(`li[data-id="${newParentId}"]`);
                    if (!newParentLi) return false; // new parent not yet in DOM — defer
                    let targetUl = newParentLi.querySelector(":scope > ul");
                    if (!targetUl) {
                        targetUl = document.createElement("ul");
                        targetUl.style.display = "none";
                        newParentLi.appendChild(targetUl);
                    }
                    targetUl.prepend(existing);
                    const targetChevron = newParentLi.querySelector(".folder-chevron");
                    if (targetChevron) targetChevron.style.visibility = "visible";
                    refreshDeleteIcon(newParentLi);
                }
                // Hide chevron on the old parent if it has no children left
                if (currentParentLi) {
                    const sourceUl = currentParentLi.querySelector(":scope > ul");
                    if (sourceUl && sourceUl.children.length === 0) {
                        const sourceChevron = currentParentLi.querySelector(".folder-chevron");
                        if (sourceChevron) sourceChevron.style.visibility = "hidden";
                    }
                    refreshDeleteIcon(currentParentLi);
                }
            }
        } else {
            const newLi = createFolderLi(folder, false, makeOnAdd(folder.id));
            if (!folder.parent) {
                let rootUl = document.querySelector("#lista > ul");
                if (!rootUl) {
                    rootUl = document.createElement("ul");
                    document.getElementById("lista").appendChild(rootUl);
                }
                rootUl.appendChild(newLi);
            } else {
                const parentLi = document.querySelector(`li[data-id="${folder.parent}"]`);
                if (!parentLi) return false; // parent not yet in DOM — defer
                let ul = parentLi.querySelector(":scope > ul");
                if (!ul) {
                    ul = document.createElement("ul");
                    ul.style.display = "none";
                    parentLi.appendChild(ul);
                }
                const chevron = parentLi.querySelector(".folder-chevron");
                if (chevron) chevron.style.visibility = "visible";
                ul.appendChild(newLi);
                refreshDeleteIcon(parentLi);
            }
        }
        return true; // consumed
    };

    // First pass
    for (const folder of toProcess) {
        if (!processFolder(folder)) deferred.push(folder);
    }

    // Retry deferred folders until no progress is made (handles arbitrary nesting depth)
    let progress = true;
    while (deferred.length > 0 && progress) {
        progress = false;
        const remaining = [...deferred];
        deferred.length = 0;
        for (const folder of remaining) {
            if (processFolder(folder)) {
                progress = true;
            } else {
                deferred.push(folder);
            }
        }
    }

    // Any folders still unresolved will be retried when the next page arrives
    pendingFolderSync = deferred;
    // Folders just landed in the DOM may unblock notes that were deferred.
    if (pendingNoteSync.length > 0) {
        await applyNoteBatch([]);
    }
}

async function applyNoteBatch(notes) {
    if (!document.getElementById("lista")) return;
    // Include notes deferred from previous pages so they get another chance
    // now that more folders may have arrived in the DOM.
    const toProcess = [...pendingNoteSync, ...notes];
    pendingNoteSync = [];
    const deferred = [];
    for (const note of toProcess) {
        if (note.active === 0) {
            const li = document.querySelector(`li[note-id="${note.id}"]`);
            if (li) {
                li.remove();
                incrementFolderCounterOfOne(note.folder, true);
            }
            continue;
        }
        const existing = document.querySelector(`li[note-id="${note.id}"]`);
        if (existing) {
            const titleSpan = existing.querySelector(".note-title");
            if (titleSpan) titleSpan.textContent = note.title;
            const rawText = (note.text || "").replace(/<[^>]+>/g, "").trim();
            let snippetSpan = existing.querySelector(".note-snippet");
            if (rawText) {
                if (!snippetSpan) {
                    snippetSpan = document.createElement("span");
                    snippetSpan.className = "note-snippet";
                    existing.appendChild(snippetSpan);
                }
                snippetSpan.textContent = rawText.slice(0, 60) + (rawText.length > 60 ? "…" : "");
            } else if (snippetSpan) {
                snippetSpan.remove();
            }
            // Check if the note was moved to a different folder
            const currentFolderLi = existing.closest("li[data-id]");
            const currentFolderId = currentFolderLi?.getAttribute("data-id");
            if (currentFolderId && currentFolderId !== String(note.folder)) {
                const targetFolderLi = document.querySelector(`li[data-id="${note.folder}"]`);
                if (targetFolderLi) {
                    let targetUl = targetFolderLi.querySelector(":scope > ul");
                    if (!targetUl) {
                        targetUl = document.createElement("ul");
                        targetUl.classList.add("titoli");
                        targetUl.style.display = "none";
                        targetFolderLi.appendChild(targetUl);
                    }
                    targetUl.appendChild(existing);
                    // Show chevron on the target folder
                    const targetChevron = targetFolderLi.querySelector(".folder-chevron");
                    if (targetChevron) targetChevron.style.visibility = "visible";
                    // Update counters: decrement source, increment destination
                    incrementFolderCounterOfOne(currentFolderId, true);
                    incrementFolderCounterOfOne(note.folder);
                    // Hide chevron on the source folder if it has no children left (notes or subfolders)
                    const sourceUl = currentFolderLi.querySelector(":scope > ul");
                    if (sourceUl && sourceUl.children.length === 0) {
                        const sourceChevron = currentFolderLi.querySelector(".folder-chevron");
                        if (sourceChevron) sourceChevron.style.visibility = "hidden";
                    }
                }
            }
        } else {
            const folderLi = document.querySelector(`li[data-id="${note.folder}"]`);
            if (!folderLi) {
                // Parent folder isn't in the DOM yet — defer to the next batch
                deferred.push(note);
                continue;
            }
            await writeSingleNote(note.folder, note.title, note.id, note.text);
            incrementFolderCounterOfOne(note.folder);
        }
    }
    pendingNoteSync = deferred;
}

async function addFolder(item) {
    const newName = prompt("Enter name");
    // If the user entered a name, create the folder
    if (newName && newName.trim() !== "") {
        let newDir = {
            id: crypto.randomUUID(),
            text: newName.trim(),
            parent: item.id,
            timestamp: transformDateToUnix(),
            active: 1,
        };
        // Save to local DB; sync to server happens via the service worker
        try {
            await jsstoreCon.insert({
                into: "Folder",
                values: [newDir],
            });
            //writeLogsInPage(`Name inserted: ID=${newDir.id}`);
        } catch (error) {
            writeLogsInPage("Error inserting folder:", error);
        }

        return newDir;
    } else {
        //writeLogsInPage("Edit cancelled or invalid name.");
        return;
    }
}

// Touch event handling for mobile devices
let touchStartY = 0;
let touchStartX = 0;
let isTouchDragging = false;
let lastTouchHighlightedElement = null;
let lastTouchHighlightedBg = "";
let longPressTimer = null;
let touchStartElement = null;

function handleTouchCancel() {
    clearTimeout(longPressTimer);
    longPressTimer = null;
    // Restore any highlighted element's background
    if (lastTouchHighlightedElement) {
        lastTouchHighlightedElement.style.backgroundColor = lastTouchHighlightedBg;
        lastTouchHighlightedElement = null;
        lastTouchHighlightedBg = "";
    }
    // Restore dragged element's opacity
    if (draggedElement) {
        draggedElement.style.opacity = "1";
    }
    draggedElement = null;
    touchStartElement = null;
    isTouchDragging = false;
}

function handleTouchStart(event) {
    const touch = event.touches[0];
    touchStartX = touch.clientX;
    touchStartY = touch.clientY;
    isTouchDragging = false;
    draggedElement = null;
    touchStartElement = null;
    clearTimeout(longPressTimer);
    longPressTimer = null;

    // Clear any leftover highlight from a previously cancelled drag
    if (lastTouchHighlightedElement) {
        lastTouchHighlightedElement.style.backgroundColor = lastTouchHighlightedBg;
        lastTouchHighlightedElement = null;
        lastTouchHighlightedBg = "";
    }

    const closestLi = event.target.closest("li");
    if (closestLi && (closestLi.getAttribute("data-id") || closestLi.getAttribute("note-id"))) {
        touchStartElement = closestLi;
            // Activate drag only after a 300ms long press;
        // quick movements are treated as normal scroll, not drag
        longPressTimer = setTimeout(() => {
            longPressTimer = null;
            draggedElement = touchStartElement;
            isTouchDragging = true;
            if (draggedElement) {
                draggedElement.style.opacity = "0.5";
            }
        }, 300);
    }
}

function handleTouchMove(event) {
    const touch = event.touches[0];
    const deltaX = Math.abs(touch.clientX - touchStartX);
    const deltaY = Math.abs(touch.clientY - touchStartY);

    // If the user moves before the long press timer fires, cancel drag and allow normal scroll
    if (longPressTimer && (deltaX > 10 || deltaY > 10)) {
        clearTimeout(longPressTimer);
        longPressTimer = null;
        touchStartElement = null;
        return;
    }

    if (!isTouchDragging) return;

    // Drag is active: block scroll and iOS pull-to-refresh
    // Block scroll and iOS pull-to-refresh during active drag
    event.preventDefault();

    // Find element under touch point
    const elementAtPoint = document.elementFromPoint(touch.clientX, touch.clientY);
    const targetLi = elementAtPoint ? elementAtPoint.closest("li[data-id]") : null;

    // Only update highlighting if target changed
    if (targetLi !== lastTouchHighlightedElement) {
        if (lastTouchHighlightedElement) {
            lastTouchHighlightedElement.style.backgroundColor = lastTouchHighlightedBg;
        }

        if (targetLi && targetLi !== draggedElement) {
            lastTouchHighlightedBg = targetLi.style.backgroundColor;
            targetLi.style.backgroundColor = "wheat";
            lastTouchHighlightedElement = targetLi;
        } else {
            lastTouchHighlightedElement = null;
            lastTouchHighlightedBg = "";
        }
    }
}

function handleTouchEnd(event) {
    clearTimeout(longPressTimer);
    longPressTimer = null;

    if (!draggedElement) {
        touchStartElement = null;
        return;
    }

    // Reset background color of highlighted element
    if (lastTouchHighlightedElement) {
        lastTouchHighlightedElement.style.backgroundColor = lastTouchHighlightedBg;
        lastTouchHighlightedElement = null;
        lastTouchHighlightedBg = "";
    }
    
    if (isTouchDragging) {
        event.preventDefault(); // Prevent click event
        
        const touch = event.changedTouches[0];
        const elementAtPoint = document.elementFromPoint(touch.clientX, touch.clientY);
        const targetLi = elementAtPoint ? elementAtPoint.closest("li[data-id]") : null;
        
        if (targetLi && targetLi !== draggedElement) {
            const draggedItemId = draggedElement.getAttribute("data-id");
            const draggedNoteId = draggedElement.getAttribute("note-id");
            
            if (draggedItemId) {
                // Folder being dragged
                const newParentId = targetLi.getAttribute("data-id");
                const ul = targetLi.querySelector("ul") || document.createElement("ul");
                targetLi.appendChild(ul);
                ul.appendChild(draggedElement);
                updateDirectoryPosition(draggedItemId, newParentId);
            } else if (draggedNoteId) {
                // Note being dragged
                const newParentIdDir = targetLi.getAttribute("data-id");
                const ul = targetLi.querySelector("ul") || document.createElement("ul");
                targetLi.appendChild(ul);
                ul.appendChild(draggedElement);
                updateNotePosition(draggedNoteId, newParentIdDir);
                countNoteForFolder();
            }
        }
    }
    
    // Restore opacity
    if (draggedElement) {
        draggedElement.style.opacity = "1";
    }
    draggedElement = null;
    touchStartElement = null;
    isTouchDragging = false;
}

function handleDragStart(event) {
    writeLogsInPage("Dir dragstart", event);
    draggedElement = event.target;
    //event.dataTransfer.setData("text/plain", event.target.getAttribute("data-id"));
    event.target.style.opacity = "0.5";
}

function handleDragOver(event) {
    event.preventDefault(); // Required to allow dropping
    const li = event.target.closest("li[data-id]");
    if (li) {
        document.querySelectorAll("li[data-id]").forEach((el) => {
            el.style.backgroundColor = "white";
        });
        li.style.backgroundColor = "wheat";
    }
}

function handleDrop(event) {
    event.preventDefault();
    document.querySelectorAll("li[data-id]").forEach((li) => {
        if (li.getAttribute("data-id")) {
            li.style.backgroundColor = "white";
        }
    });
    const draggedItemId = draggedElement.getAttribute("data-id");
    const draggedNoteId = draggedElement.getAttribute("note-id");

    if (draggedItemId) {
        //writeLogsInPage(`Folder dropped: ID=${draggedItemId}`);
        // Only folder li[data-id] are valid drop targets — dropping on a note
        // would corrupt the DOM/DB (folder nested inside a note).
        const newParent = event.target.closest("li[data-id]");
        const newParentId = newParent ? newParent.getAttribute("data-id") : null;

        // Move the dragged element into the new parent
        if (newParent && newParent !== draggedElement) {
            const oldParentLi = draggedElement.parentElement?.closest("li[data-id]");
            let ul = newParent.querySelector(":scope > ul");
            if (!ul) {
                ul = document.createElement("ul");
                ul.style.display = "none";
                newParent.appendChild(ul);
            }
            ul.appendChild(draggedElement);
            updateDirectoryPosition(draggedItemId, newParentId);

            // Expand the new parent so the dropped folder is visible
            ul.style.display = "block";
            const newChevron = newParent.querySelector(".folder-chevron");
            const newRowDiv = newParent.querySelector(".folder-row");
            if (newChevron) {
                newChevron.style.visibility = "visible";
                newChevron.classList.add("open");
            }
            if (newRowDiv) newRowDiv.classList.add("open");
            if (!directoryElementOpen.includes(newParentId)) directoryElementOpen.push(newParentId);
            refreshDeleteIcon(newParent);
            if (oldParentLi) {
                const oldUl = oldParentLi.querySelector(":scope > ul");
                if (oldUl && oldUl.children.length === 0) {
                    const oldChevron = oldParentLi.querySelector(".folder-chevron");
                    if (oldChevron) oldChevron.style.visibility = "hidden";
                }
                refreshDeleteIcon(oldParentLi);
            }
        }

        // Restore opacity
        draggedElement.style.opacity = "1";
    }
    if (draggedNoteId) {
        //writeLogsInPage("Note dropped ID:", draggedNoteId);

        const newParent = event.target.closest("li[data-id]");
        if (!newParent) {
            draggedElement.style.opacity = "1";
            return;
        }
        const newParentIdDir = newParent.getAttribute("data-id");
        //writeLogsInPage("Note dropped, new parent:", newParentIdDir);

        // Move the dragged note element into the new parent folder
        if (newParent && newParent !== draggedElement) {
            const ul = newParent.querySelector("ul") || document.createElement("ul");
            newParent.appendChild(ul);
            ul.appendChild(draggedElement);
        }

        // Restore opacity
        draggedElement.style.opacity = "1";
        updateNotePosition(draggedNoteId, newParentIdDir);

        // Ideally we'd recalculate only the source and destination folders,
        // but this requires resolving multi-action draggable conflicts first
        countNoteForFolder();
        //writeLogsInPage(`Note moved: ID=${draggedNoteId}, new parent: ID=${newParentIdDir}`);
    }
}
async function updateDirectoryPosition(itemId, newParentId) {
    await updateDirectoryPositionDb(itemId, newParentId);
    await messageToServiceWorker("syncChangeFolderParent", {
        item: itemId,
        parent: newParentId,
    });
}
async function updateDirectoryPositionDb(itemId, newParentId) {
    try {
        await jsstoreCon.update({
            in: "Folder",
            set: {
                parent: newParentId,
            },
            where: {
                id: itemId,
            },
        });
        //writeLogsInPage(`Folder updated: ID=${itemId}, new parent: ID=${newParentId}`);
    } catch (error) {
        writeLogsInPage("Error updating folder position:", error);
    }
}

async function editFolder(item, newName) {
    await editFolderDb(item, newName);
    await messageToServiceWorker("syncEditFolder", item);
}

async function editFolderDb(item, newName) {
    try {
        await jsstoreCon.update({
            in: "Folder",
            set: { text: newName.trim() },
            where: { id: item.id },
        });
        //writeLogsInPage(`Name updated: ID=${item.id}, new name=${newName.trim()}`);
    } catch (error) {
        writeLogsInPage("Error updating folder name:", error);
    }
}

async function deleteDirectory(itemId) {
    await deleteDirectoryDb(itemId);
    let liElement = document.querySelector(`li[data-id="${itemId}"]`);
    liElement.remove();
    //writeLogsInPage(`Item deleted: ID=${itemId}`);
    await messageToServiceWorker("syncDeleteFolder", {
        item: itemId,
    });
}

async function deleteDirectoryDb(itemId) {
    try {
        await jsstoreCon.update({
            in: "Folder",
            set: {
                active: 0,
            },
            where: {
                id: itemId,
            },
        });

        await jsstoreCon.update({
            in: "Note",
            set: {
                active: 0,
            },
            where: {
                folder: itemId,
            },
        });
        //writeLogsInPage(`Folder deleted: ID=${itemId}`);
    } catch (error) {
        writeLogsInPage("Error deleting folder:", error);
    }
}

function reopenDirectoryElements() {
    directoryElementOpen.forEach((element) => {
        const liElement = document.querySelector(`li[data-id="${element}"]`);
        if (liElement) {
            const childUl = liElement.querySelector("ul");
            if (childUl) {
                childUl.style.display = "block"; // Show the child list
                const expandButton = liElement.querySelector(".folder-chevron");
                if (expandButton) {
                    expandButton.classList.add("open");
                    const rowDivEl = expandButton.closest(".folder-row");
                    if (rowDivEl) rowDivEl.classList.add("open");
                }
            }
        }
    });
}

async function updateNotePosition(itemId, newParentId) {
    let result = await updateNotePositionDb(itemId, newParentId);
    if (result) {
        //await changeFolderNoteRest(itemId, newParentId);
        await messageToServiceWorker("syncChangeFolderNote", {
            item: itemId,
            parent: newParentId,
        });
    }
}

async function updateNotePositionDb(itemId, newParentId) {
    try {
        await jsstoreCon.update({
            in: "Note",
            set: {
                folder: newParentId,
            },
            where: {
                id: itemId,
            },
        });
        //writeLogsInPage(`Note folder updated: ID=${itemId}, new parent: ID=${newParentId}`);
        return true;
    } catch (error) {
        writeLogsInPage("Error updating note position:", error);
        return false;
    }
}

async function editNoteDb(id, title, text) {
    try {
        await jsstoreCon.update({
            in: "Note",
            set: {
                title: title,
                text: text,
            },
            where: {
                id: id,
            },
        });
        //writeLogsInPage(`Note updated: ID=${id}`);
        return true;
    } catch (error) {
        writeLogsInPage("Error updating note:", error);
        return false;
    }
}

async function initDb() {
    var isDbCreated = await jsstoreCon.initDb(getDbSchema(transformDateToUnix));
    if (isDbCreated) {
        writeLogsInPage("db created");
    } else {
        writeLogsInPage("db opened");
    }
}

function writeError(message) {
    let errorSpace = document.getElementById("errorsPanel");
    errorSpace.textContent = message;
    errorSpace.style.color = "red";
    setTimeout(() => {
        errorSpace.textContent = "";
    }, 5000);
}

function writeLog(message) {
    let errorSpace = document.getElementById("errorsPanel");
    errorSpace.textContent = message;
    errorSpace.style.color = "green";
    setTimeout(() => {
        errorSpace.textContent = "";
    }, 5000);
}

async function zappaDb() {
    //await jsstoreCon.remove({
    //    from: "SyncState",
    //});
    //await jsstoreCon.remove({
    //    from: "Folder",
    //});
    //await jsstoreCon.remove({
    //    from: "Note",
    //});
    // writeError("DB deleted!");
    await messageToServiceWorker("db_zappa_request");
}

// Create a top-level folder (parent: null) and insert it at the top of the list.
async function createRootFolder() {
    const newName = prompt("Enter name");
    if (!newName || newName.trim() === "") return;

    const newDir = {
        id: crypto.randomUUID(),
        text: newName.trim(),
        parent: null,
        timestamp: transformDateToUnix(),
        active: 1,
    };
    try {
        await jsstoreCon.insert({
            into: "Folder",
            values: [newDir],
        });
        // New root folder goes at the top of the list (no full rebuild)
        const listaDiv = document.getElementById("lista");
        let rootUl = listaDiv.querySelector("ul");
        if (!rootUl) {
            rootUl = document.createElement("ul");
            listaDiv.appendChild(rootUl);
        }
        const newLi = createFolderLi(newDir, false, makeOnAdd(newDir.id));
        rootUl.prepend(newLi);

        await messageToServiceWorker("syncAddFolder", newDir);
    } catch (error) {
        console.error("Error inserting root folder:", error);
    }
}

function registerEvents() {
    let searchIcon = document.getElementById("showSearchIcon");
    searchIcon.innerHTML = '<i class="fa-solid fa-magnifying-glass"></i>';
    searchIcon.onclick = function () {
        showOrHideSearchorFolderSpace("search");
    };

    let dirIcon = document.getElementById("showDirIcon");
    dirIcon.innerHTML = '<i class="fa-solid fa-folder"></i>';
    dirIcon.onclick = function () {
        showOrHideSearchorFolderSpace("dir");
    };

    let resyncIcon = document.getElementById("resync");
    resyncIcon.innerHTML = '<i class="fa-solid fa-cloud-arrow-down"></i>';
    resyncIcon.onclick = async function () {
        writeLog("🚀");
        await messageToServiceWorker("syncDataNoteAndFolder", { limit: limitApi });
    };

    let refreshIcon = document.getElementById("refresh");
    refreshIcon.innerHTML = '<i class="fa-solid fa-rotate"></i>';
    refreshIcon.onclick = async function () {
        writeLog("🧹");
        await cleanListFolder();
        await writeDirAndTitle();
    };

    // Clear local DB
    let zappa = document.getElementById("zappadb");
    zappa.innerHTML = '<i class="fa-solid fa-skull-crossbones"></i>';
    zappa.onclick = async function () {
        await zappaDb();
    };

    // Admin settings
    let adminSettings = document.getElementById("adminSettings");
    adminSettings.innerHTML = '<i class="fa-solid fa-gear"></i>';
    adminSettings.onclick = function () {
        showAdminPanelModal();
    };

    // Logout
    let logout = document.getElementById("logout");
    logout.innerHTML = '<i class="fa-solid fa-right-from-bracket"></i>';
    logout.onclick = async function () {
        if (confirm("Are you sure to logout?")) {
            await zappaDb();
            await messageToServiceWorker("logout_request");
        }
    };

    let changePassword = document.getElementById("changePassword");
    changePassword.innerHTML = '<i class="fa-solid fa-key"></i>';
    changePassword.onclick = async function () {
        showChangePasswordModal();
    };

    // Root "add folder" button in the sidebar header. Always visible and clearly
    // actionable (its only job is creating a top-level folder), placed next to
    // the "Folders" label so it reads as the way to add the first folder.
    const rootAddBtn = document.getElementById("firstAdd");
    rootAddBtn.className = "folder-add-btn";
    rootAddBtn.innerHTML = '<i class="fa-solid fa-folder-plus"></i>';
    rootAddBtn.setAttribute("title", "New folder");
    rootAddBtn.onclick = function (e) {
        e.stopPropagation();
        createRootFolder();
    };

    // Search panel
    const searchInput = document.getElementById("searchInput");
    const searchResults = document.getElementById("searchResults");

    let searchTimeout;

    searchInput.addEventListener("input", function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(async () => {
            let value = searchInput.value.trim().toLowerCase();
            searchResults.innerHTML = "";

            if (value.length === 0) {
                return;
            }

            value = value.replace(" ", "%");

            if (value.length < 2) {
                searchResults.innerHTML = "<div>minimum 2 char..</div>";
                return;
            }

            const filtered = await jsstoreCon.select({
                from: "Note",
                where: [
                    {
                        active: 1,
                    },
                    {
                        title: { like: "%" + value + "%" },
                        or: { text: { like: "%" + value + "%" } },
                    },
                ],
                order: {
                    by: "timestamp",
                    type: "desc",
                },
            });

            if (filtered.length === 0) {
                searchResults.innerHTML = "<div>No result.</div>";
                return;
            }

            await Promise.all(
                filtered.map(async (item) => {
                    const div = document.createElement("div");
                    div.classList.add("search-result-item");

                    const dirSpan = document.createElement("div");
                    dirSpan.classList.add("search-result-folder");
                    const dirData = await getDirCompletePathFromId(item.folder);
                    const folderIcon = document.createElement("i");
                    folderIcon.classList.add("fa-solid", "fa-folder");
                    folderIcon.style.color = "#f59e0b";
                    folderIcon.style.marginRight = "4px";
                    dirSpan.appendChild(folderIcon);
                    dirSpan.appendChild(document.createTextNode(dirData));
                    div.appendChild(dirSpan);

                    const titleSpan = document.createElement("div");
                    titleSpan.classList.add("search-result-title");
                    titleSpan.textContent = item.title;
                    div.appendChild(titleSpan);

                    const textSpan = document.createElement("div");
                    textSpan.classList.add("search-result-snippet");
                    const rawSearchText = (item.text || "").replace(/<[^>]+>/g, "").trim();
                    textSpan.textContent = rawSearchText.slice(0, 60) + (rawSearchText.length > 60 ? "…" : "");
                    div.appendChild(textSpan);

                            div.onclick = async function () {
                        showOrHideSearchorFolderSpace("note");
                        await noteFromId(item.id);
                    };

                    searchResults.appendChild(div);
                })
            );

            searchResults.style.maxHeight = "calc(100vh - " + (searchResults.getBoundingClientRect().top + 30) + "px)";
            searchResults.style.overflowY = "auto";
            searchResults.style.height = "auto";
        }, 300); // debounce
    });

    window.addEventListener("online", async function () {
        writeLog("Back online!");
        await messageToServiceWorker("syncDataNoteAndFolder", { limit: limitApi });
        //await messageToServiceWorker("workSyncRetry", null);
    });

    const mobileBack = document.getElementById("mobileBack");
    if (mobileBack) {
        mobileBack.onclick = function () {
            showOnlyNoteSpace(true);
        };
    }

    const logsEl = document.getElementById("logs");
    if (logsEl) {
        logsEl.addEventListener("click", function () {
            logsEl.classList.toggle("logs-expanded");
        });
    }
}

async function countNoteForFolder() {
    const result = await jsstoreCon.select({
        from: "Note",
        groupBy: ["folder"],
        aggregate: {
            count: "folder",
        },
        where: {
            active: 1,
        },
    });

    document.querySelectorAll("span[data-count-id]").forEach((span) => {
        span.textContent = " (0)";
    });

    // Update note count for each folder
    for (let i = 0; i < result.length; i++) {
        let folderId = result[i].folder;
        let count = result[i]["count(folder)"];
        //writeLogsInPage("Folder ID:", folderId, "Count:", count);
        const spanElement = document.querySelector(`span[data-count-id='${folderId}']`);
        if (spanElement) {
            spanElement.textContent = ` (${count})`;
                    const liElement = spanElement.closest("li");
            const chevronEl = liElement.querySelector(".folder-chevron");
            if (chevronEl) chevronEl.style.visibility = "visible";
        }
    }
}

async function writeTitleForDir() {
    const result = await jsstoreCon.select({
        from: "Note",
        where: {
            active: 1,
        },
    });

    // Write each note's title under its folder in the tree
    for (let i = 0; i < result.length; i++) {
        let dirId = result[i].folder;
        let title = result[i].title;
        let noteId = result[i].id;
        await writeSingleNote(dirId, title, noteId, result[i].text);
    }
}

async function writeSingleNote(dirId, title, noteId, text) {
    //writeLogsInPage("Folder ID:", dirId, "Title:", title);
    // Find the folder li element
    const liData = document.querySelector(`li[data-id='${dirId}']`);
    //console.log("liData:", liData);
    //writeLogsInPage("li Element:", liData.outerHTML);
    // Check if a ul for notes already exists inside this folder; create it if not
    let ulElement = liData.querySelector("ul");
    if (!ulElement) {
        ulElement = document.createElement("ul");
        ulElement.classList.add("titoli");
        ulElement.style.display = "none";
        liData.appendChild(ulElement);
    }
    // Make chevron visible when the folder has at least one note
    const chevronEl = liData.querySelector(".folder-chevron");
    if (chevronEl) chevronEl.style.visibility = "visible";
    // Create the note list item
    const liElement = document.createElement("li");
    liElement.className = "noteItem";
    liElement.setAttribute("note-id", noteId);

    const titleSpan = document.createElement("span");
    titleSpan.className = "note-title";
    titleSpan.textContent = title;
    liElement.appendChild(titleSpan);

    const rawText = (text || "").replace(/<[^>]+>/g, "").trim();
    if (rawText) {
        const snippetSpan = document.createElement("span");
        snippetSpan.className = "note-snippet";
        snippetSpan.textContent = rawText.slice(0, 60) + (rawText.length > 60 ? "…" : "");
        liElement.appendChild(snippetSpan);
    }
    liElement.setAttribute("draggable", "true");
    liElement.onclick = async function () {
        //writeLogsInPage(`Nota cliccata: ID=${noteId}`);
        await noteFromId(noteId);
    };
    // Desktop drag-and-drop events
    liElement.addEventListener("dragstart", handleDragStart);
    liElement.addEventListener("dragover", handleDragOver);
    liElement.addEventListener("drop", handleDrop);
    // Touch events for mobile devices
    liElement.addEventListener("touchstart", handleTouchStart, { passive: false });
    liElement.addEventListener("touchmove", handleTouchMove, { passive: false });
    liElement.addEventListener("touchend", handleTouchEnd, { passive: false });
    liElement.addEventListener("touchcancel", handleTouchCancel);
    ulElement.appendChild(liElement);
}

async function noteFromId(noteId) {
    const result = await jsstoreCon.select({
        from: "Note",
        where: {
            id: noteId,
        },
    });
    //writeLogsInPage("Note found:", result);
    const notaid = document.getElementById("notaid");
    notaid.value = result[0].id;
    const notatitle = document.getElementById("notatitle");
    notatitle.value = result[0].title;
    const notatext = document.getElementById("notatext");
    notatext.innerText = result[0].text;
    const folderId = document.getElementById("notadirid");
    folderId.value = result[0].folder;
    const notadirectory = document.getElementById("notadirectory");
    notadirectory.textContent = await getDirCompletePathFromId(result[0].folder);
    // Remove active class from all notes, add it to the selected one
    document.querySelectorAll(".noteItem.active").forEach(el => el.classList.remove("active"));
    const activeItem = document.querySelector(`li[note-id="${noteId}"]`);
    if (activeItem) activeItem.classList.add("active");
    await showNoteSpace("edit");
}

async function getDirCompletePathFromId(id) {
    let dirData = await getFolderFromId(id);
    let treeDir = dirData.text;

    while (dirData.parent != null) {
        dirData = await getFolderFromId(dirData.parent);
        treeDir = dirData.text + " / " + treeDir;
    }

    return treeDir;
}

async function showNoteSpace(tipo) {
    if (isScreenLittle()) {
        showOnlyNoteSpace();
    }

    const noteSpace = document.getElementById("notaspace");
    noteSpace.style.display = "flex";
    const notaSave = document.getElementById("notasave");
    const notaDelete = document.getElementById("notadelete");
    const notaOperation = document.getElementById("notaOperation");
    if (tipo === "edit") {
        notaSave.textContent = "Save";
        notaDelete.style.display = "inline";
        if (notaOperation) notaOperation.textContent = "Edit note";
    } else {
        notaSave.textContent = "Add";
        notaDelete.style.display = "none";
        if (notaOperation) notaOperation.textContent = "New note";
    }
    notaSave.onclick = async function () {
        await saveNote();
        notaSave.textContent = "Save";
    };
    notaDelete.onclick = async function () {
        let confirmDelete = confirm("Are you sure you want to delete this note?");
        if (confirmDelete) {
            const notaid = document.getElementById("notaid");
            deleteNote(notaid.value);
        }
    };
}
async function deleteNote(noteId) {
    const result = await deleteNoteDb(noteId);
    if (result) {
        const liElement = document.querySelector(`li[note-id='${noteId}']`);
        if (liElement) {
            liElement.remove();
        }
        incrementFolderCounterOfOne(document.getElementById("notadirid").value, true);
        showOrHideSearchorFolderSpace("dir");
        document.getElementById("notaspace").style.display = "none";
        //await deleteNoteRest(notaid);
        await messageToServiceWorker("syncDeleteNote", {
            item: noteId,
        });
    } else {
        writeError("Error deleting note.");
    }
}

async function deleteNoteDb(notaid) {
    try {
        await jsstoreCon.update({
            in: "Note",
            set: {
                active: 0,
            },
            where: {
                id: notaid,
            },
        });
        return true;
    } catch (error) {
        console.error("Error deleting note:", error);
        return false;
    }
}

async function saveNote() {
    const notaid = document.getElementById("notaid");
    const notatitle = document.getElementById("notatitle").value;
    const notatext = document.getElementById("notatext").innerText;
    const folderId = document.getElementById("notadirid").value;

    const notatextRaw = document.getElementById("notatext").textContent.replace(/\s/g, "");
    if (!notatextRaw) {
        writeError("Note text cannot be empty.");
        return;
    }

    if (notaid.value === "") {
        // Add new note
        let newNote = {
            id: crypto.randomUUID(),
            title: notatitle,
            text: notatext,
            folder: folderId,
            timestamp: transformDateToUnix(),
            active: 1,
        };
        let resultDb = await insertDb([newNote], "Note");
        if (resultDb) {
            notaid.value = newNote.id;
            // Write the note to the list
            await writeSingleNote(folderId, notatitle, newNote.id, notatext);
            // Update folder note counter
            incrementFolderCounterOfOne(folderId);
            await messageToServiceWorker("syncAddNote", newNote);
        }
    } else {
        // Update existing note
        let resultDb = await editNoteDb(notaid.value, notatitle, notatext);
        if (resultDb) {
            // Update title and snippet in the list
            const existingLi = document.querySelector(`li[note-id='${notaid.value}']`);
            if (existingLi) {
                existingLi.innerHTML = "";
                const titleSpan = document.createElement("span");
                titleSpan.className = "note-title";
                titleSpan.textContent = notatitle;
                existingLi.appendChild(titleSpan);
                const rawText = (notatext || "").replace(/<[^>]+>/g, "").trim();
                if (rawText) {
                    const snippetSpan = document.createElement("span");
                    snippetSpan.className = "note-snippet";
                    snippetSpan.textContent = rawText.slice(0, 60) + (rawText.length > 60 ? "…" : "");
                    existingLi.appendChild(snippetSpan);
                }
            }
            let noteData = {
                id: notaid.value,
                title: notatitle,
                text: notatext,
                folder: folderId,
                timestamp: transformDateToUnix(),
                active: 1,
            };
            //await editNoteRest(noteData);
            await messageToServiceWorker("syncEditNote", noteData);
        }
    }

    //await updateDbSyncState("Note");
}

function incrementFolderCounterOfOne(folderId, decrement = false) {
    const spanElement = document.querySelector(`span[data-count-id='${folderId}']`);
    if (spanElement) {
        let currentCount = parseInt(spanElement.textContent.replace(/\D/g, ""), 10) || 0;
        spanElement.textContent = decrement ? ` (${currentCount - 1})` : ` (${currentCount + 1})`;
        // Make chevron visible
        const liElement = spanElement.closest("li");
        const chevronEl = liElement.querySelector(".folder-chevron");
        if (chevronEl) chevronEl.style.visibility = "visible";
    }
}

function showOrHideSearchorFolderSpace(section) {
    let directorySpace = document.getElementById("directoryspace");
    let searchSpace = document.getElementById("searchSpace");

    if (section === "search") {
        searchSpace.style.display = "block";
        directorySpace.style.display = "none";
        let searchInput = document.getElementById("searchInput");
        searchInput.value = "";
        searchInput.focus();
    }
    if (section === "dir") {
        searchSpace.style.display = "none";
        directorySpace.style.display = "block";
    }
    if (section === "note") {
        if (isScreenLittle()) {
            searchSpace.style.display = "none";
            directorySpace.style.display = "none";
        }
    }
}

async function getFolderFromId(id) {
    const result = await jsstoreCon.select({
        from: "Folder",
        where: {
            id: id,
        },
    });
    return result[0];
}

async function insertDb(data, tipo) {
    try {
        await jsstoreCon.insert({
            into: tipo,
            upsert: true,
            values: data,
        });
        return true;
    } catch (error) {
        console.error("Error updating the database:", error);
        return false;
    }
}

function showOnlyNoteSpace(invertSelection) {
    const noteSpace = document.getElementById("notaspace");
    noteSpace.style.display = invertSelection ? "none" : "flex";
    const dirSpace = document.getElementById("directoryspace");
    dirSpace.style.display = invertSelection ? "block" : "none";
    const searchSpace = document.getElementById("searchSpace");
    searchSpace.style.display = "none";
    const mobileBack = document.getElementById("mobileBack");
    if (mobileBack) mobileBack.style.display = invertSelection ? "none" : "flex";
}

// Service worker response handler
navigator.serviceWorker.addEventListener("message", async (event) => {
    //uncomment for debug
    //writeLogsInPage("Message from Service Worker:", event.data);
    if (event.data.type === "userData_response") {
        let userDataFromSW = event.data.result;
        let userData = document.getElementById("userData");
        userData.textContent = "User: " + userDataFromSW.username;
        let changePassword = document.getElementById("changePassword");
        if (userDataFromSW.isGuest) {
            changePassword.style.display = "none";
        } else {
            changePassword.style.display = "inline";
        }
        document.getElementById("adminSettings").style.display = userDataFromSW.isAdmin ? "inline" : "none";
    }
    if (event.data.type === "db_zappa_request_response") {
        if (event.data.success) {
            writeLog("DB Deleted!");
            await cleanListFolder();
        } else {
            writeError("Errore during DB purge.");
        }
    }
    if (event.data.type === "syncDataNoteAndFolder_response") {
        // Drain deferred queues now that all sync pages have arrived.
        // If anything is still unresolved (e.g. parent folder never came), do
        // a full rebuild from local DB as a safety net so the UI never shows
        // stale state.
        if (pendingFolderSync.length > 0) await applyFolderBatch([]);
        if (pendingNoteSync.length > 0) await applyNoteBatch([]);
        if (pendingFolderSync.length > 0 || pendingNoteSync.length > 0) {
            await cleanListFolder();
            await writeDirAndTitle();
        }
        if (event.data.result.folder >= 0 && event.data.result.note >= 0) {
            writeLog("Sync ok!");
        } else {
            let errorMessage = "Error Sync:";
            if (event.data.result.folder == -1) {
                errorMessage += " folder";
            }
            if (event.data.result.note == -1) {
                errorMessage += " note";
            }
            writeError(errorMessage);
        }
    }
    if (event.data.type === "sync_response") {
        if (event.data.success) {
            writeLog("Sync ok.");
        } else {
            writeError("Error during sync. offline?");
        }
    }
    if (event.data.type === "syncNewData_response") {
        const { data, entityType, total, processed } = event.data.obj;
        const label = entityType === "folder" ? "Folders" : "Notes";
        writeLog(`Sincronizzazione ${label}: ${processed} / ${total}`);
        if (entityType === "folder") {
            await applyFolderBatch(data);
        } else {
            await applyNoteBatch(data);
        }
    }
    if (event.data.type === "writeLogsInPage") {
        console.log("writeLogsInPage:", event.data.obj.log);
        writeLogsInPage(event.data.obj.log);
    }
    if (event.data.type === "login_need") {
        writeError("Login needed.");
        showLoginError("Login needed.");
        showLoginModal();
    }
    if (event.data.type === "login_ok") {
        hideLoginModal();
        writeLog("🚀");
        await messageToServiceWorker("syncDataNoteAndFolder", { limit: limitApi });
    }
    if (event.data.type === "guest_ok") {
        hideLoginModal();
        writeLog(event.data.message);
    }
    if (event.data.type === "guest_mode_on") {
        //writeLog("You are in guest mode, you can hide some elements.");
    }
    if (event.data.type === "login_error") {
        showLoginError(event.data.message);
    }
    if (event.data.type === "changePassword_response") {
        if (event.data.result.success) {
            hideChangePasswordModal();
            writeLog("Password changed!");
        } else {
            showChangePasswordError(event.data.result.message);
        }
    }
    if (event.data.type === "adminGetUsers_response") {
        if (event.data.result.success) {
            renderAdminUsersTable(event.data.result.data);
        } else {
            showAdminPanelError(event.data.result.message || "Error loading users.");
        }
    }
    if (event.data.type === "adminAddUser_response") {
        if (event.data.result.success) {
            hideAdminUserFormModal();
            showAdminPanelSuccess("User added.");
            await messageToServiceWorker("adminGetUsers_request");
        } else {
            showAdminUserFormError(event.data.result.message || "Error creating user.");
        }
    }
    if (event.data.type === "adminEditUser_response") {
        if (event.data.result.success) {
            hideAdminUserFormModal();
            showAdminPanelSuccess("User updated.");
            await messageToServiceWorker("adminGetUsers_request");
        } else {
            showAdminUserFormError(event.data.result.message || "Error updating user.");
        }
    }
    if (event.data.type === "adminChangeUserPassword_response") {
        if (event.data.result.success) {
            hideAdminUserPasswordModal();
            showAdminPanelSuccess("Password updated.");
        } else {
            showAdminUserPasswordError(event.data.result.message || "Error changing password.");
        }
    }
});

async function cleanListFolder() {
    let listaDiv = document.getElementById("lista");
    listaDiv.innerHTML = "";
    pendingFolderSync = [];
    pendingNoteSync = [];
}

async function messageToServiceWorker(type, data) {
    let message = {
        type: type,
        obj: data || null,
    };
    await messageToServiceWorkerWithWait(message);
}

async function messageToServiceWorkerWithWait(message) {
    //writeLogsInPage("request to service worker :", message);
    let sw = await navigator.serviceWorker.ready;
    //writeLogsInPage("Service Worker active:", sw);
    if (sw.active) {
        sw.active.postMessage(message);
    } else {
        writeError("Service Worker no active.");
    }
}

// Authentication UI functions

// Login modal
let loginModalInstance;

function initLoginModal() {
    // eslint-disable-next-line no-undef
    loginModalInstance = new bootstrap.Modal(document.getElementById("loginModal"));
}

function showLoginModal() {
    if (!loginModalInstance) initLoginModal();
    if (!loginModalInstance) return;

    const passwordInput = document.getElementById("passwordInput");
    const usernameInput = document.getElementById("usernameInput");
    const guestBtn = document.getElementById("guestBtn");
    const loginBtn = document.getElementById("loginBtn");

    loginBtn.onclick = async () => {
        const password = passwordInput.value;
        const username = usernameInput.value;

        await messageToServiceWorker("login_request", { username, password });
    };

    guestBtn.onclick = async () => {
        await messageToServiceWorker("guest_request");
        hideLoginModal();
        // Write somewhere that you are in guest mode
    };

    loginModalInstance.show();
}

function hideLoginModal() {
    if (!loginModalInstance) initLoginModal();
    if (loginModalInstance) loginModalInstance.hide();
}

function showLoginError(message) {
    const errorDiv = document.getElementById("loginError");
    errorDiv.textContent = message;
    errorDiv.style.display = "block";
}

// Change password modal
let changePasswordModalInstance;

function initchangePasswordModal() {
    // eslint-disable-next-line no-undef
    changePasswordModalInstance = new bootstrap.Modal(document.getElementById("changePasswordModal"));
}

function showChangePasswordModal() {
    if (!changePasswordModalInstance) initchangePasswordModal();
    if (!changePasswordModalInstance) return;

    const changePasswordOldPasswordInput = document.getElementById("changePasswordOldPasswordInput");
    const changePasswordNewPasswordInput = document.getElementById("changePasswordNewPasswordInput");
    const changePasswordNewPassword2Input = document.getElementById("changePasswordNewPassword2Input");
    const changePasswordBtn = document.getElementById("changePasswordBtn");
    const changePasswordCancelBtn = document.getElementById("changePasswordCancelBtn");

    changePasswordBtn.onclick = async () => {
        if (changePasswordNewPasswordInput.value !== changePasswordNewPassword2Input.value) {
            showChangePasswordError("New passwords do not match.");
            return;
        }
        if (changePasswordNewPasswordInput.value.length < 6) {
            showChangePasswordError("New password must be at least 6 characters.");
            return;
        }

        await messageToServiceWorker("changePassword_request", {
            oldPassword: changePasswordOldPasswordInput.value,
            newPassword: changePasswordNewPasswordInput.value,
        });
    };

    changePasswordCancelBtn.onclick = async () => {
        hideChangePasswordModal();
    };

    changePasswordModalInstance.show();
}

function hideChangePasswordModal() {
    if (!changePasswordModalInstance) initchangePasswordModal();
    if (changePasswordModalInstance) changePasswordModalInstance.hide();
}

function showChangePasswordError(message) {
    const errorDiv = document.getElementById("changePasswordError");
    errorDiv.textContent = message;
    errorDiv.style.display = "block";
}

// ─── Admin Panel ───────────────────────────────────────────────────────────────

let adminPanelModalInstance;
let adminUserFormModalInstance;
let adminUserPasswordModalInstance;

function initAdminPanelModal() {
    // eslint-disable-next-line no-undef
    adminPanelModalInstance = new bootstrap.Modal(document.getElementById("adminPanelModal"));
}

async function showAdminPanelModal() {
    if (!adminPanelModalInstance) initAdminPanelModal();
    if (!adminPanelModalInstance) return;

    document.getElementById("adminPanelError").style.display = "none";
    document.getElementById("adminPanelSuccess").style.display = "none";
    document.getElementById("adminUsersTableBody").innerHTML = "";

    document.getElementById("adminAddUserBtn").onclick = () => openAdminUserForm(null);

    await messageToServiceWorker("adminGetUsers_request");
    adminPanelModalInstance.show();
}

function showAdminPanelError(message) {
    const el = document.getElementById("adminPanelError");
    el.textContent = message;
    el.style.display = "block";
    document.getElementById("adminPanelSuccess").style.display = "none";
}

function showAdminPanelSuccess(message) {
    const el = document.getElementById("adminPanelSuccess");
    el.textContent = message;
    el.style.display = "block";
    document.getElementById("adminPanelError").style.display = "none";
}

function renderAdminUsersTable(users) {
    const tbody = document.getElementById("adminUsersTableBody");
    tbody.innerHTML = "";
    users.forEach((u) => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td class="d-none d-md-table-cell">${u.id}</td>
            <td>${escapeHtml(u.username)}</td>
            <td>${escapeHtml(u.name)}</td>
            <td>${u.active ? '<span class="badge bg-success">Yes</span>' : '<span class="badge bg-secondary">No</span>'}</td>
            <td>${u.admin ? '<span class="badge bg-warning text-dark">Yes</span>' : '<span class="badge bg-secondary">No</span>'}</td>
            <td class="d-none d-md-table-cell">${new Date(u.created).toLocaleDateString("it-IT")}</td>
            <td>
                <div class="d-flex flex-column align-items-center gap-1">
                    <button class="btn btn-sm btn-link text-secondary p-0 admin-edit-btn" data-id="${u.id}" title="Edit"><i class="fa-solid fa-pen"></i></button>
                    <button class="btn btn-sm btn-link text-secondary p-0 admin-pwd-btn" data-id="${u.id}" title="Change password"><i class="fa-solid fa-key"></i></button>
                </div>
            </td>
        `;
        tbody.appendChild(tr);
    });

    tbody.querySelectorAll(".admin-edit-btn").forEach((btn) => {
        const userId = parseInt(btn.dataset.id);
        btn.onclick = () => openAdminUserForm(users.find((u) => u.id === userId));
    });
    tbody.querySelectorAll(".admin-pwd-btn").forEach((btn) => {
        const userId = parseInt(btn.dataset.id);
        btn.onclick = () => openAdminUserPasswordModal(userId);
    });
}

function escapeHtml(str) {
    return String(str).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

function openAdminUserForm(user) {
    if (!adminUserFormModalInstance) {
        // eslint-disable-next-line no-undef
        adminUserFormModalInstance = new bootstrap.Modal(document.getElementById("adminUserFormModal"));
    }

    const isNew = !user;
    document.getElementById("adminUserFormModalLabel").textContent = isNew ? "New user" : "Edit user";
    document.getElementById("adminUserFormId").value = isNew ? "" : user.id;
    document.getElementById("adminUserFormUsername").value = isNew ? "" : user.username;
    document.getElementById("adminUserFormName").value = isNew ? "" : user.name;
    document.getElementById("adminUserFormPassword").value = "";
    document.getElementById("adminUserFormPasswordGroup").style.display = isNew ? "block" : "none";
    document.getElementById("adminUserFormActive").checked = isNew ? true : !!user.active;
    document.getElementById("adminUserFormAdmin").checked = isNew ? false : !!user.admin;
    document.getElementById("adminUserFormError").style.display = "none";

    document.getElementById("adminUserFormSaveBtn").onclick = async () => {
        const username = document.getElementById("adminUserFormUsername").value.trim();
        const name = document.getElementById("adminUserFormName").value.trim();
        const password = document.getElementById("adminUserFormPassword").value;
        const active = document.getElementById("adminUserFormActive").checked ? 1 : 0;
        const admin = document.getElementById("adminUserFormAdmin").checked ? 1 : 0;

        if (!username || !name) {
            showAdminUserFormError("Username and name are required.");
            return;
        }
        if (/\s/.test(username)) {
            showAdminUserFormError("Username must not contain spaces.");
            return;
        }
        if (isNew && password.length < 6) {
            showAdminUserFormError("Password must be at least 6 characters.");
            return;
        }

        if (isNew) {
            await messageToServiceWorker("adminAddUser_request", { username, name, password, active, admin });
        } else {
            await messageToServiceWorker("adminEditUser_request", { id: parseInt(document.getElementById("adminUserFormId").value), username, name, active, admin });
        }
    };

    document.getElementById("adminUserFormCancelBtn").onclick = () => hideAdminUserFormModal();

    adminUserFormModalInstance.show();
}

function hideAdminUserFormModal() {
    if (adminUserFormModalInstance) adminUserFormModalInstance.hide();
}

function showAdminUserFormError(message) {
    const el = document.getElementById("adminUserFormError");
    el.textContent = message;
    el.style.display = "block";
}

function openAdminUserPasswordModal(userId) {
    if (!adminUserPasswordModalInstance) {
        // eslint-disable-next-line no-undef
        adminUserPasswordModalInstance = new bootstrap.Modal(document.getElementById("adminUserPasswordModal"));
    }

    document.getElementById("adminUserPasswordId").value = userId;
    document.getElementById("adminUserPasswordInput").value = "";
    document.getElementById("adminUserPassword2Input").value = "";
    document.getElementById("adminUserPasswordError").style.display = "none";

    document.getElementById("adminUserPasswordSaveBtn").onclick = async () => {
        const password = document.getElementById("adminUserPasswordInput").value;
        const password2 = document.getElementById("adminUserPassword2Input").value;
        if (password !== password2) {
            showAdminUserPasswordError("Passwords do not match.");
            return;
        }
        if (password.length < 6) {
            showAdminUserPasswordError("Password must be at least 6 characters.");
            return;
        }
        const id = parseInt(document.getElementById("adminUserPasswordId").value);
        await messageToServiceWorker("adminChangeUserPassword_request", { id, password });
    };

    document.getElementById("adminUserPasswordCancelBtn").onclick = () => hideAdminUserPasswordModal();

    adminUserPasswordModalInstance.show();
}

function hideAdminUserPasswordModal() {
    if (adminUserPasswordModalInstance) adminUserPasswordModalInstance.hide();
}

function showAdminUserPasswordError(message) {
    const el = document.getElementById("adminUserPasswordError");
    el.textContent = message;
    el.style.display = "block";
}
