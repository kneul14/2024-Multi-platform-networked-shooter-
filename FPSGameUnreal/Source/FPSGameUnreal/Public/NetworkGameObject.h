// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "NetworkGameObject.generated.h"


UCLASS(ClassGroup = (Custom), meta = (BlueprintSpawnableComponent))
class FPSGAMEUNREAL_API UNetworkGameObject : public UActorComponent
{
	GENERATED_BODY()

public:

	UPROPERTY(EditAnywhere)
	bool isLocallyOwned = false;
	UPROPERTY(VisibleAnywhere)
	int32 globalID;
	UPROPERTY(VisibleAnywhere)
	int32 localID;
	static int32 lastLocalID;
	UPROPERTY(BlueprintReadWrite, EditAnywhere)
	float maxHealth = 100;
	UPROPERTY(BlueprintReadWrite, EditAnywhere)
	float currentHealth;
	UPROPERTY(BlueprintReadWrite, EditAnywhere)
	bool bIsHit;

public:

	bool isUNIDRequested = false;

	// Sets default values for this component's properties (default constructor)
	UNetworkGameObject();

	// OOP lol 
	int32 GetLID();
	int32 GetGID();
	int32 GetGIDFromPacket(FString receivedMsg);
	bool  GetIsLocallyOwned();

	void SetLID(int32 globalID_);
	void SetGID(int32 localID_);
	
	void FromPacket(FString receivedMsg);
	FString ToPacket();

	UFUNCTION(BlueprintCallable, Category = "Networking")
	void TakeDamage();

	UFUNCTION(BlueprintCallable, Category = "Networking")
	void Death();

protected:
	// Called when the game starts
	virtual void BeginPlay() override;


public:
	// Called every frame
	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

	virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;


};
