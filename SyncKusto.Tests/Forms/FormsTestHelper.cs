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
        string? failure = null;
        var handled = false;
        var threadId = GetCurrentThreadId();
        var stopwatch = Stopwatch.StartNew();
        using var timer = new System.Windows.Forms.Timer { Interval = 10 };
        timer.Tick += (_, _) =>
        {
            if (handled || failure != null)
                return;

            nint dialog = 0;
            EnumWindows((window, _) =>
            {
                var windowThreadId = GetWindowThreadProcessId(window, out var processId);
                if (processId != Environment.ProcessId || windowThreadId != threadId || !IsWindowVisible(window))
                    return true;
                var className = new StringBuilder(256);
                GetClassName(window, className, className.Capacity);
                if (className.ToString() != "#32770")
                    return true;

                dialog = window;
                return false;
            }, 0);

            nint button = 0;
            nint fallbackButton = 0;
            if (dialog != 0)
            {
                title = WindowText(dialog);
                var children = new List<string>();
                EnumChildWindows(dialog, (child, _) =>
                {
                    var childText = WindowText(child);
                    children.Add(childText);
                    var className = new StringBuilder(256);
                    GetClassName(child, className, className.Capacity);
                    if (className.ToString() != "Button")
                        return true;

                    fallbackButton = child;
                    if (string.Equals(childText.Replace("&", ""), response.ToString(), StringComparison.OrdinalIgnoreCase))
                        button = child;
                    return true;
                }, 0);
                text = string.Join(" | ", children);
            }

            if (button != 0)
            {
                // Freeze the snapshot before dismissal; never inspect a closing dialog.
                handled = true;
                timer.Stop();
                if (!PostMessage(button, 0x00F5, 0, 0))
                    failure = $"Could not dismiss the '{title}' message box (Win32 error {Marshal.GetLastWin32Error()}).";
            }
            else if (stopwatch.Elapsed > TimeSpan.FromSeconds(10))
            {
                timer.Stop();
                failure = dialog == 0
                    ? $"The '{expectedTitle}' message box did not open."
                    : $"The '{title}' message box did not provide the '{response}' response. Contents: {text}";
                if (dialog != 0)
                    PostMessage(fallbackButton != 0 ? fallbackButton : dialog, fallbackButton != 0 ? 0x00F5u : 0x0010u, 0, 0);
                else
                {
                    foreach (var form in Application.OpenForms.Cast<Form>().Where(f => !f.InvokeRequired && f.Modal).ToArray())
                    {
                        form.DialogResult = DialogResult.Cancel;
                        form.Close();
                    }
                }
            }
        };
        timer.Start();
        try
        {
            action();
        }
        finally
        {
            timer.Stop();
        }
        Assert.That(failure, Is.Null);
        Assert.That(handled, Is.True, $"The '{expectedTitle}' message box did not open.");
        Assert.That(title, Is.EqualTo(expectedTitle));
        Assert.That(text, Does.Contain(expectedText));
    }

    public static void WithNativeDialog(Action show, string expectedTitle, Action<nint> inspect)
    {
        Exception? error = null;
        bool handled = false;
        var captions = new HashSet<string>();
        var stopwatch = Stopwatch.StartNew();
        using var timer = new System.Windows.Forms.Timer { Interval = 50 };
        timer.Tick += (_, _) =>
        {
            nint dialog = 0;
            EnumWindows((window, _) =>
            {
                GetWindowThreadProcessId(window, out var processId);
                if (processId == Environment.ProcessId && IsWindowVisible(window))
                {
                    string caption = WindowText(window);
                    captions.Add(caption);
                    if (caption == expectedTitle)
                        dialog = window;
                }
                return true;
            }, 0);
            if (dialog == 0 && stopwatch.Elapsed < TimeSpan.FromSeconds(10))
                return;

            timer.Stop();
            if (dialog == 0)
            {
                error = new TimeoutException($"The '{expectedTitle}' native dialog did not open. Visible captions: {string.Join(", ", captions)}");
                CloseModalForms();
                return;
            }

            handled = true;
            try
            {
                inspect(dialog);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                PostMessage(dialog, 0x0010, 0, 0);
            }
        };
        timer.Start();
        show();
        Assert.That(error, Is.Null);
        Assert.That(handled, Is.True, $"The '{expectedTitle}' native dialog did not open.");
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
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(nint window, WindowCallback callback, nint parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint window, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder text, int count);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
