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
#### Virtual networks, peering et network security groups
Les robots équipés de leur capteurs ainsi que la gateway Azure IoT Edge sont situés dans le réseau privé de l'entreprise.
Pour simuler ce scénario, nous utilisons deux virtual networks dans Azure:
* un virtual network contenant l'ensemble des robots, simulés dans notre cas par une VM;
* un second virtual network contenant la gateway Azure IoT Edge.

Il est rare qu'un réseau local contenant des équipements critiques, tels que des robots d'une chaîne de montage ou production, soit ouvert sur Internet. Nous appliquons donc cette restriction au sous-réseau contenant notre VM "Robot" en interdisant tout trafic vers Internet.

Enfin, ces deux réseaux sont liés par un network peering afin que les robots puissent envoyer leurs données vers la gateway Azure IoT Edge. Seul le réseau de la garteway est autorisé à sortir sur Internet.

![](/Pictures/AIRobot%20Network.png?raw=true)

#### DNS private zone
Les robots doivent pouvoir contacter la gateway Azure IoT Edge, soit directement par son adresse IP ou par une entrée DNS dans le réseau de l'entreprise.
Dans notre exemple, nous utilisons une zone Azure DNS privée pour router les requêtes faites à edge.corporate.lan vers la VM Azure IoT Edge.

#### Bastion
Le service Azure Bastion est déployé dans le réseau privé "simulé". L'utilisation de ce service est optionel et ne nous sert uniquement qu'à prendre rapidement la main sur nos VMs Azure de manière plus sécurisée.
Une connexion SSH directe reste possible (configuration réseau nécessaire), et sera sans doute utilisée dans le cas d'un déploiement réel dans l'entreprise.

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
Toute la partie réseau privé d'entreprise et ses machines, que l'on appelle ici "at the edge", est simulée dans Azure par souci de confort et d'efficacité.

Les modèles ARM suivants sont utilisables pour la déployer:
* `deployment.json`: Modèle général déployant toute la partie "at the edge" en faisant appel aux modèles liés ci-dessous;
* `deployment-network.json`: modèle contenant toutes les ressources réseau;
* `deployment-edge.json`: modèle correspondant à la VM Azure IoT Edge;
* `deployment-robot.json`: modèle de la VM simulant un robot;
* `deployment-resources.json`: modèle comprenant les ressources PaaS Azure requises au déploiement d'Azure IoT Edge (comme IoT Hub) et autres ressources en liant avec le déploiement "at the edge", comme le Storage Account de synchronisation des données.

Par simplicité, seul le modèle `deployment.json` peut être utilisé en lui fournissant les paramètres requis suivants ou en modifiant directement le fichier `deployment.parameters.json`.

| Paramètre | Description |
| --- | --- |
| TemplatesLocation | Adresse des modèles et modèles liés (ce repo) |
| NetworkTemplateLocationAccessToken | Token d'accès au repo GitHub (laisser vide si public) |
| EdgeTemplateLocationAccessToken | Idem |
| RobotTemplateLocationAccessToken | Idem |
| CloudResourcesTemplateLocationAccessToken | Idem |
| VirtualMachinAdminUsername | Nom du compte admin des VMs |
| VirtualMachineAdminPassword | Mot de passe du compte admin des VMs |

Tous les modèles sont disponibles dans le répertoire [ARM Templates](/ARM%20Templates) de ce repo.

### Déploiement (via Azure CLI)

Se loguer à Azure
```Shell
az login
```

Création d'un resource group de votre choix
```Shell
az group create -l <LOCATION> -n <RG_NAME>
```

Créer un déploiement utilisant le modèle ARM `deployment.json` et son fichier de paramètres.
```Shell
az deployment group create -n "AIRobotDeployment" --resource-group <RG_NAME> --template-file deployment.json --parameters deployment.parameters.json
```

Une fois le déploiement terminé, vous devriez obtenir le résultat suivant:
![](/Pictures/DeploymentResults.jpg?raw=true)

## Déploiement du simulateur des mesures du robot

## Configuration de la gateway Azure IoT Edge
La gateway Azure Iot Edge est une machine (physique ou virtuelle) sur laquelle nous déployons les modules suivants:
- Le runtime Azure IoT Edge;
- Les services IoT Edge Hub & Agent;
- SQL Edge;
- Azure Blob Storage Edge;
- Module personnalisé avec le runtime Azure Function.

### Déploiement du runtime Azure IoT Edge
Pour déployer le runtime Azure IoT Edge, nous devons d'abord déclarer notre gateway dans le service Azure IoT Hub, puis récupérer sa chaîne de connexion. Le runtime IoT Edge se connectera alors au service pour récupérer la configuration nécessaire de la gateway.

#### Déclaration de la gateway dans IoT Hub

La gateway doit être déclarée dans IoT Hub, via la commande suivante:
```Shell
az iot hub device-identity create --device-id AIRobotEdge --edge-enabled --hub-name <HUB_NAME>
```

> **Notes:** Vous serez amener à installer l'extension `azure-iot` à l'exécution de cette commande, si elle n'est pas déjà présente sur votre machine.

Récupération de la chaîne de connexion au service IoT Hub:
```Shell
az iot hub device-identity connection-string show --device-id AIRobotEdge --hub-name <HUB_NAME>
```

