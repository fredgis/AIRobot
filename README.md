# AIRobot
AIRobot ! From robots to maintenance service

* Blabla sur contexte
* Approche globale d'architecture

# Architecture overview
![](/Pictures/iRobotArchitecture.png?raw=true)
* Deep dive technique @edge

# Deep architecture design
![](/Pictures/iRobotArchitecture-DEEP%20ARCHITECTURE$.png?raw=true)

# Architecture "at the edge"

## Définition

### Réseau
Les robots équipés de leur capteurs ainsi que la gateway Azure IoT Edge sont situés dans le réseau privé de l'entreprise.
Pour simuler ce scénario, nous utilisons deux virtual networks dans Azure:
* un virtual network contenant l'ensemble des robots, simulés dans notre cas par une VM;
* un second virtual network contenant la gateway Azure IoT Edge.

Il est rare qu'un réseau local contenant des équipements critiques, tels que des robots d'une chaîne de montage ou production, soit ouvert sur Internet. Nous appliquons donc cette restriction au sous-réseau contenant notre VM "Robot" en interdisant tout trafic vers Internet.

Enfin, ces deux réseaux sont liés par un network peering afin que les robots puissent envoyer leurs données vers la gateway Azure IoT Edge. Seul le réseau de la garteway est autorisé à sortir sur Internet.

![](/Pictures/AIRobot%20Network.png?raw=true)

### Robots
Les robots de notre chaîne de production sont équipés de capteurs qui relèvent à intervalle régulier différentes mesures que l'on souhaite surveiller et historiser. Ces données sont envoyées directement à la gateway Azure IoT Edge.

Un tel robot est simulé dans notre exemple par une marchine virtuel Linux (Ubuntu) sur lequel est déployé un programme qui simule les différents capteurs et envoie les données au Hub de la gateway Azure IoT Edge.

### Gateway Azure IoT Edge
La gateway IoT Edge est une machine sur laquelle est déployée les différents composants d'Azure IoT Edge (Hub & Agent), ainsi que des modules supplémentaires suivant les besoins business et techniques.

Dans notre exemple, notre gateway est une marchine virtuelle Linux (Ubuntu) sur laquelle est déployée les modules suivants:
* IoT Edge Hub & Agent;
* SQL Edge;
* Azure Blob Storage for IoT Edge;
* Azure Functions (sous forme de module privé).

## Déploiement des ressources "at the edge" dans Azure

## Déploiement du simulateur des mesures du robot

## Configuration de la gateway Azure IoT Edge
- HubAgent
- Hub
- SQLEdge
- StorageEdge
- Custom module avec Azure Function

### Azure IoT Edge as Transparent Gateway

### Azure IoT Edge as Translation Gateway (si déploiement de SDK impossible, utilisation d'une translation Gateway (module de protocol et identity translation))
https://docs.microsoft.com/en-us/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2018-06


## Configuration du module SQL Edge
### Déploiement de la base de donées
### Création du streaming job
### Déploiement du modèle ONNX dans SQLEdge
https://docs.microsoft.com/fr-fr/azure/azure-sql-edge/deploy-onnx

## Ordonnancement des prédictions via Azure Function
### Création du compute Azure Function
- Pulling des tables de prédictions
- Dépôt sur le stockage edge de l'export
### Configuration du pulling (cron 0 * * * *)

## Configuration de la synchronisation du storage account avec le cloud

# Architecture "Cloud"
4. Deep dive technique cloud

## 4.1 Création et entrainement du modèle ML de prédiction des pannes

## 4.2 Création du compute Azure Function pour router la transaction (transaction builder)

## 4.3 Création de l'environnement Blockchain (POA) pour validation des transactions (Quorum)

## 4.4 Routage de l'informationd e transaction validée sur un event grid (Blockchain Data Manager)

## 4.5 Création de la CosmosDB et interaction avec Azure Function et Event Grid

## 4.6 Interconnexion avec ERP

## 4.7 Création de la PowerApps de maintenance



