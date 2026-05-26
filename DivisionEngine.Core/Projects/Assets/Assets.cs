//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Projects.Assets
{
    /// <summary>
    /// Helper class for loading and using assets.
    /// </summary>
    public static class Assets
    {
        public static async Task<T?> LoadAsync<T>(string id) where T : Asset
            => await ProjectManager.AssetManager?.LoadAssetAsync<T>(id)!;

        public static T? Get<T>(string id) where T : Asset
            => ProjectManager.AssetManager?.Get<T>(id);
    }
}
