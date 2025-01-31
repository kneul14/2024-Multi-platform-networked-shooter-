// Fill out your copyright notice in the Description page of Project Settings.
#include "NetManager.h"

TArray<UNetworkGameObject*> ANetManager::localNetObjects;
TArray<UNetworkGameObject*> ANetManager::worldState;
ANetManager* ANetManager::instance = nullptr;

// Sets default values
ANetManager::ANetManager()
{
    otherPlayerAvatar = nullptr;
    instance = nullptr;
    localNetObjects.Empty();
    worldState.Empty();

    if (instance == nullptr)
    {
        // Create the singleton instance if it doesn't exist
        instance = this;
        //this->AddToRoot();
    }

    // Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
    PrimaryActorTick.bCanEverTick = true;

    UE_LOG(LogTemp, Warning, TEXT("BING BONG"));
    FIPv4Address serveripAddr;
    FIPv4Address::Parse("127.0.0.1", serveripAddr);
    uint16 serverPort = 9050;
    uint16 cPort = 0;//5000;//49501;        //50000; //use this one at uni.

    serverep = { serveripAddr, serverPort };
    localep = { FIPv4Address::Any, cPort };

    socket = FUdpSocketBuilder("UPD Socket").BoundToEndpoint(localep);
    if (socket) {
        UE_LOG(LogTemp, Log, TEXT("Client complete..."));
    }
    else
    {
        UE_LOG(LogTemp, Error, TEXT("Issue with socket..."));
    }

    serverAddress = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
    serverAddress->SetIp(serveripAddr.Value);
    serverAddress->SetPort(serverPort);

    // * Move to the Tick() eventually
    FString message = "I'm an Unreal client - Hi!";
    TCHAR* charMessage = message.GetCharArray().GetData();
    uint8* array = (uint8*)TCHAR_TO_UTF8(charMessage);
    int32 arrayLength = FCString::Strlen(charMessage);

    int32 bytesSentAmount;

    socket->SendTo(array, arrayLength, bytesSentAmount, *serverAddress);
}

// Called when the game starts or when spawned  // like Start() from Unity
void ANetManager::BeginPlay()
{
    Super::BeginPlay();
}

// Called every frame       // like Update() from Unity
void ANetManager::Tick(float DeltaTime)
{
    Super::Tick(DeltaTime);

    for (int i = 0; i < localNetObjects.Num(); i++)
    {
        if (localNetObjects[i]->GetIsLocallyOwned() && localNetObjects[i]->GetGID() != 0)
        {
            //UE_LOG(LogTemp, Warning, TEXT("Sending: %s"), *localNetObjects[i]->ToPacket());
            RequestUNIDs(localNetObjects[i]->ToPacket());
        }
    }

    Listen(); // Main Loop for server communication

}

void ANetManager::RequestUNIDs(FString UNID) {

    TCHAR* charMessage = UNID.GetCharArray().GetData();
    uint8* array = (uint8*)TCHAR_TO_UTF8(charMessage);
    int32 arrayLength = FCString::Strlen(charMessage);

    int32 bytesSentAmount;

    socket->SendTo(array, arrayLength, bytesSentAmount, *serverAddress);
}

void ANetManager::SetUNIDs(FString receivedMsg, UNetworkGameObject* GO) {

    UE_LOG(LogTemp, Log, TEXT("UNID is in this message: %s"), *receivedMsg);
    FString info;
    // After the colon 
    if (receivedMsg.Split(TEXT(":"), &receivedMsg, &info)) {
        FString lid, gid;
        // Split into local and global ID
        if (info.Split(TEXT(";"), &lid, &gid)) {

            // To convert strings to int32, use FCString::Atoi().
            int32 intGlobalID = FCString::Atoi(*gid);
            int32 intLocalID = FCString::Atoi(*lid);

            if (GO->GetLID() == intLocalID) {
                GO->SetGID(intGlobalID);
                UE_LOG(LogTemp, Warning, TEXT("Assigned: %d"), intGlobalID);
            }

        }
    }
}

void ANetManager::AddNetworkObject(UNetworkGameObject* GO, FString UNID)
{
    worldState.Add(GO);
    if (GO->GetGID() == 0 && GO->GetIsLocallyOwned())
    {
        RequestUNIDs(UNID);
    }
}

