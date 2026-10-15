// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SyncKusto.Tests.Forms;

internal static class FormsTestHelper
{
    public static T Field<T>(object instance, string name) =>
        (T)instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;

    public static void PumpUntil(Func<bool> condition)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition() && stopwatch.Elapsed < TimeSpan.FromSeconds(10))
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        Assert.That(condition(), Is.True, "The UI operation did not complete.");
    }

    public static void WithModal<T>(Action show, Action<T> interact) where T : Form
    {
        Exception? error = null;
        var handled = false;
        var stopwatch = Stopwatch.StartNew();
        using var timer = new System.Windows.Forms.Timer { Interval = 10 };
        timer.Tick += (_, _) =>
        {
            var dialog = Application.OpenForms.OfType<T>().LastOrDefault();
            if (dialog == null)
            {
                if (stopwatch.Elapsed > TimeSpan.FromSeconds(10))
                {
                    timer.Stop();
                    error = new TimeoutException($"The {typeof(T).Name} dialog did not open.");
                    CloseModalForms();
                }
                return;
            }

            timer.Stop();
            handled = true;
            try
            {
                interact(dialog);
            }
            catch (Exception ex)
            {
                error = ex;
                dialog.Dispose();
            }
        };
        timer.Start();
        show();
        Assert.That(error, Is.Null);
        Assert.That(handled, Is.True, $"The {typeof(T).Name} dialog did not open.");
    }

    public static void WithMessageBox(Action action, string expectedTitle, DialogResult response, string expectedText)
    {
        string? title = null;
        string? text = null;
        var timedOut = false;
        var stopwatch = Stopwatch.StartNew();
        using var cancellation = new CancellationTokenSource();
        var watcher = Task.Run(() =>
        {
            while (!cancellation.IsCancellationRequested)
            {
                EnumWindows((window, _) =>
                {
                    GetWindowThreadProcessId(window, out var processId);
                    if (processId != Environment.ProcessId || !IsWindowVisible(window))
                        return true;
                    if (stopwatch.Elapsed > TimeSpan.FromSeconds(10))
                    {
                        timedOut = true;
                        if (GetWindow(window, 4) != 0)
                            PostMessage(window, 0x0010, 0, 0);
                    }
                    var className = new StringBuilder(256);
                    GetClassName(window, className, className.Capacity);
                    if (className.ToString() != "#32770")
                        return true;

                    title = WindowText(window);
                    var children = new List<string>();
                    nint button = 0;
                    EnumChildWindows(window, (child, _) =>
                    {
                        var childText = WindowText(child);
                        children.Add(childText);
                        if (string.Equals(childText.Replace("&", ""), response.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            button = child;
                        }
                        return true;
                    }, 0);
                    text = string.Join(" | ", children);
                    if (button != 0)
                    {
                        PostMessage(button, 0x00F5, 0, 0);
                    }
                    return true;
                }, 0);
                Thread.Sleep(10);
            }
        });
        try
        {
            action();
        }
        finally
        {
            cancellation.Cancel();
            watcher.GetAwaiter().GetResult();
        }
        Assert.That(timedOut, Is.False, "An unexpected modal dialog blocked the operation.");
        Assert.That(title, Is.EqualTo(expectedTitle));
        Assert.That(text, Does.Contain(expectedText));
    }

    private static void CloseModalForms()
    {
        foreach (var dialog in Application.OpenForms.Cast<Form>().Where(f => f.Modal).ToArray())
        {
            dialog.DialogResult = DialogResult.Cancel;
            dialog.Close();
        }
    }

    private static string WindowText(nint window)
    {
        var text = new StringBuilder(8192);
        GetWindowText(window, text, text.Capacity);
        return text.ToString();
    }

    private delegate bool WindowCallback(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, nint parameter);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out int processId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(nint window, WindowCallback callback, nint parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint window, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
