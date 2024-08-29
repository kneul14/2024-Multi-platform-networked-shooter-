// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;

public class FPSGameUnreal : ModuleRules
{
	public FPSGameUnreal(ReadOnlyTargetRules Target) : base(Target)
	{
		PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;

		PublicDependencyModuleNames.AddRange(new string[] { "Core", "CoreUObject", "Engine", "InputCore", "Sockets", "Networking", "HeadMountedDisplay", "EnhancedInput" });
	}
}
