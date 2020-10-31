﻿using System;
using System.IO;

namespace MK94.SeeRaw
{
    public static class Extensions
    {
        public static T Render<T>(this T obj)
        {
            return obj.Render(out _);
        }

        public static T Render<T>(this T obj, out RenderTarget target)
        {
            if(SeeRawDefault.localSeeRawContext == null)
                throw new InvalidProgramException($"Default renderer is not set for extension method. Call {nameof(SeeRawDefault)}.{nameof(SeeRawDefault.WithServer)}().{nameof(SeeRawDefault.WithGlobalRenderer)}() first");

            target = SeeRawDefault.localSeeRawContext.Value.RenderRoot.Render(obj);

            return obj;
        }
    }
}
