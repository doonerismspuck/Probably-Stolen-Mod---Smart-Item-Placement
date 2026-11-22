using System;
using System.Collections.Generic;
using HarmonyLib;

// Starter implementation: improves only empty-grid placement.
// It leaves stacking and nested inventory destinations alone.
[HarmonyPatch(typeof(GameGridInventory), "TryFindOneValidInventorySlot",
    new Type[] { typeof(GameItem), typeof(bool) })]
internal static class SmartPlacementPatch
{
    private const int MaxGridCells = 512;

    [HarmonyPostfix]
    private static void Postfix(
        GameGridInventory __instance,
        GameItem item,
        bool keepOrientation,
        ref SlotMarker __result)
    {
        if (item == null || __result == null || __result.numTransfer <= 0)
            return;
        if (item.parentInventory == __instance)
            return;

        if (__result.inventory != __instance || __result.targetItem != null)
            return;

        GridShape grid = __instance.inventoryShape;
        if (grid == null ||
            grid.width <= 1 ||
            grid.height <= 1 ||
            grid.width * grid.height > MaxGridCells)
        {
            return;
        }

        int amount = item.MaxNumRemove();
        if (amount <= 0)
            return;

        bool[,] blocked = BuildBlockedGrid(__instance, item);
        SlotMarker best = __result;
        int bestScore = Score(__result.itemGridShape, blocked, grid.width, grid.height);
        int orientations = keepOrientation ? 1 : 4;
        int flips = keepOrientation ? 1 : 2;
        var builder = new GridShapeBuilder(item.modifiedShape);

        for (int flip = 0; flip < flips; flip++)
        {
            for (int rotation = 0; rotation < orientations; rotation++)
            {
                for (int y = 0; y < grid.height; y++)
                {
                    for (int x = 0; x < grid.width; x++)
                    {
                        builder.SetTransform(x, y, flip != 0, 4 - rotation);
                        GridShape candidateShape = builder.Build();
                        if (!InsideBounds(candidateShape, grid.width, grid.height))
                            continue;
                        if (OverlapsBlocked(candidateShape, blocked))
                            continue;

                        int score = Score(candidateShape, blocked, grid.width, grid.height);
                        if (score <= bestScore)
                            continue;

                        SlotMarker candidate = __instance.TryInventorySlot(
                            item, amount, candidateShape);
                        if (candidate != null && candidate.numTransfer > 0)
                        {
                            best = candidate;
                            bestScore = score;
                        }
                    }
                }
            }
        }

        __result = best;
    }

    private static bool[,] BuildBlockedGrid(GameGridInventory inventory, GameItem incoming)
    {
        GridShape grid = inventory.inventoryShape;
        bool[,] blocked = new bool[grid.width, grid.height];
        for (int y = 0; y < grid.height; y++)
        for (int x = 0; x < grid.width; x++)
            blocked[x, y] = grid.Get(x, y) != 0;

        foreach (GameItem existing in inventory.childItems)
        {
            if (existing == incoming)
                continue;
            GridShape shape = existing.modifiedShape;
            for (int y = Math.Max(0, shape.minY); y <= Math.Min(grid.height - 1, shape.maxY); y++)
            for (int x = Math.Max(0, shape.minX); x <= Math.Min(grid.width - 1, shape.maxX); x++)
                if (shape.Get(x, y) != 0)
                    blocked[x, y] = true;
        }
        return blocked;
    }

    private static bool InsideBounds(GridShape shape, int width, int height)
    {
        return shape != null && shape.minX >= 0 && shape.minY >= 0 &&
               shape.maxX < width && shape.maxY < height;
    }

    private static bool OverlapsBlocked(GridShape shape, bool[,] blocked)
    {
        for (int y = shape.minY; y <= shape.maxY; y++)
        for (int x = shape.minX; x <= shape.maxX; x++)
            if (shape.Get(x, y) != 0 && blocked[x, y])
                return true;
        return false;
    }

    private static int Score(GridShape shape, bool[,] blocked, int width, int height)
    {
        if (!InsideBounds(shape, width, height))
            return int.MinValue;

        int contacts = 0;
        for (int y = shape.minY; y <= shape.maxY; y++)
        for (int x = shape.minX; x <= shape.maxX; x++)
        {
            if (shape.Get(x, y) == 0)
                continue;
            contacts += Contact(shape, blocked, x - 1, y, width, height);
            contacts += Contact(shape, blocked, x + 1, y, width, height);
            contacts += Contact(shape, blocked, x, y - 1, width, height);
            contacts += Contact(shape, blocked, x, y + 1, width, height);
        }
        return contacts;
    }

    private static int Contact(GridShape shape, bool[,] blocked, int x, int y,
                               int width, int height)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return 1;
        if (x >= shape.minX && x <= shape.maxX &&
            y >= shape.minY && y <= shape.maxY && shape.Get(x, y) != 0)
            return 0;
        return blocked[x, y] ? 1 : 0;
    }
}
