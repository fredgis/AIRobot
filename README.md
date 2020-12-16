# AIRobot
AIRobot ! From robots to maintenance service

1. Blabla sur contexte
2. Approche globale d'architecture
# Architecture overview
![](/Pictures/iRobotArchitecture.png?raw=true)

3. Deep dive technique @edge
# Deep architecture design
![](/Pictures/iRobotArchitecture-DEEP%20ARCHITECTURE$.png?raw=true)

# Architecture "At the Edge"
## 3.1 Réseau

## 3.2 Simulateur des données AIRobot

## 3.3 Déploiement du edge (Gateway)
- HubAgent
- Hub
- SQLEdge
- StorageEdge
- Custom module avec Azure Function

## 3.4 Edge as Transparent Gateway

## 3.5 Si déploiement de SDK impossible, utilisation d'une translation Gateway (module de protocol et identity translation)
https://docs.microsoft.com/en-us/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2018-06

## 3.6 Déploiement du modèle ONNX dans SQLEdge
https://docs.microsoft.com/fr-fr/azure/azure-sql-edge/deploy-onnx

## 3.7 Ordonnancement des prédictions via Azure Function

## 3.8 Création des jobs de streaming (SQLEdge)

## 3.9 Création du compute Azure Function
- Pulling des tables de prédictions
- Dépôt sur le stockage edge de l'export

## 3.10 Configuration du pulling (cron 0 * * * *)

## 3.11 Configuration de la synchronisation du storage account avec le cloud

# Architecture "Cloud"
4. Deep dive technique cloud

## 4.1 Création et entrainement du modèle ML de prédiction des pannes

## 4.2 Création du compute Azure Function pour router la transaction (transaction builder)

## 4.3 Création de l'environnement Blockchain (POA) pour validation des transactions (Quorum)

## 4.4 Routage de l'informationd e transaction validée sur un event grid (Blockchain Data Manager)

## 4.5 Création de la CosmosDB et interaction avec Azure Function et Event Grid

## 4.6 Interconnexion avec ERP

## 4.7 Création de la PowerApps de maintenance



