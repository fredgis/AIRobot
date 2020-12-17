# 1. Architecture introduction
Imaginez un monde où les robots (dixit robots industriels) effecturais eux-mêmes un diagnostique de leur état de santé et demanderai eux-mêmes une intervention de maintenance.

Cette approche, utopique il y a quelques années, s'avère bien plus réaliste aujourd'hui grâce aux "nouvelles technologies" et plus précisément services cloud.  En effet, les services "At the Edge", de "Machine Learning", de "Blockchain" et globalement d'infrastructure cloud permettent de s'approcher de telles réalisations.

Nous allons parcourir au gré de cet article les aspects suivants qui permettent la mise en place d'une telle approche:
- Déploiement d'un ensemble de services du cloud Azure @Edge
- Intégration d'un modèle de machine learning pour des prises de décisions "intelligentes"
- Remontée de ces informations dans le cloud
- Prise de décision déléguée au travers de services de Blockchain
- Planification automatique d'actes de maintenance de part l'intégration des décisions sur des applications low code (PowerApps)

L'idée globale du projet est donc la remontée de données depuis plusieurs capteurs d'un robot industriel sur une gateway @Edge qui, via un algorithme de machine learning embarqué, prendra la décision de remonter une future défaillance du système.

Nous parlons ici d'une défaillance non prédictible et non liée à une seule remontée de capteur. En effet, beaucoup de robots industriels embarquent désormais leurs propres algorithmes et recueil de données afin d'anticiper une panne sur une pièce et ainsi alerter les opérateurs en amont.

La mise en place d'une telle architecture va permettre de croiser les remontées de différents capteurs afin d'identifier un fonctionnement anormal du robot dans le temps, une dérive et globalement anticiper un disfonctionnement global très en amont de défaillances unitaires. 
C'est donc une approche globale sur de multiples capteurs non superviser qui visera une prise de décision sur une intervention / commande de matériel.

La remontée des informations se fait @Edge depuis un service Azure IOT Hub déployé @Edge. 
Les données sont ensuite routées dans une base Azure SQL Edge afin de les stocker. 

Un modèle de machine learning embarqué dans la base Azure SQL Edge (format ONNX) permet, sur une période définie (dans notre étude toutes les heures), d'analyser les envois des différents capteurs afin de détecter ou non un futur disfonctionnement.

Imaginez un robot de découpe qui envoi un certain nombre d'informations
- Vitesse de rotation
- Température de fonctionnement extérieur
- Température de la mèche de perçage ou lame de découpe
- Bruit émis par la machine en phase de coupe
- etc.

Tous ces capteurs remontent des informations dans la base Azure SQL Edge qui seront traitées par un modèle de machine learning (préalablement entrainé via un service tel que Azure Machine Learning Services). Si le modèle détecte une future interruption de service alors les données qui lui ont permis de modéliser cet état sont déposées sous la forme d'un fichier dans un Azure Storage déployé @Edge.

A partir du moment où le modèle détecte une dérive un ordre de maintenance doit être créé. 
Les données déposées dans le stockage @Edge sont alors automatiquement synchronisées sur un Azure Storage dans le cloud afin d'y être traitée.

Cette "décision" en mode autonome du robot d'établir un ordre de maintenance ou ordre de commande doit être validée et pour ne pas briser cette chaîne de décision automatique celle-ci va être déléguée à une architecture blockchain de type POA (Proof of Authority).

Cette architecture construite dans le cloud Azure en utilisant le service Azure Blockchain Services (sur protocole Ethereum/Quorum) ou via Azure AKS (Hyperledger) va donc devoir valider la demande provenant du "field", donc une décision prise en autonomie par le robot (et plus précisément la gateway qui lui est liée).

L'architecture blockchain est de type Quorum (POA) sur un algorithme de consensus qui oblige l'approbation de la transaction par plusieurs acteurs afin que celle-ci soit validée.
Cette approche est très efficace car ne demande pas beaucoup de ressources afin de valider la transaction.

Dans notre cas le consensus pourrai être:
- Noeud de consensus de l'usine héberfeant le robot
- Noeud de consensus du fabricant du robot
- Noeud de consensus de l'authorité de sécurité du robot
- etc.

