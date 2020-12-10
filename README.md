# AIRobot
AIRobot ! From robots to maintenance service

1. Blabla sur contexte
2. Approche globale d'architecture
# Architecture overview
![](/Architecture/iRobotArchitecture.png?raw=true)

3. Deep dive technique
# Deep architecture design
![](/Architecture/iRobotArchitecture-DEEP%20ARCHITECTURE$.png?raw=true)

# Architecture "At the Edge"
3.1 Réseau
3.2 Simulateur des données AIRobot
3.3 Déploiement du edge (Gateway)
- HubAgent
- Hub
- SQLEdge
- StorageEdge
- Custom module avec Azure Function

3.4 Edge as Transparent Gateway

3.5 Si déploiement de SDK impossible, utilisation d'une translation Gateway (module de protocol et identity translation)
https://docs.microsoft.com/en-us/azure/iot-edge/iot-edge-as-gateway?view=iotedge-2018-06

3.6 Déploiement du modèle ONNX dans SQLEdge

3.7 Ordonnancement des prédictions via Azure Function

3.8 Création des jobs de streaming (SQLEdge)

3.9 Création du compute Azure Function
- Pulling des tables de prédictions
- Dépôt sur le stockage edge de l'export

3.10 Configuration du pulling (cron 0 * * * *)

3.11 Configuration de la synchronisation du storage account avec le cloud

# Architecture "Cloud"




