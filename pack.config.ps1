# Per-repo configuration consumed by the canonical pack.ps1.
# Only this file may differ between repos; pack.ps1 itself is byte-identical everywhere.
@{
    # Projects to pack, in dependency order (least-dependent first).
    Projects = @(
        @{ Path = 'AuthBlocksModels/AuthBlocksModels.csproj';       Name = 'Cerebellum.AuthBlocks.Models' }
        @{ Path = 'AuthBlocksData/AuthBlocksData.csproj';           Name = 'Cerebellum.AuthBlocks.Data' }
        @{ Path = 'AuthBlocksLib/AuthBlocksLib.csproj';             Name = 'Cerebellum.AuthBlocks' }
        @{ Path = 'AuthBlocksWeb.Client/AuthBlocksWeb.Client.csproj'; Name = 'Cerebellum.AuthBlocks.Web.Client' }
        @{ Path = 'AuthBlocksWeb/AuthBlocksWeb.csproj';             Name = 'Cerebellum.AuthBlocks.Web' }
    )
    PushSymbols = $true
}