Toutes ces autorités participent à la validation de la transaction d'ordre de maintenance et ont toutes validées un contrat "Smart contract" qui valide un certain nombre de règles lorsqu'une transaction est fournit en entrée de celui-ci.

Les différentes autorités ont un trust sur ce contrat, s'il s'avère exacte et donc que la transaction présentée (sous jacente de la remonté du modèle ML) est validée par chacun des noeuds composant le consensus alors la transaction est validée et écrite dans la blockchain. 

On trace donc non seulement le fait que cette transaction (image numéroque de l'ordre de maitenance) est valide mais on y adjoint l'ensemble du dataset qui a permis de prendre cette décision (pour potentiellement des besoins d'audit par un tiers).
Cette blockchain déployée est une blockchain privée, sécurisée, reposant sur du POA et donc sans interaction publique.

Dès que cette transaction est validée le service pousse une notification sur un Azure Grid qui permet de broadcaster à plusieurs services l'information.

Dans notre cas nous ferons les actions suivantes:
- Déclenchement d'une commande dans un ERP d'entreprise (Dynamics 365 !)
- Ecriture de l'ordre de maintenance dans une base NoSQL (Azure CosmosDB)
- Mise à disposition et notification de l'acteur de maintenance via une application terrain (PowerApps)

Le schéma d'architecture ci-après présente l'approche globable d'architecture.
![](/Pictures/iRobotArchitecture.png?raw=true)

# 2. Architecture détaillée
Il est maintenant temps de voir comment doit s'implémenter finement cette approche :)
Le schéma ci-après présente l'approche détaillée, chacun des bloc fera l'objet d'un chapitre vous présentant comment l'implémenter.

L'architecture est découpée en plusieurs blocs distincts qui dialoguent entre eux ou via des messages (évènements sur un bus de données), ou via flag (fichier dans un container).
![](/Pictures/iRobotArchitecture-DEEP%20ARCHITECTURE$.png?raw=true)

Nous pouvons résumer cette architecture en cinq blocs dinstincts:
#### Bloc de services déployés @Edge (1)

Les différents services Azure permettant la collecte de données depuis les capteurs positionnés sur le robot se feront directement sur une gateway associé au(x) robot(s) industriel(s).
Les services utilisés sont les suivants
- Le runtime Azure IoT Edge;
- Les services IoT Edge Hub & Agent;
- Azure SQL Edge;
- Azure Blob Storage Edge;
- Module personnalisé avec le runtime Azure Function.

Deux tables seront modélisées, l'une permettant d'intégrer l'ensembles des évènements provenant des sources, l'aure permettant d'exposer les résultats du modèle de machine learning embarqué.
L'intégration des données sera géré par le nouveau service de streaming contenu dans Azure SQL Edge. Ce service permet de créer des job de streaming permettant la capture temps réel d'évènement @edge et l'insertion directement en base de données.

#### Modèle de machine learning entrainé dans le cloud et déployé @edge (2)

#### <<<<<<<<<<< Courte description du modèle ML >>>>>>>>>>>
Ce modèle de machine learning est exporté au format ONNX et directement intégré dans une base/table Azure SQL Edge.
La nouvelle fonctionnalité PREDICT de Azure SQL Edge permettra d'appeler ce modèle depuis une procédure stockée afin, toutes les heures, d'étudier les évènements reçus afin de déterminer les risques d'anomalies au niveau du robot.

La périodicité du lancement sera géré depuis une Azure Function directement depuis un custom runtime embarqué dans la gateway @Edge.

#### Services de synchronisation dans le cloud Azure (3)
La résultante du modèle ML sera matérialisée dans une table SQL (dans Azure SQL Edge) puis traité par le hub d'évènement sur une route spécifique qui permettra l'export de l'information sur un fichier / flag dans le service Azure Storage Edge.

Ce service de stockage se syncrhonisera en automatique sur un service Azure Storage dans le cloud Azure qui sera le trigger d'une chaine de services permettant l'inégration de la transaction dans le système aval.

#### Création et validation de la transaction dans le cloud Azure via les services de Blockchain (4)
Une Azure function sera déclenchée à réception de l'évènement de trigger lié à la syncrhonisation du flag de déclenchement de la transaction.

Dès lors, une sous Azure Function sera utilisé comme transaction builder et va créer la transaction qui sera présenter à l'environnement blockchain.

L'infrastructure blockchain est de type privée, basé sur Quorum (Ethereum) ou Hyperledger permettant ainsi de répondre à une problématique de validation de transactions privées en POA (Proof of Authority).

L'utilisation d'un service tel que Azure Blockchain Service ou AKS Hyperledger permet, via la création d'une application smart contract, de valider la transaction issu du transaction builder.

Dès que celle-ci est validée le Azure Blockchain Data Manager intégré à Azure BLockchain Service permet de router l'information de validation (ainsi qu'un sous ensemble de propriétés) vers un Azure Event Grid afin de "broadcaster" la notification sur plusieurs systèmes dépendant de cette information tels que le système ERP, une fonction pour mise à jour de la base Azure CosmosDB etc.

#### Déclenchement de l'ordre de maintenance dans les systèmes ERP et applications (5)
L'information a été validé par toutes les entitées dans le service de blockchain.

Le système ERP est alors notifié et une transaction est déclenchée dans celui-ci.
L'information est aussi écrite dans une base Azure CosmosDB (configurée en serverless afin de ne pas engender de coûts quand il n'y a pas de problèmes remontés). Cette base est source d'une application liée à la maintenance des robots industriels. 

