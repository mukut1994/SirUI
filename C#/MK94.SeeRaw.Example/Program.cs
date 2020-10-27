﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MK94.SeeRaw.Example
{
    class Program
    {
        static Progress progress = new Progress { Speed = "10kb/s", Max = "10MB" };
        static Lazy<Task> progressTask = new Lazy<Task>(() => UpdateFakeProgress(progress));

        static void Main()
        {
            SeeRawDefault
                .WithServer()
                .WithPerClientRenderer(RenderClientMenu)
                .RunInBackground();
                //.ServeFile(() => GenerateStreamFromString("TEST TEXT"), "test.txt", times: 1, path: "test");

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        public static System.IO.Stream GenerateStreamFromString(string s)
        {
            var stream = new System.IO.MemoryStream();
            var writer = new System.IO.StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        static void RenderClientMenu()
        {
            // Show a basic hello world
            "Hello World".Render();

            // Show a simple menu
            // a. create a placeholder and keep a reference to the renderTarget so we can update it later
            "Loading Menu...".Render(out var menuTarget);
            "Click on a menu item above".Render(out var contentTarget);

            // b. Create actions/menu items to update the contentTarget depending on whats invoked
            var hiAction = SeeRawTypes.Action("Say Hi", () => SayHi(contentTarget));
            var calcAction = SeeRawTypes.Action("Calculator", () => Calc(contentTarget));
            var progressAction = SeeRawTypes.Action("Show file copy progress", () => ShowProgress(contentTarget));

            // c. Update the menuTarget to display the actions
            menuTarget.Value = new List<object> { hiAction, calcAction, progressAction };
        }

        enum Title
        {
            Mr, Mrs
        }

        static void SayHi(RenderTarget contentTarget)
        {
            contentTarget.Value = SeeRawTypes.Action("Please enter your details", (string name, Title title) => contentTarget.Value = $"Hi {title} {name}");
        }

        static void Calc(RenderTarget contentTarget)
        {
            contentTarget.Value = SeeRawTypes.Action("Add", (int a, int b) => contentTarget.Value = $"Result: {a + b}");
        }

        static void ShowProgress(RenderTarget contentTarget)
        {
            // initialize the lazy; starts up a background thread to update the progress
            _ = progressTask.Value;
            contentTarget.Value = progress;
        }

        static Task UpdateFakeProgress(Progress progress)
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    progress.Percent = (progress.Percent + 1) % 100;
                    Thread.Sleep(100);
                }
            });

        }
    }
}
