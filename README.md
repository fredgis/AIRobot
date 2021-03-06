# AIRobot - la maintenance intelligente @Edge !

> **Notes:** Cet article est un mixte entre hands on et hackaton, nous espérons qu'il vous donnera une meilleure vision des possibilités industrielles de telles approches :)

## Contenu
- [Introduction à l'architecture et approche globale](https://github.com/fredgis/AIRobot#1-architecture-introduction);
- [Architecture technique détaillée](https://github.com/fredgis/AIRobot#2-architecture-d%C3%A9taill%C3%A9e);
- [@Edge : Réseau et sécurité](https://github.com/fredgis/AIRobot#virtual-networks-peering-et-network-security-groups);
- [@Edge : Déploiement des ressources Azure IOT Edge, Azure SQL Edge, Azure Function](https://github.com/fredgis/AIRobot#d%C3%A9ploiement-des-ressources-at-the-edge-dans-azure);
- [@Edge : Configuration de la gateway Azure IOT Edge](https://github.com/fredgis/AIRobot#configuration-de-la-gateway-azure-iot-edge);
- [@Edge : Azure IOT Edge comme Gateway Transparente](https://github.com/fredgis/AIRobot#azure-iot-edge-as-transparent-gateway);
- [@Edge : Déploiement du simulateur de robots](https://github.com/fredgis/AIRobot#d%C3%A9ploiement-du-simulateur-des-mesures-du-robot);
- [@Edge : Configuration du module Azure SQL Edge et Streaming Job](https://github.com/fredgis/AIRobot#configuration-du-module-sql-edge);
- [@Edge : Déploiement du modèle ONNX dans SQLEdge](https://github.com/fredgis/AIRobot#d%C3%A9ploiement-du-mod%C3%A8le-onnx-dans-sqledge);
- [@Edge : Prédictions via Custom Module IoT Edge](https://github.com/fredgis/AIRobot#pr%C3%A9dictions-via-custom-module-iot-edge);
- [@Cloud : Création du modèle de machine learning de détection d'anomalies](https://github.com/fredgis/AIRobot#mod%C3%A8le-de-machine-learning--deep-learning-pour-la-d%C3%A9tection-danomalie);
- [@Cloud : Création de l'environnement Blockchain (POA) pour validation des transactions (Quorum)](https://github.com/fredgis/AIRobot#cr%C3%A9ation-de-lenvironnement-blockchain-poa-pour-validation-des-transactions-quorum);
- [@Cloud : Routage de l'information de transaction validée sur un event grid](https://github.com/fredgis/AIRobot#routage-de-linformation-de-transaction-valid%C3%A9e-sur-un-event-grid);
- [@Cloud : Création de la CosmosDB et interaction avec Azure Function et Event Grid](https://github.com/fredgis/AIRobot#cr%C3%A9ation-de-la-cosmosdb-et-interaction-avec-azure-function-et-event-grid);
- [@Cloud : Création de la PowerApps de maintenance](https://github.com/fredgis/AIRobot#cr%C3%A9ation-de-la-powerapps-de-maintenance).

# 1. Architecture introduction
Imaginez un monde où les robots (dixit robots industriels) effectuaient eux-mêmes un diagnostic de leur état de santé et demanderaient eux-mêmes une intervention de maintenance.

Cette approche, utopique il y a quelques années, s'avère bien plus réaliste aujourd'hui grâce aux "nouvelles technologies" et plus précisément aux services cloud. En effet, les services "At The Edge", de "Machine Learning", de "Blockchain" et globalement d'infrastructure cloud permettent de s'approcher de telles réalisations.

Nous allons parcourir au gré de cet article les aspects qui permettent la mise en place d'une telle approche:
- Déploiement d'un ensemble de services du cloud Azure @Edge
- Intégration d'un modèle de machine learning pour des prises de décisions "intelligentes"
- Remontée de ces informations dans le cloud
- Prise de décision déléguée au travers de services de Blockchain
- Planification automatique d'actes de maintenance de par l'intégration des décisions sur des applications low code (PowerApps)

L'idée globale du projet est donc la remontée des données depuis plusieurs capteurs d'un robot industriel sur une gateway @Edge qui, via un algorithme de machine learning embarqué, prendra la décision de remonter une future défaillance du système.

Nous parlons ici d'une défaillance non prédictible et non liée à une seule remontée de capteur. En effet, beaucoup de robots industriels embarquent désormais leurs propres algorithmes et recueils de données afin d'anticiper une panne sur une pièce et ainsi alerter les opérateurs en amont.

La mise en place d'une telle architecture va permettre de croiser les remontées de différents capteurs afin d'identifier un fonctionnement anormal du robot dans le temps, une dérive et globalement anticiper un dysfonctionnement global très en amont des défaillances unitaires. 

C'est donc une approche globale sur de multiples capteurs non superviser qui visera une prise de décision sur une intervention / commande de matériel.

La remontée des informations se fait @Edge depuis un service Azure IOT Hub déployé @Edge. 
Les données sont ensuite routées dans une base Azure SQL Edge afin de les stocker. 

Un modèle de machine learning embarqué dans la base Azure SQL Edge (format ONNX) permet, sur une période définie (dans notre étude toutes les heures), d'analyser les envois des différents capteurs afin de détecter ou non un futur dysfonctionnement.

Imaginez un robot de découpe qui envoi un certain nombre d'informations
- Vitesse de rotation
- Température de fonctionnement extérieur
- Température de la mèche de perçage ou lame de découpe
- Bruit émis par la machine en phase de coupe
- etc.

Tous ces capteurs remontent des informations dans la base Azure SQL Edge qui seront traitées par un modèle de machine learning (préalablement entrainé via un service tel que Azure Machine Learning Service). 
Si le modèle détecte une future interruption de service alors les données qui lui ont permis de modéliser cet état sont déposées sous la forme d'un fichier dans un Azure Storage déployé @Edge.

A partir du moment où le modèle détecte une dérive un ordre de maintenance doit être créé. 
Les données déposées dans le stockage @Edge sont alors automatiquement synchronisées sur un Azure Storage dans le cloud afin d'y être traitées.

Cette "décision" en mode autonome du robot d'établir un ordre de maintenance ou ordre de commande doit être validée et pour ne pas briser cette chaîne de décision automatique celle-ci va être déléguée à une architecture blockchain de type POA (Proof of Authority).

Cette architecture construite dans le cloud Azure en utilisant le service Azure Blockchain Service (sur protocole Ethereum/Quorum) ou via Azure AKS (Hyperledger) va donc devoir valider la demande provenant du "field", donc une décision prise en autonomie par le robot (et plus précisément la gateway qui lui est liée).

L'architecture blockchain est de type Quorum (POA) sur un algorithme de consensus qui oblige l'approbation de la transaction par plusieurs acteurs afin que celle-ci soit validée.
Cette approche est très efficace car ne demande pas beaucoup de ressources afin de valider la transaction.

Dans notre cas le consensus pourrai être:
- Noeud de consensus de l'usine hébergeant le robot
- Noeud de consensus du fabricant du robot
- Noeud de consensus de l'autorité de sécurité du robot
- etc.

Toutes ces autorités participent à la validation de la transaction d'ordre de maintenance et ont toutes validées un contrat "Smart contract" qui valide un certain nombre de règles lorsqu'une transaction est fourni en entrée de celui-ci.

Les différentes autorités ont un trust sur ce contrat, s'il s'avère exacte et donc que la transaction présentée (sous-jacente de la remonté du modèle ML) est validée par chacun des noeuds composant le consensus alors la transaction est validée et écrite dans la blockchain. 

On trace donc non seulement le fait que cette transaction (image numérique de l'ordre de maintenance) est valide mais on y adjoint l'ensemble du dataset qui a permis de prendre cette décision (pour potentiellement des besoins d'audit par un tiers).
Cette blockchain déployée est une blockchain privée, sécurisée, reposant sur du POA et donc sans interaction publique.

Dès que cette transaction est validée le service pousse une notification sur un service Azure Event Grid qui permet de broadcaster à plusieurs services l'information.

Dans notre cas nous ferons les actions suivantes:
- Déclenchement d'une commande dans un ERP d'entreprise (D365 !)
- Ecriture de l'ordre de maintenance dans une base NoSQL (Azure CosmosDB)
- Mise à disposition et notification de l'acteur de maintenance via une application terrain (PowerApps)

Le schéma d'architecture ci-après présente l'approche globable d'architecture.
![](/Pictures/iRobotArchitecture.png?raw=true)

# 2. Architecture détaillée
Il est maintenant temps de voir comment doit s'implémenter finement cette approche :)
Le schéma ci-après présente l'approche détaillée, chacun des blocs fera l'objet d'un chapitre vous présentant comment l'implémenter.

L'architecture est découpée en plusieurs blocs distincts qui dialoguent entre eux ou via des messages (évènements sur un bus de données), ou via flag (fichier dans un container).
![](/Pictures/iRobotArchitecture-DEEP%20ARCHITECTURE$.png?raw=true)

Nous pouvons résumer cette architecture en cinq blocs distincts:
#### Bloc de services déployés @Edge (1)

Les différents services Azure permettant la collecte de données depuis les capteurs positionnés sur le robot se feront directement sur une gateway associé au(x) robot(s) industriel(s).
Les services utilisés sont les suivants
- Le runtime Azure IoT Edge;
- Les services IoT Edge Hub & Agent;
- Azure SQL Edge;
- Azure Blob Storage Edge;
- Module personnalisé avec le runtime Azure Function.

Deux tables seront modélisées, l'une permettant d'intégrer l'ensembles des évènements provenant des sources, l'autre permettant d'exposer les résultats du modèle de machine learning embarqué.
L'intégration des données sera gérée par le nouveau service de streaming contenu dans Azure SQL Edge. Ce service permet de créer des jobs de streaming permettant la capture temps réel d'évènement @edge et l'insertion directement en base de données.

#### Modèle de machine learning entrainé dans le cloud et déployé @edge (2)

Le développement de la solution de machine learning utilise le service Azure Machine Learning. Une fois le modèle développé, il est exporté sous ONNX. Open Neural Network Exchange est un moteur d'inférence haute performance, optimisé pour le cloud et les infrastructures at the edge. Le modèle utilisé est un autoencoder lstm (long short-term memory). Cette approche, basée sur les auto encodeurs et sur les réseaux de neurones récurrents, offre plusieurs avantages dans le cadre de la détection d'anomalie pour la flotte de robots :

* Algorithme pour la prévision de séries temporelles permettant de prendre en compte l'évolution des capteurs au cours du temps et non seulement l'état des capteurs à l'instant t.
* Algorithme se basant sur le fonctionnement nominal des robots et qui sera capable de détecter n'importe quel type d'anomalie dans le futur. Il n'y a donc pas besoin d'une base de données d'entraînement comportant des anomalies. C'est un algorithme de type non supervisé.

Plus précisément, l'entrée de l'algorithme reçoit la valeur des capteurs lors de la dernière heure à différents instants. L'auto encodeur se compose en deux parties, la phase d'encodage et de décodage. La phase d'encodage compacte l'information en un nombre de neurones inférieur au nombre de neurones de la couche d'entrée du réseau. La phase de décodage reconstruit l'entrée de l'algorithme. Le modèle apprend donc à reconstruite les données initiales en les faisant passer par un goulot d'étranglement. Une fois les données d'entrée reconstruites, on peut déterminer la présence ou non d'une anomalie en comparant les données d'entrée et de sortie.

Pour cela, il suffit de calculer l'erreur générée par les données. L'erreur correspond à la moyenne de la différence en valeur absolue terme à terme entre les données d'entrées et de sorties. Si cette erreur est supérieure à un seuil qui est préalablement fixé lors de la phase d'entrainement, alors le robot est en fonctionnement anormal. D'un point de vue du modèle, cela signifie que l'auto encodeur lstm n'a pas bien réussi à reconstruire les données d'entrée. Si cette erreur est inférieure à ce seuil, alors l'algorithme a suffisamment bien reconstruit l'entrée et cela signifie que le robot est en fonctionnement nominal.

Le modèle est développé à l'aide du service Azure Machine Learning. Dans un workspace, deux scripts sont créés. Le premier script permet de mettre en oeuvre le contexte d'exécution du modèle. Nous indiquons notamment la cible de calcul et nous mettons en place un environnement d'exécution. Nous définissons également une expérience qui va permettre de récupérer toutes les informations, les métriques et les graphiques générés lors de la phase d'entrainement. Ce script appelle le script d'entrainement qui charge les données, réalise le data preprocessing et entraine le modèle.

Ce modèle de machine learning est exporté au format ONNX et directement intégré dans une base/table Azure SQL Edge.

Nous voulions appeler le modèle intégré dans Azure SQL Edge via la fonction PREDICT (T-SQL). Mais les tableaux multi-dimensionnels ne sont pas supportés par la fonction PREDICT (voir citation ci-dessous). Or, notre modèle prédit des séries temporelles qui ont donc plusieurs dimensions. Il est indiqué qu’il faut que chaque entrée corresponde à une colonne de la table de scoring. Il faudrait modifier l’entrée du modèle ONNX ou passer par une fonction Python qui est difficilement convertible en ONNX.  Nous décidons finalement de créer un custom module IoT Edge sous forme de conteneur Docker qui embarque le runtime ONNX.

> The scoring data needs to be in the same format as the training data. Complex data types such as multi-dimensional arrays are not supported by PREDICT. So, for training make sure that each input of the model corresponds to a single column of the scoring table instead of passing a single array containing all inputs. 
> [Lien](https://docs.microsoft.com/fr-fr/azure/synapse-analytics/sql-data-warehouse/sql-data-warehouse-predict)

La périodicité du lancement sera géré depuis une Azure Function directement depuis un custom runtime embarqué dans la gateway @Edge.

#### Services de synchronisation dans le cloud Azure (3)
La résultante du modèle ML sera matérialisée dans une table SQL (dans Azure SQL Edge) puis traité par le hub d'évènement sur une route spécifique qui permettra l'export de l'information sur un fichier / flag dans le service Azure Storage Edge.

Ce service de stockage se synchronisera en automatique sur un service Azure Storage dans le cloud Azure qui sera le trigger d'une chaine de services permettant l'intégration de la transaction dans le système aval.

#### Création et validation de la transaction dans le cloud Azure via les services de Blockchain (4)
Une Azure function sera déclenchée à réception de l'évènement de trigger lié à la synchronisation du flag de déclenchement de la transaction.

Dès lors, une sous Azure Function sera utilisé comme transaction builder et va créer la transaction qui sera présenter à l'environnement blockchain.

L'infrastructure blockchain est de type privée, basé sur Quorum (Ethereum) ou Hyperledger permettant ainsi de répondre à une problématique de validation de transactions privées en POA (Proof of Authority).

L'utilisation d'un service tel que Azure Blockchain Service ou AKS Hyperledger permet, via la création d'une application smart contract, de valider la transaction issu du transaction builder.

Dès que celle-ci est validée le Azure Blockchain Data Manager intégré à Azure BLockchain Service permet de router l'information de validation (ainsi qu'un sous ensemble de propriétés) vers un Azure Event Grid afin de "broadcaster" la notification sur plusieurs systèmes dépendant de cette information tels que le système ERP, une fonction pour mise à jour de la base Azure CosmosDB etc.

#### Déclenchement de l'ordre de maintenance dans les systèmes ERP et applications (5)
L'information a été validé par toutes les entités dans le service de blockchain.

Le système ERP est alors notifié et une transaction est déclenchée dans celui-ci.
L'information est aussi écrite dans une base Azure CosmosDB (configurée en serverless afin de ne pas engendrer de coûts quand il n'y a pas de problèmes remontés). Cette base est source d'une application liée à la maintenance des robots industriels. 

Cette application est développée en "low code" depuis le service Microsoft PowerApps et mis à disposition sur les smartphones des différentes techniciens d'interventions.

![](/Pictures/Archi%20bulletsShort.png?raw=true)

# 3. Architecture "at the edge"
## Définition
### Réseau
#### Virtual networks, peering et network security groups
Les robots équipés de leurs capteurs ainsi que la gateway Azure IoT Edge sont situés dans le réseau privé de l'entreprise.
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
Le service Azure Bastion est déployé dans le réseau privé "simulé". L'utilisation de ce service est optionnel et ne nous sert uniquement qu'à prendre rapidement la main sur nos VMs Azure de manière plus sécurisée.
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

> **Notes:** Vous serez amené à installer l'extension `azure-iot` à l'exécution de cette commande, si elle n'est pas déjà présente sur votre machine.

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
Attention, plusieurs occurences de ces paramètres sont à remplacer dans le fichier.

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
- [Déploiement de Azure Blob Storage on IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-blob?view=iotedge-2018-06);
- [Store data at the edge with Azure Blob Storage on IoT Edge](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-store-data-blob?view=iotedge-2018-06).

### Azure IoT Edge as Transparent Gateway
Pour que nos robots dans leur réseau privé puissent communiquer avec notre gateway IoT Edge et lui envoyer leur télémétrie, nous devons configurer la gateway en mode "Transparent Gateway".

Les robots seront alors des "downstream devices" et enverrons leurs informations uniquement à la gateway. Quant à elle, elle acheminera les messages vers le service Azure IoT Hub dans Azure si connectivité, sinon gardera un historique des messages en mode "offline".
Ce mode de fonctionnement permet également de réaliser des analyses directement en local "at the edge", comme notamment appeler notre modèle de Machine Learning de détection d'anomalies.

Se connecter à la VM gateway IoT Edge via le service `Azure Bastion` ou autre.

Un certificat `root`, puis un certificat propre à la gateway `IoT Edge` et sa clé privée doivent être générés. Bien entendu, en production ces certificats doivent être générés via votre propre authorité de certification.
Dans cet article, nous allons générer des certificats auto-signés en suivant la procédure suivante:

[https://docs.microsoft.com/en-us/azure/iot-edge/how-to-create-transparent-gateway?view=iotedge-2018-06#set-up-the-device-ca-certificate](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-create-transparent-gateway?view=iotedge-2018-06#set-up-the-device-ca-certificate)

Une fois la génération des certificats terminée, vous devriez être en possession de:
- Root CA certificat appellé `azure-iot-test-only.root.ca.cert.pem`;
- Certficat CA de la gateway `iot-edge-device-ca-vm-airobot-edge-full-chain.cert.pem`;
- Et de la clé privé associée `iot-edge-device-ca-vm-airobot-edge.key.pem`.

>**Notes:** Le nom du certificat CA de la gateway dans cet exemple est `vm-airobot-edge` et peut être différent de celui que vous avez choisi.

>**Notes:** Par simplicité, ces certificats sont à disposition dans le répertoire `Certificates` de ce repo. Néanmoins, étant valables 30 jours, il est possible qu'ils soient expirés au moment de votre lecture.

Copier ces certificats sur la VM simulant la gateway IoT Edge.

Editer le fichier de config YAML de IoT Edge.
```Shell
sudo nano /etc/iotedge/config.yaml
```

Chercher la section `Certificate settings`, puis la décommenter. Renseigner la locatlisation des certificats copiés sur la gateway IoT Edge pour chaque type demandé:
- device_ca_cert: certificat de la gateway IoT Edge;
- device_ca_pk: la clé privée associée;
- trusted_ca_certs: certificat CA root.

Vous devriez obtenir un résultat similaire à celui-ci:

```YAML
certificates:
  device_ca_cert: "file:///home/crobin/certs/iot-edge-device-ca-vm-airobot-edge-full-chain.cert.pem"
  device_ca_pk: "file:///home/crobin/certs/iot-edge-device-ca-vm-airobot-edge.key.pem"
  trusted_ca_certs: "file:///home/crobin/certs/azure-iot-test-only.root.ca.cert.pem"
```

Editer également la valeur du paramètre `hostname` ainsi:
```YAML
hostname: "edge.corporate.lan"
```

>**Notes:** Il est important que la valeur de ce paramètre soit `edge.corporate.lan` car c'est cette valeur qui sera utilisée dans la chaîne de connexion des robots à la gateway IoT Edge. Si ces deux valeurs sont différentes, une erreur de connexion se produira.
Il est possible de spécifier une valeur différente de `edge.corporate.lan` afin de reflêter votre déploiement, en prenant soin de bien utiliser cette même valeur dans la chaîne de connexion des robots.

Sauvegarder les modifications puis redémarrer le service `IoT Edge`.

```Shell
sudo systemctl restart iotedge
```

#### Procédures complètes pour référence:
- [Configuration Transparent Gateway](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-create-transparent-gateway?view=iotedge-2018-06)

### Azure IoT Edge comme Translation Gateway
L'utilisation de la gateway IoT Edge comme Transparent Gateway nécessite que les robots, ou downstream devices, utilisent le SDK IoT Hub pour communiquer avec cette dernière.

Néanmoins, cela n'est toujours pas possible. Dans ce cas, la gateway IoT Edge doit être configurée comme `Translation Gateway`. Des développements spécifiques sont à prévoir.

>**Notes:** Dans le cadre de cet article, le simulateur de robot utilise le SDK IoT Hub.

#### Procédures complètes pour référence:
- [Configuration Translation Gateway](https://docs.microsoft.com/en-us/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2018-06)

## Déploiement du simulateur des mesures du robot
### Configuration Azure IoT Hub
Dans cet article, un robot du réseau privé d'entreprise est simulé par une VM sur laquelle un programme .NET Core est déployé qui simule et envoie des mesures télémétriques à la gateway IoT Edge.

Chaque robot doit être déclaré au préalable dans le service Azure IoT Hub.

Cet article simule un unique robot. Nous allons le déclarer dans Azure IoT Hub et récupérer sa chaîne de connexion depuis Azure CLI.

Depuis le shell de votre choix, se loguer à Azure.

```Shell
az login
```

Déclarer le device `AIRobot1`, enfant de la gateway IoT Edge `AIRobotEdge` configurée précédemment.

```Shell
az iot hub device-identity create -n <IOT_HUB_NAME> -d AIRobot1 --pd AIRobotEdge
```

Récupérer sa chaîche de connexion et la noter.

```Shell
az iot hub device-identity connection-string show --device-id AIRobot1 --hub-name <IOT_HUB_NAME>
```

Le résultat obtenu doit être de la forme:
```JSON
{
  "ConnectionString": "HostName=<IOT_HUB_NAME>.azure-devices.net;DeviceId=AIRobot1;SharedAccessKey=<ACCESS_KEY>"
}
```

Noter cette chaîne de connexion et lui ajouter `;GatewayHostName=edge.corporate.lan` de façon à obtenir une chaîne de la forme:

`
HostName=<IOT_HUB_NAME>.azure-devices.net;DeviceId=AIRobot1;SharedAccessKey=<ACCESS_KEY>;GatewayHostName=edge.corporate.lan
`

#### Procédures complètes pour référence:
- [Downstream Devices Authentication](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-authenticate-downstream-device?view=iotedge-2018-06)

### Déploiement du simulateur
Cet article simule les mesures d'un robot via un programme .NET Core dont les sources sont disponibles dans le répertoire [Src/RobotSimulator](/Src/RobotSimulator) dans ce repos.

Par simplicité, une build stand-alone pour Linux x64 est utilisable et disponible directement dans le répertoire [Builds/Linux](/Builds/Linux). Elle ne nécessite pas le déploiement préalable du runtime .NET Core sur la VM "Robot".

Se connecter à la VM `Robot1` via le service Azure Bastion ou autre méthode de votre choix.

>**Notes:** Suivant la méthode utilisée pour se connecter à la VM, l'ouverture des ports tels que 22 (entrant), 80 et 443 (sortant) peut être requise **temporairement** au niveau des règles du Network Security Group `nsg-vnet-airobot-private` afin de pouvoir copier / récupérer les différents fichiers et exécutables.

Copier l'exécutable `RobotSimulator` et son fichier de configuration `appsettings.json` dans `/usr/sbin/`.

Rendre le fichier `/usr/sbin/RobotSimulator` exécutable

```Shell
sudo chmod +x /usr/sbin/RobotSimulator
```

Copier le fichier `robotsimulator.service` dans `/etc/systemd/system/`.

Redémarrer le daemon systemd pour prendre en compte le nouveau service `robotsimulator.service`.
```Shell
sudo systemctl daemon-reload
```

Editer le fichier `/usr/sbin/appsettings.json`.
```Shell
sudo nano /usr/sbin/appsettings.json
```

Remplacer `<IOT HUB CONNECTION STRING>` par la chaîne de connexion du device `AIRobot1` récupérée précedemment sans oublier d'y ajouter l'attribut `;GatewayHostName=edge.corporate.lan` de façon à obtenir un résultat similaire à:

```JSON
{
    "EdgeHubConnectionString": "HostName=<IOT_HUB_NAME>.azure-devices.net;DeviceId=AIRobot1;SharedAccessKey=<ACCESS_KEY>;GatewayHostName=edge.corporate.lan",
    "IoTHubTimeoutInSec": 30
}
```

Sauvegarder les modifications apporter au fichier `/usr/sbin/appsettings.json`.

Copier le certificat CA root `azure-iot-test-only.root.ca.cert.pem` généré précédemment sur la VM.

Installer le certificat dans les certificats de l'OS:
```Shell
sudo cp azure-iot-test-only.root.ca.cert.pem /usr/local/share/ca-certificates/azure-iot-test-only.root.ca.cert.pem.crt

sudo update-ca-certificates
```

Lancer le simulateur:
```Shell
sudo systemctl start robotsimulator.service
sudo systemctl enable robotsimulator.service
```

Vérifier que le simulateur est bien connecté au Edge Hub et envoie bien la télémétrie:
```Shell
sudo systemctl status robotsimulator.service
```

#### Procédures complètes pour référence:
- [How to connect downstream device](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-connect-downstream-device?view=iotedge-2018-06)

## Configuration du module SQL Edge
### Déploiement de la base de données
Le module SQL Edge comporte une base de données dans laquelle sera déversé tous les événements envoyés par les robots. Pour rappel, les robots envoient leur données au IoT Hub, qui sont ensuite lus par un STREAM job SQL Edge pour les sauvegarder dans la table `dbo.Events` afin d'être traités par d'autres processus par la suite.

Se connecter à la VM simulant la gateway IoT Edge via le service `Azure Bastion` ou autre méthode de votre choix.

Prendre la main sur le conteneur `SQL Edge`:
```Shell
sudo docker exec -it AzureSQLEdge bash
```

Se connecter à l'instance SQL Edge via `sqlcmd`:
```Shell
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P <SQL_PASSWORD>
```

Nous allons créer un login `EdgeJob` qui sera utilisé par la suite par le STREAM job pour se connecter à la base et y déverser les événements reçus.
Nous allons en profiter pour créer la base de données `airobotedgedb` par la même occasion, ainsi que la table `dbo.Events` qui contiendra les données reçues et la table `dbo.Models` qui contiendra les modèles de prédiction ONNX.

```SQL
USE [master]
GO

--CREATE EdgeJob LOGIN
CREATE LOGIN [edgejob] WITH PASSWORD=N'P@ssw0rd123!'
GO

ALTER LOGIN [edgejob] DISABLE
GO

--CREATE [airobotedgedb] DATABASE
CREATE DATABASE [airobotedgedb]
GO

USE [airobotedgedb]
GO

CREATE USER [edgejob] FOR LOGIN [edgejob] WITH DEFAULT_SCHEMA=[dbo]
GO

CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'P@ssw0rd123!';

--CREATE [dbo].[Events] TABLE
CREATE TABLE [dbo].[Events](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Timestamp] [bigint] NOT NULL,
	[DrillingTemperature] [decimal](9, 5) NULL,
	[DrillBitFriction] [decimal](9, 5) NULL,
	[DrillingSpeed] [decimal](9, 5) NULL,
	[LiquidCoolingTemperature] [decimal](9, 5) NULL,
 CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- CREATE [dbo].[Models] TABLE
CREATE TABLE Models (
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Data] [varbinary](MAX) NULL,
	[Description] varchar(1000))
GO
```

Une fois la base de données et la table créées, il nous reste plus qu'à créer le STREAM job.

### Création du Streaming Job
Un stream job SQL Edge nécessite un source de données en `input` qui sera ici les données du IoT Hub Edge, et une sortie qui sera la table `dbo.Events`.

#### Création de l'input data source
```SQL
--Create an external file format of the type JSON.
CREATE EXTERNAL FILE FORMAT InputFileFormat
WITH 
(  
   format_type = JSON
)
GO

--Create an external data source for Azure IoT Edge hub
CREATE EXTERNAL DATA SOURCE EdgeHubInput 
WITH 
(
    LOCATION = 'edgehub://'
)
GO

--Create the external stream object for Azure IoT Edge hub.
CREATE EXTERNAL STREAM RobotSensors
WITH 
(
    DATA_SOURCE = EdgeHubInput,
    FILE_FORMAT = InputFileFormat,
	  LOCATION = N'RobotSensors',
    INPUT_OPTIONS = N'',
    OUTPUT_OPTIONS = N''
);
GO
```

>**Notes:** Noter la `LOCATION` de l'external stream `RobotSensors` créé. La valeur renseignée doit être égale à la valeur utilisée pour nomer l'input SQL Edge dans la route configurée dans le module Edge Hub.
>Pour rappel:
>```JSON
>"routes": {
>    "route": "FROM /messages/* INTO $upstream",
>    "RobotSensorsTopic": "FROM /messages/* INTO BrokeredEndpoint(\"/modules/AzureSQLEdge/inputs/>RobotSensors\")"
>}
>```

#### Création de l'ouput
```SQL
--Create the external stream object for local SQL Edge database.
CREATE DATABASE SCOPED CREDENTIAL SQLCredential
WITH IDENTITY = 'edgejob', SECRET = 'P@ssw0rd123!'
GO

CREATE EXTERNAL DATA SOURCE LocalSQLOutput
WITH 
(
    LOCATION = 'sqlserver://tcp:.,1433',
    CREDENTIAL = SQLCredential
)
GO

CREATE EXTERNAL STREAM EventsTableOutput
WITH 
(
    DATA_SOURCE = LocalSQLOutput,
    LOCATION = N'airobotedgedb.dbo.Events',
    INPUT_OPTIONS = N'',
    OUTPUT_OPTIONS = N''
);
GO
```

#### Création du STREAM job SQL Edge
Une fois l'input et l'ouput définit, le STREAM job peut être créé et démarré.

```SQL
--Create the streaming job and start it.
EXEC sys.sp_create_streaming_job @name=N'StreamingJob1',
	@statement= N'SELECT [Timestamp],
						 [drillingTemperature] AS [DrillingTemperature],
						 [drillBitFriction] AS [DrillBitFriction],
						 [drillingSpeed] AS [DrillingSpeed],
						 [liquidCoolingTemperature] AS [LiquidCoolingTemperature]
				  INTO [EventsTableOutput]
				  FROM [RobotSensors]'

exec sys.sp_start_streaming_job @name=N'StreamingJob1'
GO
```

>**Notes:** L'ensemble du script SQL est disponible dans le répertoire [Src/SQLEdge](/Src/SQLEdge) de ce repo.

### Déploiement du modèle ONNX dans SQLEdge

Si ce n'est pas déjà je cas, se connecter à la VM simulant la gateway IoT Edge via le service `Azure Bastion` ou autre méthode de votre choix.

Prendre la main sur le conteneur `SQL Edge`:
```Shell
sudo docker exec -it AzureSQLEdge bash
```

Télécharger les deux modèles ONNX présents dans ce repo via la méthode de votre choix. Ici, par exemple, nous utilisons l'utilitaire `wget`.

>**Notes:** Le téléchargement d'éléments depuis Internet peut nécessiter l'ouverture de flux adéquats dans le Network Security Group `nsg-vnet-airobot-edge`.

```Shell
cd /var/opt/mssql
wget https://raw.githubusercontent.com/fredgis/AIRobot/main/Src/Models/model_final.onnx
wget https://raw.githubusercontent.com/fredgis/AIRobot/main/Src/Models/pipeline_std.onnx
```

Une fois téléchargés, il ne reste plus qu'à les insérer dans la table `dbo.Models`.

Se connecter à l'instance SQL Edge via `sqlcmd`:
```Shell
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P <SQL_PASSWORD>
```

Insérer les modèles:
```SQL
INSERT INTO dbo.Models ([Description], [Data]) SELECT N'model_final.onnx', * FROM OPENROWSET(BULK N'/var/opt/mssql/model_final.onnx', SINGLE_BLOB) AS [Model]

INSERT INTO dbo.Models ([Data]) SELECT N'pipeline_std.onnx', * FROM OPENROWSET(BULK N'/var/opt/mssql/pipeline_std.onnx', SINGLE_BLOB) AS [Model]
```

## Prédictions via Custom Module IoT Edge
Les modèles de types ONNX déployés dans le module SQL Edge peuvent être exécutés par le mot-clé T-SQL `PREDICT`.
Néanmoins, dans notre exemple les modèles nécessitent en paramètre un jeu de donnés à deux ou trois dimensions. Le runtime ONNX inclu dans SQL Edge supporte des paramètres à une dimension à la date de rédaction de cet article.

De ce fait, nous appliquons les modèles ONNX grâce à un custom module IoT Edge dont la source est disponible dans le répertoire [Src/ModelsRuntime](/Src/ModelsRuntime).
Il consiste en un Docker container embarquant un programme .NET Core 3.1, le runtime ONNX et utilise les APIs ONNX pour C#.

Toutes les minutes, l'utilitaire requête la table `dbo.Events` du SQL Edge pour récupérer les 60 événements les plus vieux à traiter, un événement par seconde, puis applique le modèle de standardisation des données et ensuite le modèle de prédiction des données pour enfin calculer l'erreur entre les deux jeux de données obtenus.
En cas d'erreur supérieur à un seuil, ici 0.9, le score et le jeu de données concerné sont sauvegardés sur le Blob Storage de l'IoT Edge puis répliqué dans le cloud pour de potentiels futurs traitements.

Ce module de prédiction est automatiquement déployé sur la gateway IoT Edge grâce au fichier de configuration utilisé précédemment.

### Construction du Custom Module IoT Edge
L'image Docker du Custom Module IoT Edge est cons truite à partir d'une image Ubuntu 18.04 à laquelle est ajoutée le runtime .NET Core 3.1, la dépendance `libgomp1` nécessaire au runtime ONNX pour Linux, ainsi que le package de l'utilitaire en .NET Core qui applique les modèles ONNX aux données reçues dans Edge Hub.

```Dockerfile
FROM ubuntu:18.04

RUN apt-get update
RUN apt-get --assume-yes install wget

RUN wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb

RUN apt-get update; \
    apt-get --assume-yes install -y apt-transport-https && \
    apt-get update && \
    apt-get --assume-yes install -y dotnet-runtime-3.1

RUN apt-get --assume-yes install p7zip

RUN apt-get install libgomp1

WORKDIR /home
RUN mkdir afpredict
WORKDIR /home/afpredict
RUN wget https://raw.githubusercontent.com/fredgis/AIRobot/main/Builds/ModelsRuntime/Linux-x64/publish.7z
RUN 7zr e publish.7z
RUN rm publish.7z

RUN apt-get --assume-yes remove p7zip
RUN apt-get --assume-yes remove wget
RUN apt-get clean

CMD ["dotnet", "afpredict.dll"]
```

### Liens vers document de références
- [ONNX Runtime C# API](https://www.onnxruntime.ai/docs/reference/api/csharp-api.html)

# 4. Architecture "Cloud"
### Modèle de Machine Learning / Deep Learning pour la détection d'anomalie 

Cet article présente l'algortihme de machine learning / deep learning utilisé pour détecter les anomalies des robots de la flotte.

#### Introduction : 
Pour notre étude, nous voulons mettre en oeuvre un algorithme de machine learning / deep learning en python permettant de détecter les anomalies sur des robots dans le cadre d'une maintenance prédictive. Nous aborderons dans la suite le modèle que nous avons choisi pour détecter les anomalies des robots. Nous décrirons l'ensemble des étapes de développement de l'algorithme puis nous finirons par expliquer la démarche générale de mise en production du modèle sous ONNX.

#### Choix du modèle :
Pour faire de la détection d'anomalie, nous avons à notre disposition plusieurs algorithmes. 
En plus des approches classiques de classification ou de clustering, des algorithmes spécifiques à la détection d'anomalie comme le One-Class SVM ou l'Isolation Forest peuvent s'avérer efficace. 

Des méthodes statistiques telles que ARIMA permettent également de faire de la détection d'anomalie (sur des séries temporelles). 

Dans notre étude, à la vue des données dont nous disposons et des résultats que nous souhaitons obtenir, nous réalisons un autoencodeur lstm. Cette approche est différente des algorithmes de détection d'anomalie classique. Mais elle offre plusieurs avantages. 
D'abord, c'est un algorithme de séries temporelles permettant de prendre en compte l'évolution des capteurs au cours du temps pour détecter une anomalie. 
Ensuite, la base de données doit seulement comporter des données sur le fonctionnement nominal du robot. 

Le modèle dira s'il est dans un état nominal ou s'il y a une anomalie. Il sera alors possible de détecter n'importe quel type d'anomalie (contrairement aux algorithmes ci-dessus qui détecteront les anomalies présentes dans la base de données d'entrainement). Par conséquent, la base n'a pas besoin d'être annotée ni de comporter un historique des pannes. 

C'est une approche plus générale d'un problème de maintenance prédictive.

#### Préparation des données :
Notre dataset comporte quatre features décrivant la vitesse de perçage, la température de perçage, le frottement du foret et la température de l'eau de refroidissement de nos robots au cours du temps. 

A partir de ces données, nous allons construire un autoencodeur lstm. Nous n'avons pas de données manquantes. 
Une sélection de variables ou du feature enginieering ne sont pas des étapes utiles dans notre cas. Il faut cependant procéder à une normalisation des données. 
La méthode StandardScaler de Scikit Learn est utilisée ici. 

La deuxième étape de preprocessing est la mise en forme des données sous la forme de séries temporelles pour l'entrainement. Pour cela, on utilise la fonction suivante permettant de créer des blocs de (60x4) correspondant à une série temporelle (60 périodes de temps avec les quatre features du dataset. X_final est donc un array de N blocs de (60x4).

```python
def mise_en_serie_temporelle(X):
    periode = 60
    X_final = []
    for i in range(len(X)-periode-1):
        t = []
        for j in range(1,periode+1):
            t.append(X[[(i+j+1)], :])
        X_final.append(t)
    X_final = np.array(X_final)
    X_final = X_final.reshape(X_final.shape[0],periode,4)
    return X_final
```

#### Modèle :
Les données sont maintenant prêtes pour l'autoencodeur lstm. On dispose de données d'entrainement et de validation. Voici le modèle :

```python
#Définition du modèle
model = Sequential()
model.add(LSTM(180,  activation = 'relu', input_shape=(timesteps,n_features), return_sequences = True))
model.add(Dropout(d))
model.add(LSTM(120,   activation = 'relu', return_sequences = False))
model.add(Dropout(d))
model.add(RepeatVector(n = timesteps))
model.add(LSTM(120,   activation = 'relu', return_sequences=True))
model.add(Dropout(d))
model.add(LSTM(180,  activation = 'relu', return_sequences=True))
model.add(TimeDistributed(Dense(n_features)))
```
Un autoencodeur doit reconstruire l'entrée qui lui est fournie en passant d'abord par un encodeur puis par un décodeur. L'entrée est de dimension 3 (Nb de séries temporelles, Nb de période de temps (60), nombre de features (4)). 
C'est un modèle de type séquentiel avec deux couches cachées pour l'encodeur et le décodeur. On ajoute en premier lieu une couche de type LSTM avec 180 neurones puis une seconde couche LSTM de 120 neurones. Ces deux couches constituent la partie encodage de l'autoencodeur. 

La fonction RepeatVector réalise la transition entre la partie encodage et décodage. Elle transforme les données de dimension 2 qu'elle reçoit en entrée en 3 dimensions. Ensuite, on a la partie décodage avec deux layers LSTM de 120 et 180 neurones par symétrie avec l'encodage (par définition de l'autoencodeur). 

Enfin, on a une dernière couche qui permet de transformer l'entrée reçue de dimension 3 (Nb de séries, 60, Nombre de neurones de la couche (180)) en une entrée de dimension 3 (Nb de séries, 60, 4) correspondant à l'entrée de l'algorithme. 

On retrouve donc bien l'entrée de l'algorithme après avoir fait passer les données dans différentes couches, ce qui définit le principe de l'autoencodeur. Le dropout sert à prevenir l'overfitting.
Il ne reste plus qu'à compiler le modèle puis à l'entrainer. On utilise l'optimisateur Adam car il offre les meilleurs résultats ainsi que la fonction de coût mean_absolute_error, on y reviendra dans une prochaine section.

```python
#Compilation du modèle
adam = optimizers.Adam()
model.compile(loss='mae', optimizer=adam)

#Entrainement du modele
history = model.fit(X_train_std, X_train_std, 
                     validation_data=(X_valid_std,X_valid_std),
                     epochs=epochs, 
                     batch_size=batch)
```

#### Evaluation / Seuil d'erreur :
Le modèle est entrainé à reconstruire les données générées par le robot en fonctionnement normal. 
L'objectif du modèle ci-dessus et d'attribuer une erreur de reconstruction à chaque série temporelle passée en entrée. C'est pour cela que la fonction de coût choisie est la mean_absolute_error mesurant l'erreur moyenne de la différence en valeur absolue entre les données d'entrées et de sorties. 

Une erreur faible signifie que le modèle est bien arrivé à reconstruire l'entrée donnée à l'algorithme, le robot est donc en fonctionnement normal. 
Une erreur élevée signifie que le modèle a rencontré des données éloignées des données normales sur lesquels il a été entrainé. 

Ces nouvelles données signifient donc que le robot est en fonctionnement anormal. Une erreur est faible si elle est inférieure à un seuil d'erreur et élevée si elle est supérieure à ce même seuil. Plus l'erreur est éloignée de ce dernier et plus le fonctionnement du robot est anormal. 
Déterminons graphiquement le seuil associé à notre modèle. 

Le graphique qui suit représente l'erreur de chaque série lors de la phase d'entrainement, c'est-à-dire en fonctionnement normal du robot. L'erreur de reconstruction maximale de la phase d'entrainement est inférieure à 0,9. C'est donc le seuil qui va nous servir à déterminer si les nouvelles données exploitées représentent le fonctionnement normal ou non du robot.
![](/Pictures/Plot_erreur_train_data_1609258385.png?raw=true)

#### Préparation de la mise en production sous ONNX :
Nous disposons de deux étapes différentes dans notre algorithme. Le prétraitement des données puis le modèle. 
Nous mettons dans un pipeline la standardisation des données puis nous convertissons cette pipeline en modèle ONNX à l'aide de la commande convert_sklearn du module skl2onnx. Pour le modèle, nous utilisons la commande convert_keras du module keras2onnx pour convertir le modèle en ONNX. 

Le script ci-dessus montre comment utiliser les deux fichiers ONNX. 

#### Exemple de prédictions sur des nouvelles données:
Prenons maintenant de nouvelles données et voyons si le robot est en fonctionnement anormal.
Nous utilisons le script ci-dessous pour tester les modèles et les nouvelles données

```python
#data est un tableau de (60x4)
sess1 = rt.InferenceSession("pipeline_std.onnx")
pred_onnx_std = sess1.run(None, {sess1.get_inputs()[0].name: data})[0]

batch = np.array(pred_onnx_std,ndmin=3)

sess2 = rt.InferenceSession("model_final.onnx")
pred_onnx = sess2.run(None, {sess2.get_inputs()[0].name: batch})[0]

def erreur(a,b):
    err = 0
    for i in range(a.shape[0]):
        for j in range(a.shape[1]):
            err = err + abs(a[i][j] - b[i][j])
    err = err / (a.shape[0] * a.shape[1])        
    return err

print(erreur(pred_onnx_std,pred_onnx[0]))
```
Une importation des deux modèles est effectuée. Nous faisons passer les nouvelles données dans les modèles puis nous finissons par calculer l'erreur grâce à la fonction erreur. 

Si le résultat est inférieur au seuil, alors le robot est en fonctionnement normal sinon il est en fonctionnement anormal. 
Sur des données de plusieurs jours, on calcule l'erreur toutes les minutes. Les résultats sont résumés sur le graphique ci-dessous. On s'aperçoit qu'il y a la détection d'une anomalie entre les séries temporelles 4000 et 9000. Le fonctionnement du robot est normal le reste du temps. 
![](/Pictures/Prediction_test_data_1609258462.png?raw=true)

#### Conclusion :
Le développement de ce modèle s'est déroulé en plusieurs étapes. Après le traitement des données, l'autoencodeur lstm est créé, entrainé puis une recherche des meilleurs hyperparamètres est effectuée. L'algorithme est ensuite exporté au format ONNX. Il est maintenant prêt à ingérer de nouvelles données afin de déterminer si le robot est en fonctionnement normal ou non.

### Création de l'environnement Blockchain (POA) pour validation des transactions (Quorum)
Afin de créer l'environnement Blockchain de validation des transactions de maintenance nous utiliserons le service encore en Preview Azure Blockchain Services.
Ce nouveau service permet de créer un environnement Blockchain privé sur protocole Quorum (basé du Ethereum) avec un consensus de type POA (Proof of Authority).

Le consensus de type POA permet à plusieurs noeuds de validations, donc autorités faisant parties d'un cercle de consensus, de valider une transaction émise sur la blockchain.
Vous pourrez trouver toutes les informations sur ce type de consensus sur le lien suivant : [Proof of Authority](https://en.wikipedia.org/wiki/Proof_of_authority)

Le service de Blockchain Microsoft permet de créer toute l'infrastructure de validation de transaction en mode PaaS (Platform as a Service). Vous trouverez toutes les informations ici : [Azure Blockchain Service](https://docs.microsoft.com/fr-fr/azure/blockchain/service/overview)

Vous êtes libre d'utiliser le service Azure Blockchain service ou le template AKS Hyperledger pour réaliser la validation des transactions.
Le template AKS est disponible ici : [AKS Template](https://docs.microsoft.com/fr-fr/azure/blockchain/templates/hyperledger-fabric-consortium-azure-kubernetes-service)

Nous privilégons ici l'utilisation du service PaaS Azure Blockchain Service basé sur Quorum (Ethereum).
Quorum est un fork de go-ethereum, open source et toutes les informations sont disponibles ici : [GitHub Quorum](https://github.com/ConsenSys/quorum)

Le déploiement va permettre la mise à disposition d'un noeud Blockchain.
![](/Pictures/Blockchain-MEMBER.png?raw=true)

Chacune des parties prenantes se verra attribuer sa propre architecture de validation, soir un consortium constitué de n noeuds de validation.
![](/Pictures/Blockchain-CONSORTIUM.png?raw=true)

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
Il vous faudra dans un premier temps récupérer la chaîne de connexion, vous la trouverez dans le portail sur le noeud de transaction. Celle-ci est de la forme suivante.

```Shell
https://<your dns>.blockchain.azure.com:3200/<your access key>
```

Dès lors via l'extension Metamask et une connexion par RPC personnalisé vous pouvez accéder à votre réseau de blockchain privé et commencer le déploiement de "Smart Contract".
Un smart contrat est un contrat numérique immuable déployée sur la blockchain. Toutes les transactions devant être validées sur la blockchain doivent répondre aux exigences de ce smart contrat.

Dans une vision simplifiée un smart contract représente un ensemble de règles que doit respecter une transaction pour que celle-ci soit validée. Chacun des noeuds du consortium valide à son tour la transaction, si tous la valide alors elle est écrite dans la Blockchain de façon immuable.
Toutes les informations sur les smart contract ici : [Wikipedia Smart Contract](https://en.wikipedia.org/wiki/Smart_contract)

Afin de déployer un smart contract vous pouvez utiliser Visual Studio Code avec l'extension "Azure Blockchain Development Kit" qui est compatible avec tout type de déploiement cloud et non cloud.
[Connexion avec Visual Studio Code](https://docs.microsoft.com/fr-fr/azure/blockchain/service/connect-vscode)

Une autre solution est d'accéder directement à [Remix](https://remix.ethereum.org) dans votre navigateur.
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
Pour se faire il vous faudra récupérer l'adresse de votre smart contract une fois déployé et l'insérer en paramètre dans la tâche logic apps.

Exemple de paramétrage.
![](/Pictures/LogicApps.jpg?raw=true)

Il est maintenant possible de créer son propre transaction builder en appelant depuis une Azure Function ou la logic apps préalablement créé enrichie de tous les paramètres attendus ou directement d'interagir avec l'infrastructure Blockchain.
La logique étant qu'à la réception du flag (fichier) synchronisé depuis le Azure Storage Edge vers le Azure Storage Cloud celui-ci active le trigger d'une Azure Function et déclenche alors le process d'interaction avec la Blockchain.

#### Procédures complètes pour référence:
- [Création d'un noeud via Azure CLI](https://docs.microsoft.com/fr-fr/azure/blockchain/service/create-member-cli);
- [Création d'un noeud via un modèle ARM](https://docs.microsoft.com/fr-fr/azure/blockchain/service/create-member-template);
- [Se connecter à un noeud via Metamask](https://docs.microsoft.com/fr-fr/azure/blockchain/service/connect-metamask);
- [Création d'un smart contract via Visual Studio Code](https://docs.microsoft.com/fr-fr/azure/blockchain/service/send-transaction);
- [Gestion du consortium via PowerShell](https://docs.microsoft.com/fr-fr/azure/blockchain/service/manage-consortium-powershell);
- [Création d'une interface d'appel avec Azure Logic Apps](https://docs.microsoft.com/fr-fr/azure/blockchain/service/ethereum-logic-app).

### Routage de l'information de transaction validée sur un event grid

Une fois l'information validée dans la blockchain il est nécessaire de router cette information, tout du moins de notifier les systèmes sous-jacents de cette validation pour déclencher des actions dans d'autres systèmes.
Cette étape est par exemple réalisable vie le service Azure Blockchain Data Manager qui permet de router l'information de validation sur par exemple un Azure Event Grid.

Blockchain Data Manager capture, transforme et fournit des données de transaction Azure Blockchain Service aux rubriques Azure Event Grid proposant une intégration de registre blockchain évolutive et fiable aux services Azure.

Le principe est donc de s'abonner à un smart contract et à la validation d'une transaction un message / notification est postée sur le service Azure Event Grid.
![](/Pictures/AzureBlockchainDataManager.jpg?raw=true)

Une instance de Blockchain Data Manager surveille un nœud de transaction Azure Blockchain Service. Une instance capture toutes les données de bloc brut et de transaction brute à partir du nœud de transaction. Blockchain Data Manager publie un message RawBlockAndTransactionMsg qui est un sur-ensemble des informations retournées par les requêtes web3.eth [getBlock](https://web3js.readthedocs.io/en/v1.2.0/web3-eth.html#getblock) et [getTransaction](https://web3js.readthedocs.io/en/v1.2.0/web3-eth.html#gettransaction).

Dans notre cas il s'agira de créer une entrée sur le cluster Blockchain préalablement créé.

```Shell
az resource create \
                   --resource-group <Resource group> \
                   --name <Input name> \
                   --namespace Microsoft.Blockchain \
                   --resource-type inputs \
                   --parent watchers/<Watcher name> \
                   --is-full-object \
                   --properties <input resource properties>
```

Une fois la connexion en entrée créée il s'agit maintenant de créer une connexion de sprtie sur une rubrique d'un service Azure Event Grid.
Une connexion sortante envoie des données blockchain à Azure Event Grid. Vous pouvez envoyer des données blockchain à une ou plusieurs destinations. Blockchain Data Manager prend en charge plusieurs connexions sortantes de rubrique Event Grid pour une instance de Data Manager Blockchain donnée.

```Shell
az resource create \
                   --resource-group myRG \
                   --name myoutput \
                   --namespace Microsoft.Blockchain \
                   --resource-type outputs \
                   --parent watchers/mywatcher \
                   --is-full-object \
                   --properties '{"location":"eastus","properties":{"outputType":"EventGrid","dataSource":{"resourceId":"/subscriptions/<Subscription ID>/resourceGroups/<Resource group>/providers/Microsoft.EventGrid/topics/<event grid topic>"}}}'
```

La dernière étape consiste à ajouter l'application.
Si vous ajoutez une application blockchain, Blockchain Data Manager décode l’état de l’événement et de la propriété pour l’application. Dans le cas contraire, seules les données de bloc brut et de transaction brute sont envoyées. Blockchain Data Manager détecte également les adresses de contrat lors du déploiement du contrat. Vous pouvez ajouter plusieurs applications blockchain à une instance Blockchain Data Manager.

```Shell
az resource create \
                   --resource-group <Resource group> \
                   --name <Application name> \
                   --namespace Microsoft.Blockchain \
                   --resource-type artifacts \
                   --parent watchers/<Watcher name> \
                   --is-full-object \
                   --properties <Application resource properties>
```

Il est maintenant temps de démarrer l'instance.
Lors de son exécution, une instance Blockchain Manager surveille les événements Blockchain à partir des entrées définies et envoie des données aux sorties définies.

```Shell
az resource invoke-action \
                          --action start \
                          --ids /subscriptions/<Subscription ID>/resourceGroups/<Resource group>/providers/Microsoft.Blockchain/watchers/<Watcher name>
```

#### Procédures complètes pour référence:
- [Utiliser Blockchain Data Manager pour envoyer des données à Azure Cosmos DB](https://docs.microsoft.com/fr-fr/azure/blockchain/service/data-manager-cosmosdb);
- [Configurer Data Manager](https://docs.microsoft.com/fr-fr/azure/blockchain/service/data-manager-cli);

### Création de la CosmosDB et interaction avec Azure Function et Event Grid
Azure Cosmos DB serverless vous permet d’utiliser votre compte Azure Cosmos sur la base de la consommation ; dans ce cas, vous êtes facturé uniquement pour les unités de requête consommées par vos opérations de base de données et le stockage consommé par vos données. L’utilisation d’Azure Cosmos DB en mode serverless n’implique pas de frais minimum.

Dans notre architecture la base de données NoSQL; Azure CosmosDB, pourrai être utilisée en mode complètement à la demande, celle-ci étant accédée uniquement lors d'un appel en consultation via l'application de maintenance ou quand une transaction a été validée dans la Blockchain, donc écrite comme ordre de maintenance.

L'article suivant vous permettra de mieux comprendre les différents modes et de faire le bon choix: [Comment choisir entre le mode débit approvisionné et le mode serverless](https://docs.microsoft.com/fr-fr/azure/cosmos-db/throughput-serverless).

Les conteneurs serverless exposent les mêmes fonctionnalités que les conteneurs créés en mode de débit approvisionné, ce qui vous permet de lire, d’écrire et d’interroger vos données exactement de la même façon. Toutefois, les comptes et les conteneurs serverless ont également des caractéristiques spécifiques.

L'approche de création est donc très standard :
- Créer un compte Azure Cosmos
- Ajouter ou supprimer des régions
- Activer les écritures multirégions
- Créer le coneneur associé à l'application.

Toutes les informations de création :  [Gérer les ressources de l’API Azure Cosmos DB Core (SQL) à l’aide d’Azure CLI](https://docs.microsoft.com/fr-fr/azure/cosmos-db/manage-with-cli);

Afin d'envoyer les informations depuis le service Azure Event Grid vers la base Azure CosmosDB nous pouvons utiliser une Azure Logicapps (ou une Azure Function).
Celle-ci est configurée avec une connexion sur la grille d'évènement en entrée ainsi qu'une tâche de création de document en sortie.
Cette fonction (Loicapps) sera donc appelée lors de chaque validation de transaction dans la blockchain. Rappelons que ces transactions correspondent à des évènements de maintenance, ce n'est donc en théorie que très peu transactionnel. Une architecture de bout en bout en serverless est donc à prisilégier.

La Logicapps se présente comme suit:
- Un trigger de type Microsoft.EventGrid.Topics sur la ressource considérée, dans notre cas "ledgergrid"
- Une tâched de sortie pour créer le document ou faire une mise à jour.

![](/Pictures/AzureBlockchainToCosmosDB.jpg?raw=true)

Les propriétés ajoutées sont les suivantes : addProperty(triggerBody()?['data'], 'id', utcNow())
Le document écrit dans la base de donnée est alors du type :

![](/Pictures/AzureBlockchainToCosmosDBDocument.jpg?raw=true)

#### Procédures complètes pour référence:
- [Azure Cosmos DB serverless (préversion)](https://docs.microsoft.com/fr-fr/azure/cosmos-db/serverless);
- [Utiliser Blockchain Data Manager pour envoyer des données à Azure Cosmos DB](https://docs.microsoft.com/fr-fr/azure/blockchain/service/data-manager-cosmosdb);

### Création de la PowerApps de maintenance
Et bien nous laissons dans cette partie libre cours à votre imagination :)
Un excellent point d'entrée pour créer une application de bout en bout : [Créer ou modifier une application à l’aide du concepteur d’application](https://docs.microsoft.com/fr-fr/dynamics365/customerengagement/on-premises/customize/create-edit-app)