Cette application est développée en "low code" depuis le service Microsoft PowerApps et mis à disposition sur les smartphones des différentes techniciens d'interventions.

![](/Pictures/Archi%20bulletsShort.png?raw=true)

# 3. Architecture "at the edge"

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

# 4. Architecture "Cloud"
Deep dive technique cloud

### Création et entrainement du modèle ML de prédiction des pannes

### Création de l'environnement Blockchain (POA) pour validation des transactions (Quorum)
Afin de créer l'environnement Blockchain de validation des transactions de maintenance nous utiliserons le service encore en Preview Azure Blockchain Services.
Ce nouveau service permet de créer un environnement Blockchain privé sur protocole Quorum (basé du Ethereum) avec un consensus de type POA (Proof of Authority).

Le consensus de type POA permet a plusieurs noeuds de validations, donc autorités faisant parties d'un cercle de consensus, de valider une transaction émise sur la blockchain.
Vous pourrez trouver toutes les informations sur ce type de consensus sur le lien suivant : https://en.wikipedia.org/wiki/Proof_of_authority

Le service de Blockchain Microsoft permet de créer toute l'infrastructure de validation de transaction en mode PaaS (Platform as a Service). Vous trouverez toutes les informations ici : https://docs.microsoft.com/fr-fr/azure/blockchain/service/overview

Vous êtes libre d'utiliser le service Azure Blockchain service ou le template AKS Hyperledger pour réaliser la validation des transactions.
Le template AKS est disponible ici : https://docs.microsoft.com/fr-fr/azure/blockchain/templates/hyperledger-fabric-consortium-azure-kubernetes-service

Nous privilégons ici l'utilisation du service PaaS Azure Blockchain Service basé sur Quorum (Ethereum).
Quorum est un fork de go-ethereum, open source et toutes les informations sont disponibles ici : https://github.com/ConsenSys/quorum

Le déploiement va permettre la mise à disposition d'un noeud Blockchain.
![](/Pictures/Blockchain1.jpg?raw=true){ width=50% }

Chacune des parties prenante se verra attribuer sa propre architecture de validation, soir un consortium constitué de n noeuds de validation.
![](/Pictures/BlockchainConsortium.jpg?raw=true){ width=50% }

Afin de permettre la validation de nos transactions il nous faudra donc déployer
- Un environnement de blockchain provisionné dans le tenant Azure de la solution
- Un consortium créé avec plusieurs noeuds de validation
- Un smart contract développé et déployé sur la blockchain

