// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.Core;

namespace Geta.Optimizely.Categories.Find
{
    public class ContentCount
    {
        public readonly string Name;
        public readonly ContentReference ContentLink;
        public readonly int Count;

        public ContentCount(IContent content, int count)
        {
            Name = content.Name;
            ContentLink = content.ContentLink;
            Count = count;
        }
    }
}