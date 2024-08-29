// Fill out your copyright notice in the Description page of Project Settings.

#include "NetworkGameObject.h"
#include "Containers/UnrealString.h"
#include "FPSGameUnreal/NetManager.h"

int32 UNetworkGameObject::lastLocalID = 0;

// Sets default values for this component's properties
UNetworkGameObject::UNetworkGameObject()
{
	// Set this component to be initialized when the game starts, and to be ticked every frame.  You can turn these features
	// off to improve performance if you don't need them.
	PrimaryComponentTick.bCanEverTick = true;

	// ...
}

// Called when the game starts
void UNetworkGameObject::BeginPlay()
{
	Super::BeginPlay();

	//isLocallyOwned = false;

	if (isLocallyOwned)
	{
		//Assign this object's ID to be the last local ID
		localID = lastLocalID;
		//Increment the last local ID variable, ready for the next object
		lastLocalID++;

		ANetManager::localNetObjects.Add(this);

		//FString UNID = "Please give UNID for the game object that has the localID :" + FString::FromInt(localID);
		//ANetManager::instance->AddNetworkObject(this, UNID);

		// For logging how many Local objs there are
		int32 NumItems = ANetManager::localNetObjects.Num();

		//// Convert the integer value to FString
		FString NumItemsString = FString::Printf(TEXT("%d"), NumItems);

		//// Log the count of items in the TArray
		UE_LOG(LogTemp, Warning, TEXT("There are %s items in the TArray."), *NumItemsString);

	}
	FString UNID = "Please give UNID for the game object that has the localID :" + FString::FromInt(localID);
	ANetManager::instance->AddNetworkObject(this, UNID);

	// ...

}

// Called every frame
void UNetworkGameObject::TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	if (bIsHit) {
		AActor* aActorComponent = GetOwner();
		

	}

	// ...
}


int32 UNetworkGameObject::GetLID()
{
	return localID;
}

int32 UNetworkGameObject::GetGID()
{
	return globalID;
}

int32 UNetworkGameObject::GetGIDFromPacket(FString receivedMsg)
{
	TArray<FString> parsedInfo;
	const TCHAR* sentinels[] = { TEXT(":"), TEXT(";") };
	receivedMsg.ParseIntoArray(parsedInfo, sentinels, 2);

	int32 globalIDFromPacket = FCString::Atoi(*parsedInfo[1]);

	return globalIDFromPacket;

	//TArray<FString> parsed;
	//receivedMsg.ParseIntoArray(parsed, TEXT(":"), false);
	//return FCString::Atoi(*parsed[1]);
}

bool UNetworkGameObject::GetIsLocallyOwned()
{
	return isLocallyOwned;
}

void UNetworkGameObject::SetLID(int32 localID_)
{
	localID = localID_;
}

void UNetworkGameObject::SetGID(int32 globalID_)
{
	globalID = globalID_;
}

void UNetworkGameObject::FromPacket(FString receivedMsg)
{
	TArray<FString> parsedInfo;
	const TCHAR* sentinels[] = { TEXT(":"), TEXT(";") };
	receivedMsg.ParseIntoArray(parsedInfo, sentinels, 2);

	int globalIDFromPacket = FCString::Atoi(*parsedInfo[1]);
	AActor* aActorComponent = GetOwner();

	//if (globalIDFromPacket == globalID) {

	float PosX = FCString::Atof(*parsedInfo[2]);
	float PosY = FCString::Atof(*parsedInfo[3]);
	float PosZ = FCString::Atof(*parsedInfo[4]);

	float RotW = FCString::Atof(*parsedInfo[5]);
	float RotX = FCString::Atof(*parsedInfo[6]);
	float RotY = FCString::Atof(*parsedInfo[7]);
	float RotZ = FCString::Atof(*parsedInfo[8]);

	int client = FCString::Atof(*parsedInfo[9]);


	if (client == 1) { // Info from Unity
		FVector position = FVector(PosX, PosZ, -PosY);
		FRotator rotation(FQuat(RotW, RotX, RotZ, -RotY));
	}

	if (client == 2) { // Info from Unreal
		FVector position = FVector(PosX * 100, PosY * 100, PosZ * 100);
		FRotator rotation(FQuat(RotX, RotY, RotZ, RotW));
	}

	FString myMessage = FString::Printf(TEXT("PositionInformation:%i;%f;%f;%f;%f;%f;%f;%f"), globalID, PosX, PosY, PosZ, RotW, RotX, RotY, RotZ);

	UE_LOG(LogTemp, Warning, TEXT("myMessage"), *myMessage);
}

FString UNetworkGameObject::ToPacket()
{
	AActor* aActorComponent = GetOwner();

	FVector ActorLocation = aActorComponent->GetActorLocation();
	FRotator ActorRotation = aActorComponent->GetActorRotation();

	float PosX = ActorLocation.X / 100;
	float PosY = ActorLocation.Y / 100;  // Y and Z coordinates are swaps to match the Axis conversion
	float PosZ = ActorLocation.Z / 100;  // The Z is funky (awful)

	//float PosY = ActorLocation.Z;  // Y and Z coordinates are swaps to match the Axis conversion
	//float PosZ = -ActorLocation.Y;  // The Z is funky (awful)

	FQuat QuaternRot = FQuat(ActorRotation); // Conversion func for Euler to Quaternion

	float RotX = QuaternRot.X;
	float RotY = QuaternRot.Y;  // Y and Z coordinates are swaps to match the Axis conversion
	float RotZ = QuaternRot.Z;  // The Z is funky (awful)
	float RotW = QuaternRot.W;

	//float RotY = QuaternRot.Z;  // Y and Z coordinates are swaps to match the Axis conversion
	//float RotZ = -QuaternRot.Y;  // The Z is funky (awful)

	// Unreal puts WXYZ but if i just try to make it so that it works Unreal - Unity, 
	// then I cant do Unreal - Unreal gameplay
	// or Unity - Unity gameplay because the values are hardcoded for the from and to packets.
	FString myMessage = FString::Printf(TEXT("PositionInformation:%i;%f;%f;%f;%f;%f;%f;%f;%i"), globalID, PosX, PosY, PosZ, RotW, RotX, RotY, RotZ + 2);

	return myMessage;
}

void UNetworkGameObject::TakeDamage()
{
	ANetManager::instance->DamageNotification(25, globalID);
}

void UNetworkGameObject::Death()
{
	ANetManager::instance->DeathNotification(globalID);
}


void UNetworkGameObject::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
	Super::EndPlay(EndPlayReason);

	globalID = 0;
	localID = 0;
	if (isLocallyOwned)
	{
		lastLocalID = 0;
	}
	// ...
}

