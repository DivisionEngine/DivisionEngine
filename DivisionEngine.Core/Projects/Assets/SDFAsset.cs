//
// Copyright (c) 2025-2026 Rex Woodfield and Division Engine contributors
//
// This file is part of Division Engine and is subject to the terms
// of the Division Engine License. See the LICENSE.txt file in the
// project root for full license terms.
//
namespace DivisionEngine.Projects.Assets
{
    [AssetType(AssetType.SDF)]
    public class SDFAsset(AssetMetadata metadata) : Asset(metadata)
    {
        public override Task<bool> LoadAsync()
        {
            throw new NotImplementedException();
        }

        public override void Unload()
        {
            throw new NotImplementedException();
        }
    }
}
