# Probably Stolen Mod - Smart Item Placement
A mod for the game Probably Stolen. 

It adds another step to the game logic when placing items inside a container, prefering placements where the item will fit better into.

It uses harmonypostfix to add to the already existing method "TryFindOneValidInventorySlot" in "GameGridInventory" by taking the slot that it found and assigning it as the best candidate. Then it tests every other candidate with a scoring system and compares it to this first candidate the game already found, assigning any cadidate with a higher score as the new best. It assigns score by checking whether it will fit neatly into it's position or not, by scoring higher if it is touching a wall or another item.

Currently it will prefer walls over the middle, I'm willing to try to patch it to prefer shelving like the original sorting mechanism if enough people are interested in that.
