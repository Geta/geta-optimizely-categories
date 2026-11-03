// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;

namespace Geta.Optimizely.Categories
{
    /// <summary>
    /// The CMS shell's type identifier format (lower-cased full name), shared by the editor metadata
    /// and the Selectable endpoint so both sides of the type match encode types the same way.
    /// </summary>
    internal static class TypeIdentifier
    {
        public static string Of(Type type) => type?.FullName?.ToLowerInvariant();
    }
}