La commande vous retournera un résultat de la forme:
```JSON
{
  "connectionString": "HostName=<HUB_NAME>.azure-devices.net;DeviceId=AIRobotEdge;SharedAccessKey=xxx"
}
```

Noter la chaîne de connexion.

#### Création de la configuration pour la gateway dans IoT Hub
Nous allons définir ici la configuration de la gateway dans le service IoT Hub. C'est cette configuration que récupèrera le runtime Azure IoT Edge déployé sur la gateway, et à partir de laquelle le runtime exécutera les actions nécessaires.

La configuration consiste en:
- l'utilisation des deux modules systèmes obligatoire Hub & Agent, dans leur configuration par défaut;
- l'installation de SQL Edge (plan Premium) avec le compte `sa` et mot de passe `P@ssw0rd123!`, port 1433;
- l'installation de Blob Storage on IoT Edge.

##### Récupération de la chaîne de connexion vers le Storage Account dans Azure
Le service Blob Storage on IoT Edge réplique ses données dans le Storage Account créé dans Azure. Pour cela, nous devons récupérer la chaîne de connexion à ce service déployé précédemment.
```Shell
az storage account show-connection-string -n <STORAGE_ACCOUNT_NAME>
```
La commande vous retournera un résultat de la forme:
```JSON
{
  "connectionString": "<CHAÎNE DE CONNEXION>"
}
```

Noter la chaîne de connection.

##### Création de la configuration
La configuration de la gateway via Azure IoT Hub est définie par un fichier JSON de configuration.
Le fichier [EdgeConfiguration.json](/EdgeConfiguration.json) dans ce repo GitHub reprend la configuration nécessaire. Néanmoins, il doit être modifié avec les paramètres correspondant à votre déploiement:

| Paramètre | Description |
| --- | --- |
| <SQL_PASSWORD> | Mot de passe du compte `sa` de l'instance SQL Edge |
| <SA_EDGE_NAME> | Nom du compte Blob Storage on IoT Edge dans la gateway |
| <SA_EDGE_KEY> | Clé d'accès au Blob Storage on IoT Edge dans la gateway (base 64 string) |
| <STORAGE_ACCOUNT_CONNECTION_STRING> | Chaîne de connection au Storage Account dans Azure, récupéré ci-dessus. |

> **Notes:** <SQL_PASSWORD>, <SA_EDGE_NAME> et <SA_EDGE_KEY> sont des valeurs à générer par vos soins.

Une fois le fichier de configuration mis à jour avec vos paramètres, exécuter la commande suivante pour déployer la configuration dans IoT Hub.

```Shell
az iot edge set-modules --device-id AIRobotEdge --hub-name <IOT_HUB_NAME> --content EdgeConfiguration.json
```

#### Installer le runtime Azure IoT Edge sur la gateway
Le runtime Azure IoT Edge doit être déployé sur la gateway. Dans notre exemple, nous devons nous connecter à la VM `Edge` qui représente notre gateway Azure IoT Edge.

Une fois connectée à la VM via le service `Bastion` ou une connexion SSH, exécuter les commandes suivantes.

Configuration du repository pour notre VM (Ubuntu 18.04):
```Shell
curl https://packages.microsoft.com/config/ubuntu/18.04/multiarch/prod.list > ./microsoft-prod.list
```
Copie de la liste générée vers la liste des sources:
```Shell
sudo cp ./microsoft-prod.list /etc/apt/sources.list.d/
```
Installation de la clé GPG de Microsoft:
```Shell
curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
sudo cp ./microsoft.gpg /etc/apt/trusted.gpg.d/
```
Installation du moteur de container `moby-engine`
```Shell
sudo apt-get update
sudo apt-get install moby-engine
```

> **Notes:** Moby engine est le moteur de container recommandé pour Azure IoT Edge en production et est compatible avec les images Docker.

Installation du runtime Azure IoT Edge:
```Shell
sudo apt-get update
sudo apt-get install iotedge
```

Configuration de la connexion à IoT Hub:

La chaîne de connexion à IoT Hub récupérée précédemment doit être renseignée dans le fichier de configuration du runtime IoT Edge `config.yaml`.
```Shell
sudo nano /etc/iotedge/config.yaml
```

```YAML
# Manual provisioning configuration using a connection string
provisioning:
  device_connection_string: "<CHAÎNE DE CONNECTION IOT HUB>"
  dynamic_reprovisioning: false
```

Sauvegarder le fichier, puis redémarrer le runtime IoT Edge:
```Shell
sudo systemctl restart iotedge
```
Vérifier l'état de l'installation:
```Shell
systemctl status iotedge
```

Il est également possible de se rendre dans le portail Azure, puis dans le service Azure IoT Hub.
Dans la section `IoT Edge`, cliquer sur le device `AIRobotEdge`, et vérifier pour tous les modules que le statut de la colonne `Reported by Device` soit bien `Yes`.

#### Procédures complètes pour référence:
- [Installation runtime IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-install-iot-edge?view=iotedge-2018-06&tabs=linux);
- [Configuration IoT Edge avec clé symétrique](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-manual-provision-symmetric-key?view=iotedge-2018-06&tabs=azure-portal%2Clinux);
- [Déploiement de Azure SQL Edge](https://docs.microsoft.com/en-us/azure/azure-sql-edge/deploy-portal);
- [Déploiement de Azure Blob Storage on IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-blob?view=iotedge-2018-06).

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