int32 ANetManager::GetGlobalIDFromPacket(FString receivedMsg)
{
    TArray<FString> parsedInfo;
    const TCHAR* sentinels[] = { TEXT(":"), TEXT(";") };
    receivedMsg.ParseIntoArray(parsedInfo, sentinels, 2);

    int32 globalIDFromPacket = FCString::Atoi(*parsedInfo[0]);

    return globalIDFromPacket;
}

void ANetManager::DamageNotification(int damage, int UNID)
{
    if (instance == nullptr) {
        UE_LOG(LogTemp, Error, TEXT("ANetManager instance is null. Cannot send damage notification."));
        return;
    }

    FString damageInfo = FString::Printf(TEXT("PlayerDamaged:%d;%d"), UNID, damage);

    TCHAR* charMessage = damageInfo.GetCharArray().GetData();
    uint8* array = (uint8*)TCHAR_TO_UTF8(charMessage);
    int32 arrayLength = FCString::Strlen(charMessage);

    int32 bytesSentAmount;

    instance->socket->SendTo(array, arrayLength, bytesSentAmount, *instance->serverAddress);
}

void ANetManager::DeathNotification(int UNID)
{
    FString destroyInfo = FString::Printf(TEXT("PlayerDestroyed:%d"), UNID);

    TCHAR* charMessage = destroyInfo.GetCharArray().GetData();
    uint8* array = (uint8*)TCHAR_TO_UTF8(charMessage);
    int32 arrayLength = FCString::Strlen(charMessage);

    int32 bytesSentAmount;

    instance->socket->SendTo(array, arrayLength, bytesSentAmount, *instance->serverAddress);
}

void ANetManager::Listen()
{
    uint32 dataSent;                         // Used to store the expected amount of data pending to be received.

    // This checks if there is data to be received by the socket.
    while (socket->HasPendingData(dataSent)) // dataSent is constantly updated with the data waiting
    {
        TArray<uint8> array;                 // Will store the data
        array.SetNumUninitialized(dataSent); // Resizes the array to match the size of the data sent.

        int32 byteStorage;                   // Stores the actual number of bytes received.
        TSharedRef<FInternetAddr> targetAddr = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
        if (socket->RecvFrom(array.GetData(), dataSent, byteStorage, *targetAddr)) {
            TCHAR* message = (TCHAR*)UTF8_TO_TCHAR(array.GetData()); // Converts the data to TCHAR chars.
            //UE_LOG(LogTemp, Log, TEXT("packet - %s"), message);      // Same as Debug.Log() in Unity.

            // Convert TCHAR* to FString
            FString receivedMsg(message); // Converting to an FString(pls work)

            bool isGameObjectFound = false;

            if (receivedMsg.Contains(TEXT("UNID")))
            {
                // Go through the GameObjects and set the globalID if the localIDs match.
                for (int i = 0; i < worldState.Num(); i++) {
                    SetUNIDs(receivedMsg, worldState[i]);
                }
            }

            if (receivedMsg.Contains("PositionInfo")) {
                //check if the recieved string is object data
                bool foundActor = false;
                for (int i = 0; i < worldState.Num(); i++)
                {
                    //check if an object with that global id already exists
                    if (worldState[i]->globalID == worldState[i]->GetGIDFromPacket(receivedMsg) || worldState[i]->globalID == 0) {
                        isGameObjectFound = true;

                        if (!worldState[i]->GetIsLocallyOwned()) {
                            worldState[i]->FromPacket(receivedMsg);
                        }
                    }
                }

                //if no object exists with that global id then spawn a new object
                if (!isGameObjectFound) {

                    UE_LOG(LogTemp, Warning, TEXT("spawning"));

                    AActor* otherClient;
                    otherClient = GetWorld()->SpawnActor<AActor>(otherPlayerAvatar, FVector::ZeroVector, FRotator::ZeroRotator);
                    //otherClient->FindComponentByClass<UNetworkGameObject>()->globalID;
                    //otherClient->FindComponentByClass<UNetworkGameObject>()->FromPacket(receivedMsg);
                    UNetworkGameObject* newPlayer = otherClient->FindComponentByClass<UNetworkGameObject>();
                    newPlayer->SetGID(newPlayer->GetGIDFromPacket(receivedMsg));
                    newPlayer->FromPacket(receivedMsg);
                }
            }
        }
    }
}

void ANetManager::EndPlay(const EEndPlayReason::Type EndPlayReason)
{
    Super::EndPlay(EndPlayReason);
    instance = nullptr;
    otherPlayerAvatar = nullptr;
    localNetObjects.Empty();
    worldState.Empty();
    ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->DestroySocket(socket);
}