Pour se faire il vous faudra vous connecter à l'aide de la commande az login et installer l'extension Blockchain.
```Shell
az login
az extension add --name blockchain
```

Vous pouvez maintenant créer un membre blockchain (membre unique que vous pourrez par la suite faire évoluer sur plusieurs noeuds de validation si les transactions augmentent)
```Shell
az blockchain member create \
                            --resource-group "MyResourceGroup" \
                            --name "myblockchainmember" \
                            --location "eastus" \
                            --password "strongMemberAccountPassword@1" \
                            --protocol "Quorum" \
                            --consortium "myconsortium" \
                            --consortium-management-account-password "strongConsortiumManagementPassword@1" \
                            --sku "Basic"
```
Afin de tester le bon fonctionnement de votre infrastructure vous pouvez maintenant tester la connexion sur votre blockchain privée en utilisant par exemple l'extension de navigateur Metamask.
Il vous faudra dans un premier temps récupérer la chaîne de connexion, vous la trouverez dans le portail sur le neoud de trabnsaction. Celle-ci est de la forme suivante.

```Shell
https://<your dns>.blockchain.azure.com:3200/<your access key>
```

Dès lors via l'extension Metamask et une connexion par RPC personnalisé vous pouvez accéder à votre réseau de blockchain privé et commencer le déploiement de "Smart Contract".
Un smart contrat est un contrat numérique immuable déployé sur la blockchain. Toutes les transactions devant être validées sur la blockchain doivent répondre aux exigences de ce smart contrat.

Dans une vision simplifiée un smart contract représente un ensemble de règles que doit respecter une transaction pour que celle-ci soit validée. Chacun des noeuds du consortium valide à son tour la transaction, si tous la valide alors elle est écrite dans la Blockchain de façon immuable.
Toutes les informations sur les smart contract ici : https://en.wikipedia.org/wiki/Smart_contract

Afin de déployer un smart contract vous pouvez utiliser Visual Studio Code avec l'extension "Azure Blockchain Development Kit" qui est compatible avec tout type de déploiement cloud et non cloud.
https://docs.microsoft.com/fr-fr/azure/blockchain/service/connect-vscode

Une autre solution est d'accéder directement à https://remix.ethereum.org dans votre navigateur.
La programmation se fait via Solidity et se présente comme suit. Vous pouvez y déployer vos règles de validation.

```Solidity
pragma solidity ^0.5.0;

contract simple {
    uint balance;
    constructor() public{
        balance = 0;
    }
    function add(uint _num) public {
        balance += _num;
    }
    function get() public view returns (uint){
        return balance;
    }
}
```
Dès lors que votre infrastructure est déployé ainsi que votre smart contract il est possible de le tester directement depuis par exemple une logic apps.
Pour se faire il vous faudra récupérer l'adresse de votre smart contract une dois déployé et l'entrée en paramètre dans la tâche logic apps.

Exemple de paramétrage.
![](/Pictures/LogicApps.jpg?raw=true)

#### Procédures complètes pour référence:
- [Création d'un noeud via Azure CLI](https://docs.microsoft.com/fr-fr/azure/blockchain/service/create-member-cli);
- [Création d'un noeud via un modèle ARM](https://docs.microsoft.com/fr-fr/azure/blockchain/service/create-member-template);
- [Se connecter à un noeud via Metamask](https://docs.microsoft.com/fr-fr/azure/blockchain/service/connect-metamask);
- [Création d'un smart contract via Visual Studio Code](https://docs.microsoft.com/fr-fr/azure/blockchain/service/send-transaction);
- [Gestion du consortium via PowerShell](https://docs.microsoft.com/fr-fr/azure/blockchain/service/manage-consortium-powershell);
- [Création d'une interface d'appel avec Azure Logic Apps](https://docs.microsoft.com/fr-fr/azure/blockchain/service/ethereum-logic-app).

### Création du compute Azure Function pour router la transaction (transaction builder)

### Routage de l'informationd e transaction validée sur un event grid (Blockchain Data Manager)

### Création de la CosmosDB et interaction avec Azure Function et Event Grid

### Interconnexion avec ERP

### Création de la PowerApps de maintenance



