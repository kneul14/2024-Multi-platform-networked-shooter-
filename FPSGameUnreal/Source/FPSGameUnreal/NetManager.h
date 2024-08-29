// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h" //default UE library
#include "GameFramework/Actor.h" //class we are extending
#include "Networking.h"
#include "IPAddress.h"
#include "SocketSubsystem.h"
#include "Interfaces/IPv4/IPv4Address.h"

#include <array>
#include "NetworkGameObject.h"
#include "NetManager.generated.h" //needs to be the bottom and be the last include

UCLASS() //boilerplate code
class FPSGAMEUNREAL_API ANetManager : public AActor
{
    GENERATED_BODY() //boilerplate code

public:
    //Place we will declare variables for this class
    FIPv4Endpoint serverep;
    FIPv4Endpoint localep;
    TSharedPtr<FInternetAddr> serverAddress;
    FSocket* socket;
    static TArray<UNetworkGameObject*> localNetObjects;
    static TArray<UNetworkGameObject*> worldState;
    static ANetManager* instance;

    UPROPERTY(EditAnywhere)
        TSubclassOf <AActor> otherPlayerAvatar; // Enforce the choice of the type of item to be stored in this variable.
    UPROPERTY(VisibleAnywhere)
    bool whereAreYou = true;

    //FString receivedMsg = "";

public:
    // Sets default values for this actor's properties
    ANetManager();

    void Listen();

    void RequestUNIDs(FString msg);
    void SetUNIDs(FString receivedMsg, UNetworkGameObject* GO);

    void AddNetworkObject(UNetworkGameObject* GO, FString UNID);

    int32 GetGlobalIDFromPacket(FString receivedMsg);

    static void DamageNotification(int damage, int UNID);

    static void DeathNotification(int UNID);

protected:
    // Called when the game starts or when spawned
    virtual void BeginPlay() override;

public:
    // Called every frame
    virtual void Tick(float DeltaTime) override;

    virtual void EndPlay(const EEndPlayReason::Type EndPlayReason) override;

};
