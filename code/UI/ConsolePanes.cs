namespace FootballSystem.UI;

using System;
using System.Collections.Generic;

public static class ConsolePanes
{
    private const int DefaultLogHeight = 10;

    private static readonly List<string> buffered = new();
    private static int logHeight = DefaultLogHeight;
    private static int height;
    private static int width;
    private static int mainBottom;
    private static int logTop;
    private static bool panesReady;
    private static bool simpleMode;

    public static void init(int heightOfLog = DefaultLogHeight)
    {
        logHeight = heightOfLog;

        if (Console.IsOutputRedirected || !tryGetSize(out height, out width) || height < logHeight + 6)
        {
            simpleMode = true;
            panesReady = false;
            foreach (string line in buffered)
            {
                Console.Error.WriteLine(line);
            }
            buffered.Clear();
            return;
        }

        simpleMode = false;
        mainBottom = height - logHeight - 1;
        logTop = mainBottom + 2;
        panesReady = true;

        Console.Write("\x1b[2J");
        setMainRegion();
        gotoMainTop();
        renderLog();
        gotoMainTop();
    }

    public static void clearMain()
    {
        if (!panesReady)
        {
            return;
        }

        Console.Write("\x1b[2J");
        setMainRegion();
        gotoMainTop();
        renderLog();
        gotoMainTop();
    }

    public static void log(string line)
    {
        if (!panesReady)
        {
            if (simpleMode)
            {
                Console.Error.WriteLine(line);
            }
            else
            {
                buffered.Add(line);
                trim();
            }
            return;
        }

        buffered.Add(line);
        trim();
        renderLog();
    }

    private static void trim()
    {
        int max = Math.Max(1, logHeight - 1);
        while (buffered.Count > max)
        {
            buffered.RemoveAt(0);
        }
    }

    private static void renderLog()
    {
        Console.Write("\u001b7");
        Console.Write("\x1b[r");

        writeAt(logTop - 1, new string('-', Math.Max(0, width - 1)));
        writeAt(logTop, "=== ЛОГ ВЫЗОВОВ ===");

        for (int i = 0; i < buffered.Count; i++)
        {
            writeAt(logTop + 1 + i, buffered[i]);
        }

        for (int row = logTop + 1 + buffered.Count; row <= height; row++)
        {
            writeAt(row, "");
        }

        setMainRegion();
        Console.Write("\u001b8");
    }

    private static void writeAt(int row, string text)
    {
        Console.Write($"\x1b[{row};1H");
        Console.Write("\x1b[K");
        Console.Write(fit(text));
    }

    private static string fit(string text)
    {
        string line = text.Replace("\r", " ").Replace("\n", " ");
        int max = Math.Max(1, width - 1);
        return line.Length <= max ? line : line[..(max - 3)] + "...";
    }

    private static void setMainRegion()
    {
        Console.Write($"\x1b[1;{mainBottom}r");
    }

    private static void gotoMainTop()
    {
        Console.Write("\x1b[1;1H");
    }

    private static bool tryGetSize(out int heightValue, out int widthValue)
    {
        try
        {
            heightValue = Console.WindowHeight;
            widthValue = Console.WindowWidth;
            return heightValue > 0 && widthValue > 20;
        }
        catch
        {
            heightValue = 0;
            widthValue = 0;
            return false;
        }
    }
}
